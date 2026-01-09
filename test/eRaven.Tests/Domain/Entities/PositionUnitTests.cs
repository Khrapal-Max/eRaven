//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitTests -> PositionUnit
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Domain.Entities;

public class PositionUnitTests
{
    [Fact]
    public void PositionUnit_Initialization_Test()
    {
        // Arrange
        var id = Guid.NewGuid();
        var number = 1;
        var code = "POS001";
        var shortName = "Manager";
        var fullName = "Senior Manager";
        var specialNumber = "SPC123";
        var state = PositionUnitState.Vacant;
        var rank = "Captain";
        var tarif = "T1";
        var isActived = true;

        // Act
        var positionUnit = new PositionUnit
        {
            Id = id,
            Number = number,
            Code = code,
            ShortName = shortName,
            FullName = fullName,
            SpecialNumber = specialNumber,
            Rank = rank,
            Tarif = tarif,
            IsActived = isActived
        };

        // Assert
        Assert.Equal(id, positionUnit.Id);
        Assert.Equal(number, positionUnit.Number);
        Assert.Equal(code, positionUnit.Code);
        Assert.Equal(shortName, positionUnit.ShortName);
        Assert.Equal(fullName, positionUnit.FullName);
        Assert.Equal(specialNumber, positionUnit.SpecialNumber);
        Assert.Equal(state, positionUnit.State);
        Assert.Equal(rank, positionUnit.Rank);
        Assert.Equal(tarif, positionUnit.Tarif);
        Assert.Equal(isActived, positionUnit.IsActived);
    }

    [Fact]
    public void PositionUnit_Default_String_Properties_Are_NotNull()
    {
        // Act
        var positionUnit = new PositionUnit();

        // Assert (defaults)
        Assert.NotNull(positionUnit.Code);
        Assert.NotNull(positionUnit.ShortName);
        Assert.NotNull(positionUnit.FullName);
        Assert.NotNull(positionUnit.SpecialNumber);
        Assert.NotNull(positionUnit.Rank);
        Assert.NotNull(positionUnit.Tarif);
    }
}
