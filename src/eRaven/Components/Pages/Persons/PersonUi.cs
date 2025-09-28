//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonUi
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Domain.Models;

namespace eRaven.Components.Pages.Persons;

public static class PersonUi
{
    public static string? NullIfWhite(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static Person ToPerson(CreatePersonViewModel vm) => new()
    {
        Id = Guid.NewGuid(),
        LastName = vm.LastName!.Trim(),
        FirstName = vm.FirstName!.Trim(),
        MiddleName = NullIfWhite(vm.MiddleName),
        Rnokpp = vm.Rnokpp!.Trim(),
        Rank = vm.Rank!.Trim(),
        BZVP = vm.BZVP!.Trim(),
        Weapon = NullIfWhite(vm.Weapon),
        Callsign = NullIfWhite(vm.Callsign),

        // опційно — якщо додамо ці поля в імпортний шаблон:
        IsAttached = false,
        AttachedFromUnit = null
    };
}
