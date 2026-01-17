//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotHeaderTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Cards;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Persons.Cards;

public sealed class PersonSnapshotHeaderTests : BunitContext
{
    [Theory]
    [InlineData(PersonLifecycle.Reserved, "Резерв", "badge bg-primary")]
    [InlineData(PersonLifecycle.Enrolled, "В табелі", "badge bg-success")]
    public void Render_should_show_fullname_rnokpp_lifecycle_badge_and_id(
        PersonLifecycle lifecycle,
        string expectedBadgeText,
        string expectedBadgeClass)
    {
        // arrange
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var person = CreatePerson(lifecycle);

        // act
        var cut = Render<PersonSnapshotHeader>(ps =>
        {
            ps.Add(p => p.Person, person);
            ps.Add(p => p.PersonId, id);
        });

        // assert: fullname + rnokpp + id
        var markup = cut.Markup;
        Assert.Contains(person.FullName, markup, StringComparison.Ordinal);
        Assert.Contains(person.Rnokpp, markup, StringComparison.Ordinal);
        Assert.Contains(id.ToString(), markup, StringComparison.Ordinal);

        // badge
        var badge = cut.FindAll("span")
            .FirstOrDefault(s => s.ClassName!.Contains("badge", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(badge);
        Assert.Equal(expectedBadgeText, badge!.TextContent.Trim());
        Assert.Equal(expectedBadgeClass, badge.ClassName);
    }

    [Fact]
    public async Task Clicking_back_should_navigate_to_persons()
    {
        // arrange
        var id = Guid.NewGuid();
        var person = CreatePerson(PersonLifecycle.Reserved);

        var cut = Render<PersonSnapshotHeader>(ps =>
        {
            ps.Add(p => p.Person, person);
            ps.Add(p => p.PersonId, id);
        });

        var nav = Services.GetRequiredService<NavigationManager>();

        // act
        var backBtn = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Назад", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => backBtn.Click());

        // assert
        Assert.EndsWith("/persons", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    private static PersonDetailsDto CreatePerson(PersonLifecycle lifecycle)
        => new(
            Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Rnokpp: "1234567890",
            LastName: "Тест",
            FirstName: "Юзер",
            MiddleName: "Мідл",
            FullName: "Тест Юзер Мідл",
            Callsign: null,
            Lifecycle: lifecycle,
            Bzvp: null,
            Weapon: null,
            EnrollmentKind: null,
            EnrollmentReference: null,
            EnrolledAt: null,
            ExcludedAt: null,
            Rank: null,
            PositionSort: null,
            Position: null,
            Version: 1,
            UpdatedAtUtc: DateTime.UtcNow
        );
}
