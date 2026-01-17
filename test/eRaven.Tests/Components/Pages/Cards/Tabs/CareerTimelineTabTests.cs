//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CareerTimelineTabTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Components.Pages.Persons.Cards.Tabs;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Cards.Tabs;

public sealed class CareerTimelineTabTests : BunitContext
{
    [Fact]
    public void Render_should_resolve_required_services_and_call_history_query()
    {
        // arrange
        var personId = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>>(MockBehavior.Strict);
        var cmd = new Mock<ICommandHandler<VoidPersonEventCommand>>(MockBehavior.Loose);

        query.Setup(x => x.HandleAsync(
                It.Is<GetPersonHistoryQuery>(q => q.PersonId == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cut = RenderSut(personId, query, cmd);

        // assert
        cut.WaitForAssertion(() =>
        {
            // порожній список => показує "Подій немає."
            Assert.Contains("Подій немає.", cut.Markup);
        });

        query.VerifyAll();

        Assert.NotNull(cut.Services.GetService<ToastService>());
        Assert.NotNull(cut.Services.GetService<IValidator<VoidPersonEventDto>>());
        Assert.NotNull(cut.Instance.HistoryQuery);
        Assert.NotNull(cut.Instance.VoidHandler);
        Assert.Equal(personId, cut.Instance.PersonId);
    }

    [Fact]
    public void Voided_item_should_show_badge_and_hide_void_button()
    {
        // arrange
        var personId = Guid.NewGuid();
        var item = new PersonEventListItemDto(
            Version: 5,
            EventId: Guid.NewGuid(),
            EffectiveDate: new DateOnly(2026, 1, 5),
            Title: "Зміна посади",
            Details: "На: #2 Командир",
            Author: "tester",
            OccurredAtUtc: DateTime.UtcNow,
            IsVoided: true);

        var query = new Mock<IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>>(MockBehavior.Strict);
        var cmd = new Mock<ICommandHandler<VoidPersonEventCommand>>(MockBehavior.Loose);

        query.Setup(x => x.HandleAsync(It.Is<GetPersonHistoryQuery>(q => q.PersonId == personId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var cut = RenderSut(personId, query, cmd);

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Скасовано", cut.Markup);
            Assert.DoesNotContain(">Відміна події<", cut.Markup);
        });

        query.VerifyAll();
    }

    // -------------------------
    // helpers
    // -------------------------

    private IRenderedComponent<CareerTimelineTab> RenderSut(
        Guid personId,
        Mock<IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>> historyQuery,
        Mock<ICommandHandler<VoidPersonEventCommand>> voidHandler)
    {
        Services.AddSingleton(new ToastService());
        Services.AddSingleton<IValidator<VoidPersonEventDto>>(new VoidPersonEventDtoValidator());

        Services.AddSingleton(historyQuery.Object);
        Services.AddSingleton(voidHandler.Object);

        return Render<CareerTimelineTab>(ps => ps.Add(p => p.PersonId, personId));
    }
}