//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// Tests: ExcelExportButtonTests (bUnit + xUnit + Moq)
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Components.Shared.ExcelExportButton;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace eRaven.Tests.Components.Tests.Shared;

public class ExcelExportButtonTests : TestContext
{
    private readonly Mock<IExcelService> _excelServiceMock;
    private readonly Mock<IJSRuntime> _jsMock;

    public ExcelExportButtonTests()
    {
        _excelServiceMock = new();
        _jsMock = new();

        Services.AddSingleton(_excelServiceMock.Object);
        Services.AddSingleton(_jsMock.Object);
    }

    private class TestItem { public int Id { get; set; } };

    // ---------- Test 1 ----------    

    [Fact]
    public void ShouldRender_ExcelExportButton()
    {
        // Arrange
        var items = new[] { new TestItem { Id = 1 }, new TestItem { Id = 2 } };

        // Act
        var cut = RenderComponent<ExcelExportButton<TestItem>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.Disabled, false)
            .Add(p => p.FileName, "Rows")
            .Add(p => p.Text, "Вивантажити")
            .Add(p => p.ButtonClass, "btn btn-sm btn-success custom")
            .Add(p => p.IconClass, "bi bi-download")
            .Add(p => p.Title, "Мій експорт")
            .Add(p => p.OnBusyChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
        );

        // Assert
        Assert.NotNull(cut);
        Assert.Contains("btn btn-sm btn-success custom", cut.Markup);
        Assert.Contains("Мій експорт", cut.Markup);
        Assert.Contains("bi bi-download", cut.Markup);
        Assert.Contains("Вивантажити", cut.Markup);

        Assert.False(cut.Instance.Disabled);
        Assert.NotNull(cut.Instance.Items);

        Assert.NotNull(cut.Instance.ExcelService);
        Assert.NotNull(cut.Instance.JS);

        DisposeComponents();
    }
    // ---------- Test 2 ----------
    [Fact]
    public async Task Click_ShouldInvokeOnBusyChanged_TrueThenFalse()
    {
        // Arrange
        _excelServiceMock
            .Setup(s => s.ExportAsync(It.IsAny<IEnumerable<TestItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1])); // будь-які дані

        var calls = new List<bool>();
        var cut = RenderComponent<ExcelExportButton<TestItem>>(ps => ps
            .Add(p => p.Items, [new TestItem { Id = 1 }])
            .Add(p => p.Disabled, false)
            .Add(p => p.OnBusyChanged, EventCallback.Factory.Create<bool>(this, b => calls.Add(b)))
        );

        // мок JS — дозволяє виклик downloadFile
        _jsMock
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                "downloadFile",
                It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        // Act
        cut.Find("button").Click();
        await Task.Yield(); // дочекатися async логіки

        // Assert
        Assert.Equal([true, false], calls);

        DisposeComponents();
    }
}