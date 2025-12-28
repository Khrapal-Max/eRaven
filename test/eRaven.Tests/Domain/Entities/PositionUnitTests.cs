//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitTests -> PositionUnit
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Tests.Domain.Entities;

public class PositionUnitTests
{
    [Fact]
    public void PositionUnit_Initialization_Test()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = "POS001";
        var shortName = "Manager";
        var fullName = "Senior Manager";
        var specialNumber = "SPC123";
        var isActived = true;

        // Act
        var positionUnit = new PositionUnit
        {
            Id = id,
            Code = code,
            ShortName = shortName,
            FullName = fullName,
            SpecialNumber = specialNumber,
            IsActived = isActived
        };

        // Assert
        Assert.Equal(id, positionUnit.Id);
        Assert.Equal(code, positionUnit.Code);
        Assert.Equal(shortName, positionUnit.ShortName);
        Assert.Equal(fullName, positionUnit.FullName);
        Assert.Equal(specialNumber, positionUnit.SpecialNumber);
        Assert.Equal(isActived, positionUnit.IsActived);
    }
}
