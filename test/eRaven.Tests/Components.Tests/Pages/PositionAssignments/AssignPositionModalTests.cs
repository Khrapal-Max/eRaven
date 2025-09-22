//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// AssignPositionModalTests
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
            OrgPath = full,             // для FullName в домені
            SpecialNumber = "123",
            IsActived = true
        };

    [Fact]
    public void Initially_Hidden()
    {
        // Arrange
        var positions = new[] { P("Стрілець", "Стрілець Відділення/Взвод/Рота") };
        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        // Act
        var modal = cut.Find("div.modal");

        // Assert
        Assert.DoesNotContain("show", modal.ClassList);
        Assert.DoesNotContain("d-block", modal.ClassList);
    }

    [Fact]
    public async Task Open_Sets_Today_Utc_And_ModelDefaults()
    {
        // Arrange
        var person = MakePerson();
        var positions = new[] { P("Стрілець", "Стрілець Відділення/Взвод/Рота") };

        var cut = _ctx.RenderComponent<AssignPositionModal>(ps => ps
            .Add(p => p.Positions, positions)
            .Add(p => p.OnAssigned, EventCallback.Factory.Create<PersonPositionAssignment>(this, _ => { }))
        );

        // Act (на Dispatcher)
        await cut.InvokeAsync(() => cut.Instance.Open(person));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);

            // дата сьогодні (UTC), формат yyyy-MM-dd
            var inputDate = cut.Find("input[type=date]");
            var val = inputDate.GetAttribute("value");
            var todayUtc = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            Assert.Equal(todayUtc, val);

            // select існує і стоїть Guid.Empty (початкове значення)
            var select = cut.Find("select.form-select");
            Assert.Equal(Guid.Empty.ToString(), select.GetAttribute("value"));
        });
    }

    [Fact]
    public async Task Submit_Calls_Service_With_MidnightUtc_And_Raises_OnAssigned()
    {
        // Arrange
        var person = MakePerson();
        var pos = P("Снайпер", "Снайпер Взвод/Рота/Батальйон");
        var positions = new[] { pos };

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
                        ShortName = pos.ShortName,
                        OrgPath = pos.OrgPath,
                        SpecialNumber = "123",
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

        await cut.InvokeAsync(() => cut.Instance.Open(person));

        // Обираємо посаду
        cut.Find("select.form-select").Change(pos.Id.ToString());

        // Встановлюємо дату (завтра) → очікуємо 00:00:00 UTC цього дня
        var target = DateTime.UtcNow.Date.AddDays(1);
        cut.Find("input[type=date]").Change(target.ToString("yyyy-MM-dd"));

        // Нотатка (не date): візьмемо перший input, який не має type=date
        var noteInput = cut.Find("input:not([type=date])");
        noteInput.Change("   Призначити згідно наказу   ");

        // Act: важливо виконати сабміт через Dispatcher
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert: почекаємо, поки асинхронний хендлер завершиться
        cut.WaitForAssertion(() =>
        {
            _svc.Verify(s => s.AssignAsync(
                    person.Id,
                    pos.Id,
                    It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc
                                         && d.Date == target
                                         && d.TimeOfDay == TimeSpan.Zero),
                    It.Is<string?>(n => n == "Призначити згідно наказу"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(received);
            Assert.Equal(created!.Id, received!.Id);

            var modal = cut.Find("div.modal");
            Assert.DoesNotContain("show", modal.ClassList);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LongOptionText_Is_Truncated_And_FullShown_Below()
    {
        // Arrange
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
        Assert.EndsWith("…", optionText);                   // урізано
        Assert.True(optionText!.Length <= 70);              // межа з Trunc(..., 70)

        // Оберемо цю посаду — під селектом має з’явитися повна назва
        var select = cut.Find("select.form-select");
        select.Change(pos.Id.ToString());

        var fullBlock = cut.FindAll("div.form-text.small.text-muted")
                           .FirstOrDefault();
        Assert.NotNull(fullBlock);
        Assert.Contains(pos.FullName, fullBlock!.TextContent);
    }
}
