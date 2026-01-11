//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateDrawerTests
//-----------------------------------------------------------------------------

using AngleSharp.Dom;
using Bunit;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Components.Pages.Persons.Registry.Drawer;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class CreateCandidateDrawerTests : BunitContext
{
    private readonly ToastService _toasts = new();

    private readonly Mock<IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>>> _vacantQuery
        = new(MockBehavior.Strict);

    public CreateCandidateDrawerTests()
    {
        Services.AddSingleton(_toasts);
        Services.AddSingleton(_vacantQuery.Object);
    }

    // -----------------------------
    // helpers
    // -----------------------------

    private static IReadOnlyList<IElement> GetPersonalInputs(IRenderedComponent<CreateCandidateDrawer> cut)
        // беремо всі input.form-control, але відсікаємо picker-search (він має placeholder)
        => [.. cut.FindAll("input.form-control").Where(i => !i.HasAttribute("placeholder"))];

    private static async Task SetIsOpenAsync(IRenderedComponent<CreateCandidateDrawer> cut, bool value)
        => await cut.InvokeAsync(() =>
        cut.Render(ps => ps.Add(p => p.IsOpen, value)));

    private static EventCallback<bool> ECBool(object receiver, Action<bool> cb)
        => EventCallback.Factory.Create<bool>(receiver, cb);

    private static EventCallback<CreateCandidateDto> ECCreate(object receiver, Action<CreateCandidateDto> cb)
        => EventCallback.Factory.Create<CreateCandidateDto>(receiver, cb);

    // -----------------------------
    // tests
    // -----------------------------

    [Fact]
    public async Task Open_should_load_positions_and_reset_form_each_time_drawer_opens()
    {
        // arrange
        var calls = 0;

        _vacantQuery
            .Setup(x => x.HandleAsync(
                It.IsAny<GetVacantPositionUnitsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                calls++;
                return (IReadOnlyList<PositionUnitOptionDto>)
                [
                    new(Guid.NewGuid(), "BBS-001", "Short 1", "Full 1", "R1", "T1"),
                    new(Guid.NewGuid(), "BBS-002", "Short 2", "Full 2", "R2", "T2"),
                ];
            });

        var isOpen = false;

        var cut = Render<CreateCandidateDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, ECBool(this, v => isOpen = v))
            .Add(p => p.OnCreate, ECCreate(this, _ => { }))
        );

        // act #1: open
        await SetIsOpenAsync(cut, true);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, calls);
            Assert.NotNull(cut.Find("form.drawer-form"));
        });

        // fill inputs (щоб потім перевірити reset)
        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            Assert.True(inputs.Count >= 3);

            inputs[0].Change("1234567890"); // RNOKPP
        });

        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            inputs[1].Change("Ivanov");     // LastName
        });

        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            inputs[2].Change("Ivan");       // FirstName
        });

        // close (parent-driven)
        await SetIsOpenAsync(cut, false);

        // open again
        await SetIsOpenAsync(cut, true);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, calls);
            Assert.NotNull(cut.Find("form.drawer-form"));
        });

        // assert: inputs reset
        cut.WaitForAssertion(() =>
        {
            var inputs = GetPersonalInputs(cut);

            // після reset вони мають бути пусті
            Assert.Equal(string.Empty, inputs[0].GetAttribute("value") ?? string.Empty);
            Assert.Equal(string.Empty, inputs[1].GetAttribute("value") ?? string.Empty);
            Assert.Equal(string.Empty, inputs[2].GetAttribute("value") ?? string.Empty);
        });

        // and query called with Take=200
        _vacantQuery.Verify(x => x.HandleAsync(
                It.Is<GetVacantPositionUnitsQuery>(q => q.Take == 200),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Create_should_invoke_OnCreate_show_success_toast_and_request_close()
    {
        // arrange
        _vacantQuery
            .Setup(x => x.HandleAsync(It.IsAny<GetVacantPositionUnitsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PositionUnitOptionDto>)
            [
                new(Guid.NewGuid(), "BBS-010", "Short", "Full name", "R", "T")
            ]);

        ToastMessage? lastToast = null;
        _toasts.OnShow += msg => lastToast = msg;

        CreateCandidateDto? created = null;
        var isOpen = true;

        var cut = Render<CreateCandidateDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, ECBool(this, v => isOpen = v))
            .Add(p => p.OnCreate, ECCreate(this, dto => created = dto))
        );

        // дочекайся, що форма з’явилась (після async ResetFormAsync буде re-render)
        cut.WaitForAssertion(() => cut.Find("form.drawer-form"));

        // fill required inputs (ВАЖЛИВО: RNOKPP без пробілів, бо regex ^\d{10}$)
        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            inputs[0].Change("1234567890");
        });

        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            inputs[1].Change("Ivanov");
        });

        await cut.InvokeAsync(() =>
        {
            var inputs = GetPersonalInputs(cut);
            inputs[2].Change("Ivan");
        });

        // click "Створити"
        await cut.InvokeAsync(() =>
        {
            cut.Find(".drawer-footer .btn-primary").Click();
        });

        // assert
        cut.WaitForAssertion(() => Assert.NotNull(created));
        cut.WaitForAssertion(() => Assert.False(isOpen));

        Assert.NotNull(lastToast);
        Assert.Equal(ToastKind.Success, lastToast!.Kind);

        Assert.Equal("1234567890", created!.Rnokpp);
        Assert.Equal("Ivanov", created.LastName);
        Assert.Equal("Ivan", created.FirstName);
    }

    [Fact]
    public async Task Backdrop_click_should_request_close()
    {
        // arrange
        _vacantQuery
            .Setup(x => x.HandleAsync(It.IsAny<GetVacantPositionUnitsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PositionUnitOptionDto>)
            [
                new(Guid.NewGuid(), "BBS-001", "S", "F", "R", "T")
            ]);

        var isOpen = true;

        var cut = Render<CreateCandidateDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, ECBool(this, v => isOpen = v))
            .Add(p => p.OnCreate, ECCreate(this, _ => { }))
        );

        cut.WaitForAssertion(() => cut.Find("form.drawer-form"));

        // act
        await cut.InvokeAsync(() => cut.Find(".drawer-backdrop").Click());

        // assert
        Assert.False(isOpen);
    }
}

