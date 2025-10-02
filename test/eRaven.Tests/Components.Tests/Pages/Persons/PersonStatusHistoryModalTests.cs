//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusHistoryModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PersonStatusHistoryModalTests : TestContext
{
    private readonly Mock<IPersonStatusService> _statuses = new();

    public PersonStatusHistoryModalTests()
    {
        Services.AddSingleton(_statuses.Object);
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

    private IRenderedComponent<PersonStatusHistoryModal> Render(bool open, Person? person, IReadOnlyList<PersonStatus>? history = null)
    {
        _statuses.Reset();

        if (person is not null)
        {
            _statuses
                .Setup(s => s.GetHistoryAsync(person.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(history ?? []);
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

    [Fact(DisplayName = "Modal: кнопка 'Закрити' викликає OnClose")]
    public void Close_Button_Fires_OnClose()
    {
        var p = NewPerson();
        var closed = false;

        _statuses
            .Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

        _statuses
            .Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var cut = RenderComponent<PersonStatusHistoryModal>(ps =>
        {
            ps.Add(pz => pz.Open, false);
            ps.Add(pz => pz.Person, p);
        });

        // open=true -> має повторно сходити по історію
        cut.SetParametersAndRender(ps => ps.Add(pz => pz.Open, true));

        _statuses.Verify(s => s.GetHistoryAsync(p.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
