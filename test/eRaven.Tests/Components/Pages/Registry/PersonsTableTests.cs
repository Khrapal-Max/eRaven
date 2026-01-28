//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsTableTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Person;
using eRaven.Components.Pages.Persons.Registry;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Pages.Registry;

public sealed class PersonsTableTests : BunitContext
{
    private static PersonListItemDto Item(
        PersonLifecycle lc,
        DateOnly? excludedAt = null,
        DateOnly? enrolledAt = null,
        string? rank = "сержант",
        int? positionSort = 1,
        string? position = "Оператор")
        => new(
            Id: Guid.NewGuid(),
            FullName: "Ivanov Ivan",
            Rnokpp: "1234567890",
            Lifecycle: lc,
            Rank: rank,
            PositionSort: positionSort,
            Position: position,
            EnrollmentKind: null,
            EnrolledAt: enrolledAt,
            ExcludedAt: excludedAt,
            UpdatedAtUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        );

    [Fact]
    public async Task OpenCard_click_should_invoke_OnOpenCard_with_row()
    {
        // arrange
        var row = Item(PersonLifecycle.Reserved);
        PersonListItemDto? captured = null;

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
            .Add(p => p.OnOpenCard, EventCallback.Factory.Create<PersonListItemDto>(this, r => captured = r))
        );

        // act
        var btn = cut.FindAll("button")
            .First(x => x.TextContent.Contains("Відкрити картку"));

        await cut.InvokeAsync(() => btn.Click());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(row.Id, captured!.Id);
        Assert.Equal(row.Rnokpp, captured.Rnokpp);
    }

    [Fact]
    public async Task OpenCard_click_when_OnOpenCard_not_set_should_fallback_to_OnRowClick()
    {
        // arrange
        var row = Item(PersonLifecycle.Reserved);
        PersonListItemDto? clicked = null;

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
            .Add(p => p.OnRowClick, EventCallback.Factory.Create<PersonListItemDto>(this, r => clicked = r))
        // OnOpenCard НЕ передаємо
        );

        // act
        var btn = cut.FindAll("button")
            .First(x => x.TextContent.Contains("Відкрити картку"));

        await cut.InvokeAsync(() => btn.Click());

        // assert
        Assert.NotNull(clicked);
        Assert.Equal(row.Id, clicked!.Id);
    }

    [Fact]
    public void LifecycleBadge_should_render_expected_text_and_class()
    {
        // arrange
        var enrolled = Item(PersonLifecycle.Enrolled, enrolledAt: new DateOnly(2026, 01, 10), excludedAt: null);
        var reservedExcluded = Item(PersonLifecycle.Reserved, excludedAt: new DateOnly(2026, 01, 31), enrolledAt: null);

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [enrolled, reservedExcluded])
        );

        // act
        var badges = cut.FindAll("span.badge");

        // assert (порядок може бути будь-який — шукаємо по тексту)
        var enrolledBadge = badges.First(x => x.TextContent.Contains("В ТАБЕЛІ"));
        Assert.Contains("bg-success", enrolledBadge.GetAttribute("class"));

        var reservedExcludedBadge = badges.First(x => x.TextContent.Contains("РЕЗЕРВ (ВИКЛ)"));
        Assert.Contains("bg-secondary", reservedExcludedBadge.GetAttribute("class"));
    }

    [Fact]
    public async Task Enroll_click_should_invoke_OnEnroll_with_row()
    {
        // arrange
        var row = Item(PersonLifecycle.Reserved);

        PersonListItemDto? captured = null;

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
            .Add(p => p.OnEnroll, EventCallback.Factory.Create<PersonListItemDto>(this, r => captured = r))
        );

        // act
        var action = cut.Find(".action-enroll");
        action.Click();

        // assert
        Assert.NotNull(captured);
        Assert.Equal(row.Id, captured!.Id);
    }

    [Fact]
    public async Task Exclude_click_should_invoke_OnExclude_with_row()
    {
        // arrange
        var row = Item(PersonLifecycle.Enrolled, enrolledAt: new DateOnly(2026, 01, 10));

        PersonListItemDto? captured = null;

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
            .Add(p => p.OnExclude, EventCallback.Factory.Create<PersonListItemDto>(this, r => captured = r))
        );

        // act
        var action = cut.Find(".action-exclude");
        action.Click();

        // assert
        Assert.NotNull(captured);
        Assert.Equal(row.Id, captured!.Id);
    }

    [Fact]
    public async Task Enroll_click_when_OnEnroll_not_set_should_not_throw_and_not_invoke_anything()
    {
        // arrange
        var row = Item(PersonLifecycle.Reserved);

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
        // OnEnroll НЕ передаємо
        );

        // act + assert (не має падати)
        var action = cut.Find(".action-enroll");
        action.Click();
    }

    [Fact]
    public async Task Exclude_click_when_OnExclude_not_set_should_not_throw_and_not_invoke_anything()
    {
        // arrange
        var row = Item(PersonLifecycle.Enrolled, enrolledAt: new DateOnly(2026, 01, 10));

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
        // OnExclude НЕ передаємо
        );

        // act + assert (не має падати)
        var action = cut.Find(".action-exclude");
        action.Click();
    }

    [Fact]
    public async Task Enroll_is_disabled_when_person_is_enrolled()
    {
        // arrange
        var row = Item(PersonLifecycle.Enrolled, enrolledAt: new DateOnly(2026, 01, 10));

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
        );

        // assert: disabled attribute має бути на HTML-елементі кнопки
        var action = cut.Find(".action-enroll");

        // Button компонент може рендерити disabled на <button> всередині, тому перестрахуємось:
        var btn = action.QuerySelector("button") ?? action;
        Assert.True(btn.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Exclude_is_disabled_when_person_is_reserved()
    {
        // arrange
        var row = Item(PersonLifecycle.Reserved);

        var cut = Render<PersonsTable>(ps => ps
            .Add(p => p.Items, [row])
        );

        // assert
        var action = cut.Find(".action-exclude");
        var btn = action.QuerySelector("button") ?? action;
        Assert.True(btn.HasAttribute("disabled"));
    }
}
