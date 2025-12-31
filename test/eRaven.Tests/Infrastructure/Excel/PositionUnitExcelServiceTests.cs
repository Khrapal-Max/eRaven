//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitExcelServiceTests -> PositionUnitExcelService
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Excel;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace eRaven.Tests.Infrastructure.Excel;

public class PositionUnitExcelServiceTests
{
    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> fill)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("PositionUnits");

        // header (як в сервісі)
        ws.Cell(1, 1).Value = "#";
        ws.Cell(1, 2).Value = "Індекс";
        ws.Cell(1, 3).Value = "Назва посади";
        ws.Cell(1, 4).Value = "Повна посада";
        ws.Cell(1, 5).Value = "ВОС";
        ws.Cell(1, 6).Value = "ШПК";
        ws.Cell(1, 7).Value = "ТР";

        fill(ws);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Export_ShouldWriteHeaderAndRows()
    {
        // Arrange
        var validator = Mock.Of<IValidator<PositionUnit>>();
        var svc = new PositionUnitExcelService(validator);

        var items = new List<PositionUnit>
        {
            new() { Number = 1, Code="A1", ShortName="S1", FullName="F1", SpecialNumber="001", Rank="R1", Tarif="1" },
            new() { Number = 2, Code="A2", ShortName="S2", FullName="F2", SpecialNumber="002", Rank="R2", Tarif="2" }
        };

        // Act
        var bytes = svc.Export(items);

        // Assert
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        Assert.Equal("#", ws.Cell(1, 1).GetString());
        Assert.Equal("Індекс", ws.Cell(1, 2).GetString());
        Assert.Equal("Назва посади", ws.Cell(1, 3).GetString());
        Assert.Equal("Повна посада", ws.Cell(1, 4).GetString());
        Assert.Equal("ВОС", ws.Cell(1, 5).GetString());
        Assert.Equal("ШПК", ws.Cell(1, 6).GetString());
        Assert.Equal("ТР", ws.Cell(1, 7).GetString());

        Assert.Equal(1, ws.Cell(2, 1).GetValue<int>());
        Assert.Equal("A1", ws.Cell(2, 2).GetString());
        Assert.Equal("S1", ws.Cell(2, 3).GetString());
        Assert.Equal("F1", ws.Cell(2, 4).GetString());
        Assert.Equal("001", ws.Cell(2, 5).GetString());
        Assert.Equal("R1", ws.Cell(2, 6).GetString());
        Assert.Equal("1", ws.Cell(2, 7).GetString());

        Assert.Equal(2, ws.Cell(3, 1).GetValue<int>());
        Assert.Equal("A2", ws.Cell(3, 2).GetString());
        Assert.Equal("S2", ws.Cell(3, 3).GetString());
        Assert.Equal("F2", ws.Cell(3, 4).GetString());
        Assert.Equal("002", ws.Cell(3, 5).GetString());
        Assert.Equal("R2", ws.Cell(3, 6).GetString());
        Assert.Equal("2", ws.Cell(3, 7).GetString());
    }

    [Fact]
    public async Task ParseAsync_ShouldTrimFields_AndReturnValidItems()
    {
        // Arrange
        var v = new Mock<IValidator<PositionUnit>>(MockBehavior.Strict);
        v.Setup(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult()); // valid

        var svc = new PositionUnitExcelService(v.Object);

        using var ms = BuildWorkbook(ws =>
        {
            // row 2
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = " A1 ";
            ws.Cell(2, 3).Value = " S ";
            ws.Cell(2, 4).Value = " F ";
            ws.Cell(2, 5).Value = " 001 ";
            ws.Cell(2, 6).Value = " Солдат ";
            ws.Cell(2, 7).Value = " 1 ";
        });

        // Act
        var result = await svc.ParseAsync(ms, CancellationToken.None);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.ValidItems);

        var item = result.ValidItems[0];
        Assert.True(item.IsActived);
        Assert.NotEqual(Guid.Empty, item.Id);

        Assert.Equal(10, item.Number);
        Assert.Equal("A1", item.Code);
        Assert.Equal("S", item.ShortName);
        Assert.Equal("F", item.FullName);
        Assert.Equal("001", item.SpecialNumber);
        Assert.Equal("Солдат", item.Rank);
        Assert.Equal("1", item.Tarif);

        v.Verify(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);
        v.VerifyAll();
    }

    [Fact]
    public async Task ParseAsync_ShouldSkipEmptyRows()
    {
        // Arrange
        var v = new Mock<IValidator<PositionUnit>>(MockBehavior.Strict);

        // валідатор має бути викликаний тільки 1 раз (для непорожнього рядка)
        v.Setup(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult());

        var svc = new PositionUnitExcelService(v.Object);

        using var ms = BuildWorkbook(ws =>
        {
            // row 2: пустий 1..7 -> має бути пропущений
            // row 3: непорожній
            ws.Cell(3, 1).Value = 1;
            ws.Cell(3, 2).Value = "A1";
            ws.Cell(3, 3).Value = "S";
            ws.Cell(3, 4).Value = "F";
            ws.Cell(3, 5).Value = "001";
            ws.Cell(3, 6).Value = "R";
            ws.Cell(3, 7).Value = "1";
        });

        // Act
        var result = await svc.ParseAsync(ms, CancellationToken.None);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Single(result.ValidItems);

        v.Verify(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);
        v.VerifyAll();
    }

    [Fact]
    public async Task ParseAsync_WhenInvalid_ShouldAddErrors_WithRowNumber()
    {
        // Arrange
        var v = new Mock<IValidator<PositionUnit>>(MockBehavior.Strict);

        v.Setup(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult(
         [
             new ValidationFailure(nameof(PositionUnit.Code), "Code required"),
             new ValidationFailure(nameof(PositionUnit.Number), "Number invalid"),
         ]));

        var svc = new PositionUnitExcelService(v.Object);

        using var ms = BuildWorkbook(ws =>
        {
            // row 2: непорожній, але "невалідний" за валідатором
            ws.Cell(2, 1).Value = 0;
            ws.Cell(2, 2).Value = "";   // code empty
            ws.Cell(2, 3).Value = "S";
            ws.Cell(2, 4).Value = "F";
            ws.Cell(2, 5).Value = "001";
            ws.Cell(2, 6).Value = "R";
            ws.Cell(2, 7).Value = "1";
        });

        // Act
        var result = await svc.ParseAsync(ms, CancellationToken.None);

        // Assert
        Assert.Empty(result.ValidItems);
        Assert.Equal(2, result.Errors.Count);

        Assert.All(result.Errors, e => Assert.Equal(2, e.RowNumber));
        Assert.Contains(result.Errors, e => e.Field == nameof(PositionUnit.Code) && e.Message == "Code required");
        Assert.Contains(result.Errors, e => e.Field == nameof(PositionUnit.Number) && e.Message == "Number invalid");

        v.Verify(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);
        v.VerifyAll();
    }

    [Fact]
    public async Task ParseAsync_ShouldRespectCancellation()
    {
        // Arrange
        var v = new Mock<IValidator<PositionUnit>>(MockBehavior.Strict);

        // навіть якщо викличуть — неважливо, ми відмінимо до/під час проходу
        v.Setup(x => x.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ValidationResult());

        var svc = new PositionUnitExcelService(v.Object);

        using var ms = BuildWorkbook(ws =>
        {
            // декілька рядків
            for (var r = 2; r <= 100; r++)
            {
                ws.Cell(r, 1).Value = r;
                ws.Cell(r, 2).Value = $"A{r}";
                ws.Cell(r, 3).Value = "S";
                ws.Cell(r, 4).Value = "F";
                ws.Cell(r, 5).Value = "001";
                ws.Cell(r, 6).Value = "R";
                ws.Cell(r, 7).Value = "1";
            }
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => svc.ParseAsync(ms, cts.Token));
    }
}