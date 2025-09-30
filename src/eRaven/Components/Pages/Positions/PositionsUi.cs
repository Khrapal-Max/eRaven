//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionsUi
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Components.Pages.Positions;

public static class PositionsUi
{
    // ---- Фільтр/Сорт/Маппінг --------------------------------------------

    public static IEnumerable<PositionUnit> Filter(IEnumerable<PositionUnit> src, string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return src;

        q = q.Trim();
        return src.Where(p =>
            (p.Code ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.ShortName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.SpecialNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (p.CurrentPerson?.FullName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    public static IOrderedEnumerable<PositionUnit> Sort(IEnumerable<PositionUnit> items) =>
        items.OrderBy(p => p.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
             .ThenBy(p => p.ShortName, StringComparer.OrdinalIgnoreCase);

    public static PositionUnitViewModel MapToVm(PositionUnit p)
    {
        string? personName = null;
        if (p.CurrentPerson is not null)
        {
            // якщо домен дає готовий FullName — беремо його,
            // інакше будуємо з FirstName/LastName
            personName = !string.IsNullOrWhiteSpace(p.CurrentPerson.FullName)
                ? p.CurrentPerson.FullName
                : string.Join(' ', new[]
                  {
                  p.CurrentPerson.FirstName,
                  p.CurrentPerson.LastName
                  }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
        }

        return new PositionUnitViewModel
        {
            Id = p.Id,
            Code = p.Code ?? string.Empty,
            ShortName = p.ShortName,
            SpecialNumber = p.SpecialNumber,
            FullName = p.FullName, // читати — ОК, але в тестах не призначаємо
            CurrentPersonFullName = string.IsNullOrWhiteSpace(personName) ? "Вакантна" : personName,
            IsActived = p.IsActived
        };
    }

    public static List<PositionUnitViewModel> Transform(IEnumerable<PositionUnit> all, string? search) =>
        [.. Sort(Filter(all, search)).Select(MapToVm)];

    // ---- Імпорт з валідацією -------------------------------------------

    /// <summary>
    /// Валідує та створює позиції з файлу. Ніяких викликів рендера або стейту.
    /// </summary>
    public static async Task<ImportReportViewModel> ImportAsync(
        IReadOnlyList<PositionUnit> rows,
        IValidator<CreatePositionUnitViewModel> validator,
        Application.Services.PositionService.IPositionService service,
        CancellationToken ct = default)
    {
        var added = 0;
        var updated = 0;
        var errors = new List<string>();

        // локальні дублікати кодів у файлі (щоб відразу попередити)
        var duplicateCodes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .Select(r => r.Code!.Trim())
            .GroupBy(c => c, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateCodes.Length > 0)
            errors.Add($"У файлі знайдено дублікати кодів: {string.Join(", ", duplicateCodes)}");

        foreach (var r in rows)
        {
            try
            {
                var vm = new CreatePositionUnitViewModel
                {
                    Code = (r.Code ?? string.Empty).Trim(),
                    ShortName = (r.ShortName ?? string.Empty).Trim(),
                    SpecialNumber = (r.SpecialNumber ?? string.Empty).Trim(),
                    OrgPath = (r.OrgPath ?? string.Empty).Trim(),
                };

                var result = await validator.ValidateAsync(vm, ct);
                if (!result.IsValid)
                {
                    var msg = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                    errors.Add($"{r.ShortName ?? "(без назви)"}: {msg}");
                    continue;
                }

                var entity = new PositionUnit
                {
                    Code = string.IsNullOrWhiteSpace(vm.Code) ? null : vm.Code,
                    ShortName = vm.ShortName,
                    SpecialNumber = vm.SpecialNumber,
                    OrgPath = string.IsNullOrWhiteSpace(vm.OrgPath) ? null : vm.OrgPath,
                    IsActived = true
                };

                await service.CreatePositionAsync(entity, ct);
                added++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ValidationException exItem)
            {
                errors.Add($"{r.ShortName ?? "(без назви)"}: {exItem.Message}");
            }
            catch (ArgumentException exItem)
            {
                errors.Add($"{r.ShortName ?? "(без назви)"}: {exItem.Message}");
            }
            catch (InvalidOperationException exItem)
            {
                errors.Add($"{r.ShortName ?? "(без назви)"}: {exItem.Message}");
            }
            catch (DbUpdateException exItem)
            {
                var message = exItem.GetBaseException().Message;
                errors.Add($"{r.ShortName ?? "(без назви)"}: {message}");
            }
        }

        return new ImportReportViewModel(added, updated, errors);
    }
}