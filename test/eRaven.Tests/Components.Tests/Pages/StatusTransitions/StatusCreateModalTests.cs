//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusCreateModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.StatusKindViewModels;
using eRaven.Components.Pages.StatusTransitions.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.StatusTransitions;

public class StatusCreateModalTests : TestContext
{
    private readonly Mock<IStatusKindService> _service = new(MockBehavior.Strict);

    public StatusCreateModalTests()
    {
        // DI: сервіс для компонента і для валідатора
        Services.AddSingleton(_service.Object);
        // Реєструємо валідатор для FluentValidationValidator
        Services.AddTransient<IValidator<CreateKindViewModel>, CreateKindViewModelValidator>();
        // ⚠️ НЕ викликаємо Services.AddBlazoredFluentValidation(), у тестах це не потрібно
    }

    // ---------------- helpers ----------------

    private static Task OpenAsync(IRenderedComponent<StatusCreateModal> cut)
        => cut.InvokeAsync(() => cut.Instance.Open());

    private static Task SetNameAsync(IRenderedComponent<StatusCreateModal> cut, string v)
        // перший текстовий інпут у формі — Name
        => cut.InvokeAsync(() => cut.FindAll("input")[0].Change(v));

    private static Task SetCodeAsync(IRenderedComponent<StatusCreateModal> cut, string v)
        // другий текстовий інпут — Code
        => cut.InvokeAsync(() => cut.FindAll("input")[1].Change(v));

    private static Task SetOrderAsync(IRenderedComponent<StatusCreateModal> cut, int v)
        // третій інпут (type=number) — Order
        => cut.InvokeAsync(() => cut.FindAll("input")[2].Change(v));

    private static Task ToggleActiveAsync(IRenderedComponent<StatusCreateModal> cut, bool on)
    {
        // четвертий інпут — чекбокс IsActive
        var chk = cut.FindAll("input")[3];
        return cut.InvokeAsync(() => chk.Change(on));
    }

    private static async Task FillValidAsync(IRenderedComponent<StatusCreateModal> cut)
    {
        await SetNameAsync(cut, "Новий статус");
        await SetCodeAsync(cut, "NEW");
        await SetOrderAsync(cut, 0);
        await ToggleActiveAsync(cut, true);
    }

    // ---------------- tests ----------------

    [Fact]
    public async Task Open_ShowsModal_And_InitialState()
    {
        // сервісні дефолти для валідатора
        _service.Setup(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _service.Setup(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var cut = RenderComponent<StatusCreateModal>();
        Assert.False(cut.Instance.IsOpen);

        await OpenAsync(cut);

        Assert.True(cut.Instance.IsOpen);
        // є заголовок модалки
        Assert.Contains("Новий статус", cut.Markup);
    }

    [Fact]
    public async Task Submit_Valid_CallsService_Emits_OnCreated_And_Closes()
    {
        // Валідатор: унікальні
        _service.Setup(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _service.Setup(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Сервіс створення
        var created = new StatusKind { Id = 42, Name = "Новий статус", Code = "NEW", IsActive = true, Order = 0 };
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateKindViewModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(created);

        StatusKind? received = null;

        var cut = RenderComponent<StatusCreateModal>(ps => ps
            .Add(p => p.OnCreated, EventCallback.Factory.Create<StatusKind>(this, k => received = k))
        );

        await OpenAsync(cut);
        await FillValidAsync(cut);

        // submit
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            _service.Verify(s => s.CreateAsync(It.Is<CreateKindViewModel>(m =>
                    m.Name == "Новий статус" &&
                    m.Code == "NEW" &&
                    m.Order == 0 &&
                    m.IsActive == true),
                It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(received);
            Assert.False(cut.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Submit_Invalid_ShowsValidation_Errors_And_DoesNotCallService()
    {
        // Валідатор все одно питає сервіс тільки на DependentRules; тут ми зламаємо базові правила,
        // тож до сервісних перевірок не дійде.
        var cut = RenderComponent<StatusCreateModal>();
        await OpenAsync(cut);

        // пропускаємо Name/Code (порожні)
        await SetOrderAsync(cut, 1);
        await ToggleActiveAsync(cut, true);

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            // мають з’явитись помилки
            Assert.NotEmpty(cut.FindAll(".validation-message"));
            // сервіс створення не викликається
            _service.Verify(s => s.CreateAsync(It.IsAny<CreateKindViewModel>(), It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Fact]
    public async Task Submit_Duplicate_Name_Blocked_By_Validator_Service_Not_Called()
    {
        // Дублікат імені -> валідатор заверне
        _service.Setup(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service.Setup(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var cut = RenderComponent<StatusCreateModal>();
        await OpenAsync(cut);
        await FillValidAsync(cut);

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            // помилки валідації присутні
            Assert.NotEmpty(cut.FindAll(".validation-message"));
            // сервіс CreateAsync не викликано
            _service.Verify(s => s.CreateAsync(It.IsAny<CreateKindViewModel>(), It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Fact]
    public async Task Submit_Duplicate_Code_Blocked_By_Validator_Service_Not_Called()
    {
        _service.Setup(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _service.Setup(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cut = RenderComponent<StatusCreateModal>();
        await OpenAsync(cut);
        await FillValidAsync(cut);

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".validation-message"));
            _service.Verify(s => s.CreateAsync(It.IsAny<CreateKindViewModel>(), It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Fact]
    public async Task Cancel_Button_Closes_Modal()
    {
        // дефолти для валідатора
        _service.Setup(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _service.Setup(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var cut = RenderComponent<StatusCreateModal>();
        await OpenAsync(cut);

        // кнопка «Скасувати» у футері — друга кнопка
        await cut.InvokeAsync(() => cut.Find("button.btn.btn-warning.btn-sm").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsOpen);
        });
    }
}
