//------------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//------------------------------------------------------------------------------
// StatusTransitionsPageTests
// - smoke-render
// - toggle OneShotMode
// - single-result search → opens modal
// - import: skips invalid rows, saves valid
//------------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.Services.StatusTransitionService;
using eRaven.Application.ViewModels.PersonStatusViewModels;
using eRaven.Components.Pages.Statuses;
using eRaven.Components.Pages.Statuses.Modals;
using eRaven.Domain.Models;
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

    private static StatusKind[] StatusKindsSample() =>
    [
        new StatusKind { Id = 30,  Code = "30",  Name = "В районі" },
        new StatusKind { Id = 100, Code = "100", Name = "В БР"     }
    ];

    // ---------- Mocks ----------

    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IPersonStatusService> _personStatusSvc = new();
    private readonly Mock<IStatusKindService> _statusKindSvc = new();
    private readonly Mock<IStatusTransitionService> _transitionSvc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();

    private void RegisterServices(IEnumerable<Person>? persons = null)
    {
        Services.AddSingleton(_personSvc.Object);
        Services.AddSingleton(_personStatusSvc.Object);
        Services.AddSingleton(_statusKindSvc.Object);
        Services.AddSingleton(_transitionSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_excel.Object);

        // ВАЖЛИВО: у сервісу статусів очікуємо сигнатуру з bool + CancellationToken.
        // Використовуємо It.IsAny<bool>() — так не залежимо від дефолтного значення параметра.
        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StatusKindsSample());

        _personSvc
            .Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(persons?.ToList() ?? new List<Person>());

        _transitionSvc
            .Setup(t => t.GetToIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
    }

    // ---------- Tests ----------

    [Fact(DisplayName = "Page: рендер без крешів (smoke)")]
    public void Page_Renders_Smoke()
    {
        RegisterServices();

        var componentUnderTest = RenderComponent<StatusesPage>();

        Assert.Contains("Режим: одиничний", componentUnderTest.Markup);
    }

    [Fact(DisplayName = "Page: перемикач режиму — одиничний ↔ пакетний")]
    public void Toggle_OneShotMode_Works()
    {
        RegisterServices();

        var componentUnderTest = RenderComponent<StatusesPage>();

        var toggleButton = componentUnderTest.Find("button[title^=\"Одиничний:\"]");
        Assert.Contains("Режим: одиничний", toggleButton.TextContent);

        toggleButton.Click();

        var toggleButtonAfter = componentUnderTest.Find("button[title^=\"Одиничний:\"]");
        Assert.Contains("Режим: пакетний", toggleButtonAfter.TextContent);
        Assert.Contains("btn-warning", toggleButtonAfter.ClassList); // було btn-primary
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
        var wasClosedInvoked = false;
        var onCloseCallback = EventCallback.Factory.Create(this, () => wasClosedInvoked = true);

        var modal = RenderComponent<StatusSetModal>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.OnClose, onCloseCallback)
        );

        modal.Find("button.btn-outline-secondary").Click();

        Assert.True(wasClosedInvoked);
    }

    [Fact(DisplayName = "Modal: кнопка 'Підтвердити' неактивна, доки форма не валідна")]
    public void Modal_Submit_Disabled_Until_Valid()
    {
        var person = PersonSample();

        var modal = RenderComponent<StatusSetModal>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.AllowedNextStatuses, new List<StatusKind>
            {
                new() { Id = 30, Name = "В районі" }
            })
        );

        var submitButton = modal.Find("button[type=submit]");
        Assert.True(submitButton.HasAttribute("disabled"));

        var statusSelect = modal.Find("select.form-select");
        statusSelect.Change("30");

        var dateInput = modal.Find("input[type=date]");
        dateInput.Change("2025-09-01");

        submitButton = modal.Find("button[type=submit]");
        Assert.False(submitButton.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Modal: валідна форма викликає OnSubmit з коректними даними")]
    public void Modal_Submit_Fires_OnSubmit_With_Correct_Payload()
    {
        var person = PersonSample();
        SetPersonStatusViewModel? capturedVm = null;

        var onSubmitCallback = EventCallback.Factory.Create<SetPersonStatusViewModel>(
            this, vm => capturedVm = vm);

        var modal = RenderComponent<StatusSetModal>(parameters => parameters
            .Add(p => p.Open, true)
            .Add(p => p.PersonCard, person)
            .Add(p => p.OnSubmit, onSubmitCallback)
            .Add(p => p.AllowedNextStatuses, new List<StatusKind>
            {
                new() { Id = 30, Name = "В районі" }
            })
        );

        modal.Find("select.form-select").Change("30");
        modal.Find("input[type=date]").Change("2025-09-01");
        modal.Find("button[type=submit]").Click();

        Assert.NotNull(capturedVm);
        Assert.Equal(person.Id, capturedVm!.PersonId);
        Assert.Equal(30, capturedVm.StatusId);
        Assert.NotEqual(default, capturedVm.Moment);
    }
}