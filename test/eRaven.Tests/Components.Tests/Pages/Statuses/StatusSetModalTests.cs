//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusSetModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Statuses.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Tests.Components.Tests.Pages.Statuses;

public sealed class StatusSetModalTests : TestContext
{
    private static Person P(string? rnokpp = "1234567890") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Іван",
        LastName = "Іваненко",
        MiddleName = "Іванович",
        Callsign = "Мисливець",
        Rank = "Сержант",
        Rnokpp = rnokpp!,
        PositionUnit = new PositionUnit { ShortName = "Взвод Звʼязку" }
    };

    private static StatusKind K(int id, string name, string? code = null) => new()
    {
        Id = id,
        Name = name,
        Code = code ?? $"CODE{id}"
    };

    [Fact(DisplayName = "Modal: коли Open=false — нічого не рендериться")]
    public void DoesNotRender_WhenClosed()
    {
        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, false)
        );

        Assert.Empty(cut.FindAll("div.modal"));
    }

    [Fact(DisplayName = "Modal: коли Open=true — заголовок і коротка інформація про особу відображаються")]
    public void RendersHeaderAndPersonInfo_WhenOpen()
    {
        var person = P();
        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
        );

        // Заголовок
        cut.Markup.Contains("Встановлення статусу");
        // ПІБ та РНОКПП
        Assert.Contains(person.FullName, cut.Markup);
        Assert.Contains(person.Rnokpp!, cut.Markup);
        // Підрозділ
        Assert.Contains(person.PositionUnit!.ShortName!, cut.Markup);
    }

    [Fact(DisplayName = "Modal: селект статусів — disabled якщо немає дозволених або Busy=true")]
    public void StatusSelect_Disabled_WithoutOptions_Or_WhenBusy()
    {
        var person = P();

        // 1) Немає AllowedNextStatuses
        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
            .Add(x => x.AllowedNextStatuses, [])
        );
        var select = cut.Find("select.form-select");
        Assert.True(select.HasAttribute("disabled"));

        // 2) Є опції, але Busy=true
        cut.SetParametersAndRender(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
            .Add(x => x.Busy, true)
            .Add(x => x.AllowedNextStatuses, [K(1, "В районі", "30")])
        );
        select = cut.Find("select.form-select");
        Assert.True(select.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Modal: кнопка 'Підтвердити' активується лише коли вибрано статус та дату")]
    public void Submit_Enabled_Only_WhenStatus_And_Date_Selected()
    {
        var person = P();
        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
            .Add(x => x.AllowedNextStatuses, [K(7, "На завданні", "TASK")])
        );

        var submit = cut.Find("button[type=submit]");
        Assert.True(submit.HasAttribute("disabled")); // старт: вимкнено

        // Обрати статус
        var select = cut.Find("select.form-select");
        select.Change(7);
        submit = cut.Find("button[type=submit]");
        Assert.True(submit.HasAttribute("disabled")); // все ще вимкнено (нема дати)

        // Встановити дату
        var date = cut.Find("input[type=date]");
        date.Change("2025-09-01");

        submit = cut.Find("button[type=submit]");
        Assert.False(submit.HasAttribute("disabled")); // тепер ок
    }

    [Fact(DisplayName = "Modal: OnSubmit викликається з коректною VM (PersonId, StatusId, Moment=00:00 Unspecified)")]
    public void OnSubmit_Fires_With_CorrectViewModel()
    {
        var person = P();
        SetPersonStatusViewModel? captured = null;

        var cb = EventCallback.Factory.Create(new object(), (SetPersonStatusViewModel vm) => captured = vm);

        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
            .Add(x => x.AllowedNextStatuses, [K(5, "Відрядження", "DUTY")])
            .Add(x => x.OnSubmit, cb)
        );

        // Обрати статус
        cut.Find("select.form-select").Change(5);
        // Встановити дату
        cut.Find("input[type=date]").Change("2025-09-02");

        // Сабміт
        cut.Find("button[type=submit]").Click();

        Assert.NotNull(captured);
        Assert.Equal(person.Id, captured!.PersonId);
        Assert.Equal(5, captured.StatusId);
        Assert.Equal(DateTimeKind.Unspecified, captured.Moment.Kind);
        Assert.Equal(new DateTime(2025, 9, 2, 0, 0, 0), captured.Moment);
    }

    [Fact(DisplayName = "Modal: OnClose викликається при натисканні 'Скасувати'")]
    public void OnClose_Fires_WhenCancel()
    {
        var person = P();
        var closed = false;

        var cb = EventCallback.Factory.Create(new object(), () => closed = true);

        var cut = RenderComponent<StatusSetModal>(p => p
            .Add(x => x.Open, true)
            .Add(x => x.PersonCard, person)
            .Add(x => x.OnClose, cb)
        );

        cut.Find("button.btn-outline-secondary").Click();
        Assert.True(closed);
    }
}
