//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonalEventDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs;
using eRaven.Application.Validations.Personal;
using eRaven.Components.Pages.Persons.Cards.Drawers;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace eRaven.Tests.Components.Pages.Cards.Drawers;

public sealed class VoidPersonalEventDrawerTests : BunitContext
{
    public VoidPersonalEventDrawerTests()
    {
        // Мінімально необхідні сервіси для Drawer (Toasts) і FluentValidationValidator
        Services.AddSingleton(new ToastService());
        Services.AddSingleton<IValidator<VoidPersonEventDto>>(new VoidPersonEventDtoValidator());
    }

    [Fact]
    public void Render_open_should_show_target_info_title_details_and_form()
    {
        // arrange
        var personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var cut = Render<VoidPersonalEventDrawer>(ps =>
        {
            ps.Add(p => p.PersonId, personId);
            ps.Add(p => p.TargetEventId, targetId);
            ps.Add(p => p.TargetTitle, "Зміна звання");
            ps.Add(p => p.TargetDetails, "На: сержант. Наказ 240.");
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }));
            ps.Add(p => p.OnVoidEvent, EventCallback.Factory.Create<VoidPersonEventDto>(this, _ => { }));
        });

        // assert (basic content visible)
        Assert.Contains("Відміна події (Void)", cut.Markup);
        Assert.Contains(targetId.ToString(), cut.Markup);
        Assert.Contains("Зміна звання", cut.Markup);
        Assert.Contains("На: сержант. Наказ 240.", cut.Markup);

        // form + submit button exist
        cut.Find("form#void-event-form");
        cut.Find("button[type='submit'][form='void-event-form']");
    }

    [Fact]
    public void Submit_button_should_be_disabled_when_TargetEventId_is_null()
    {
        // arrange
        var cut = Render<VoidPersonalEventDrawer>(ps =>
        {
            ps.Add(p => p.PersonId, Guid.NewGuid());
            ps.Add(p => p.TargetEventId, (Guid?)null);
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }));
        });

        // assert
        var submit = cut.Find("button[type='submit'][form='void-event-form']");
        Assert.True(submit.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Cancel_should_invoke_IsOpenChanged_false()
    {
        // arrange
        bool? closed = null;

        var cut = Render<VoidPersonalEventDrawer>(ps =>
        {
            ps.Add(p => p.PersonId, Guid.NewGuid());
            ps.Add(p => p.TargetEventId, Guid.NewGuid());
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => closed = v));
        });

        // act
        // наша кастомна Button рендерить <button ...> з текстом Label
        var cancel = cut.FindAll("button").First(b => b.TextContent.Trim() == "Скасувати");
        cancel.Click();

        // assert
        await Task.Yield(); // дати циклу синхронізації Blazor відпрацювати
        Assert.Equal(false, closed);
    }

    [Fact]
    public async Task Valid_submit_should_invoke_OnVoidEvent_and_close()
    {
        // arrange
        var personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        VoidPersonEventDto? captured = null;
        bool? closed = null;

        var cut = Render<VoidPersonalEventDrawer>(ps =>
        {
            ps.Add(p => p.PersonId, personId);
            ps.Add(p => p.TargetEventId, targetId);
            ps.Add(p => p.IsOpen, true);
            ps.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, v => closed = v));
            ps.Add(p => p.OnVoidEvent, EventCallback.Factory.Create<VoidPersonEventDto>(this, dto => captured = dto));
        });

        // act: вводимо reason
        cut.Find("input.form-control").Change("   помилка в посаді №240   ");

        // submit
        await cut.InvokeAsync(() => cut.Find("form#void-event-form").Submit());

        // assert
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(captured);

            // TargetEventId має бути встановлений з параметра (ResetModel)
            Assert.Equal(targetId, captured!.TargetEventId);

            // Normalize() тримає trim
            Assert.Equal("помилка в посаді №240", captured.Reason);

            // PersonId теж має бути встановлений з параметра (ResetModel)
            Assert.Equal(personId, captured.PersonId);

            // drawer має закритися
            Assert.Equal(false, closed);
        });
    }
}
