//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EnrollDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Personal;
using eRaven.Application.Validations;
using eRaven.Components.Pages.Persons.Registry.Drawers;
using eRaven.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class EnrollDrawerTests : BunitContext
{
    private static PersonDetailsDto Person(Guid id, int? sort = 12)
        => new(
            id,
            PersonLifecycle.Reserved,
            null,
            null,
            "1234567890",
            "Ivanov",
            "Ivan",
            "M",
            "Ivanov Ivan M",
            "Солдат",
            sort,
            "Стрілець",
            null,
            null,
            null,
            null,
            null,
            1,
            new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc));

    private void RegisterCommonServices(PersonDetailsDto? person)
    {
        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([new RankOption("Солдат"), new RankOption("Сержант")]);
        Services.AddSingleton(rankCatalog.Object);

        var details = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        details.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(person);
        Services.AddSingleton(details.Object);

        Services.AddSingleton<IValidator<EnrollDto>>(new EnrollDtoValidator());
    }

    [Fact]
    public void When_open_should_load_person_and_render_form()
    {
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id));

        var isOpen = true;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, _ => Task.CompletedTask)
        );

        cut.Find("form#enroll-form");

        var posSort = cut.Find("input[type='number']");
        Assert.False(posSort.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Kind_change_to_attached_should_set_9999_and_disable_PositionSort_and_show_hint()
    {
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id, sort: 12));

        var isOpen = true;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, _ => Task.CompletedTask)
        );

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByOrder.ToString()));

        var posSort = cut.Find("input[type='number']");
        Assert.True(posSort.HasAttribute("disabled"));

        var value = posSort.GetAttribute("value") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            Assert.Contains("9999", posSort.OuterHtml);
        else
            Assert.Equal("9999", value);

        Assert.Contains("9999", cut.Markup);
    }

    [Fact]
    public async Task Kind_change_back_to_unit_should_restore_previous_sort_and_enable_input()
    {
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id, sort: 12));

        var isOpen = true;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, _ => Task.CompletedTask)
        );

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByOrder.ToString()));

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.Unit.ToString()));

        var posSort = cut.Find("input[type='number']");
        Assert.False(posSort.HasAttribute("disabled"));
        Assert.Equal("12", posSort.GetAttribute("value"));
    }

    [Fact]
    public async Task Submit_when_attached_should_send_PositionSort_9999_and_close_drawer()
    {
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id, sort: 7));

        var isOpen = true;
        EnrollDto? captured = null;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, dto =>
            {
                captured = dto;
                return Task.CompletedTask;
            })
        );

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByList.ToString()));

        await cut.InvokeAsync(() =>
            cut.Find("textarea").Change("Підстава тест"));

        await cut.InvokeAsync(() => cut.Find("form#enroll-form").Submit());

        Assert.NotNull(captured);
        Assert.Equal(id, captured!.Id);
        Assert.Equal(EnrollmentKind.AttachedByList, captured.Kind);
        Assert.Equal(9999, captured.PositionSort);

        Assert.False(isOpen);
    }

    [Fact]
    public async Task Kind_toggle_should_restore_user_entered_unit_PositionSort()
    {
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id, sort: 12));

        var isOpen = true;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, _ => Task.CompletedTask)
        );

        await cut.InvokeAsync(() => cut.Find("input[type='number']").Change("33"));

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByOrder.ToString()));

        await cut.InvokeAsync(() =>
            cut.Find("select.form-select").Change(EnrollmentKind.Unit.ToString()));

        var posSort = cut.Find("input[type='number']");
        Assert.False(posSort.HasAttribute("disabled"));
        Assert.Equal("33", posSort.GetAttribute("value"));
    }

    [Fact]
    public void When_open_with_empty_personId_should_not_query_and_should_show_warning()
    {
        // arrange
        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([new RankOption("Солдат")]);
        Services.AddSingleton(rankCatalog.Object);

        var details = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        // важливо: Strict + не Setup => будь-який виклик впаде, тобто ми гарантуємо "не викликається"
        Services.AddSingleton(details.Object);

        Services.AddSingleton<IValidator<EnrollDto>>(new EnrollDtoValidator());

        var isOpen = true;

        // act
        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.PersonId, Guid.Empty)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
        );

        // assert
        details.VerifyNoOtherCalls();

        // _person null => warning
        cut.Find(".alert.alert-warning");
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnSubmit_and_should_not_close()
    {
        // arrange
        var id = Guid.NewGuid();
        RegisterCommonServices(Person(id));

        var isOpen = true;
        var called = false;

        var cut = Render<EnrollDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.PersonId, id)
            .Add(p => p.OnSubmit, _ =>
            {
                called = true;
                return Task.CompletedTask;
            })
        );

        // act:
        // Reason required => лишаємо порожнім/пробіли, submit має не пройти OnValidSubmit
        await cut.InvokeAsync(() =>
        {
            // textarea тут — поле Reason (rows="3")
            cut.Find("textarea").Change("   ");
        });

        await cut.InvokeAsync(() => cut.Find("form#enroll-form").Submit());

        // assert
        Assert.False(called);
        Assert.True(isOpen);

        // повідомлення валідатора (EnrollDtoValidator)
        Assert.Contains("Вкажіть підставу", cut.Markup);
    }
}