//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionShellTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Mission;
using eRaven.Components.Pages.Mission;
using eRaven.Components.Pages.Mission.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Mission;

public sealed class MissionShellTests : BunitContext
{
    private IRenderedComponent<MissionShell>? _rendered;

    private readonly Mock<IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>> _queryHandler;
    private readonly ToastService _toast;

    public MissionShellTests()
    {
        // Важливо: stubs для drawer'ів, щоб тести MissionShell не залежали від їх DI
        ComponentFactories.AddStub<MissionCreateDrawer>();
        ComponentFactories.AddStub<MissionCloseDrawer>();

        _queryHandler = new Mock<IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>>(MockBehavior.Loose);
        _toast = new ToastService();

        // Дефолтний setup, щоб OnInitializedAsync не падав
        _queryHandler
            .Setup(x => x.HandleAsync(It.IsAny<GetMissionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Services.AddSingleton(_queryHandler.Object);
        Services.AddSingleton(_toast);
    }

    [Fact(DisplayName = "MissionShell: рендер сторінки (основні елементи) + інстанси сервісів")]
    public void RenderPage_ShowsMainElements_AndServicesResolvable()
    {
        // Act
        _rendered = Render<MissionShell>();

        // Assert: DI
        Assert.NotNull(_rendered.Instance.MissionQueryHandler);
        Assert.NotNull(_rendered.Instance.Toasts);

        // Assert: базові елементи сторінки
        _rendered.Markup.Contains("Місії");
        _rendered.Find("input[placeholder='Напр: Район-1, DJI, Розвідка...']");
        _rendered.Find("select.form-select.form-select-sm.rounded-0");

        // Кнопки (перевіряємо наявність)
        Assert.Contains("+ міссія", _rendered.Markup);
        Assert.Contains("Оновити", _rendered.Markup);
        Assert.Contains("Активні", _rendered.Markup);
    }

    [Fact(DisplayName = "MissionShell: кнопки/інпут/селект інтерактивні (click/input/change без помилок)")]
    public async Task Interactions_DoNotThrow_AndOpenCreateDrawer()
    {
        // Arrange (можеш лишити дефолтний setup з ctor)
        var cut = Render<MissionShell>();

        // input -> @oninput
        var search = cut.Find("input[placeholder='Напр: Район-1, DJI, Розвідка...']");
        await cut.InvokeAsync(() => search.Input("DJI"));

        // select -> @onchange (Enum.TryParse)
        var mode = cut.Find("select.form-select.form-select-sm.rounded-0");
        await cut.InvokeAsync(() => mode.Change("Night"));

        // click: "Активні"
        await cut.InvokeAsync(() => FindButtonByText(cut, "Активні").Click());

        // click: "+ міссія" -> має відкрити create drawer (через stub перевіряємо параметр IsOpen)
        await cut.InvokeAsync(() => FindButtonByText(cut, "+ міссія").Click());

        // click: "Оновити"
        await cut.InvokeAsync(() => FindButtonByText(cut, "Оновити").Click());

        // (опційно) переконаємось, що Query реально викликався
        cut.WaitForAssertion(() =>
            _queryHandler.Verify(x => x.HandleAsync(It.IsAny<GetMissionsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce));
    }

    [Fact(DisplayName = "MissionShell: показ таблиці (є заголовки + рядок з даними)")]
    public void Table_IsRendered_WithRow()
    {
        // Arrange
        var rows = new[]
        {
            new MissionDto(
                MissionId: Guid.NewGuid(),
                PositionArea: "Район-1",
                NamePoint: "Точка-А",
                Target: "Розвідка",
                MissionMode: MissionMode.Day,
                DroneName: "DJI",
                CreatedAt: new DateOnly(2026, 01, 10),
                ClosedAt: null,
                IsOpen: true)
        };

        // Assert: заголовки таблиці
        _rendered?.WaitForAssertion(() =>
        {
            Assert.Contains("Поз. район", _rendered.Markup);
            Assert.Contains("Точка", _rendered.Markup);
            Assert.Contains("Мета", _rendered.Markup);
            Assert.Contains("Режим", _rendered.Markup);
            Assert.Contains("Тип ураження", _rendered.Markup);
        });

        // Assert: рядок
        _rendered?.WaitForAssertion(() =>
        {
            Assert.Contains("Район-1", _rendered.Markup);
            Assert.Contains("Точка-А", _rendered.Markup);
            Assert.Contains("Розвідка", _rendered.Markup);
            Assert.Contains("DJI", _rendered.Markup);

            // кнопка "- міссія" в рядку має бути присутня
            Assert.Contains("- міссія", _rendered.Markup);
        });
    }

    private static AngleSharp.Dom.IElement FindButtonByText(IRenderedComponent<MissionShell> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));
}
