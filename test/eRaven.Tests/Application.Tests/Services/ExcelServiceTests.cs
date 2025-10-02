//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcelServiceTests
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Domain.Models;

namespace eRaven.Tests.Application.Tests.Services;

public class ExcelServiceTests
{
    // =========================================================================
    // Export
    // =========================================================================

    [Fact]
    public async Task ExportAsync_WritesHeaders_AndRows_ForPerson()
    {
        // Arrange
        var svc = new ExcelService();
        var people = new List<Person>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Rnokpp = "1234567890",
                Rank = "С-т",
                LastName = "Шевченко",
                FirstName = "Тарас",
                MiddleName = "Григорович",
                BZVP = "БЗВП-1",
                Weapon = "АК",
                Callsign = "Вітер",
                StatusKindId = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Rnokpp = "2222222222",
                Rank = "Сол.",
                LastName = "Іваненко",
                FirstName = "Іван",
                MiddleName = null,
                Weapon = "ПКМ",
                StatusKindId = 2
            }
        };

        // Act
        using var stream = await svc.ExportAsync(people);

        // Assert
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var headers = ws.Row(1).CellsUsed().Select(c => c.GetString()).ToList();

        // Ключові колонки присутні; FullName (NotMapped / без сеттера) — відсутній
        Assert.Contains("Rnokpp", headers, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LastName", headers, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("FirstName", headers, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("FullName", headers, StringComparer.OrdinalIgnoreCase);

        // Перевірка значень першого рядка
        var r2 = ws.Row(2);
        Assert.Equal("1234567890", r2.Cell(headers.IndexOf(headers.First(h => h.Equals("Rnokpp", StringComparison.OrdinalIgnoreCase))) + 1).GetString());
        Assert.Equal("Шевченко", r2.Cell(headers.IndexOf(headers.First(h => h.Equals("LastName", StringComparison.OrdinalIgnoreCase))) + 1).GetString());
        Assert.Equal("Тарас", r2.Cell(headers.IndexOf(headers.First(h => h.Equals("FirstName", StringComparison.OrdinalIgnoreCase))) + 1).GetString());
    }

    [Fact]
    public async Task ExportImport_RoundTrip_Person_PreservesCoreScalars()
    {
        // Arrange
        var svc = new ExcelService();
        var id1 = Guid.NewGuid();
        var people = new List<Person>
        {
            new()
            {
                Id = id1,
                Rnokpp = "9999999999",
                Rank = "К-н",
                LastName = "Петренко",
                FirstName = "Петро",
                MiddleName = "Петрович",
                BZVP = "B-1",
                Weapon = "M4",
                Callsign = "Сокіл",
                StatusKindId = 1
            }
        };

        // Act
        using var export = await svc.ExportAsync(people);
        export.Position = 0;
        var (rows, errors) = await svc.ImportAsync<Person>(export);

        // Assert
        Assert.Empty(errors);
        Assert.Single(rows);
        var p = rows[0];

        // Скаляри збережено (FullName — обчислюваний у моделі)
        Assert.Equal("9999999999", p.Rnokpp);
        Assert.Equal("Петренко", p.LastName);
        Assert.Equal("Петро", p.FirstName);
        Assert.Equal("Петрович", p.MiddleName);
        Assert.Equal("M4", p.Weapon);
        Assert.Equal("Сокіл", p.Callsign);
        Assert.Equal(1, p.StatusKindId);
    }

    // =========================================================================
    // Import
    // =========================================================================

    [Fact]
    public async Task ImportAsync_ReadsPerson_FromCustomWorkbook_IgnoresUnknownColumn()
    {
        // Arrange
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Data");
            // Заголовки (включно з невідомим "FullName" — має ігноруватись)
            ws.Cell(1, 1).Value = "Rnokpp";
            ws.Cell(1, 2).Value = "LastName";
            ws.Cell(1, 3).Value = "FirstName";
            ws.Cell(1, 4).Value = "FullName";  // має бути проігноровано
            ws.Cell(1, 5).Value = "StatusKindId";

            ws.Cell(2, 1).Value = "1111111111";
            ws.Cell(2, 2).Value = "Данилюк";
            ws.Cell(2, 3).Value = "Данило";
            ws.Cell(2, 5).Value = 9;

            wb.SaveAs(ms);
        }
        ms.Position = 0;
        var svc = new ExcelService();

        // Act
        var (rows, errors) = await svc.ImportAsync<Person>(ms);

        // Assert
        Assert.Empty(errors);
        Assert.Single(rows);
        var p = rows[0];
        Assert.Equal("1111111111", p.Rnokpp);
        Assert.Equal("Данилюк", p.LastName);
        Assert.Equal("Данило", p.FirstName);
        // FullName ігнорується при імпорті, але доступний як конкатенація
        Assert.Equal("Данилюк Данило", p.FullName);
    }

    [Fact]
    public async Task ImportAsync_ParsesDateTime_ForPersonStatus_AndReportsBadInt()
    {
        // Arrange
        using var ms = new MemoryStream();
        var personId = Guid.NewGuid();

        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Data");
            ws.Cell(1, 1).Value = "PersonId";
            ws.Cell(1, 2).Value = "StatusKindId";
            ws.Cell(1, 3).Value = "FromDate";
            ws.Cell(1, 4).Value = "ToDate";
            ws.Cell(1, 5).Value = "Note";
            ws.Cell(1, 6).Value = "Author";

            // Валідний рядок
            ws.Cell(2, 1).Value = personId.ToString();
            ws.Cell(2, 2).Value = 1;
            ws.Cell(2, 3).Value = new DateTime(2025, 9, 2, 13, 30, 0); // ← ось так
            ws.Cell(2, 4).Value = string.Empty;
            ws.Cell(2, 5).Value = "Ok";
            ws.Cell(2, 6).Value = "tester";

            // Невалідний рядок
            ws.Cell(3, 1).Value = personId.ToString();
            ws.Cell(3, 2).Value = "abc"; // помилка
            ws.Cell(3, 3).Value = new DateTime(2025, 9, 2, 14, 0, 0);  // ← і тут
            ws.Cell(3, 4).Value = string.Empty;
            ws.Cell(3, 5).Value = "Bad";
            ws.Cell(3, 6).Value = "tester";

            wb.SaveAs(ms);
        }
        ms.Position = 0;
        var svc = new ExcelService();

        // Act
        var (rows, errors) = await svc.ImportAsync<PersonStatus>(ms);

        // Assert
        Assert.True(errors.Count > 0);      // є помилка по "abc"
        Assert.True(rows.Count >= 1);

        var valid = rows.FirstOrDefault(r => r.PersonId == personId && r.StatusKindId == 1 && r.Note == "Ok");
        Assert.NotNull(valid);

        Assert.Equal(new DateTime(2025, 9, 2, 13, 30, 0), valid!.OpenDate);
        Assert.Equal("tester", valid.Author);

        // гарантуємо, що дата конвертувалась без помилки
        Assert.DoesNotContain(errors, e => e.Contains("OpenDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExportAsync_WritesDateTimeWithFormat_AndImportReadsBack()
    {
        // Arrange
        var svc = new ExcelService();
        var items = new List<PersonStatus>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                StatusKindId = 1,
                OpenDate = new DateTime(2025, 9, 2, 8, 15, 0),
                Note = "start",
                Author = "sys",
                Modified = new DateTime(2025, 9, 2, 8, 16, 0)
            }
        };

        // Act
        using var export = await svc.ExportAsync(items);
        export.Position = 0;
        var (rows, errors) = await svc.ImportAsync<PersonStatus>(export);

        // Assert
        Assert.Empty(errors);
        Assert.Single(rows);
        var r = rows[0];
        Assert.Equal(items[0].PersonId, r.PersonId);
        Assert.Equal(items[0].StatusKindId, r.StatusKindId);
        Assert.Equal(items[0].OpenDate, r.OpenDate);
        Assert.Equal(items[0].Note, r.Note);
    }

    [Fact]
    public async Task ImportAsync_HeaderMatching_IsCaseInsensitive()
    {
        // Arrange
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("DATA");
            ws.Cell(1, 1).Value = "rNoKpP";  // різний регістр, пробуємо нормалізацію
            ws.Cell(1, 2).Value = "LASTNAME";
            ws.Cell(1, 3).Value = "firstname";
            ws.Cell(2, 1).Value = "5555555555";
            ws.Cell(2, 2).Value = "Козак";
            ws.Cell(2, 3).Value = "Остап";
            wb.SaveAs(ms);
        }
        ms.Position = 0;
        var svc = new ExcelService();

        // Act
        var (rows, errors) = await svc.ImportAsync<Person>(ms);

        // Assert
        Assert.Empty(errors);
        Assert.Single(rows);
        var p = rows[0];
        Assert.Equal("5555555555", p.Rnokpp);
        Assert.Equal("Козак", p.LastName);
        Assert.Equal("Остап", p.FirstName);
    }
}
