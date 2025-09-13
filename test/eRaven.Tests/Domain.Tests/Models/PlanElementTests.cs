// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanElementTests — базові тести доменної інваріанти часу для PlanElement
// -----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public sealed class PlanElementTests
{
    // -------------------------- IsQuarterAligned --------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    public void IsQuarterAligned_ReturnsTrue_ForExactQuarters_ZeroSecZeroMs(int minute)
    {
        // arrange
        var dt = new DateTime(2025, 1, 1, 12, minute, 0, DateTimeKind.Utc);

        // act
        var ok = PlanElement.IsQuarterAligned(dt);

        // assert
        Assert.True(ok);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(44)]
    [InlineData(46)]
    [InlineData(59)]
    public void IsQuarterAligned_ReturnsFalse_ForNonQuarterMinutes(int minute)
    {
        // arrange
        var dt = new DateTime(2025, 1, 1, 12, minute, 0, DateTimeKind.Utc);

        // act
        var ok = PlanElement.IsQuarterAligned(dt);

        // assert
        Assert.False(ok);
    }

    [Fact]
    public void IsQuarterAligned_ReturnsFalse_WhenSecondsNotZero()
    {
        // arrange
        var dt = new DateTime(2025, 1, 1, 12, 30, 1, DateTimeKind.Utc);

        // act
        var ok = PlanElement.IsQuarterAligned(dt);

        // assert
        Assert.False(ok);
    }

    [Fact]
    public void IsQuarterAligned_ReturnsFalse_WhenMillisecondsNotZero()
    {
        // arrange
        var dt = new DateTime(2025, 1, 1, 12, 30, 0, 1, DateTimeKind.Utc);

        // act
        var ok = PlanElement.IsQuarterAligned(dt);

        // assert
        Assert.False(ok);
    }

    // -------------------------- EnsureQuarterAligned ----------------------

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    public void EnsureQuarterAligned_DoesNotThrow_ForValidTimes(int minute)
    {
        // arrange
        var el = new PlanElement
        {
            EventAtUtc = new DateTime(2025, 1, 1, 7, minute, 0, DateTimeKind.Utc)
        };

        // act
        var ex = Record.Exception(() => el.EnsureQuarterAligned());

        // assert
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(1, 0, 0)]   // неправильна хвилина
    [InlineData(30, 1, 0)]  // зайві секунди
    [InlineData(30, 0, 1)]  // зайві мс
    public void EnsureQuarterAligned_Throws_ForInvalidTimes(int minute, int second, int millisecond)
    {
        // arrange
        var el = new PlanElement
        {
            EventAtUtc = new DateTime(2025, 1, 1, 7, minute, second, millisecond, DateTimeKind.Utc)
        };

        // act + assert
        Assert.Throws<InvalidOperationException>(() => el.EnsureQuarterAligned());
    }
}
