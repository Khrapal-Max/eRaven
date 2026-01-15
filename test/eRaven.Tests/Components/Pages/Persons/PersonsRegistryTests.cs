//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistryTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Excel;
using eRaven.Application.Commands.PersonMove;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Components.Pages.Persons.Registry;
using eRaven.Components.Pages.Persons.Registry.Drawers;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class PersonsRegistryTests : BunitContext
{
    private static PersonListItemDto Row(Guid id, PersonLifecycle lc)
        => new(
            Id: id,
            FullName: "Ivanov Ivan",
            Rnokpp: "1234567890",
            Lifecycle: lc,
            Rank: "Солдат",
            Position: "Стрілець",
            EnrollmentKind: null,
            EnrolledAt: lc == PersonLifecycle.Enrolled ? new DateOnly(2026, 01, 10) : null,
            ExcludedAt: null,
            UpdatedAtUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        );

    private static PersonDetailsDto Details(Guid id, PersonLifecycle lc)
        => new(
            Id: id,
            Lifecycle: lc,
            EnrollmentKind: null,
            EnrollmentReference: null,
            Rnokpp: "1234567890",
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: null,
            FullName: "Ivanov Ivan",
            Rank: "Солдат",
            PositionSort: 10,
            Position: "Стрілець",
            Bzvp: null,
            Weapon: null,
            Callsign: null,
            EnrolledAt: lc == PersonLifecycle.Enrolled ? new DateOnly(2026, 01, 10) : null,
            ExcludedAt: null,
            Version: 1,
            UpdatedAtUtc: new DateTime(2026, 01, 07, 12, 0, 0, DateTimeKind.Utc)
        );

    private sealed record SetupResult(
        Mock<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>> PersonsPageQuery,
        Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>> DetailsQuery,
        Mock<ICommandHandler<CreateReservedCommand, Guid>> CreateReservedHandler,
        Mock<ICommandHandler<EnrollCommand, Guid>> EnrollHandler,
        Mock<ICommandHandler<ExcludeCommand, Guid>> ExcludeHandler,
        Mock<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>> BootstrapHandler,
        Mock<IRankCatalog> RankCatalog,
        ToastService Toasts);

    private SetupResult RegisterCommonServices(
        Func<GetPersonsPageQuery, PagedResult<PersonListItemDto>> pageResolver,
        Func<GetPersonDetailsQuery, PersonDetailsDto?> detailsResolver)
    {
        var toasts = new ToastService();
        Services.AddSingleton(toasts);

        var pageQuery = new Mock<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>>(MockBehavior.Strict);
        pageQuery
            .Setup(x => x.HandleAsync(It.IsAny<GetPersonsPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPersonsPageQuery q, CancellationToken _) => pageResolver(q));
        Services.AddSingleton(pageQuery.Object);

        var details = new Mock<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>>(MockBehavior.Strict);
        details
            .Setup(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPersonDetailsQuery q, CancellationToken _) => detailsResolver(q));
        Services.AddSingleton(details.Object);

        var create = new Mock<ICommandHandler<CreateReservedCommand, Guid>>(MockBehavior.Strict);
        create
            .Setup(x => x.HandleAsync(It.IsAny<CreateReservedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        Services.AddSingleton(create.Object);

        var enroll = new Mock<ICommandHandler<EnrollCommand, Guid>>(MockBehavior.Strict);
        enroll
            .Setup(x => x.HandleAsync(It.IsAny<EnrollCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        Services.AddSingleton(enroll.Object);

        var exclude = new Mock<ICommandHandler<ExcludeCommand, Guid>>(MockBehavior.Strict);
        exclude
            .Setup(x => x.HandleAsync(It.IsAny<ExcludeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        Services.AddSingleton(exclude.Object);

        var bootstrap = new Mock<ICommandHandler<BootstrapPersonsCommand, BootstrapPersonsResult>>(MockBehavior.Strict);
        bootstrap
            .Setup(x => x.HandleAsync(It.IsAny<BootstrapPersonsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BootstrapPersonsResult(
                TotalRows: 0,
                CreatedCount: 0,
                SkippedCount: 0,
                Errors: []));
        Services.AddSingleton(bootstrap.Object);

        var ranks = new Mock<IRankCatalog>(MockBehavior.Strict);
        ranks
            .Setup(x => x.GetActive())
            .Returns([new RankOption("Солдат"), new RankOption("Сержант")]);
        Services.AddSingleton(ranks.Object);

        return new SetupResult(pageQuery, details, create, enroll, exclude, bootstrap, ranks, toasts);
    }

    [Fact]
    public void Initial_render_should_query_page_1_and_show_empty_state_and_paging()
    {
        var setup = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([], 1, 10, 0),
            detailsResolver: _ => null);

        var cut = Render<PersonsRegistry>();

        setup.PersonsPageQuery.Verify(x => x.HandleAsync(
            It.Is<GetPersonsPageQuery>(q =>
                q.Page == 1 &&
                q.PageSize == 10 &&
                q.Search == null &&
                q.Lifecycle == null &&
                q.EnrollmentKind == null),
            It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Contains("Записів немає", cut.Markup);

        Assert.Contains("Сторінка", cut.Markup);
        Assert.Contains("Всього:", cut.Markup);

        var back = cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Назад", StringComparison.OrdinalIgnoreCase));
        var next = cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Далі", StringComparison.OrdinalIgnoreCase));

        Assert.True(back.HasAttribute("disabled"));
        Assert.True(next.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Clicking_create_button_should_open_CreateReservedDrawer_and_load_ranks()
    {
        var setup = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([], 1, 10, 0),
            detailsResolver: _ => null);

        var cut = Render<PersonsRegistry>();

        var createBtn = cut.FindAll("button[type='button']")
            .Single(b => b.TextContent.Trim() == "+ Створити");

        await cut.InvokeAsync(() => createBtn.Click());

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<CreateReservedDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });

        setup.RankCatalog.Verify(x => x.GetActive(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Clicking_import_export_button_should_open_RegistryImportExport_drawer()
    {
        _ = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([], 1, 10, 0),
            detailsResolver: _ => null);

        var cut = Render<PersonsRegistry>();

        var btn = cut.FindAll("button")
            .Single(b => b.TextContent.Contains("Імпорт/Експорт", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => btn.Click());

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<RegistryImportExport>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Next_and_prev_buttons_should_change_page_and_requery()
    {
        var calls = new List<GetPersonsPageQuery>();

        var setup = RegisterCommonServices(
            pageResolver: q =>
            {
                calls.Add(q);

                var total = 21; // 3 pages with pageSize 10
                var id = Guid.NewGuid();
                var items = new List<PersonListItemDto> { Row(id, PersonLifecycle.Reserved) };
                return new PagedResult<PersonListItemDto>(items, q.Page, q.PageSize, total);
            },
            detailsResolver: _ => null);

        var cut = Render<PersonsRegistry>();
        Assert.Single(calls);
        Assert.Equal(1, calls[0].Page);

        var nextBtn = cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Далі", StringComparison.OrdinalIgnoreCase));
        var backBtn = cut.FindAll("button")
            .Single(x => x.TextContent.Contains("Назад", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => nextBtn.Click());

        cut.WaitForAssertion(() => Assert.True(calls.Count >= 2));
        Assert.Equal(2, calls[1].Page);

        await cut.InvokeAsync(() => backBtn.Click());

        cut.WaitForAssertion(() => Assert.True(calls.Count >= 3));
        Assert.Equal(1, calls[2].Page);

        setup.PersonsPageQuery.Verify(x => x.HandleAsync(It.IsAny<GetPersonsPageQuery>(), It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task OpenCard_from_table_should_navigate_to_person_card()
    {
        var id = Guid.NewGuid();
        _ = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Reserved)], 1, 10, 1),
            detailsResolver: _ => null);

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.NotNull(nav);

        var cut = Render<PersonsRegistry>();

        var btn = cut.FindAll("button")
            .First(x => x.TextContent.Contains("Відкрити картку", StringComparison.OrdinalIgnoreCase));
        await cut.InvokeAsync(() => btn.Click());

        Assert.EndsWith($"/persons/{id}", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Enroll_action_should_open_EnrollDrawer_with_person_id()
    {
        var id = Guid.NewGuid();

        var setup = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Reserved)], 1, 10, 1),
            detailsResolver: q => Details(q.PersonId, PersonLifecycle.Reserved));

        var cut = Render<PersonsRegistry>();

        await cut.InvokeAsync(() => cut.Find(".action-enroll").Click());

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<EnrollDrawer>();
            Assert.True(drawer.Instance.IsOpen);
            Assert.Equal(id, drawer.Instance.PersonId);
        });

        setup.DetailsQuery.Verify(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        setup.RankCatalog.Verify(x => x.GetActive(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Exclude_action_should_open_ExcludeDrawer_with_person_id()
    {
        var id = Guid.NewGuid();

        var setup = RegisterCommonServices(
            pageResolver: _ => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Enrolled)], 1, 10, 1),
            detailsResolver: q => Details(q.PersonId, PersonLifecycle.Enrolled));

        var cut = Render<PersonsRegistry>();

        await cut.InvokeAsync(() => cut.Find(".action-exclude").Click());

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ExcludeDrawer>();
            Assert.True(drawer.Instance.IsOpen);
            Assert.Equal(id, drawer.Instance.PersonId);
        });

        setup.DetailsQuery.Verify(x => x.HandleAsync(It.IsAny<GetPersonDetailsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Enroll_submit_should_invoke_handler_show_toast_reload_and_update_selected_row()
    {
        var id = Guid.NewGuid();
        var calls = 0;

        var setup = RegisterCommonServices(
            pageResolver: _ =>
            {
                calls++;
                return calls switch
                {
                    1 => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Reserved)], 1, 10, 1),
                    _ => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Enrolled) with { EnrollmentKind = EnrollmentKind.Unit }], 1, 10, 1)
                };
            },
            detailsResolver: q => Details(q.PersonId, PersonLifecycle.Reserved));

        ToastMessage? toast = null;
        setup.Toasts.OnShow += m => toast = m;

        var cut = Render<PersonsRegistry>();

        await cut.InvokeAsync(() => cut.Find(".action-enroll").Click());

        var dto = new EnrollDto
        {
            Id = id,
            Kind = EnrollmentKind.Unit,
            Reference = "REF",
            Reason = "Тест",
            EnrollDate = new DateOnly(2026, 01, 11),
            Rank = "Солдат",
            PositionSort = 10,
            Position = "Стрілець"
        };

        var enrollDrawer = cut.FindComponent<EnrollDrawer>();
        await cut.InvokeAsync(() => enrollDrawer.Instance.OnSubmit.InvokeAsync(dto));

        setup.EnrollHandler.Verify(x => x.HandleAsync(
            It.Is<EnrollCommand>(c =>
                c.PersonId == id &&
                c.Kind == dto.Kind &&
                c.Reference == dto.Reference &&
                c.Reason == dto.Reason &&
                c.EnrollDate == dto.EnrollDate &&
                c.Rank == dto.Rank &&
                c.PositionSort == dto.PositionSort &&
                c.Position == dto.Position &&
                c.Author == "system" &&
                c.NowUtc.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()),
            Times.Once);

        cut.WaitForAssertion(() =>
        {
            var table = cut.FindComponent<PersonsTable>();
            Assert.NotNull(table.Instance.Selected);
            Assert.Equal(PersonLifecycle.Enrolled, table.Instance.Selected!.Lifecycle);
        });

        Assert.NotNull(toast);
        Assert.Equal(ToastKind.Success, toast!.Kind);
        Assert.Equal("Зараховано в табель", toast.Title);
    }

    [Fact]
    public async Task Exclude_submit_should_invoke_handler_show_toast_reload_and_update_selected_row()
    {
        var id = Guid.NewGuid();
        var calls = 0;

        var setup = RegisterCommonServices(
            pageResolver: _ =>
            {
                calls++;
                return calls switch
                {
                    1 => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Enrolled)], 1, 10, 1),
                    _ => new PagedResult<PersonListItemDto>([Row(id, PersonLifecycle.Reserved) with { ExcludedAt = new DateOnly(2026, 01, 12) }], 1, 10, 1)
                };
            },
            detailsResolver: q => Details(q.PersonId, PersonLifecycle.Enrolled));

        ToastMessage? toast = null;
        setup.Toasts.OnShow += m => toast = m;

        var cut = Render<PersonsRegistry>();

        await cut.InvokeAsync(() => cut.Find(".action-exclude").Click());

        var dto = new ExcludeDto
        {
            Id = id,
            Reason = "Тест",
            EffectiveDate = new DateOnly(2026, 01, 12)
        };

        var excludeDrawer = cut.FindComponent<ExcludeDrawer>();
        await cut.InvokeAsync(() => excludeDrawer.Instance.OnSubmit.InvokeAsync(dto));

        setup.ExcludeHandler.Verify(x => x.HandleAsync(
            It.Is<ExcludeCommand>(c =>
                c.PersonId == id &&
                c.Reason == dto.Reason &&
                c.EffectiveDate == dto.EffectiveDate &&
                c.Author == "system" &&
                c.NowUtc.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()),
            Times.Once);

        cut.WaitForAssertion(() =>
        {
            var table = cut.FindComponent<PersonsTable>();
            Assert.NotNull(table.Instance.Selected);
            Assert.Equal(PersonLifecycle.Reserved, table.Instance.Selected!.Lifecycle);
        });

        Assert.NotNull(toast);
        Assert.Equal(ToastKind.Success, toast!.Kind);
        Assert.Equal("Виключено з табеля", toast.Title);
    }
}
