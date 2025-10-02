//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CreatePlanActionModalTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Components.Pages.PlanActions.Modals;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class CreatePlanActionModalTests : IDisposable
{
    private readonly TestContext _ctx;

    public CreatePlanActionModalTests()
    {
        _ctx = new TestContext();

        // Для <FluentValidationValidator/>
        _ctx.Services.AddTransient<IValidator<PlanAction>, CreatePlanActionRequestValidator>();
        _ctx.Services.AddSingleton(new Mock<IToastService>().Object);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    // ---------------- helpers ----------------

    private static Person MakePerson(Guid? id = null, int statusKindId = 1) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Rnokpp = "1234567890",
            LastName = "Прізвище",
            FirstName = "Ім'я",
            MiddleName = "По батькові",
            Rank = "Сержант",
            PositionUnit = new PositionUnit { ShortName = "Відділення", OrgPath = "2 рота" },
            BZVP = "БЗ-42",
            Weapon = "АК",
            Callsign = "Сокіл",
            StatusKind = new StatusKind { Modified = DateTime.SpecifyKind(new DateTime(2025, 9, 20, 12, 0, 0), DateTimeKind.Utc), Id = statusKindId, Name = "В строю" },
            StatusKindId = statusKindId
        };

    private static List<StatusKind> MakeStatusKindsList(int id = 1) =>
    [
        new StatusKind { Id = id, Name = "В строю", Order = 1, IsActive = true }
    ];

    private static PlanAction MakeLastAction(Guid personId, MoveType move, string? loc = "L1", string? grp = "G1", string? crew = "C1", DateTime? at = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PlanActionName = "R-1",
            EffectiveAtUtc = at ?? DateTime.SpecifyKind(new DateTime(2025, 9, 18, 10, 0, 0), DateTimeKind.Utc),
            ActionState = ActionState.PlanAction,
            MoveType = move,
            Location = loc ?? "",
            GroupName = grp ?? "",
            CrewName = crew ?? "",
            Note = "",
            // snapshot
            Rnokpp = "1234567890",
            FullName = "Прізвище Ім'я По батькові",
            RankName = "Сержант",
            PositionName = "Відділення 2 рота",
            BZVP = "БЗ-42",
            Weapon = "АК",
            Callsign = "Сокіл",
            StatusKindOnDate = "В строю",
        };

    // ---------------- tests ----------------

    [Fact]
    public void Initially_Hidden()
    {
        var person = MakePerson();
        var cut = _ctx.RenderComponent<CreatePlanActionModal>(ps => ps
            .Add(p => p.Person, person)
            .Add(p => p.StatusKinds, MakeStatusKindsList())
            .Add(p => p.OnSaved, EventCallback.Factory.Create<PlanAction>(this, _ => { }))
        );

        var modal = cut.Find("div.modal");
        Assert.DoesNotContain("show", modal.ClassList);
        Assert.DoesNotContain("d-block", modal.ClassList);
    }

    [Fact]
    public async Task Open_WhenLastWasDispatch_ShouldCreateReturn_CopyFields_AndInvokeOnSaved()
    {
        // Arrange
        var person = MakePerson();
        var last = MakeLastAction(person.Id, MoveType.Dispatch, loc: "BaseA", grp: "Alpha", crew: "Crew-1",
                                  at: DateTime.SpecifyKind(new DateTime(2025, 9, 19, 8, 0, 0), DateTimeKind.Utc));

        PlanAction? received = null;

        var cut = _ctx.RenderComponent<CreatePlanActionModal>(ps => ps
            .Add(p => p.Person, person)
            .Add(p => p.LastPlanAction, last)
            .Add(p => p.StatusKinds, MakeStatusKindsList(person.StatusKindId!.Value))
            .Add(p => p.OnSaved, EventCallback.Factory.Create<PlanAction>(this, pa => received = pa))
        );

        // Act: відкриваємо модалку на Dispatcher
        await cut.InvokeAsync(() => cut.Instance.Open());

        // Переконуємося, що відкрито
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);
            Assert.Contains("d-block", modal.ClassList);
        });

        // Сабміт форми без змін (година/хвилини вже нормалізовані)
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(received);
            // Після Dispatch очікуємо Return
            Assert.Equal(MoveType.Return, received!.MoveType);

            // Копія полів з last
            Assert.Equal("BaseA", received.Location);
            Assert.Equal("Alpha", received.GroupName);
            Assert.Equal("Crew-1", received.CrewName);

            // Снапшот наповнений з Person
            Assert.Equal(person.Rnokpp, received.Rnokpp);
            Assert.Equal(person.FullName, received.FullName);
            Assert.Equal(person.Rank, received.RankName);
            Assert.Equal(person.PositionUnit!.FullName, received.PositionName);
        });
    }

    [Fact]
    public async Task Open_NoLastAction_ShouldBeEditable_EmptyLocation_ShowsValidation_And_NotInvokeOnSaved()
    {
        // Arrange: немає останньої дії → редагований режим
        var person = MakePerson();
        bool called = false;

        var cut = _ctx.RenderComponent<CreatePlanActionModal>(ps => ps
            .Add(p => p.Person, person)
            .Add(p => p.LastPlanAction, null)
            .Add(p => p.StatusKinds, MakeStatusKindsList(person.StatusKindId!.Value))
            .Add(p => p.OnSaved, EventCallback.Factory.Create<PlanAction>(this, _ => called = true))
        );

        // Відкрити
        await cut.InvokeAsync(() => cut.Instance.Open());

        // Location порожня (за замовч.), одразу Submit
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert: є повідомлення валідації для Location, OnSaved не викликано
        cut.WaitForAssertion(() =>
        {
            var messages = cut.FindAll("div.validation-message, .validation-message, .invalid-feedback");
            Assert.True(messages.Any(), "Очікувалося повідомлення валідації (наприклад для 'Локація обов'язкова.').");
            Assert.False(called);
        });
    }
}
