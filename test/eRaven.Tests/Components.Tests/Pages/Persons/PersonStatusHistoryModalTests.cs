//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusHistoryModalTests
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using Bunit;
using eRaven.Application.Services.PersonStatusReadService;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PersonStatusHistoryModalTests : TestContext
{
    private readonly Mock<IPersonStatusReadService> _readService = new();

    public PersonStatusHistoryModalTests()
    {
        Services.AddSingleton(_readService.Object);
        _readService
            .Setup(s => s.ResolveNotPresentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatusKind?)null);
    }

    // ----------------- Helpers -----------------

    private static Person NewPerson()
        => new()
        {
            Id = Guid.NewGuid(),
            LastName = "Іваненко",
            FirstName = "Іван",
            MiddleName = "Іванович",
            Rnokpp = "1111111111",
            Rank = "сержант"
        };

    private static PersonStatus S(Guid personId, string name, string? note, DateTime utc, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = 1,
            StatusKind = new StatusKind { Id = 1, Name = name, IsActive = true },
            OpenDate = utc,
            Note = note,
            IsActive = isActive,
            Sequence = 0,
            Author = "test",
            Modified = DateTime.UtcNow
        };

    private IRenderedComponent<PersonStatusHistoryModal> Render(
        bool open,
        Person? person,
        IReadOnlyList<PersonStatus>? history = null,
        DateTime? firstPresenceUtc = null,
        StatusKind? notPresentKind = null)
    {
        _readService.Reset();

        _readService
            .Setup(s => s.ResolveNotPresentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(notPresentKind);

        if (person is not null)
        {
            _readService
                .Setup(s => s.OrderForHistoryAsync(person.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(history ?? []);
            _readService
                .Setup(s => s.GetFirstPresenceUtcAsync(person.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(firstPresenceUtc);
        }

        return RenderComponent<PersonStatusHistoryModal>(ps =>
        {
            ps.Add(p => p.Open, open);
            ps.Add(p => p.Person, person);
            ps.Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }));
        });
    }

    // ----------------- Tests -----------------

    [Fact(DisplayName = "Modal: при Open=false не рендериться")]
    public void NotOpen_RendersNothing()
    {
        var p = NewPerson();
        var cut = Render(open: false, person: p, history: []);
        Assert.DoesNotContain("Історія статусів", cut.Markup);
        Assert.DoesNotContain("modal-content", cut.Markup);
    }

    [Fact(DisplayName = "Modal: заголовок містить ПІБ особи")]
    public void Header_ShowsFullName()
    {
        var p = NewPerson();
        p.LastName = "Петров";
        p.FirstName = "Петро";
        p.MiddleName = "Петрович";

        var cut = Render(open: true, person: p, history: []);
        Assert.Contains("Історія статусів: Петров Петро Петрович", cut.Markup);
    }

    [Fact(DisplayName = "Modal: порожня історія → 'Історія порожня.'")]
    public void EmptyHistory_ShowsEmptyMessage()
    {
        var p = NewPerson();
        var cut = Render(open: true, person: p, history: []);
        Assert.Contains("Історія порожня.", cut.Markup);
    }

    [Fact(DisplayName = "Modal: формат дати — dd.MM.yyyy (локальний час)")]
    public void Date_Format_Is_ddMMyyyy()
    {
        var p = NewPerson();
        var utc = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        var list = new[] { S(p.Id, "Статус", null, utc, isActive: true) };

        var cut = Render(open: true, person: p, history: list);

        // Перевіряємо шаблон дати (без годин)
        Assert.Contains(utc.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);
    }

    [Fact(DisplayName = "Modal: до першої появи показує рядок 'нб'")]
    public void BeforeFirstPresence_ShowsNotPresentRow()
    {
        var p = NewPerson();
        var firstPresence = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var notPresentKind = new StatusKind { Id = 2, Code = "нб", Name = "Не був" };
        var history = new[] { S(p.Id, "В районі", null, firstPresence.AddDays(1)) };

        var cut = Render(open: true, person: p, history: history, firstPresenceUtc: firstPresence, notPresentKind: notPresentKind);

        Assert.Contains("нб", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"до {firstPresence.ToLocalTime():dd.MM.yyyy}", cut.Markup);
    }

    [Fact(DisplayName = "Modal: кнопка 'Закрити' викликає OnClose")]
    public void Close_Button_Fires_OnClose()
    {
        var p = NewPerson();
        var closed = false;

        _readService
            .Setup(s => s.OrderForHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _readService
            .Setup(s => s.GetFirstPresenceUtcAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        var cut = RenderComponent<PersonStatusHistoryModal>(ps =>
        {
            ps.Add(pz => pz.Open, true);
            ps.Add(pz => pz.Person, p);
            ps.Add(pz => pz.OnClose, EventCallback.Factory.Create(this, () => closed = true));
        });

        cut.Find("button.btn.btn-success.btn-sm").Click();
        Assert.True(closed);
    }

    [Fact(DisplayName = "Modal: повторне відкриття перевантажує історію")]
    public void Reopen_Reloads_History()
    {
        var p = NewPerson();

        _readService
            .Setup(s => s.OrderForHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _readService
            .Setup(s => s.GetFirstPresenceUtcAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        var cut = RenderComponent<PersonStatusHistoryModal>(ps =>
        {
            ps.Add(pz => pz.Open, false);
            ps.Add(pz => pz.Person, p);
        });

        // open=true -> має повторно сходити по історію
        cut.SetParametersAndRender(ps => ps.Add(pz => pz.Open, true));

        _readService.Verify(s => s.OrderForHistoryAsync(p.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
