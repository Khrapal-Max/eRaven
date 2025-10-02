// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// StaffOnDatePageSmokeTests
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Components.Pages.Reports;
using eRaven.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Reports;

public class StaffOnDatePageSmokeTests : TestContext
{
    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IStatusKindService> _statusKindSvc = new();
    private readonly Mock<IPersonStatusService> _personStatusSvc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();

    public StaffOnDatePageSmokeTests()
    {
        // JSInterop для ExcelExportButton
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_personSvc.Object);
        Services.AddSingleton(_statusKindSvc.Object);
        Services.AddSingleton(_personStatusSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_excel.Object);
    }

    // ---------------- helpers ----------------

    private static List<StatusKind> Kinds() =>
    [
        new() { Id = 30,  Code = "30",    Name = "В районі",          IsActive = true },
        new() { Id = 100, Code = "100",   Name = "В БР",              IsActive = true },
        new() { Id = 3,   Code = "нб",    Name = "Переміщення",       IsActive = true },
        new() { Id = 5,   Code = "РОЗПОР",Name = "Розпорядження",     IsActive = true },
        new() { Id = 7,   Code = "ВДР",   Name = "Відрядження",       IsActive = true },
        new() { Id = 9,   Code = "В",     Name = "Відпустка",         IsActive = true },
    ];

    private static Person MakePerson(string code = "001", string shortName = "Стрілець", string full = "Стрілець Взв/Рота")
    {
        var pos = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = code,
            ShortName = shortName,
            OrgPath = full,
            SpecialNumber = "123",
            IsActived = true
        };

        return new Person
        {
            Id = Guid.NewGuid(),
            LastName = "Донченко",
            FirstName = "Ігор",
            Rank = "сержант",
            Rnokpp = "1234567890",
            PositionUnit = pos
        };
    }

    private static PersonStatus Status(Guid personId, int kindId, DateTime openUtc) => new()
    {
        Id = Guid.NewGuid(),
        PersonId = personId,
        StatusKindId = kindId,
        OpenDate = openUtc,
        Sequence = 0,
        IsActive = true
    };

    // ---------------- tests ----------------

    [Fact(DisplayName = "StaffOnDate: рендер тулбара і неактивний експорт")]
    public void Page_Renders_Toolbar_And_Export_Disabled()
    {
        _statusKindSvc.Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cut = RenderComponent<StaffOnDate>();

        // Є інпут дати та кнопка "Побудувати"
        Assert.Contains("type=\"date\"", cut.Markup);
        Assert.Contains("bi bi-play", cut.Markup);

        // Кнопка експорту неактивна без рядків
        var exportBtn = cut.Find("button.btn.btn-success.btn-sm");
        Assert.True(exportBtn.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "StaffOnDate: побудова звіту з однією особою (код 30)")]
    public void Page_Builds_With_One_Person_Code30()
    {
        var p = MakePerson(code: "010", shortName: "Оператор", full: "Оператор Рота/Батальйон");

        _statusKindSvc.Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([p]);
        _personStatusSvc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Status(p.Id, 30, new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc))
            ]);

        var cut = RenderComponent<StaffOnDate>();

        // Клік «Побудувати»
        cut.Find("button.btn.btn-primary.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            // має бути ПІБ, індекс посади, код статусу
            Assert.Contains(p.Rnokpp!, cut.Markup);
            Assert.Contains(p.PositionUnit!.Code!, cut.Markup);
            Assert.Contains(">30<", cut.Markup);

            // експорт доступний
            var exportBtn = cut.Find("button.btn.btn-success.btn-sm");
            Assert.False(exportBtn.HasAttribute("disabled"));
        });
    }

    [Fact(DisplayName = "StaffOnDate: фільтр — нб/РОЗПОР не потрапляють у звіт")]
    public void Page_Filters_Excluded_Codes()
    {
        var p1 = MakePerson(code: "001");
        var p2 = MakePerson(code: "002");

        _statusKindSvc.Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([p1, p2]);

        _personStatusSvc.Setup(s => s.GetHistoryAsync(p1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Status(p1.Id, 3, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc))]); // нб
        _personStatusSvc.Setup(s => s.GetHistoryAsync(p2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Status(p2.Id, 5, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc))]); // РОЗПОР

        var cut = RenderComponent<StaffOnDate>();

        cut.Find("button.btn.btn-primary.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            // Обидва виключені — таблиця порожня
            Assert.Contains("Порожньо", cut.Markup);
        });
    }

    [Fact(DisplayName = "StaffOnDate: сортування за індексом посади")]
    public void Page_Sorts_By_PositionCode()
    {
        var pA = MakePerson(code: "002", shortName: "A", full: "A path");
        var pB = MakePerson(code: "001", shortName: "B", full: "B path");

        _statusKindSvc.Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pA, pB]);

        // Обом ставимо код 30, щоб пройшли фільтр
        _personStatusSvc.Setup(s => s.GetHistoryAsync(pA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Status(pA.Id, 30, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc))]);
        _personStatusSvc.Setup(s => s.GetHistoryAsync(pB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Status(pB.Id, 30, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc))]);

        var cut = RenderComponent<StaffOnDate>();

        cut.Find("button.btn.btn-primary.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            var html = cut.Markup;
            var idxB = html.IndexOf(pB.PositionUnit!.Code!, StringComparison.Ordinal);
            var idxA = html.IndexOf(pA.PositionUnit!.Code!, StringComparison.Ordinal);

            Assert.True(idxB >= 0 && idxA >= 0, "Обидва коди повинні бути у розмітці.");
            Assert.True(idxB < idxA, "Код '001' має йти раніше за '002'.");
        });
    }

    [Fact(DisplayName = "StaffOnDate: експорт активується і викликає downloadFile")]
    public void Page_Export_Enabled_And_Triggers_Download()
    {
        var p = MakePerson(code: "010");

        _statusKindSvc.Setup(s => s.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Kinds());
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([p]);
        _personStatusSvc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Status(p.Id, 30, new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc))
            ]);

        // ExcelService: будь-які ReportRow → повертаємо байти
        _excel.Setup(s => s.ExportAsync(It.IsAny<IEnumerable<ReportRow>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(() =>
              {
                  var ms = new MemoryStream([1, 2, 3, 4])
                  {
                      Position = 0
                  };
                  return ms;
              });

        var cut = RenderComponent<StaffOnDate>();

        // Побудова
        cut.Find("button.btn.btn-primary.btn-sm").Click();

        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button.btn.btn-success.btn-sm");
            Assert.False(btn.HasAttribute("disabled"));
        });

        // Клік на експорт
        cut.Find("button.btn.btn-success.btn-sm").Click();

        // Перевіряємо, що був JS-виклик downloadFile
        JSInterop.VerifyInvoke("downloadFile");
    }
}