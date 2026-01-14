//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanel
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class PersonActionsPanel
{
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
}
