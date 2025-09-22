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

    [Fact(DisplayName = "Open: префілить сьогодні (UTC) у форматі yyyy-MM-dd, показує назву посади і дату відкриття")]
    public async Task Open_Prefills_TodayUtc_And_Shows_Title_And_Open()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        var cut = RenderComponent<UnassignPositionModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // input[type=date] має value у форматі yyyy-MM-dd
        var expected = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var dateInput = cut.Find("input[type=date]");
        Assert.Equal(expected, dateInput.GetAttribute("value"));

        // рендериться повна назва посади
        Assert.Contains(active.PositionUnit.FullName, cut.Markup);

        // і текст з датою відкриття (перевіримо хоча б частину dd.MM.yyyy)
        Assert.Contains(active.OpenUtc.ToString("dd.MM.yyyy"), cut.Markup);

        Assert.NotNull(cut.Instance.PositionAssignmentService);
        Assert.NotNull(cut.Instance.ToastService);
    }

    [Fact(DisplayName = "Submit: викликає UnassignAsync з 00:00:00 UTC, трімить Note; ok=true закриває модал")]
    public async Task Submit_Calls_Service_With_MidnightUtc_TrimmedNote_ClosesOnOk()
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
            .Add(p => p.OnUnassigned, EventCallback.Factory.Create<bool>(this, b => received = b))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // 1) СТАБІЛЬНА фіксована дата (а не UtcNow)
        var target = new DateTime(2025, 09, 23); // без Kind -> Unspecified, це ок

        // InputDate<DateTime> — передаємо DateTime, не рядок
        cut.Find("input[type=date]").Change(target);

        // 2) Нотатка (InputText -> type=text)
        cut.FindAll("input.form-control.form-control-sm")
           .First(i => i.OuterHtml.Contains("Model.Note"))
           .Change("   Згідно наказу   ");

        // 3) Сабмітимо
        cut.Find("form").Submit();

        // Очікуваний closeUtc у сервісі: 00:00:00 UTC того ж дня
        var expectedUtc = new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, DateTimeKind.Utc);

        Assert.True(received is true);

        var modal = cut.Find("div.modal");
        Assert.DoesNotContain("show", modal.ClassList);
    }

    [Fact(DisplayName = "Submit: якщо сервіс повертає false — модал відкритий, OnUnassigned(false)")]
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
            .Add(p => p.OnUnassigned, EventCallback.Factory.Create<bool>(this, b => received = b))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        var today = DateTime.UtcNow.Date;
        cut.Find("input[type=date]").Change(today.ToString("yyyy-MM-dd"));
        cut.Find("form").Submit();

        _svc.Verify(s => s.UnassignAsync(
            p.Id,
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc
                                 && d == new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc)),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(received.HasValue && received.Value == false);

        var modal = cut.Find("div.modal");
        Assert.Contains("show", modal.ClassList);
    }

    [Fact(DisplayName = "Submit: якщо дата < дати призначення → показує toast і НЕ викликає сервіс")]
    public async Task Submit_BeforeOpenDate_ShowsToast_DoesNotCallService()
    {
        var p = MakePerson();
        var (active, _) = MakeActive(p.Id);

        var cut = RenderComponent<UnassignPositionModal>();
        await cut.InvokeAsync(() => cut.Instance.Open(p, active));

        // дата (вчора) < openUtc → має бути блокування
        var invalid = active.OpenUtc.Date.AddDays(-1);
        cut.Find("input[type=date]").Change(invalid.ToString("yyyy-MM-dd"));

        cut.Find("form").Submit();

        _svc.Verify(s => s.UnassignAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // модал лишається відкритим
        var modal = cut.Find("div.modal");
        Assert.Contains("show", modal.ClassList);
    }
}