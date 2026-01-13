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
        details.Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>()))
               .ReturnsAsync(person);
        Services.AddSingleton(details.Object);

        // валідатор для OnValidSubmit
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

        // PositionSort має бути enabled (Unit дефолт)
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

        // change select -> AttachedByOrder
        await cut.InvokeAsync(() =>
        {
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByOrder.ToString());
        });

        // re-find after render
        var posSort = cut.Find("input[type='number']");
        Assert.True(posSort.HasAttribute("disabled"));
        Assert.Equal("9999", posSort.GetAttribute("value"));

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

        // switch to attached
        await cut.InvokeAsync(() => cut.Find("select.form-select")
            .Change(EnrollmentKind.AttachedByOrder.ToString()));

        // switch back to Unit
        await cut.InvokeAsync(() => cut.Find("select.form-select")
            .Change(EnrollmentKind.Unit.ToString()));

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

        // set kind -> attached (auto 9999)
        await cut.InvokeAsync(() =>
        {
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByList.ToString());
        });

        // fill reason (required)
        await cut.InvokeAsync(() =>
        {
            // textarea is the reason one (rows="3")
            cut.Find("textarea").Change("Підстава тест");
        });

        // submit
        await cut.InvokeAsync(() => cut.Find("form#enroll-form").Submit());

        Assert.NotNull(captured);
        Assert.Equal(id, captured!.Id);
        Assert.Equal(EnrollmentKind.AttachedByList, captured.Kind);
        Assert.Equal(9999, captured.PositionSort);

        Assert.False(isOpen); // drawer closed
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

        // ✅ user enters 33 in Unit
        await cut.InvokeAsync(() =>
        {
            cut.Find("input[type='number']").Change("33");
        });

        // Unit -> Attached
        await cut.InvokeAsync(() =>
        {
            cut.Find("select.form-select").Change(EnrollmentKind.AttachedByOrder.ToString());
        });

        // Attached -> Unit
        await cut.InvokeAsync(() =>
        {
            cut.Find("select.form-select").Change(EnrollmentKind.Unit.ToString());
        });

        // ✅ should restore 33 (not person default 12)
        var posSort = cut.Find("input[type='number']");
        Assert.False(posSort.HasAttribute("disabled"));
        Assert.Equal("33", posSort.GetAttribute("value"));
    }
}
