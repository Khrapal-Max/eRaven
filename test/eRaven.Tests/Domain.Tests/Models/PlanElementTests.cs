//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanElementTests (final; без TimeKind; з валідацією 15-хвилинної сітки)
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanElementTests
{
    [Fact(DisplayName = "PlanElement: дефолти — GUID-и порожні, enum-и = default, дати = default, колекції ініціалізовані")]
    public void Defaults_AreCorrect()
    {
        var e = new PlanElement();

        Assert.Equal(Guid.Empty, e.Id);
        Assert.Equal(Guid.Empty, e.PlanId);

        Assert.Equal(default, e.Type);
        Assert.Equal(default, e.EventAtUtc);
        Assert.Null(e.Location);
        Assert.Null(e.GroupName);
        Assert.Null(e.ToolType);
        Assert.Null(e.Note);
        Assert.Null(e.Author);

        Assert.Equal(DateTimeKind.Utc, e.RecordedUtc.Kind);
        Assert.InRange(DateTime.UtcNow - e.RecordedUtc, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        Assert.NotNull(e.Participants);
        Assert.Empty(e.Participants);
    }

    [Fact(DisplayName = "PlanElement: IsQuarterAligned/EnsureQuarterAligned працюють як очікується")]
    public void QuarterAlignment_Checks_Work()
    {
        static DateTime T(int h, int m) => new(2025, 9, 10, h, m, 0, DateTimeKind.Utc);

        Assert.True(PlanElement.IsQuarterAligned(T(10, 0)));
        Assert.True(PlanElement.IsQuarterAligned(T(10, 15)));
        Assert.True(PlanElement.IsQuarterAligned(T(10, 30)));
        Assert.True(PlanElement.IsQuarterAligned(T(10, 45)));

        Assert.False(PlanElement.IsQuarterAligned(new DateTime(2025, 9, 10, 10, 1, 0, DateTimeKind.Utc)));
        Assert.False(PlanElement.IsQuarterAligned(new DateTime(2025, 9, 10, 10, 14, 59, DateTimeKind.Utc)));

        var ok = new PlanElement { EventAtUtc = T(12, 30) };
        var bad = new PlanElement { EventAtUtc = new DateTime(2025, 9, 10, 12, 31, 0, DateTimeKind.Utc) };

        ok.EnsureQuarterAligned(); // не кидає

        Assert.Throws<InvalidOperationException>(() => bad.EnsureQuarterAligned());
    }

    [Fact(DisplayName = "PlanElement: можна задати та прочитати базові поля")]
    public void Can_Set_And_Read_Properties()
    {
        var id = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var when = new DateTime(2025, 9, 10, 18, 0, 0, DateTimeKind.Utc);

        var e = new PlanElement
        {
            Id = id,
            PlanId = planId,
            Type = PlanType.Dispatch,
            EventAtUtc = when,
            Location = "Локація А",
            GroupName = "Група 1",
            ToolType = "Екіпаж",
            Note = "примітка",
            Author = "tester",
            RecordedUtc = when
        };

        Assert.Equal(id, e.Id);
        Assert.Equal(planId, e.PlanId);
        Assert.Equal(PlanType.Dispatch, e.Type);
        Assert.Equal(when, e.EventAtUtc);
        Assert.Equal("Локація А", e.Location);
        Assert.Equal("Група 1", e.GroupName);
        Assert.Equal("Екіпаж", e.ToolType);
        Assert.Equal("примітка", e.Note);
        Assert.Equal("tester", e.Author);
        Assert.Equal(when, e.RecordedUtc);
    }
}
