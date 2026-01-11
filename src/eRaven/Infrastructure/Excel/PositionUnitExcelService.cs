//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitExcelService
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using FluentValidation;

namespace eRaven.Infrastructure.Excel;

public sealed class PositionUnitExcelService(IValidator<PositionUnit> validator) : IPositionUnitExcelService
{
    private readonly IValidator<PositionUnit> _validator = validator;

    public byte[] Export(IEnumerable<PositionUnit> items)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("PositionUnits");

        // Header
        ws.Cell(1, 1).Value = "#";
        ws.Cell(1, 2).Value = "Індекс";
        ws.Cell(1, 3).Value = "Назва посади";
        ws.Cell(1, 4).Value = "Повна посада";
        ws.Cell(1, 5).Value = "ВОС";
        ws.Cell(1, 6).Value = "Стан";
        ws.Cell(1, 7).Value = "ШПК";
        ws.Cell(1, 8).Value = "ТР";

        var r = 2;
        foreach (var x in items)
        {
            var stateStr = x.State switch
            {
                PositionUnitState.Vacant => "Вакантна",
                PositionUnitState.Occupied => "Не вакантна",
                PositionUnitState.TemporarilyOccupied => "Тимчасово зайнята",
                PositionUnitState.TemporarilyCandidate => "Призначена рекруту",
                _ => "Не визначений стан"
            };


            ws.Cell(r, 1).Value = x.Number;
            ws.Cell(r, 2).Value = x.Code;
            ws.Cell(r, 3).Value = x.ShortName;
            ws.Cell(r, 4).Value = x.FullName;
            ws.Cell(r, 5).Value = x.SpecialNumber;
            ws.Cell(r, 6).Value = stateStr;
            ws.Cell(r, 7).Value = x.Rank;
            ws.Cell(r, 8).Value = x.Tarif;
            r++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<PositionUnitImportResult> ParseAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        if (stream.CanSeek)
            stream.Position = 0;

        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var result = new PositionUnitImportResult();

        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();

        // очікуємо header у 1-му рядку
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            ct.ThrowIfCancellationRequested();

            // Порожні рядки пропускаємо
            if (ws.Row(row).Cells(1, 7).All(c => c.IsEmpty()))
                continue;

            var item = new PositionUnit
            {
                Id = Guid.NewGuid(),
                IsActived = true,
                Number = ws.Cell(row, 1).GetValue<int>(),
                Code = (ws.Cell(row, 2).GetValue<string>() ?? "").Trim(),
                ShortName = (ws.Cell(row, 3).GetValue<string>() ?? "").Trim(),
                FullName = (ws.Cell(row, 4).GetValue<string>() ?? "").Trim(),
                SpecialNumber = (ws.Cell(row, 5).GetValue<string>() ?? "").Trim(),
                Rank = (ws.Cell(row, 6).GetValue<string>() ?? "").Trim(),
                Tarif = (ws.Cell(row, 7).GetValue<string>() ?? "").Trim(),
            };

            var vr = await _validator.ValidateAsync(item, ct);
            if (!vr.IsValid)
            {
                foreach (var err in vr.Errors)
                    result.Errors.Add(new PositionUnitImportError(row, err.PropertyName, err.ErrorMessage));
                continue;
            }

            result.ValidItems.Add(item);
        }

        return result;
    }
}