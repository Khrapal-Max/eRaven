//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryImportExportTests
//-----------------------------------------------------------------------------

using Bunit;
using ClosedXML.Excel;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Components.Pages.Persons.Registry.Drawers;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;

namespace eRaven.Tests.Components.Pages.Registry;

public sealed class RegistryImportExportTests : BunitContext
{
    [Fact]
    public async Task ExportTab_ClickExport_ShouldCallQuery_AndDownloadViaJs()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var queryMock = new Mock<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>>();
        var bootstrapMock = new Mock<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>>();

        var nowUtc = DateTime.UtcNow;

        queryMock
            .Setup(x => x.HandleAsync(It.IsAny<GetPersonsPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PersonListItemDto>(
                Items:
                [
                    new PersonListItemDto(
                        Id: Guid.NewGuid(),
                        FullName: "Іванов Іван Іванович",
                        Rnokpp: "1234567890",
                        Lifecycle: PersonLifecycleDto.Enrolled,
                        EnrollmentKind: EnrollmentKindDto.Unit,
                        Rank: "Солдат",
                        PositionSort:1,
                        Position: "Стрілець",
                        EnrolledAt: new DateOnly(2025, 1, 1),
                        ExcludedAt: null,
                        UpdatedAtUtc: nowUtc
                    )
                ],
                Page: 1,
                PageSize: 200,
                TotalCount: 1
            ));

        Services.AddSingleton(queryMock.Object);
        Services.AddSingleton(bootstrapMock.Object);
        Services.AddSingleton(new ToastService());

        var cut = Render<RegistryImportExport>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.Filters, new PersonsRegistryFilters())
        );

        // act: switch to Export tab
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Експорт").Click();

        // click "Експорт Excel (.xlsx)"
        cut.FindAll("button").Single(b => b.TextContent.Contains("Експорт Excel", StringComparison.OrdinalIgnoreCase)).Click();

        // assert: query called
        queryMock.Verify(x => x.HandleAsync(It.IsAny<GetPersonsPageQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // assert: JS download invoked
        var download = JSInterop.Invocations.SingleOrDefault(i => i.Identifier == "blazorDownloadFile");

        // args: fileName, contentType, base64
        Assert.True(download!.Arguments.Count >= 3);
        var fileName = download.Arguments[0]?.ToString() ?? "";
        var contentType = download.Arguments[1]?.ToString() ?? "";
        var base64 = download.Arguments[2]?.ToString() ?? "";

        Assert.Contains("persons_export_", fileName);
        Assert.EndsWith(".xlsx", fileName);
        Assert.Contains("spreadsheetml.sheet", contentType);
        Assert.False(string.IsNullOrWhiteSpace(base64));
    }

    [Fact]
    public async Task Import_WithValidXlsx_ShouldCallBootstrap_AndCloseDrawer_AndInvokeOnImported()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var queryMock = new Mock<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>>();
        var bootstrapMock = new Mock<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>>();

        bootstrapMock
            .Setup(x => x.HandleAsync(It.IsAny<BootstrapPersonsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BootstrapPersonsResult(
                TotalRows: 1,
                CreatedCount: 1,
                SkippedCount: 0,
                Errors: []
            ));

        Services.AddSingleton(queryMock.Object);
        Services.AddSingleton(bootstrapMock.Object);
        Services.AddSingleton(new ToastService());

        var closed = (bool?)null;
        var imported = false;

        var cut = Render<RegistryImportExport>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => closed = v))
            .Add(p => p.OnImported, EventCallback.Factory.Create(this, () => imported = true))
            .Add(p => p.Author, " system ")
        );

        // feed valid xlsx into OnFileSelected (private) via reflection
        var bytes = BuildValidImportXlsxBytes();
        var browserFile = new FakeBrowserFile("import.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            bytes);

        var args = CreateInputFileChangeEventArgs(browserFile);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnFileSelected", args));
        cut.Render();

        // sanity: import button enabled
        var importBtn = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Імпортувати");
        Assert.False(importBtn.HasAttribute("disabled"));

        // act
        await cut.InvokeAsync(() => importBtn.Click());

        // assert
        await cut.WaitForAssertionAsync(() =>
            bootstrapMock.Verify(x => x.HandleAsync(
                    It.Is<BootstrapPersonsCommand>(c => c.Rows.Count == 1 && c.Author == "system"),
                    It.IsAny<CancellationToken>()),
                Times.Once));

        await cut.WaitForAssertionAsync(() => Assert.True(imported));
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static byte[] BuildValidImportXlsxBytes()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Імпорт");

        // headers (must match component ImportHeadersUa)
        var headers = new[]
        {
            "РНОКПП", "Прізвище", "Імʼя", "По батькові", "Тип", "Номер/посилання",
            "Дата зарахування", "Підстава", "Звання", "Порядок посади", "Посада",
            "БЗВП", "Зброя", "Позивний"
        };

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        // one valid data row
        ws.Cell(2, 1).Value = "1234567890";
        ws.Cell(2, 2).Value = "Іванов";
        ws.Cell(2, 3).Value = "Іван";
        ws.Cell(2, 4).Value = "Іванович";
        ws.Cell(2, 5).Value = "Штат";
        ws.Cell(2, 6).Value = "REF-001";
        ws.Cell(2, 7).Value = new DateTime(2025, 01, 01);
        ws.Cell(2, 8).Value = "Підстава імпорту";
        ws.Cell(2, 9).Value = "Солдат";
        ws.Cell(2, 10).Value = 1;
        ws.Cell(2, 11).Value = "Стрілець";
        ws.Cell(2, 12).Value = "";
        ws.Cell(2, 13).Value = "";
        ws.Cell(2, 14).Value = "";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static InputFileChangeEventArgs CreateInputFileChangeEventArgs(params IBrowserFile[] files)
    {
        var t = typeof(InputFileChangeEventArgs);

        var ctors = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // 1) ctor(IReadOnlyList<IBrowserFile>) / ctor(IEnumerable<IBrowserFile>)
        var ctorList = ctors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 1 &&
                   (typeof(IReadOnlyList<IBrowserFile>).IsAssignableFrom(p[0].ParameterType) ||
                    typeof(IEnumerable<IBrowserFile>).IsAssignableFrom(p[0].ParameterType));
        });

        if (ctorList is not null)
        {
            return (InputFileChangeEventArgs)ctorList.Invoke([files.ToList()]);
        }

        // 2) ctor(IBrowserFile) (зустрічається в деяких версіях)
        var ctorSingle = ctors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 1 && typeof(IBrowserFile).IsAssignableFrom(p[0].ParameterType);
        });

        if (ctorSingle is not null)
        {
            return (InputFileChangeEventArgs)ctorSingle.Invoke([files[0]]);
        }

        throw new InvalidOperationException(
            "Cannot construct InputFileChangeEventArgs: no suitable constructor found for current framework version.");
    }

    private static async Task InvokePrivateAsync(object instance, string method, params object[] args)
    {
        var mi = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);

        var result = mi!.Invoke(instance, args);
        if (result is Task t) await t;
    }

    private sealed class FakeBrowserFile(string name, string contentType, byte[] bytes) : IBrowserFile
    {
        private readonly byte[] _bytes = bytes;

        public string Name { get; } = name;
        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;
        public long Size { get; } = bytes.LongLength;
        public string ContentType { get; } = contentType;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(_bytes, writable: false);
    }
}