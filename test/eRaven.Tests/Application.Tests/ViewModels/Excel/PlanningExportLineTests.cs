//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanningExportLineTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanningOnDateViewModels;
using System.Text.Json;

namespace eRaven.Tests.Application.Tests.ViewModels.Excel;

public class PlanningExportLineTests
{
    [Fact(DisplayName = "PlanningExportLine: за замовчуванням усі поля null")]
    public void Defaults_Are_Null()
    {
        // Act
        var line = new PlanningExportLine();

        // Assert
        Assert.Null(line.Location);
        Assert.Null(line.GroupName);
        Assert.Null(line.CrewName);

        Assert.Null(line.RankName);
        Assert.Null(line.FullName);
        Assert.Null(line.Callsign);
        Assert.Null(line.PlanActionName);
        Assert.Null(line.Order);
        Assert.Null(line.EffectiveAtUtc);
        Assert.Null(line.Note);
    }

    [Fact(DisplayName = "PlanningExportLine: ініціалізація шапки блоку (тільки Location/GroupName/CrewName)")]
    public void Can_Init_Header_Only()
    {
        // Arrange
        const string loc = "Sector A";
        const string grp = "Alpha";
        const string crew = "Crew-1";

        // Act
        var line = new PlanningExportLine
        {
            Location = loc,
            GroupName = grp,
            CrewName = crew
            // інші поля не задаємо → мають лишитись null
        };

        // Assert
        Assert.Equal(loc, line.Location);
        Assert.Equal(grp, line.GroupName);
        Assert.Equal(crew, line.CrewName);

        Assert.Null(line.RankName);
        Assert.Null(line.FullName);
        Assert.Null(line.Callsign);
        Assert.Null(line.PlanActionName);
        Assert.Null(line.Order);
        Assert.Null(line.EffectiveAtUtc);
        Assert.Null(line.Note);
    }

    [Fact(DisplayName = "PlanningExportLine: ініціалізація рядка людини (перші три колонки null)")]
    public void Can_Init_Person_Row_Only()
    {
        // Arrange
        var when = new DateTime(2025, 9, 24, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var line = new PlanningExportLine
        {
            RankName = "Сержант",
            FullName = "ПІБ Тест",
            Callsign = "Сокіл",
            PlanActionName = "Р-001/25",
            Order = "Н-77/25",
            EffectiveAtUtc = when,
            Note = "коментар"
            // перші 3 навмисно не задаємо
        };

        // Assert (перші три null)
        Assert.Null(line.Location);
        Assert.Null(line.GroupName);
        Assert.Null(line.CrewName);

        // інші — як задано
        Assert.Equal("Сержант", line.RankName);
        Assert.Equal("ПІБ Тест", line.FullName);
        Assert.Equal("Сокіл", line.Callsign);
        Assert.Equal("Р-001/25", line.PlanActionName);
        Assert.Equal("Н-77/25", line.Order);
        Assert.Equal(when, line.EffectiveAtUtc);
        Assert.Equal("коментар", line.Note);
    }

    [Fact(DisplayName = "PlanningExportLine: JSON round-trip (імена та значення зберігаються)")]
    public void Json_RoundTrip_Works()
    {
        // Arrange
        var src = new PlanningExportLine
        {
            Location = "L",
            GroupName = "G",
            CrewName = "C",
            RankName = "Ранг",
            FullName = "Ім'я Прізвище",
            Callsign = "Вітер",
            PlanActionName = "Р-123/25",
            Order = "Н-9/25",
            EffectiveAtUtc = new DateTime(2025, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            Note = "note"
        };

        // Act
        var json = JsonSerializer.Serialize(src);
        var back = JsonSerializer.Deserialize<PlanningExportLine>(json)!;

        // Assert
        Assert.NotNull(back);
        Assert.Equal(src.Location, back.Location);
        Assert.Equal(src.GroupName, back.GroupName);
        Assert.Equal(src.CrewName, back.CrewName);
        Assert.Equal(src.RankName, back.RankName);
        Assert.Equal(src.FullName, back.FullName);
        Assert.Equal(src.Callsign, back.Callsign);
        Assert.Equal(src.PlanActionName, back.PlanActionName);
        Assert.Equal(src.Order, back.Order);
        Assert.Equal(src.EffectiveAtUtc, back.EffectiveAtUtc);
        Assert.Equal(src.Note, back.Note);
    }

    [Fact(DisplayName = "PlanningExportLine: можна додати до списку та пройтись без винятків")]
    public void Can_Add_To_List_And_Enumerate()
    {
        // Arrange
        var list = new List<PlanningExportLine>
        {
            new() { Location = "A", GroupName = "G1", CrewName = "C1" },
            new() { RankName = "Сержант", FullName = "Петренко" }
        };

        // Act & Assert (на рівні «смок»: не падає при простій ітерації)
        foreach (var _ in list) { /* no-op */ }

        Assert.Equal(2, list.Count);
    }
}
