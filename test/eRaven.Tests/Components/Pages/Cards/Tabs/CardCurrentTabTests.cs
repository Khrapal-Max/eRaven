//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CardCurrentTabTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.Commands;
using eRaven.Application.Commands.PersonInfo;
using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Components.Pages.Persons.Cards.Tabs;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Cards.Tabs;

public sealed class CardCurrentTabTests : BunitContext
{
    // -------------------------
    // Smoke: layout
    // -------------------------

    [Fact]
    public void Render_should_show_snapshot_panel_and_actions_panel()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        Assert.NotEmpty(cut.FindComponents<PersonSnapshotPanel>());
        Assert.NotEmpty(cut.FindComponents<PersonActionsPanel>());

        var actions = FindActionsRoot(cut);
        Assert.Contains("Змінити персональну інфо", actions.TextContent);
        Assert.Contains("Змінити звання", actions.TextContent);
        Assert.Contains("Змінити посаду", actions.TextContent);
        Assert.Contains("Змінити БЗВП", actions.TextContent);
        Assert.Contains("Змінити зброю", actions.TextContent);
        Assert.Contains("Змінити позивний", actions.TextContent);
    }

    [Fact]
    public async Task Render_should_have_handlers_and_submit_should_invoke_OnReload()
    {
        // arrange
        var person = CreatePerson();

        var weaponHandler = new Mock<ICommandHandler<ChangeWeaponCommand>>(MockBehavior.Strict);
        weaponHandler
            .Setup(x => x.HandleAsync(It.IsAny<ChangeWeaponCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var onReload = EventCallback.Factory.Create(new object(), () => reloadCount++);

        var cut = RenderSut(
            person,
            onReload: onReload,
            changeWeapon: weaponHandler);

        // ✅ smoke: DI handlers exist on component instance
        Assert.NotNull(cut.Instance.UpdatePersonalInfoCommandHandler);
        Assert.NotNull(cut.Instance.ChangeRankCommandHandler);
        Assert.NotNull(cut.Instance.ChangePositionCommandHandler);
        Assert.NotNull(cut.Instance.ChangeBzvpCommandHandler);
        Assert.NotNull(cut.Instance.ChangeWeaponCommandHandler);
        Assert.NotNull(cut.Instance.ChangeCallsignCommandHandler);

        // ✅ callback parameter is реально підключений
        Assert.True(cut.Instance.OnReload.HasDelegate);

        Assert.NotNull(cut.Instance.Person);

        // act: викликаємо callback drawer-а (це тригерить handler + ReloadParentAsync)
        var dto = new ChangeWeaponDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 13),
            Weapon = "АК-74 №12345"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangeWeaponDrawer>().Instance.OnChangeWeapon.InvokeAsync(dto));

        // assert
        weaponHandler.Verify(
            x => x.HandleAsync(It.IsAny<ChangeWeaponCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(1, reloadCount);
    }

    // -------------------------
    // Click -> open drawer
    // -------------------------

    [Fact]
    public async Task Clicking_personal_button_should_open_UpdatePersonalInfoDrawer()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        var btn = FindActionsButton(cut, "Змінити персональну інфо");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<UpdatePersonalInfoDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Clicking_rank_button_should_open_ChangeRankDrawer_and_load_ranks()
    {
        var person = CreatePerson();

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([
                new RankOption("Солдат"),
                new RankOption("Сержант")
            ]);

        var cut = RenderSut(person, onReload: default, rankCatalog: rankCatalog);

        var btn = FindActionsButton(cut, "Змінити звання");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ChangeRankDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });

        rankCatalog.Verify(x => x.GetActive(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Clicking_position_button_should_open_ChangePositionDrawer()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        var btn = FindActionsButton(cut, "Змінити посаду");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ChangePositionDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Clicking_bzvp_button_should_open_ChangeBzvpDrawer()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        var btn = FindActionsButton(cut, "Змінити БЗВП");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ChangeBzvpDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Clicking_weapon_button_should_open_ChangeWeaponDrawer()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        var btn = FindActionsButton(cut, "Змінити зброю");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ChangeWeaponDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    [Fact]
    public async Task Clicking_callsign_button_should_open_ChangeCallsingDrawer()
    {
        var person = CreatePerson();
        var cut = RenderSut(person, onReload: default);

        var btn = FindActionsButton(cut, "Змінити позивний");
        await ClickAsync(cut, btn);

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.FindComponent<ChangeCallsingDrawer>();
            Assert.True(drawer.Instance.IsOpen);
        });
    }

    // -------------------------
    // Drawer callback -> handler -> OnReload
    // -------------------------

    [Fact]
    public async Task OnUpdatePersonal_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var updateHandler = new Mock<ICommandHandler<UpdatePersonalInfoCommand>>(MockBehavior.Strict);
        UpdatePersonalInfoCommand? captured = null;

        updateHandler.Setup(x => x.HandleAsync(It.IsAny<UpdatePersonalInfoCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpdatePersonalInfoCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var onReload = EventCallback.Factory.Create(this, () => reloadCount++);

        var cut = RenderSut(person, onReload, updatePersonal: updateHandler);

        var dto = new UpdatePersonalInfoDto
        {
            PersonId = person.Id,
            Rnokpp = "1234567890",
            LastName = "  Іваненко ",
            FirstName = " Петро  ",
            MiddleName = "  Іванович ",
            Note = "  note  "
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<UpdatePersonalInfoDrawer>().Instance.OnUpdatePersonal.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.Rnokpp, captured.Rnokpp);
        Assert.Equal(dto.LastName, captured.LastName);
        Assert.Equal(dto.FirstName, captured.FirstName);
        Assert.Equal(dto.MiddleName, captured.MiddleName);
        Assert.Equal(dto.Note, captured.Note);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        updateHandler.Verify(x => x.HandleAsync(It.IsAny<UpdatePersonalInfoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChangeRank_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var handler = new Mock<ICommandHandler<ChangeRankCommand>>(MockBehavior.Strict);
        ChangeRankCommand? captured = null;

        handler.Setup(x => x.HandleAsync(It.IsAny<ChangeRankCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeRankCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var cut = RenderSut(person,
            onReload: EventCallback.Factory.Create(this, () => reloadCount++),
            changeRank: handler);

        var dto = new ChangeRankDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 10),
            Rank = "Солдат",
            Note = "n"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangeRankDrawer>().Instance.OnChangeRank.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.EffectiveDate, captured.EffectiveDate);
        Assert.Equal(dto.Rank, captured.Rank);
        Assert.Equal(dto.Note, captured.Note);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        handler.Verify(x => x.HandleAsync(It.IsAny<ChangeRankCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChangePosition_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var handler = new Mock<ICommandHandler<ChangePositionCommand>>(MockBehavior.Strict);
        ChangePositionCommand? captured = null;

        handler.Setup(x => x.HandleAsync(It.IsAny<ChangePositionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangePositionCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var cut = RenderSut(person,
            onReload: EventCallback.Factory.Create(this, () => reloadCount++),
            changePosition: handler);

        var dto = new ChangePositionDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 11),
            PositionSort = 2,
            Position = "Командир відділення",
            Note = "note"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangePositionDrawer>().Instance.OnChangePosition.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.EffectiveDate, captured.EffectiveDate);
        Assert.Equal(dto.PositionSort, captured.PositionSort);
        Assert.Equal(dto.Position, captured.Position);
        Assert.Equal(dto.Note, captured.Note);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        handler.Verify(x => x.HandleAsync(It.IsAny<ChangePositionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChangeBzvp_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var handler = new Mock<ICommandHandler<ChangeBzvpCommand>>(MockBehavior.Strict);
        ChangeBzvpCommand? captured = null;

        handler.Setup(x => x.HandleAsync(It.IsAny<ChangeBzvpCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeBzvpCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var cut = RenderSut(person,
            onReload: EventCallback.Factory.Create(this, () => reloadCount++),
            changeBzvp: handler);

        var dto = new ChangeBzvpDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 12),
            Bzvp = "ВОС 123",
            Note = "n"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangeBzvpDrawer>().Instance.OnChangeBzvp.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.EffectiveDate, captured.EffectiveDate);
        Assert.Equal(dto.Bzvp, captured.Bzvp);
        Assert.Equal(dto.Note, captured.Note);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        handler.Verify(x => x.HandleAsync(It.IsAny<ChangeBzvpCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChangeWeapon_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var handler = new Mock<ICommandHandler<ChangeWeaponCommand>>(MockBehavior.Strict);
        ChangeWeaponCommand? captured = null;

        handler.Setup(x => x.HandleAsync(It.IsAny<ChangeWeaponCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeWeaponCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var cut = RenderSut(person,
            onReload: EventCallback.Factory.Create(this, () => reloadCount++),
            changeWeapon: handler);

        var dto = new ChangeWeaponDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 13),
            Weapon = "АК-74 №12345"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangeWeaponDrawer>().Instance.OnChangeWeapon.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.EffectiveDate, captured.EffectiveDate);
        Assert.Equal(dto.Weapon, captured.Weapon);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        handler.Verify(x => x.HandleAsync(It.IsAny<ChangeWeaponCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnChangeCallsing_should_call_handler_and_invoke_OnReload()
    {
        var person = CreatePerson();

        var handler = new Mock<ICommandHandler<ChangeCallsignCommand>>(MockBehavior.Strict);
        ChangeCallsignCommand? captured = null;

        handler.Setup(x => x.HandleAsync(It.IsAny<ChangeCallsignCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeCallsignCommand, CancellationToken>((cmd, _) => captured = cmd)
            .Returns(Task.CompletedTask);

        var reloadCount = 0;
        var cut = RenderSut(person,
            onReload: EventCallback.Factory.Create(this, () => reloadCount++),
            changeCallsign: handler);

        var dto = new ChangeCallsingDto
        {
            PersonId = person.Id,
            EffectiveDate = new DateOnly(2026, 1, 14),
            Callsign = "Лис"
        };

        await cut.InvokeAsync(() =>
            cut.FindComponent<ChangeCallsingDrawer>().Instance.OnChangeCallsing.InvokeAsync(dto));

        Assert.NotNull(captured);
        Assert.Equal(dto.PersonId, captured!.PersonId);
        Assert.Equal(dto.EffectiveDate, captured.EffectiveDate);
        Assert.Equal(dto.Callsign, captured.Callsign);
        Assert.Equal("system", captured.Author);
        Assert.Equal(DateTimeKind.Utc, captured.NowUtc.Kind);

        Assert.Equal(1, reloadCount);
        handler.Verify(x => x.HandleAsync(It.IsAny<ChangeCallsignCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------
    // helpers
    // -------------------------

    private static Task ClickAsync(IRenderedComponent<CardCurrentTab> cut, AngleSharp.Dom.IElement btn)
        => cut.InvokeAsync(() => btn.Click());

    private IRenderedComponent<CardCurrentTab> RenderSut(
        PersonDetailsDto person,
        EventCallback onReload,
        Mock<IRankCatalog>? rankCatalog = null,
        Mock<ICommandHandler<UpdatePersonalInfoCommand>>? updatePersonal = null,
        Mock<ICommandHandler<ChangeRankCommand>>? changeRank = null,
        Mock<ICommandHandler<ChangePositionCommand>>? changePosition = null,
        Mock<ICommandHandler<ChangeBzvpCommand>>? changeBzvp = null,
        Mock<ICommandHandler<ChangeWeaponCommand>>? changeWeapon = null,
        Mock<ICommandHandler<ChangeCallsignCommand>>? changeCallsign = null)
    {
        Services.AddSingleton(new ToastService());

        if (rankCatalog is null)
        {
            rankCatalog = new Mock<IRankCatalog>(MockBehavior.Loose);
            rankCatalog.Setup(x => x.GetActive()).Returns([]);
        }
        Services.AddSingleton(rankCatalog.Object);

        Services.AddSingleton<IValidator<UpdatePersonalInfoDto>>(new UpdatePersonalInfoDtoValidator());
        Services.AddSingleton<IValidator<ChangeRankDto>>(new ChangeRankDtoValidator());
        Services.AddSingleton<IValidator<ChangePositionDto>>(new ChangePositionDtoValidator());
        Services.AddSingleton<IValidator<ChangeBzvpDto>>(new ChangeBzvpDtoValidator());
        Services.AddSingleton<IValidator<ChangeWeaponDto>>(new ChangeWeaponDtoValidator());
        Services.AddSingleton<IValidator<ChangeCallsingDto>>(new ChangeCallsingDtoValidator());

        Services.AddSingleton((updatePersonal ?? new Mock<ICommandHandler<UpdatePersonalInfoCommand>>(MockBehavior.Loose)).Object);
        Services.AddSingleton((changeRank ?? new Mock<ICommandHandler<ChangeRankCommand>>(MockBehavior.Loose)).Object);
        Services.AddSingleton((changePosition ?? new Mock<ICommandHandler<ChangePositionCommand>>(MockBehavior.Loose)).Object);
        Services.AddSingleton((changeBzvp ?? new Mock<ICommandHandler<ChangeBzvpCommand>>(MockBehavior.Loose)).Object);
        Services.AddSingleton((changeWeapon ?? new Mock<ICommandHandler<ChangeWeaponCommand>>(MockBehavior.Loose)).Object);
        Services.AddSingleton((changeCallsign ?? new Mock<ICommandHandler<ChangeCallsignCommand>>(MockBehavior.Loose)).Object);

        return Render<CardCurrentTab>(ps =>
        {
            ps.Add(p => p.Person, person);
            if (onReload.HasDelegate)
                ps.Add(p => p.OnReload, onReload);
        });
    }

    private static PersonDetailsDto CreatePerson()
        => new(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Rnokpp: "0000000000",
            LastName: "Тест",
            FirstName: "Юзер",
            MiddleName: "Мідл",
            FullName: "Тест Юзер Мідл",
            Callsign: "Позивний",
            Lifecycle: PersonLifecycle.Reserved,
            Bzvp: "ВОС",
            Weapon: "АК",
            EnrollmentKind: null,
            EnrollmentReference: null,
            EnrolledAt: null,
            ExcludedAt: null,
            Rank: "Солдат",
            PositionSort: 1,
            Position: "Стрілець",
            Version: 1,
            UpdatedAtUtc: DateTime.Now
        );

    private static AngleSharp.Dom.IElement FindActionsRoot(IRenderedComponent<CardCurrentTab> cut)
        => cut.Find("div.card.d-grid.gap-2");

    private static AngleSharp.Dom.IElement FindActionsButton(IRenderedComponent<CardCurrentTab> cut, string exactText)
    {
        var root = FindActionsRoot(cut);
        return root.QuerySelectorAll("button")
            .First(b => string.Equals(b.TextContent.Trim(), exactText, StringComparison.Ordinal));
    }
}
