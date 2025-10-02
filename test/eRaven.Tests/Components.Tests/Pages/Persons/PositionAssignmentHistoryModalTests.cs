//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentHistoryModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Persons.Modals;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PositionAssignmentHistoryModalTests : TestContext
{
    private readonly Mock<IPositionAssignmentService> _svc = new();

    public PositionAssignmentHistoryModalTests()
    {
        Services.AddSingleton(_svc.Object);
    }

    // ---------- helpers ----------

    private static Person MkPerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Петренко",
        FirstName = "Петро",
        MiddleName = "Петрович",
        Rank = "солдат",
        Rnokpp = "1234567890"
    };

    private static PositionUnit MkUnit(string code, string shortName, string full, string? vos = "111") =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            ShortName = shortName,
            OrgPath = full,
            SpecialNumber = vos,
            IsActived = true
        };

    private static PersonPositionAssignment MkAssign(Guid pid, PositionUnit u, DateTime openUtc, DateTime? closeUtc, string? note = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = pid,
            PositionUnitId = u.Id,
            PositionUnit = u,
            OpenUtc = DateTime.SpecifyKind(openUtc, DateTimeKind.Utc),
            CloseUtc = closeUtc is null ? null : DateTime.SpecifyKind(closeUtc.Value, DateTimeKind.Utc),
            Note = note,
            Author = "ui",
            ModifiedUtc = DateTime.UtcNow
        };

    // ---------- tests ----------

    [Fact(DisplayName = "Open=true: тягне історію та рендерить рядки, активний має бейдж 'активно'")]
    public void Open_Loads_And_Renders_Rows_With_Active_Badge()
    {
        var p = MkPerson();
        var u1 = MkUnit("INF-001", "Стрілець", "Рота/Взвода/Відділення");
        var u2 = MkUnit("MED-002", "Медик", "Мед. пункт");
        var open1 = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);
        var close1 = new DateTime(2025, 09, 15, 0, 0, 0, DateTimeKind.Utc);

        // закритий запис + активний
        var a1 = MkAssign(p.Id, u1, open1, close1, "планова ротація");
        var a2 = MkAssign(p.Id, u2, open1.AddDays(16), null, "поточне");

        var list = new List<PersonPositionAssignment> { a2, a1 }; // порядок неважливий, табличка покаже всі

        _svc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var cut = RenderComponent<PositionAssignmentHistoryModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Person, p)
        );

        // є заголовок
        Assert.Contains("Історія посад", cut.Markup);

        // дати у форматі dd.MM.yyyy (локальний час)
        Assert.Contains(a1.OpenUtc.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);
        Assert.Contains(a1.CloseUtc!.Value.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);
        Assert.Contains(a2.OpenUtc.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);

        // активний показує бейдж
        Assert.Contains("активно", cut.Markup, StringComparison.OrdinalIgnoreCase);

        // поля посади
        Assert.Contains(u1.Code, cut.Markup);
        Assert.Contains(u1.ShortName, cut.Markup);
        Assert.Contains(u1.OrgPath, cut.Markup);
        Assert.Contains(u1.SpecialNumber!, cut.Markup);

        Assert.Contains(u2.Code, cut.Markup);
        Assert.Contains(u2.ShortName, cut.Markup);
        Assert.Contains(u2.OrgPath, cut.Markup);
        Assert.Contains(u2.SpecialNumber!, cut.Markup);

        // нотатки
        Assert.Contains("планова ротація", cut.Markup);
        Assert.Contains("поточне", cut.Markup);
    }

    [Fact(DisplayName = "Порожня історія: показує 'Історія призначень порожня.'")]
    public void Empty_Shows_Empty_Message()
    {
        var p = MkPerson();
        _svc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonPositionAssignment>());

        var cut = RenderComponent<PositionAssignmentHistoryModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Person, p)
        );

        Assert.Contains("Історія призначень порожня", cut.Markup);
    }

    [Fact(DisplayName = "Під час завантаження показує 'Завантаження…', потім дані")]
    public void Shows_Loading_Then_Data()
    {
        var p = MkPerson();
        var tcs = new TaskCompletionSource<IReadOnlyList<PersonPositionAssignment>>();

        _svc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var cut = RenderComponent<PositionAssignmentHistoryModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Person, p)
        );

        // поки TCS не завершений — бачимо "Завантаження…"
        Assert.Contains("Завантаження…", cut.Markup);

        // тепер віддамо результат і дочекаємось перерендеру
        var u = MkUnit("SIG-003", "Зв’язківець", "Штаб");
        var a = MkAssign(p.Id, u, new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc), null, null);
        tcs.SetResult(new List<PersonPositionAssignment> { a });

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Завантаження…", cut.Markup);
            Assert.Contains(u.Code!, cut.Markup);
        });
    }

    [Fact(DisplayName = "Кнопка 'Закрити' викликає OnClose")]
    public void Close_Button_Raises_OnClose()
    {
        var p = MkPerson();
        _svc.Setup(s => s.GetHistoryAsync(p.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonPositionAssignment>());

        var closed = false;
        var cut = RenderComponent<PositionAssignmentHistoryModal>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Person, p)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closed = true))
        );

        // кнопка у футері (зелена)
        var closeBtn = cut.Find("button.btn.btn-success.btn-sm");
        closeBtn.Click();

        Assert.True(closed);
    }
}
