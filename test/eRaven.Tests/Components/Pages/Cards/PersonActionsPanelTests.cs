//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonActionsPanelTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Components.Pages.Persons.Cards;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Pages.Cards;

public sealed class PersonActionsPanelTests : BunitContext
{
    private static PersonDetailsDto Person() => new(
        Id: Guid.NewGuid(),
        Lifecycle: PersonLifecycleDto.Enrolled,
        EnrollmentKind: EnrollmentKindDto.Unit,
        EnrollmentReference: "A",
        Rnokpp: "1234567890",
        LastName: "Іванов",
        FirstName: "Іван",
        MiddleName: null,
        FullName: "Іванов Іван",
        Rank: "Солдат",
        PositionSort: 10,
        Position: "Оператор",
        Bzvp: null,
        Weapon: null,
        Callsign: null,
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 1,
        UpdatedAtUtc: new DateTime(2026, 01, 10, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task Clicking_each_button_should_invoke_corresponding_callback()
    {
        // arrange
        var person = Person();

        var personal = 0;
        var rank = 0;
        var position = 0;
        var bzvp = 0;
        var weapon = 0;
        var callsign = 0;

        var cut = Render<PersonActionsPanel>(ps => ps
            .Add(p => p.Person, person)
            .Add(p => p.OnOpenPersonal, EventCallback.Factory.Create(this, () => personal++))
            .Add(p => p.OnOpenRank, EventCallback.Factory.Create(this, () => rank++))
            .Add(p => p.OnOpenPosition, EventCallback.Factory.Create(this, () => position++))
            .Add(p => p.OnOpenBZVP, EventCallback.Factory.Create(this, () => bzvp++))
            .Add(p => p.OnOpenWeapon, EventCallback.Factory.Create(this, () => weapon++))
            .Add(p => p.OnOpenCallsing, EventCallback.Factory.Create(this, () => callsign++))
        );

        // act
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити персональну інфо"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити звання"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити посаду"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити БЗВП"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити зброю"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити позивний"));

        // assert
        Assert.Equal(1, personal);
        Assert.Equal(1, rank);
        Assert.Equal(1, position);
        Assert.Equal(1, bzvp);
        Assert.Equal(1, weapon);
        Assert.Equal(1, callsign);
    }

    [Fact]
    public async Task Clicking_buttons_without_delegates_should_not_throw()
    {
        // arrange
        var cut = Render<PersonActionsPanel>(ps => ps
            .Add(p => p.Person, Person())
        // не передаємо callbacks
        );

        // act + assert (просто не має впасти)
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити персональну інфо"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити звання"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити посаду"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити БЗВП"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити зброю"));
        await cut.InvokeAsync(() => ClickByLabel(cut, "Змінити позивний"));
    }

    [Fact]
    public void Markup_should_render_all_buttons_and_dividers()
    {
        // arrange
        var cut = Render<PersonActionsPanel>(ps => ps
            .Add(p => p.Person, Person())
        );

        // assert: всі тексти є
        Assert.Contains("Змінити персональну інфо", cut.Markup);
        Assert.Contains("Змінити звання", cut.Markup);
        Assert.Contains("Змінити посаду", cut.Markup);
        Assert.Contains("Змінити БЗВП", cut.Markup);
        Assert.Contains("Змінити зброю", cut.Markup);
        Assert.Contains("Змінити позивний", cut.Markup);

        // + два <hr>
        Assert.Equal(2, cut.FindAll("hr").Count);
    }

    private static void ClickByLabel(IRenderedComponent<PersonActionsPanel> cut, string label)
    {
        // Оскільки <Button> — це твій компонент, найстабільніше — знайти реальний <button> по тексту.
        var btn = cut.FindAll("button")
            .Single(b => b.TextContent.Trim() == label);

        btn.Click();
    }
}
