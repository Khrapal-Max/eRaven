//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApproveModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.PlanActions.Modals;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class ApproveModalTests : IDisposable
{
    private readonly TestContext _ctx;

    public ApproveModalTests()
    {
        _ctx = new TestContext();

        _ctx.Services.AddTransient<IValidator<ApprovePlanActionViewModel>, ApprovePlanActionViewModelValidator>();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    private static PlanAction MakePlanAction() => new()
    {
        Id = Guid.NewGuid(),
        PersonId = Guid.NewGuid(),
        EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 10, 0, 0), DateTimeKind.Utc),
        PlanActionName = "R-77/25",
    };

    [Fact]
    public void Initially_Hidden()
    {
        // Arrange
        var cut = _ctx.RenderComponent<ApproveModal>(ps => ps
            .Add(p => p.OnApproved, EventCallback.Factory.Create<ApprovePlanActionViewModel>(this, _ => { }))
        );

        // Act
        var modal = cut.Find("div.modal");

        // Assert: немає класів "show" / "d-block"
        Assert.DoesNotContain("show", modal.ClassList);
        Assert.DoesNotContain("d-block", modal.ClassList);
    }

    [Fact]
    public async Task Open_ShowsModal_And_BindsData()
    {
        // Arrange
        var pa = MakePlanAction();
        var cut = _ctx.RenderComponent<ApproveModal>(ps => ps
            .Add(p => p.OnApproved, EventCallback.Factory.Create<ApprovePlanActionViewModel>(this, _ => { }))
        );

        // Act: важливо — на Dispatcher
        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Assert (чекаємо стабілізації рендера)
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("div.modal");
            Assert.Contains("show", modal.ClassList);
            Assert.Contains("d-block", modal.ClassList);

            var input = cut.Find("#order-input");
            Assert.NotNull(input);
            Assert.Equal(string.Empty, input.GetAttribute("value") ?? string.Empty);
        });
    }

    [Fact]
    public async Task Submit_With_Empty_Order_ShowsValidation_And_DoesNotInvokeCallback()
    {
        // Arrange
        var pa = MakePlanAction();
        bool callbackCalled = false;

        var cut = _ctx.RenderComponent<ApproveModal>(ps => ps
            .Add(p => p.OnApproved, EventCallback.Factory.Create<ApprovePlanActionViewModel>(this, _ => { callbackCalled = true; }))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Act: нічого не вводимо, відразу сабмітимо форму (на Dispatcher)
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert: є повідомлення валідації, колбек НЕ викликано
        cut.WaitForAssertion(() =>
        {
            var messages = cut.FindAll("div.validation-message, .validation-message, .invalid-feedback");
            Assert.True(messages.Any(), "Очікувалося повідомлення валідації для порожнього Order.");
            Assert.False(callbackCalled);
        });
    }

    [Fact]
    public async Task Submit_With_Valid_Order_Invokes_OnApproved_With_Trimmed_Value()
    {
        // Arrange
        var pa = MakePlanAction();
        ApprovePlanActionViewModel? received = null;

        var cut = _ctx.RenderComponent<ApproveModal>(ps => ps
            .Add(p => p.OnApproved, EventCallback.Factory.Create<ApprovePlanActionViewModel>(this, vm => received = vm))
        );

        await cut.InvokeAsync(() => cut.Instance.Open(pa));

        // Ввід значення — через Dispatcher
        var input = cut.Find("#order-input");
        await cut.InvokeAsync(() => input.Change("   БР-99/25   "));

        // Act: сабміт — через Dispatcher
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(received);
            Assert.Equal(pa.Id, received!.Id);
            Assert.Equal(pa.PersonId, received.PersonId);
            Assert.Equal(pa.EffectiveAtUtc, received.EffectiveAtUtc);
            Assert.Equal("БР-99/25", received.Order); // обрізане значення
        });
    }
}