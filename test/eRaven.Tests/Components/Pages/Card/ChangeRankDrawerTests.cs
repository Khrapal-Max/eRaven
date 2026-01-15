//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeRankDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Card.Drawwers;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Card;

public sealed class ChangeRankDrawerTests : BunitContext
{
    // Мінімальний валідатор для UI-моделі, щоб OnValidSubmit працював прогнозовано.
    private sealed class ChangeRankDtoValidator : AbstractValidator<ChangeRankDto>
    {
        public ChangeRankDtoValidator()
        {
            RuleFor(x => x.Rank).NotEmpty();
        }
    }

    private static PersonDetailsDto Person(Guid id, string? rank = "Солдат") => new(
        Id: id,
        Lifecycle: eRaven.Domain.Enums.PersonLifecycle.Enrolled,
        EnrollmentKind: eRaven.Domain.Enums.EnrollmentKind.Unit,
        EnrollmentReference: "A",
        Rnokpp: "1234567890",
        LastName: "Іванов",
        FirstName: "Іван",
        MiddleName: null,
        FullName: "Іванов Іван",
        Rank: rank,
        PositionSort: 10,
        Position: "Оператор",
        Bzvp: null,
        Weapon: null,
        Callsign: null,
        EnrolledAt: new DateOnly(2026, 01, 10),
        ExcludedAt: null,
        Version: 1,
        UpdatedAtUtc: new DateTime(2026, 01, 10, 0, 0, 0, DateTimeKind.Utc));

    private void RegisterCommon(Mock<IRankCatalog> rankCatalog, ToastService? toasts = null)
    {
        Services.AddSingleton(rankCatalog.Object);
        Services.AddSingleton(toasts ?? new ToastService());
        Services.AddSingleton<IValidator<ChangeRankDto>>(new ChangeRankDtoValidator());
    }

    [Fact]
    public void When_open_should_load_ranks_and_render_form()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([
                new RankOption("солдат"),
                new RankOption("сержант")
            ]);

        RegisterCommon(rankCatalog);

        var id = Guid.NewGuid();

        // act
        var cut = Render<ChangeRankDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnChangeRank, _ => Task.CompletedTask)
        );

        // assert
        rankCatalog.Verify(x => x.GetActive(), Times.Once);
        cut.Find("form#change-rank-form");

        var menuHtml = cut.Find("ul.dropdown-menu").OuterHtml;
        Assert.Contains("солдат", menuHtml);
        Assert.Contains("сержант", menuHtml);

        var submit = cut.Find("button[type='submit']");
        Assert.Equal("change-rank-form", submit.GetAttribute("form"));
        Assert.Contains("Змінити", submit.TextContent);
    }

    [Fact]
    public async Task Cancel_should_close_drawer_and_not_invoke_OnChangeRank()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([new RankOption("солдат")]);

        RegisterCommon(rankCatalog);

        var id = Guid.NewGuid();
        var isOpen = true;
        var called = false;

        var cut = Render<ChangeRankDrawer>(ps => ps
            .Add(p => p.Person, Person(id))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeRank, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        var cancel = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        Assert.False(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnChangeRank_trim_note_show_success_toast_and_close()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([
                new RankOption("солдат"),
                new RankOption("сержант")
            ]);

        var toasts = new ToastService();
        var toastMessages = new List<ToastMessage>();
        toasts.OnShow += msg => toastMessages.Add(msg);

        RegisterCommon(rankCatalog, toasts);

        var id = Guid.NewGuid();
        var isOpen = true;
        ChangeRankDto? captured = null;

        var cut = Render<ChangeRankDrawer>(ps => ps
            .Add(p => p.Person, Person(id, rank: "")) // пусто -> мусимо вибрати зі списку
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeRank, dto => { captured = dto; return Task.CompletedTask; })
        );

        // act: обрати звання
        await cut.InvokeAsync(() =>
        {
            var items = cut.FindAll(".dropdown-menu .dropdown-item");
            items.Single(x => x.TextContent.Trim() == "сержант").Click();
        });

        // дата
        await cut.InvokeAsync(() => cut.Find("input[type='date']").Change("2026-01-22"));

        // note (trim)
        await cut.InvokeAsync(() => cut.Find("input[name='Model.Note']").Change("  test note  "));

        // submit
        await cut.InvokeAsync(() => cut.Find("form#change-rank-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal(id, captured!.PersonId);
        Assert.Equal(new DateOnly(2026, 01, 22), captured.EffectiveDate);
        Assert.Equal("сержант", captured.Rank);
        Assert.Equal("test note", captured.Note);

        Assert.False(isOpen);

        Assert.Contains(toastMessages, m =>
            m.Kind == ToastKind.Success &&
            m.Title.Contains("Звання змінено", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_invalid_form_should_not_invoke_OnChangeRank_and_should_not_close()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([new RankOption("солдат")]);

        RegisterCommon(rankCatalog);

        var id = Guid.NewGuid();
        var isOpen = true;
        var called = false;

        var cut = Render<ChangeRankDrawer>(ps => ps
            .Add(p => p.Person, Person(id, rank: "")) // не вибираємо -> invalid
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeRank, _ => { called = true; return Task.CompletedTask; })
        );

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-rank-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.False(called);
    }

    [Fact]
    public async Task When_OnChangeRank_throws_InvalidOperationException_should_show_warning_and_keep_open()
    {
        // arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns([new RankOption("солдат")]);

        var toasts = new ToastService();
        var toastMessages = new List<ToastMessage>();
        toasts.OnShow += msg => toastMessages.Add(msg);

        RegisterCommon(rankCatalog, toasts);

        var id = Guid.NewGuid();
        var isOpen = true;

        var cut = Render<ChangeRankDrawer>(ps => ps
            .Add(p => p.Person, Person(id, rank: ""))
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnChangeRank, _ => throw new InvalidOperationException("boom"))
        );

        // робимо форму валідною
        await cut.InvokeAsync(() =>
        {
            cut.FindAll(".dropdown-menu .dropdown-item")
                .Single(x => x.TextContent.Trim() == "солдат")
                .Click();
        });

        // act
        await cut.InvokeAsync(() => cut.Find("form#change-rank-form").Submit());

        // assert
        Assert.True(isOpen);
        Assert.Contains(toastMessages, m =>
            m.Kind == ToastKind.Warning &&
            m.Title.Contains("Неможливо виконати дію", StringComparison.Ordinal));
    }
}
