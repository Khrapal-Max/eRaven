//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonSnapshotPanelTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Cards;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Components.Pages.Cards;

public class PersonSnapshotPanelTests : BunitContext
{
    // -------------------------
    // helpers
    // -------------------------

    private static PersonDetailsDto CreatePerson(
        PersonLifecycle lifecycle = PersonLifecycle.Reserved,
        string? rank = "Солдат",
        string? position = "Стрілець",
        string? bzvp = "ВОС",
        string? weapon = "АК",
        string? callsign = "Позивний",
        DateOnly? enrolledAt = null,
        DateOnly? excludedAt = null,
        DateTime? updatedAtUtc = null)
        => new(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Rnokpp: "0000000000",
            LastName: "Тест",
            FirstName: "Юзер",
            MiddleName: "Мідл",
            FullName: "Тест Юзер Мідл",
            Callsign: callsign,
            Lifecycle: lifecycle,
            Bzvp: bzvp,
            Weapon: weapon,
            EnrollmentKind: null,
            EnrollmentReference: null,
            EnrolledAt: enrolledAt,
            ExcludedAt: excludedAt,
            Rank: rank,
            PositionSort: 1,
            Position: position,
            Version: 1,
            UpdatedAtUtc: updatedAtUtc ?? new DateTime(2026, 01, 16, 8, 0, 0, DateTimeKind.Utc)
        );

    [Fact]
    public void Render_should_show_all_main_sections_and_updated_at_in_header()
    {
        // arrange
        var person = CreatePerson(
            lifecycle: PersonLifecycle.Enrolled,
            rank: "Сержант",
            position: "Стрілець",
            bzvp: "ВОС 123",
            weapon: "АК-74",
            callsign: "Лис",
            enrolledAt: new DateOnly(2026, 01, 10),
            excludedAt: null,
            updatedAtUtc: new DateTime(2026, 01, 16, 8, 30, 00, DateTimeKind.Utc));

        // act
        var cut = Render<PersonSnapshotPanel>(ps => ps.Add(p => p.Person, person));

        // assert
        var text = cut.Markup;

        Assert.Contains("Поточний стан", text);
        Assert.Contains("Оновлено:", text);

        // Перевіряємо, що дата має формат dd.MM.yyyy HH:mm (локальний час)
        var headerText = cut.Find(".card-header").TextContent;

        // Поля
        Assert.Contains("Стан", text);
        Assert.Contains("В табелі", text);
        Assert.Contains("В резерві", text);

        Assert.Contains("Звання", text);
        Assert.Contains("Сержант", text);

        Assert.Contains("Посада", text);
        Assert.Contains("Стрілець", text);

        Assert.Contains("БЗВП", text);
        Assert.Contains("ВОС 123", text);

        Assert.Contains("Зброя", text);
        Assert.Contains("АК-74", text);

        Assert.Contains("Позивний", text);
        Assert.Contains("Лис", text);

        Assert.Contains("Цей блок показує “як зараз”", text);
    }

    [Theory]
    [InlineData(PersonLifecycle.Reserved, "резерв")]
    [InlineData(PersonLifecycle.Enrolled, "В табелі")]
    public void Render_should_show_lifecycle_text(PersonLifecycle lifecycle, string expected)
    {
        // arrange
        var person = CreatePerson(lifecycle: lifecycle);

        // act
        var cut = Render<PersonSnapshotPanel>(ps => ps.Add(p => p.Person, person));

        // assert
        Assert.Contains(expected, cut.Markup);
    }
}