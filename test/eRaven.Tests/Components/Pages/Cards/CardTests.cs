//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CardTests
//-----------------------------------------------------------------------------

using Bunit;
using Bunit.TestDoubles;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Components.Pages.Persons.Cards;
using eRaven.Components.Pages.Persons.Cards.Tabs;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Cards;

public sealed class CardTests : BunitContext
{
    public CardTests()
    {
        // Stub heavy children (so we don't pull their DI/validators/etc)
        ComponentFactories.AddStub<PersonSnapshotHeader>();
        ComponentFactories.AddStub<CardCurrentTab>();
        ComponentFactories.AddStub<CareerTimelineTab>();
        ComponentFactories.AddStub<TimesheetTab>();
    }

    [Fact]
    public void When_person_not_found_should_show_warning()
    {
        // arrange
        var personId = Guid.NewGuid();

        var handler = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        handler.Setup(x => x.HandleAsync(
                It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDetailsDto?)null);

        Services.AddSingleton(handler.Object);

        // act
        var cut = Render<Card>(ps => ps.Add(p => p.PersonId, personId));

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Особа не знайдена.", cut.Markup);
        });

        handler.Verify(x => x.HandleAsync(
            It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
            It.IsAny<CancellationToken>()), Times.Once);

        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public void When_person_found_should_render_header_and_default_current_tab()
    {
        // arrange
        var personId = Guid.NewGuid();
        var person = CreatePerson(personId);

        var handler = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        handler.Setup(x => x.HandleAsync(
                It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        Services.AddSingleton(handler.Object);

        // act
        var cut = Render<Card>(ps => ps.Add(p => p.PersonId, personId));

        handler.Verify(x => x.HandleAsync(
            It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
            It.IsAny<CancellationToken>()), Times.Once);

        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Clicking_career_tab_should_render_CareerTimelineTab_with_personId()
    {
        // arrange
        var personId = Guid.NewGuid();
        var person = CreatePerson(personId);

        var handler = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        handler.Setup(x => x.HandleAsync(
                It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        Services.AddSingleton(handler.Object);

        var cut = Render<Card>(ps => ps.Add(p => p.PersonId, personId));

        // wait initial render (Current tab)
        cut.WaitForAssertion(() => cut.FindComponent<Stub<CardCurrentTab>>());

        // act: click "Кар’єра" (не прив’язуємось до апострофа)
        var careerBtn = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Кар", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => careerBtn.Click());

        handler.Verify(x => x.HandleAsync(
            It.Is<GetPersonDetailsQuery>(q => q.PersonId == personId),
            It.IsAny<CancellationToken>()), Times.Once);

        handler.VerifyNoOtherCalls();
    }

    private static PersonDetailsDto CreatePerson(Guid id)
        => new(
            Id: id,
            Lifecycle: PersonLifecycleDto.Reserved,
            EnrollmentKind: null,
            EnrollmentReference: null,
            Rnokpp: "0000000000",
            LastName: "Тест",
            FirstName: "Юзер",
            MiddleName: "Мідл",
            FullName: "Тест Юзер Мідл",
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Bzvp: "ВОС",
            Weapon: "null",
            Callsign: "Позивний",
            EnrolledAt: null,
            ExcludedAt: null,
            Version: 1,
            UpdatedAtUtc: DateTime.UtcNow
        );
}