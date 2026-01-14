using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Card;

public partial class Card
{
    [Parameter] public Guid Id { get; set; }
}