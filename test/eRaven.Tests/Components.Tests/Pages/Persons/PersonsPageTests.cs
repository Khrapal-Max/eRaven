//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsPageTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PersonsPageBunitTests : TestContext, IDisposable
{
    private readonly Mock<IPersonService> _svc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();
    private readonly Mock<IValidator<CreatePersonViewModel>> _validator = new();

    public PersonsPageBunitTests()
    {
        // DI для компонента
        Services.AddSingleton(_svc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_validator.Object);

        _excel
           .Setup(s => s.ExportAsync(It.IsAny<IEnumerable<object>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 })); // будь-який стрім ок
        // generic overloadи: простіше підстрахуватися двома сетапами
        _excel
            .Setup(s => s.ExportAsync<Person>(It.IsAny<IEnumerable<Person>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        _excel
            .Setup(s => s.ImportAsync<CreatePersonViewModel>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<CreatePersonViewModel>(), new List<string>()));

        Services.AddSingleton<IExcelService>(_excel.Object);

        // --- JSInterop для кнопки експорту (downloadFile) ---
        // bUnit надає JSInterop через this.JSInterop
        JSInterop.SetupVoid("downloadFile", _ => true).SetVoidResult();
        // Якщо треба явний IJSRuntime у DI:
        Services.AddSingleton<IJSRuntime>(JSInterop.JSRuntime);

        // Валідатор — "успішний" за замовчуванням
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePersonViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    // ---- helpers ----

    private static Person P(string last, string first, string rnokpp = "0000000000") => new()
    {
        Id = Guid.NewGuid(),
        LastName = last,
        FirstName = first,
        Rnokpp = rnokpp,
        Rank = "рядовий",
        BZVP = "потребує навчання"
    };

    // ---- tests ----

    [Fact(DisplayName = "Initial: коли сервіс повертає пусто → бачимо порожній список")]
    public void Initial_Empty_Shows_EmptyState()
    {
        // Arrange
        _svc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var cut = RenderComponent<PersonsPage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _svc.Verify(s => s.SearchAsync(null, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains("Список карток порожній", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "Initial: коли сервіс повертає дані → бачимо таблицю з рядками")]
    public void Initial_WithData_Renders_TableRows()
    {
        var data = new List<Person>
        {
            P("Петренко", "Іван", "1111111111"),
            P("Іваненко", "Петро", "2222222222"),
        };

        _svc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var cut = RenderComponent<PersonsPage>();

        cut.WaitForAssertion(() =>
        {
            // є таблиця
            Assert.Contains("<table", cut.Markup, StringComparison.OrdinalIgnoreCase);
            // обидва ПІБ присутні
            Assert.Contains("Петренко Іван", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Іваненко Петро", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact(DisplayName = "Search: клієнтська фільтрація без повторного звернення в сервіс")]
    public async Task Search_Filters_ClientSide()
    {
        var data = new List<Person>
        {
            P("Петренко", "Іван", "1111111111"),
            P("Іваненко", "Петро", "2222222222"),
            P("Кіт", "Максим", "3333333333")
        };

        _svc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var cut = RenderComponent<PersonsPage>();

        // початково всі
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Петренко Іван", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Іваненко Петро", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Кіт Максим", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        // введемо в пошук "Кіт" і дочекаємось перерендеру
        var search = cut.Find("input");
        await cut.InvokeAsync(() => search.Input("Кіт"));
        await cut.InvokeAsync(() => search.Input("Кіт"));

        // імітуємо клік по "пошук" якщо є кнопка/подія (у тебе SearchBox викликає OnSearch через debounce)
        // тут достатньо WaitForAssertion — стан уже перераховано на OnInput/OnChange
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Кіт Максим", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Петренко Іван", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Іваненко Петро", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        // І сервіс не викликався вдруге (клієнтська фільтрація)
        _svc.Verify(s => s.SearchAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Create: натискання кнопки → відкривається модальне вікно")]
    public async Task Create_Click_Opens_CreateModal()
    {
        _svc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cut = RenderComponent<PersonsPage>();

        // Клік по кнопці "Створити"
        var createBtn = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => createBtn.Click());

        // У DOM має з’явитись модальне вікно (шукаємо backdrop або .modal-content)
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.FindAll(".modal, .modal-backdrop").Any(),
                "Очікувалось, що модальне буде відрендерено після натискання 'Створити'.");
        });
    }

    [Fact(DisplayName = "Create: після OnCreated → сторінка перевантажує список")]
    public async Task Create_OnCreated_Reloads_List()
    {
        // 1) Початкове завантаження — порожньо
        _svc.SetupSequence(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([])            // перший Render
            .ReturnsAsync([P("Козак", "Василь", "1234567890")]); // після створення

        _svc.Setup(s => s.CreateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Person p, CancellationToken _) => p);

        var cut = RenderComponent<PersonsPage>();

        // 2) Відкриваємо модалку
        var createBtn = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Створити", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => createBtn.Click());

        // 3) Дістаємо модалку, емулюємо її колбек OnCreated (як це зробить сама модалка)
        var modal = cut.FindComponent<PersonCreateModal>();
        var newPerson = P("Козак", "Василь", "1234567890");

        await cut.InvokeAsync(async () =>
        {
            // Викликаємо публічний метод компонента модалки так,
            // ніби всередині вона завершила CreateAsync та дернула OnCreated
            await modal.Instance.OnCreated.InvokeAsync(newPerson);
        });

        // 4) Після колбеку сторінка перезавантажує список → у DOM має з’явитись нова особа
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Козак Василь", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        // Перевірка, що відбулося повторне завантаження
        _svc.Verify(s => s.SearchAsync(null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    public new void Dispose()
    {
        base.Dispose();
    }
}
