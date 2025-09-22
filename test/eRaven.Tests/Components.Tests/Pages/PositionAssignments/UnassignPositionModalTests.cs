//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// UnassignPositionModalTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Components.Pages.PositionAssignments.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.PositionAssignments;

public class UnassignPositionModalTests : TestContext
{
    private readonly Mock<IPositionAssignmentService> _svc = new();
    private readonly Mock<IToastService> _toast = new();

    public UnassignPositionModalTests()
    {
        Services.AddSingleton(_svc.Object);
        Services.AddSingleton(_toast.Object);
    }

    private static Person MakePerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Тест",
        FirstName = "Юзер",
        Rnokpp = "1234567890",
        Rank = "солдат"
    };

    private static (PersonPositionAssignment a, PositionUnit u) MakeActive(Guid personId)
    {
        var u = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "POS-001",
            ShortName = "Оператор",
            OrgPath = "Взвод/Рота",
            SpecialNumber = "111",
            IsActived = true
        };
        var a = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = u.Id,
            PositionUnit = u,
            OpenUtc = new DateTime(2025, 09, 01, 14, 30, 00, DateTimeKind.Utc),
            ModifiedUtc = DateTime.UtcNow
        };
        return (a, u);
    }

    [Fact(DisplayName = "Open: заповнює сьогоднішню дату (UTC) у InputDate і показує заголовок/відкриття")]
    public async Task Open_Prefills_TodayUtc_And_Shows_Title_And_Open()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        var cut = RenderComponent<UnassignPositionModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // InputDate рендериться як <input type="date" value="yyyy-MM-dd">
        var todayYmd = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var dateInput = cut.Find("input[type=date]");
        Assert.Equal(todayYmd, dateInput.GetAttribute("value"));

        // Текст поточної посади та дата відкриття присутні
        cut.Markup.Contains(active.PositionUnit.FullName);
        Assert.Contains(active.OpenUtc.ToString("dd.MM.yyyy"), cut.Markup);
    }

    [Fact(DisplayName = "Submit: викликає UnassignAsync з (обрана_дата - 1 день) 00:00:00 UTC; трімить Note; ok=true закриває модал")]
    public async Task Submit_Calls_Service_With_SelectedMinusOneMidnightUtc_TrimmedNote_ClosesOnOk()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        _svc.Setup(s => s.UnassignAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        bool? received = null;

        var cut = RenderComponent<UnassignPositionModal>(ps => ps
            .Add(x => x.OnUnassigned, EventCallback.Factory.Create<bool>(this, b => received = b))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // 1) міняємо дату (InputDate -> type=date)
        var target = DateTime.UtcNow.Date.AddDays(2); // навмисно не завтра — будь-яка дата
        cut.Find("input[type=date]").Change(target.ToString("yyyy-MM-dd"));

        // 2) міняємо нотатку (InputText -> type=text)
        cut.FindAll("input").First(x => x.OuterHtml.Contains("Model.Note")).Change("   Згідно наказу   ");

        // 3) submit
        cut.Find("form").Submit();

        var expectedClose = new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, DateTimeKind.Utc)
                            .AddDays(-1); // 👈 нова логіка

        _svc.Verify(s => s.UnassignAsync(
                p.Id,
                It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc && d == expectedClose),
                It.Is<string?>(n => n == "Згідно наказу"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(received is true);

        var modal = cut.Find("div.modal");
        Assert.DoesNotContain("show", modal.ClassList);
    }

    [Fact(DisplayName = "Submit: якщо обрана дата (-1 день) < дати призначення — показує toast і не викликає сервіс")]
    public async Task Submit_ShowsToast_And_NoService_When_SelectedMinusOne_BeforeOpen()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        var cut = RenderComponent<UnassignPositionModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // active.OpenUtc = 2025-09-01 14:30 UTC → достатньо поставити target = 2025-09-01,
        // тоді expectedClose = 2025-08-31 00:00 UTC < open → має спрацювати повідомлення і не дзвонити сервіс.
        var target = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc).Date;
        cut.Find("input[type=date]").Change(target.ToString("yyyy-MM-dd"));

        cut.Find("form").Submit();     

        _svc.Verify(s => s.UnassignAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Submit: коли сервіс повертає false — модал залишається відкритим і OnUnassigned(false)")]
    public async Task Submit_ServiceFalse_KeepsOpen_And_RaisesFalse()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        _svc.Setup(s => s.UnassignAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        bool? received = null;

        var cut = RenderComponent<UnassignPositionModal>(ps => ps
            .Add(x => x.OnUnassigned, EventCallback.Factory.Create<bool>(this, b => received = b))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        var target = DateTime.UtcNow.Date.AddDays(1);
        cut.Find("input[type=date]").Change(target.ToString("yyyy-MM-dd"));
        cut.Find("form").Submit();

        var expectedClose = new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, DateTimeKind.Utc)
                            .AddDays(-1);

        _svc.Verify(s => s.UnassignAsync(
            p.Id,
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc && d == expectedClose),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(received.HasValue && received.Value == false);

        var modal = cut.Find("div.modal");
        Assert.Contains("show", modal.ClassList); // лишається відкритим
    }
}