//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcludeDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs.Person;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Registry.Drawers;
using eRaven.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Registry;

public sealed class ExcludeDrawerTests : BunitContext
{
    private static readonly DateTime NowUtc = new(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc);

    private static PersonDetailsDto Person(Guid id) => new(
        Id: id,
        Lifecycle: PersonLifecycle.Enrolled,
        EnrollmentKind: EnrollmentKind.Unit,
        EnrollmentReference: "REF",
        Rnokpp: "1234567890",
        LastName: "Ivanov",
        FirstName: "Ivan",
        MiddleName: "M",
        FullName: "Ivanov Ivan M",
        Rank: "Солдат",
        PositionSort: 1,
        Position: "Оператор",
        Bzvp: null,
        Weapon: null,
        Callsign: null,
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 5,
        UpdatedAtUtc: NowUtc
    );

    private static void RegisterValidator(IServiceCollection services)
        => services.AddSingleton<IValidator<ExcludeDto>>(new ExcludeDtoValidator());

    [Fact]
    public void When_open_should_query_details_and_render_form()
    {
        // arrange
        var id = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        query.Setup(x => x.HandleAsync(It.Is<GetPersonDetailsQuery>(q => q.PersonId == id), default))
             .ReturnsAsync(Person(id));

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;

        // act
        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, id)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnSubmit, _ => Task.CompletedTask)
        );

        // assert
        query.Verify(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), default), Times.Once);

        // form exists
        cut.Find("form#exclude-form");

        // header text present
        cut.Markup.Contains("Виключення з табеля");

        // shows person summary
        Assert.Contains("Виключення з табеля", cut.Markup);
        Assert.Contains("Ivanov Ivan M", cut.Markup);
        Assert.Contains("1234567890", cut.Markup);
        Assert.Contains("Солдат", cut.Markup);
        Assert.Contains("Оператор", cut.Markup);
    }

    [Fact]
    public void When_open_with_empty_personId_should_not_query_and_should_show_warning()
    {
        // arrange
        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;

        // act
        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, Guid.Empty)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
        );

        // assert
        query.VerifyNoOtherCalls();

        // _person null => warning
        cut.Find(".alert.alert-warning");
    }

    [Fact]
    public async Task Cancel_should_close_drawer()
    {
        // arrange
        var id = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        query.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), default))
             .ReturnsAsync(Person(id));

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;

        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, id)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
        );

        // act: натискаємо кнопку "Скасувати"
        await cut.InvokeAsync(() =>
        {
            // беремо першу реальну кнопку у футері з текстом
            var cancelBtn = cut.FindAll("button")
                .First(b => b.TextContent.Contains("Скасувати", StringComparison.OrdinalIgnoreCase));
            cancelBtn.Click();
        });

        // assert
        Assert.False(isOpen);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnSubmit_with_trimmed_reason_and_close()
    {
        // arrange
        var id = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        query.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), default))
             .ReturnsAsync(Person(id));

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;
        ExcludeDto? captured = null;

        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, id)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnSubmit, dto =>
            {
                captured = dto;
                return Task.CompletedTask;
            })
        );

        // act: заповнюємо form
        await cut.InvokeAsync(() =>
        {
            // DateOnly в InputDate рендериться як input[type=date]
            var dateInput = cut.Find("input[type='date']");
            dateInput.Change("2026-01-20");
        });

        await cut.InvokeAsync(() =>
        {
            var reason = cut.Find("textarea");
            reason.Change("  причина  ");
        });

        // submit (найнадійніше — Submit() форми)
        await cut.InvokeAsync(() => cut.Find("form#exclude-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.Id);
        Assert.Equal(new DateOnly(2026, 01, 20), captured.EffectiveDate);
        Assert.Equal("причина", captured.Reason); // trimmed

        Assert.False(isOpen);
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnSubmit_and_should_not_close()
    {
        // arrange
        var id = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        query.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), default))
             .ReturnsAsync(Person(id));

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;
        var called = false;

        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, id)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnSubmit, _ =>
            {
                called = true;
                return Task.CompletedTask;
            })
        );

        // act: Reason порожній => валідатор має заблокувати submit
        await cut.InvokeAsync(() =>
        {
            var reason = cut.Find("textarea");
            reason.Change("   ");
        });

        await cut.InvokeAsync(() => cut.Find("form#exclude-form").Submit());

        // assert
        Assert.False(called);
        Assert.True(isOpen);

        // має з’явитись повідомлення валідації
        Assert.Contains("Вкажіть підставу", cut.Markup);
    }

    [Fact]
    public async Task Submit_button_should_be_disabled_when_person_is_null()
    {
        // arrange: query повертає null
        var id = Guid.NewGuid();

        var query = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        query.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), default))
             .ReturnsAsync((PersonDetailsDto?)null);

        Services.AddSingleton(query.Object);
        RegisterValidator(Services);

        var isOpen = true;

        // act
        var cut = Render<ExcludeDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, id)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
        );

        // assert: warning
        cut.Find(".alert.alert-warning");

        // submit disabled через @attributes="SubmitAttrs"
        var submit = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Виключити", StringComparison.OrdinalIgnoreCase));

        Assert.True(submit.HasAttribute("disabled"));
    }
}
