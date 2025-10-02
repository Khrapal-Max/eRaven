//------------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//------------------------------------------------------------------------------
// StatusTransitionsPageTests (refactored, no duplicate service calls)
//------------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Application.ViewModels.PersonStatusViewModels;
using eRaven.Components.Pages.Statuses;
using eRaven.Components.Pages.Statuses.Modals;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Statuses;

public sealed class StatusesPageTests : TestContext
{
    // ---------- Test data helpers ----------

    private static Person PersonSample() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Іванов",
        FirstName = "Іван",
        MiddleName = "Іванович",
        Rnokpp = "1111111111"
    };

    private static StatusKind[] StatusKindsDefault() =>
    [
        new StatusKind { Id = 30,  Code = "30",  Name = "В районі" },
        new StatusKind { Id = 100, Code = "100", Name = "В БР"     }
    ];

    // ---------- Mocks ----------

    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IPersonStatusService> _personStatusSvc = new();
    private readonly Mock<IStatusKindService> _statusKindSvc = new();
    private readonly Mock<IStatusTransitionService> _transitionSvc = new();
    private readonly Mock<IPositionAssignmentService> _positionAssignmentSvc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();

    // ---------- DI registration (base) ----------

    private void RegisterServices(IEnumerable<Person>? persons = null)
    {
        Services.AddSingleton(_personSvc.Object);
        Services.AddSingleton(_personStatusSvc.Object);
        Services.AddSingleton(_statusKindSvc.Object);
        Services.AddSingleton(_transitionSvc.Object);
        Services.AddSingleton(_positionAssignmentSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_excel.Object);

        _personSvc
            .Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons?.ToList() ?? []);

        // Базові значення (можна перекрити в конкретному тесті):
        SetupStatusKinds(StatusKindsDefault());
        SetupTransitions([]); // за замовчуванням переходів немає

        // Нічого не верифікуємо тут — лише "сухий" сетап.
    }

    // ---------- Reusable setups (перекривають базові) ----------

    private void SetupStatusKinds(IReadOnlyList<StatusKind> kinds)
    {
        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(kinds);
    }

    private void SetupTransitions(IReadOnlyList<int> toIds)
    {
        // Повертаємо List<int>, щоб відповідало IReadOnlyList<int>
        _transitionSvc
            .Setup(t => t.GetToIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toIds?.ToHashSet() ?? []);
    }

    private void SetupCurrentStatus(Person person, PersonStatus? current = null)
    {
        _personStatusSvc
            .Setup(s => s.GetActiveAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
    }

    private void SetupActiveAssignment(Person person, PersonPositionAssignment? active)
    {
        _positionAssignmentSvc
            .Setup(s => s.GetActiveAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);
    }

    private void SetupSetStatusSucceeds()
    {
        _personStatusSvc
            .Setup(s => s.SetStatusAsync(It.IsAny<PersonStatus>(), It.IsAny<CancellationToken>()));
    }

    private void SetupUnassignReturns(bool ok = true)
    {
        _positionAssignmentSvc
            .Setup(s => s.UnassignAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ok);
    }

    // ---------- DOM helpers ----------

    private static void PerformSearch(IRenderedComponent<StatusesPage> cut, string query)
    {
        var input = cut.Find("input[placeholder='Пошук: ПІБ / РНОКПП']");
        input.Input(query);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(query, cut.Markup, StringComparison.Ordinal);
            cut.Find("button[title='Змінити статус']");
        });
    }

    // ---------- Tests ----------

    [Fact(DisplayName = "Page: рендер без крешів (smoke)")]
    public void Page_Renders_Smoke()
    {
        RegisterServices();

        var cut = RenderComponent<StatusesPage>();

        Assert.Contains("Режим: одиничний", cut.Markup);
    }

    [Fact(DisplayName = "Page: перемикач режиму — одиничний ↔ пакетний")]
    public void Toggle_OneShotMode_Works()
    {
        RegisterServices();

        var cut = RenderComponent<StatusesPage>();

        var toggle = cut.Find("button[title^=\"Одиничний:\"]");
        Assert.Contains("Режим: одиничний", toggle.TextContent);

        toggle.Click();

        var toggleAfter = cut.Find("button[title^=\"Одиничний:\"]");
        Assert.Contains("Режим: пакетний", toggleAfter.TextContent);
        Assert.Contains("btn-warning", toggleAfter.ClassList);
    }

    [Fact(DisplayName = "Page: при ініціалізації викликаються сервіси статусів та осіб")]
    public void Services_Called_On_Init()
    {
        RegisterServices();

        _ = RenderComponent<StatusesPage>();

        _statusKindSvc.Verify(
            s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _personSvc.Verify(
            s => s.SearchAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Modal: OnClose викликається при натисканні 'Скасувати'")]
    public void Modal_OnClose_Fires_When_Cancel_Clicked()
    {
        var person = PersonSample();
        var wasClosed = false;
        var onClose = EventCallback.Factory.Create(this, () => wasClosed = true);

        var modal = RenderComponent<StatusSetModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.OnClose, onClose)
        );

        modal.Find("button.btn-outline-secondary").Click();

        Assert.True(wasClosed);
    }

    [Fact(DisplayName = "Modal: кнопка 'Підтвердити' неактивна, доки форма не валідна")]
    public void Modal_Submit_Disabled_Until_Valid()
    {
        var person = PersonSample();

        var modal = RenderComponent<StatusSetModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.AllowedNextStatuses,
            [
                new() { Id = 30, Name = "В районі" }
            ])
        );

        var submit = modal.Find("button[type=submit]");
        Assert.True(submit.HasAttribute("disabled"));

        modal.Find("select.form-select").Change("30");
        modal.Find("input[type=date]").Change("2025-09-01");

        submit = modal.Find("button[type=submit]");
        Assert.False(submit.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Modal: валідна форма викликає OnSubmit з коректними даними")]
    public void Modal_Submit_Fires_OnSubmit_With_Correct_Payload()
    {
        var person = PersonSample();
        SetPersonStatusViewModel? captured = null;

        var onSubmit = EventCallback.Factory.Create<SetPersonStatusViewModel>(this, vm => captured = vm);

        var modal = RenderComponent<StatusSetModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.OnSubmit, onSubmit)
            .Add(p => p.AllowedNextStatuses, [new StatusKind { Id = 30, Name = "В районі" }])
        );

        modal.Find("select.form-select").Change("30");
        modal.Find("input[type=date]").Change("2025-09-01");
        modal.Find("button[type=submit]").Click();

        Assert.NotNull(captured);
        Assert.Equal(person.Id, captured!.PersonId);
        Assert.Equal(30, captured.StatusId);
        Assert.NotEqual(default, captured.Moment);
    }

    [Fact(DisplayName = "Submit: статус з кодом 'РОЗПОР' знімає з посади (close = open-1) і зберігає статус")]
    public void Submit_Rozpor_Unassigns_And_Sets_Status()
    {
        var person = PersonSample();

        var kinds = new[]
        {
        new StatusKind { Id = 30, Code = "30", Name = "В районі" },
        new StatusKind { Id = 5,  Code = "РОЗПОР", Name = "Розпорядження" } // тригер
    };

        RegisterServices([person]);
        SetupStatusKinds(kinds);

        // поточний статус = "В районі", дозволяємо перехід 30 -> 5
        SetupCurrentStatus(person, new PersonStatus { PersonId = person.Id, StatusKindId = 30 });
        SetupTransitions([5]);

        // є активне призначення
        var activeAssign = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUtc = DateTime.UtcNow
        };
        SetupActiveAssignment(person, activeAssign);
        SetupSetStatusSucceeds();
        SetupUnassignReturns(true);

        var cut = RenderComponent<StatusesPage>();

        // пошук — інакше кнопка не з’явиться
        PerformSearch(cut, person.Rnokpp!);

        // відкриваємо модал, обираємо статус=5 і дату 2025-09-10 (локальна)
        cut.Find("button[title='Змінити статус']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("select.form-select").Change("5");
            cut.Find("input[type=date]").Change("2025-09-10");
        });

        cut.Find("button[type=submit]").Click();

        // очікування рахуємо через локальну північ -> UTC
        var localMidnight = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedOpenUtc = TimeZoneInfo.ConvertTimeToUtc(localMidnight, TimeZoneInfo.Local);
        var expectedCloseUtc = expectedOpenUtc.AddDays(-1);

        _positionAssignmentSvc.Verify(s => s.UnassignAsync(
            person.Id,
            It.Is<DateTime>(d => d == expectedCloseUtc),   // не перевіряємо Kind, лише значення
            It.Is<string?>(n => n == null),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _personStatusSvc.Verify(s => s.SetStatusAsync(
            It.Is<PersonStatus>(ps =>
                ps.PersonId == person.Id &&
                ps.StatusKindId == 5 &&
                ps.OpenDate == expectedOpenUtc),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Submit: closeUtc клампиться до Active.OpenUtc, якщо open-1 < OpenUtc")]
    public void Submit_Unassign_Close_Is_Clamped_To_Active_OpenUtc()
    {
        var person = PersonSample();

        var kinds = new[] { new StatusKind { Id = 5, Code = "РОЗПОР", Name = "Розпорядження" } };
        RegisterServices([person]);
        SetupStatusKinds(kinds);
        SetupTransitions([5]);
        SetupCurrentStatus(person, null);

        var activeAssign = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUtc = DateTime.UtcNow
        };
        SetupActiveAssignment(person, activeAssign);
        SetupSetStatusSucceeds();
        SetupUnassignReturns(true);

        var cut = RenderComponent<StatusesPage>();

        PerformSearch(cut, person.Rnokpp!);

        cut.Find("button[title='Змінити статус']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("select.form-select").Change("5");
            cut.Find("input[type=date]").Change("2025-09-10");
        });

        cut.Find("button[type=submit]").Click();

        var expectedClampedClose = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);

        _positionAssignmentSvc.Verify(s => s.UnassignAsync(
            person.Id,
            It.Is<DateTime>(d => d == expectedClampedClose && d.Kind == DateTimeKind.Utc),
            It.Is<string?>(n => n == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Submit: статус без тригерного коду НЕ знімає з посади")]
    public void Submit_NonTrigger_Status_Does_Not_Unassign()
    {
        var person = PersonSample();

        var plain = new StatusKind { Id = 30, Code = "30", Name = "В районі" };
        RegisterServices([person]);
        SetupStatusKinds([plain]);
        SetupTransitions([30]);
        SetupCurrentStatus(person, null);

        var activeAssign = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc),
            ModifiedUtc = DateTime.UtcNow
        };
        SetupActiveAssignment(person, activeAssign);
        SetupSetStatusSucceeds();

        var cut = RenderComponent<StatusesPage>();

        PerformSearch(cut, person.Rnokpp!);

        cut.Find("button[title='Змінити статус']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("select.form-select").Change("30");
            cut.Find("input[type=date]").Change("2025-09-10");
        });

        cut.Find("button[type=submit]").Click();

        _positionAssignmentSvc.Verify(s => s.UnassignAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _personStatusSvc.Verify(s => s.SetStatusAsync(
            It.Is<PersonStatus>(ps => ps.PersonId == person.Id && ps.StatusKindId == 30),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
