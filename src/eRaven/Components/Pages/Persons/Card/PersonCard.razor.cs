//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCard
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class PersonCard
{
    [Parameter] public Guid PersonId { get; set; }

    [Inject] public IQueryHandler<GetPersonCardQuery, PersonDto?> GetPersonCard { get; set; } = default!;

    private bool _loading;
    private PersonDto? _person;
    private string Tab { get; set; } = "current";

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        try
        {
            _person = await GetPersonCard.HandleAsync(new GetPersonCardQuery(PersonId));
        }
        finally
        {
            _loading = false;
        }
    }

    private void ShowCurrent() => Tab = "current";
    private void ShowCareer() => Tab = "career";
    private void ShowAudit() => Tab = "audit";
}