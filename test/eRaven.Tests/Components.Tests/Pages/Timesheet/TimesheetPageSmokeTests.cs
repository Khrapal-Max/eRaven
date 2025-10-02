//------------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//------------------------------------------------------------------------------
// TimesheetPageSmokeTests
// - рендер без крешів (порожні дані)
// - побудова таблиці з одним працівником (перевірка колонок/коду "30")
// - експорт: кнопка активується та викликає JS downloadFile
//------------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.TimesheetViewModels;
using eRaven.Components.Pages.Timesheet;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Timesheet;

public sealed class TimesheetPageSmokeTests : TestContext
{
    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IStatusKindService> _statusKindSvc = new();
    private readonly Mock<IPersonStatusService> _personStatusSvc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();

    public TimesheetPageSmokeTests()
    {
        // DI
        Services.AddSingleton(_personSvc.Object);
        Services.AddSingleton(_statusKindSvc.Object);
        Services.AddSingleton(_personStatusSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_excel.Object);

        // JS downloadFile
        JSInterop.SetupVoid("downloadFile", _ => true);
    }

    private static StatusKind[] Kinds() =>
    [
        new StatusKind { Id = 30,  Code = "30",   Name = "В районі" },
        new StatusKind { Id = 101, Code = "нб",   Name = "Переміщення/звільнення" },
        new StatusKind { Id = 100, Code = "100",  Name = "В БР" }
    ];

    private static Person MakePerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Шевченко",
        FirstName = "Тарас",
        MiddleName = "Григорович",
        Rnokpp = "9990001112",
        Rank = "ст. солдат"
    };

    // ---- helper: встановити місяць/рік і натиснути “Побудувати” ----
    private static void BuildForMonth(IRenderedComponent<TimesheetPage> cut, int year, int month)
    {
        // month select -> onchange
        var monthSelect = cut.Find("select.form-select");
        monthSelect.Change(month.ToString());

        // year input -> onchange (жодних KeyDown)
        var yearInput = cut.Find("input[type='number']");
        yearInput.Change(year);

        // “Побудувати”
        var buildBtn = cut.Find("button.btn.btn-primary[title='Побудувати']");
        buildBtn.Click();
    }

    [Fact(DisplayName = "Timesheet: рендер без крешів (порожні дані)")]
    public void Page_Renders_Smoke_NoData()
    {
        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());

        _personSvc
            .Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cut = RenderComponent<TimesheetPage>();

        // Тиснемо “Побудувати” для поточного місяця/року
        var buildBtn = cut.Find("button.btn.btn-primary[title='Побудувати']");
        buildBtn.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Період:", cut.Markup);
            Assert.Contains("Порожньо", cut.Markup);
        });
    }

    [Fact(DisplayName = "Timesheet: побудова таблиці з однією особою, вересень 2025")]
    public void Page_Builds_Table_With_One_Person()
    {
        var person = MakePerson();

        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());

        _personSvc
            .Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([person]);

        _personStatusSvc
            .Setup(s => s.GetHistoryAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PersonStatus
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                StatusKindId = 30, // "30"
                OpenDate = new DateTime(2025, 09, 11, 0, 0, 0, DateTimeKind.Utc),
                Sequence = 0,
                IsActive = true
            }
            ]);

        var cut = RenderComponent<TimesheetPage>();

        BuildForMonth(cut, 2025, 9);

        cut.WaitForAssertion(() =>
        {
            for (int d = 1; d <= 30; d++)
                Assert.Contains($">{d}<", cut.Markup);

            Assert.Contains(person.Rnokpp!, cut.Markup);
            Assert.Contains(">30<", cut.Markup);
        });
    }

    [Fact(DisplayName = "Timesheet: експорт — кнопка активна і викликає downloadFile")]
    public void Page_Export_Works()
    {
        var person = MakePerson();

        _statusKindSvc
            .Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());

        _personSvc
            .Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([person]);

        _personStatusSvc
            .Setup(s => s.GetHistoryAsync(person.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PersonStatus
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                StatusKindId = 30,
                OpenDate = new DateTime(2025, 09, 11, 0, 0, 0, DateTimeKind.Utc),
                Sequence = 0,
                IsActive = true
            }
            ]);

        _excel
            .Setup(s => s.ExportAsync(It.IsAny<IEnumerable<TimesheetExportRow>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var ms = new MemoryStream([1, 2, 3, 4])
                {
                    Position = 0
                };
                return ms;
            });

        var cut = RenderComponent<TimesheetPage>();

        BuildForMonth(cut, 2025, 9);

        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button.btn.btn-success.btn-sm");
            Assert.False(btn.HasAttribute("disabled"));
        });

        cut.Find("button.btn.btn-success.btn-sm").Click();
        JSInterop.VerifyInvoke("downloadFile");
    }
}
