//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitImportExportTests -> PositionUnitImportExport
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.RositionUnits.PositionUnitImportExport;
using eRaven.Infrastructure.Excel;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace eRaven.Tests.Components.Pages.Helpers;

public class PositionUnitImportExportTests : BunitContext
{
    private readonly Mock<IPositionUnitExcelService> _excel = new(MockBehavior.Strict);
    private readonly Mock<IPositionUnitRepository> _repo = new(MockBehavior.Strict);

    public PositionUnitImportExportTests()
    {
        Services.AddSingleton(_excel.Object);
        Services.AddSingleton(_repo.Object);

        // щоб не падало при JS.InvokeVoidAsync (Export)
        Services.AddSingleton(Mock.Of<IJSRuntime>());
    }

    [Fact]
    public void ImportButton_Click_ShouldOpenModal()
    {
        // Arrange
        var cut = Render<PositionUnitImportExport>(p =>
            p.Add(x => x.OnChanged, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // Act
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Імпорт").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Імпорт посад (Excel)", cut.Markup);
            Assert.NotNull(cut.Find("input[type=file]"));
        });
    }

    [Fact]
    public void CommitImport_WithoutFile_ShouldShowMessage_AndKeepModalOpen()
    {
        // Arrange
        var cut = Render<PositionUnitImportExport>(p =>
            p.Add(x => x.OnChanged, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        // open modal
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Імпорт").Click();
        cut.WaitForAssertion(() => Assert.Contains("Імпорт посад (Excel)", cut.Markup));

        // Act
        cut.FindAll(".modal .modal-footer button")
           .Single(x => x.TextContent.Trim() == "Імпортувати")
           .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Файл не обрано.", cut.Markup);
            Assert.Contains("Імпорт посад (Excel)", cut.Markup); // модал не закрився
        });
    }
}
