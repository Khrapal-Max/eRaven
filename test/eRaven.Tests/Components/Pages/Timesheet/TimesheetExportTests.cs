//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetExportTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheet;
using eRaven.Components.Pages.Timesheet;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Timesheet;

public sealed class TimesheetExportTests : BunitContext
{
    [Fact]
    public void Render_should_show_export_button()
    {
        var mock = new Mock<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>>(MockBehavior.Loose);
        Services.AddSingleton(mock.Object);

        var cut = Render<TimesheetExport>(ps => ps
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        var btn = cut.Find("button");
        Assert.Contains("Експорт XLSX", btn.TextContent);
        Assert.False(btn.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_should_pass_null_search_when_whitespace()
    {
        // arrange
        ExportTimesheetMonthQuery? captured = null;

        var mock = new Mock<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<ExportTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Callback<ExportTimesheetMonthQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(new DownloadFileDto("f.xlsx", "ct", "b64"));

        Services.AddSingleton(mock.Object);

        JSInterop.SetupVoid("blazorDownloadFile");

        var cut = Render<TimesheetExport>(ps => ps
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.Search, "   "));

        // act
        cut.Find("button").Click();

        // assert
        Assert.NotNull(captured);
        Assert.Equal(2026, captured!.Year);
        Assert.Equal(2, captured.Month);
        Assert.Null(captured.Search);

        mock.VerifyAll();
    }

    [Fact]
    public void When_handler_throws_should_show_error_and_not_call_js()
    {
        // arrange
        var mock = new Mock<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<ExportTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Services.AddSingleton(mock.Object);

        // Important: do NOT setup JS call (strict by default). If component calls it, test will fail.

        var cut = Render<TimesheetExport>(ps => ps
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // act
        cut.Find("button").Click();

        // assert: error rendered
        cut.Markup.Contains("boom");
        Assert.Contains("boom", cut.Find("div.text-danger").TextContent);

        // and button is not stuck disabled
        Assert.False(cut.Find("button").HasAttribute("disabled"));

        mock.VerifyAll();
    }

    [Fact]
    public async Task While_export_running_button_should_be_disabled_then_enabled_after_finish()
    {
        // arrange
        var tcs = new TaskCompletionSource<DownloadFileDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        var mock = new Mock<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>>(MockBehavior.Strict);
        mock.Setup(x => x.HandleAsync(It.IsAny<ExportTimesheetMonthQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        Services.AddSingleton(mock.Object);

        // JS will be called only after completion
        JSInterop.SetupVoid("blazorDownloadFile");

        var cut = Render<TimesheetExport>(ps => ps
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // act: start click without awaiting completion
        var clickTask = cut.InvokeAsync(() => cut.Find("button").Click());

        // assert: during await => disabled
        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button");
            Assert.True(btn.HasAttribute("disabled"));
        });

        // complete
        tcs.SetResult(new DownloadFileDto("f.xlsx", "ct", "b64"));

        await clickTask;

        // assert: enabled again
        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button");
            Assert.False(btn.HasAttribute("disabled"));
        });

        JSInterop.VerifyInvoke("blazorDownloadFile");
        mock.VerifyAll();
    }
}
