//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcelImportTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.ViewModels;
using eRaven.Components.Shared.ExcelImport;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Shared;

public class ExcelImportTests : TestContext
{
    private readonly Mock<IExcelService> _excel;
    public ExcelImportTests()
    {
        _excel = new(MockBehavior.Strict);
        Services.AddSingleton(_excel.Object);
    }

    private sealed class Row { public int Id { get; set; } }

    // ---- helper fake file ----
    private sealed class FakeBrowserFile(string name, string contentType, byte[] bytes, long? size = null) : IBrowserFile
    {
        private readonly byte[] _bytes = bytes;
        public string Name { get; } = name;
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
        public long Size { get; } = size ?? bytes.LongLength;
        public string ContentType { get; } = contentType;
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(_bytes, writable: false);
    }

    // ---------- Test 1: Render & params ----------

    [Fact]
    public void ShouldRender_ExcelImport()
    {
        // Arrange
        // Act
        var cut = RenderComponent<ExcelImport<Row>>(ps => ps
            .Add(p => p.TemplateUrl, "/templates/sample.xlsx")
            .Add(p => p.MaxSizeMb, 8)
            .Add(p => p.StopOnParseErrors, true)
            .Add(p => p.ProcessAsync, rows => Task.FromResult(new ImportReportViewModel(0, 0, [])))
        );

        // Assert
        Assert.NotNull(cut);
        Assert.Contains("accept=\".xlsx\"", cut.Markup);
    }
}
