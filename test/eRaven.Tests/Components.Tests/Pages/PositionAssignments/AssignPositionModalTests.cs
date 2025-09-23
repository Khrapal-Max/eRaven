//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignPositionModalTests (updated for latest modal behavior)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Components.Pages.PositionAssignments.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.PositionAssignments;

public sealed class AssignPositionModalTests : IDisposable
{
    private readonly TestContext _ctx;
    private readonly Mock<IPositionAssignmentService> _svc = new();
    private readonly Mock<IToastService> _toast = new();

    public AssignPositionModalTests()
    {
        _ctx = new TestContext();
        _ctx.Services.AddSingleton(_svc.Object);
        _ctx.Services.AddSingleton(_toast.Object);
    }

    public void Dispose() => _ctx.Dispose();

    private static Person MakePerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Донченко",
        FirstName = "Ігор",
        Rnokpp = "1234567890",
        Rank = "сержант",
    };

    private static PositionUnit P(string shortName, string full)
        => new()
        {
            Id = Guid.NewGuid(),
            ShortName = shortName,
            OrgPath = full,             // використовується для FullName
            SpecialNumber = "123",
            IsActived = true
        };

    [Fact(DisplayName = "Початково модал прихований")]
    public void Initially_Hidden()
    {
        var positions = new[] { P("Стрілець", "Стрілець Відділення/Взвод/Рота") };
        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        var modal = cut.Find("div.modal");
        Assert.DoesNotContain("show", modal.ClassList);
        Assert.DoesNotContain("d-block", modal.ClassList);
    }

    [Fact(DisplayName = "Open(person): дата за замовчуванням = сьогодні (UTC), min відсутній")]
    public async Task Open_NoLastClose_DefaultsTodayUtc_NoMin()
    {
        var person = MakePerson();
        var positions = new[] { P("Стрілець", "Стрілець Відділення/Взвод/Рота") };

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(person));

        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);

            var inputDate = cut.Find("input[type=date]");
            var val = inputDate.GetAttribute("value");
            var todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            Assert.Equal(todayUtc, val);

            Assert.Null(inputDate.GetAttribute("min"));
        });

        Assert.NotNull(cut.Instance.PositionAssignmentService);
        Assert.NotNull(cut.Instance.ToastService);
    }

    [Fact(DisplayName = "Open(person, lastClose): дефолт = max(lastClose+1, сьогодні UTC), min = lastClose+1")]
    public async Task Open_WithLastClose_DefaultsNextDay_And_MinSet()
    {
        // Arrange
        var person = MakePerson();
        var pos = P("Снайпер", "Снайпер Взвод/Рота/Батальйон");
        var positions = new[] { pos };

        var lastCloseUtc = new DateTime(2025, 09, 21, 0, 0, 0, DateTimeKind.Utc);
        var minDate = lastCloseUtc.AddDays(1);               // 2025-09-22
        var todayUtc = DateTime.UtcNow.Date;
        var expectedMinStr = minDate.ToString("yyyy-MM-dd");
        var expectedValueStr = (todayUtc > minDate ? todayUtc : minDate).ToString("yyyy-MM-dd");

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        // Act
        await cut.InvokeAsync(() => cut.Instance.Open(person, activeAssign: null, lastUnassignCloseUtc: lastCloseUtc));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var inputDate = cut.Find("input[type=date]");
            var valueAttr = inputDate.GetAttribute("value") ?? string.Empty;
            var minAttr = inputDate.GetAttribute("min") ?? string.Empty;

            Assert.Equal(expectedValueStr, valueAttr); // дефолт = max(min, today)
            Assert.Equal(expectedMinStr, minAttr);     // мінімум = lastClose+1

            // є підказка про останній день попередньої посади
            var hint = lastCloseUtc.ToString("dd.MM.yyyy 'UTC'");
            Assert.Contains(hint, cut.Markup);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact(DisplayName = "Open(person, activeAssign): min = activeOpen+1 день; дефолт = max(min, сьогодні)")]
    public async Task Open_WithActiveAssign_MinFromActiveOpen_Plus1()
    {
        var person = MakePerson();
        var pos = P("Оператор", "Оператор Відділення/Взвод");
        var positions = new[] { pos };

        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos.Id,
            OpenUtc = new DateTime(2025, 09, 10, 14, 0, 0, DateTimeKind.Utc),
            PositionUnit = pos,
            ModifiedUtc = DateTime.UtcNow
        };

        var minExpected = active.OpenUtc.Date.AddDays(1); // 2025-09-11

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(person, activeAssign: active, lastUnassignCloseUtc: null));

        cut.WaitForAssertion(() =>
        {
            var inputDate = cut.Find("input[type=date]");
            Assert.Equal(minExpected.ToString("yyyy-MM-dd"), inputDate.GetAttribute("min"));

            // дефолт або = minExpected (якщо сьогодні менше), або = сьогодні
            var today = DateTime.UtcNow.Date;
            var expectedDefault = (minExpected > today ? minExpected : today).ToString("yyyy-MM-dd");
            Assert.Equal(expectedDefault, inputDate.GetAttribute("value"));

            // Текст-пояснення про авто-закриття є
            Assert.Contains("автоматично закрита", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "Submit: дата < (activeOpen+1) → блок, info-toast, сервіс не викликається")]
    public async Task Submit_Blocks_When_Date_Before_Min_WithActiveAssign()
    {
        var person = MakePerson();
        var pos = P("Оператор", "Оператор Відділення/Взвод");
        var positions = new[] { pos };

        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos.Id,
            OpenUtc = new DateTime(2025, 09, 10, 14, 0, 0, DateTimeKind.Utc),
            PositionUnit = pos,
            ModifiedUtc = DateTime.UtcNow
        };

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(person, activeAssign: active));

        // обираємо посаду
        cut.Find("select.form-select").Change(pos.Id.ToString());

        // ставимо дату РАНІШЕ за (active.Open+1 день)
        var invalid = active.OpenUtc.Date; // рівна open-даті, точно < min
        cut.Find("input[type=date]").Change(invalid.ToString("yyyy-MM-dd"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        _svc.Verify(s => s.UnassignAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _svc.Verify(s => s.AssignAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);

    }

    [Fact(DisplayName = "Submit: з activeAssign викликає UnassignAsync(open-1) і AssignAsync(open 00:00 UTC); нотатка трімиться")]
    public async Task Submit_With_ActiveAssign_Performs_Unassign_Then_Assign()
    {
        var person = MakePerson();
        var posNew = P("Снайпер", "Снайпер Взвод/Рота/Батальйон");
        var positions = new[] { posNew };

        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = Guid.NewGuid(), // стара посада
            OpenUtc = new DateTime(2025, 09, 15, 9, 0, 0, DateTimeKind.Utc),
            PositionUnit = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Стара", OrgPath = "Стара/Шлях", IsActived = true },
            ModifiedUtc = DateTime.UtcNow
        };

        PersonPositionAssignment? created = null;

        _svc.Setup(s => s.AssignAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid pid, Guid puid, DateTime openUtc, string? note, CancellationToken _) =>
            {
                created = new PersonPositionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = pid,
                    PositionUnitId = puid,
                    OpenUtc = openUtc,
                    Note = note,
                    PositionUnit = new PositionUnit
                    {
                        Id = puid,
                        ShortName = posNew.ShortName,
                        OrgPath = posNew.OrgPath,
                        SpecialNumber = "SN",
                        IsActived = true
                    }
                };
                return created!;
            });

        PersonPositionAssignment? received = null;

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, a => received = a))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(person, activeAssign: active));

        // обираємо нову посаду
        cut.Find("select.form-select").Change(posNew.Id.ToString());

        // виставляємо валідну дату: active.Open.Date + 1 (мінімальна дозволена)
        var openDate = active.OpenUtc.Date.AddDays(1);
        cut.Find("input[type=date]").Change(openDate.ToString("yyyy-MM-dd"));

        // нотатка (трімаємо)
        cut.Find("input:not([type=date])").Change("   Наказ №123   ");

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            // 1) закриття попередньої посади: open-1 день (00:00 UTC)
            var expectedClose = new DateTime(openDate.Year, openDate.Month, openDate.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
            _svc.Verify(s => s.UnassignAsync(
                    person.Id,
                    It.Is<DateTime>(d => d == expectedClose && d.Kind == DateTimeKind.Utc),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // 2) нове призначення: open 00:00 UTC
            _svc.Verify(s => s.AssignAsync(
                    person.Id,
                    posNew.Id,
                    It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc
                                         && d.Date == openDate
                                         && d.TimeOfDay == TimeSpan.Zero),
                    It.Is<string?>(n => n == "Наказ №123"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(received);
            Assert.Equal(created!.Id, received!.Id);

            var modal = cut.Find("div.modal");
            Assert.DoesNotContain("show", modal.ClassList);
        });
    }

    [Fact(DisplayName = "Довгі назви: <option> урізано «…», повна назва показується під селектом")]
    public async Task LongOptionText_Is_Truncated_And_FullShown_Below()
    {
        var longName = new string('A', 120) + " / Дуже довга назва посади";
        var pos = new PositionUnit
        {
            Id = Guid.NewGuid(),
            ShortName = "Довга",
            OrgPath = longName,
            SpecialNumber = "999",
            IsActived = true
        };
        var positions = new[] { pos };
        var person = MakePerson();

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(person));

        // Перевіряємо <option>: title = повна, текст = урізана з «…»
        var option = cut.FindAll("option").FirstOrDefault(o => o.GetAttribute("value") == pos.Id.ToString());
        Assert.NotNull(option);
        Assert.Equal(pos.FullName, option!.GetAttribute("title"));

        var optionText = option.TextContent?.Trim();
        Assert.EndsWith("…", optionText);
        Assert.True(optionText!.Length <= 70);

        // Обираємо — під селектом має з’явитися повна назва
        cut.Find("select.form-select").Change(pos.Id.ToString());

        var fullBlock = cut.FindAll("div.form-text.small.text-muted")
                           .FirstOrDefault();
        Assert.NotNull(fullBlock);
        Assert.Contains(pos.FullName, fullBlock!.TextContent);
    }
}