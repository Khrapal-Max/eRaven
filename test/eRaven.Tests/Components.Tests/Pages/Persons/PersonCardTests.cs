//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonCardTests (без верифікації Toast; історія фільтрує IsActive=false)
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons;
using eRaven.Domain.Models;
using eRaven.Domain.Person;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PersonCardTests : TestContext
{
    private readonly Mock<IPersonService> _persons = new();
    private readonly Mock<IPersonStatusService> _statuses = new();
    private readonly Mock<IPositionAssignmentService> _positionAssignment = new();
    private readonly Mock<IToastService> _toast = new(MockBehavior.Loose);

    private static int _kindSeed = 100;

    public PersonCardTests()
    {
        // DI
        Services.AddSingleton(_persons.Object);
        Services.AddSingleton(_statuses.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_positionAssignment.Object);
        // FluentValidation для EditForm
        Services.AddScoped<IValidator<EditPersonViewModel>, EditPersonViewModelValidator>();
    }

    // ---------- helpers ----------

    private static Person P(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        LastName = "Петренко",
        FirstName = "Іван",
        MiddleName = "Іванович",
        Rnokpp = "1234567890",
        Rank = "сержант",
        BZVP = "пройшов",
        Weapon = "АК-74 №123",
        Callsign = "Сокіл",
        PositionUnitId = null,
        StatusKindId = 0
    };

    private static StatusKind K(string name) => new()
    {
        Id = _kindSeed++,
        Code = name,
        Name = name,
        IsActive = true,
        Author = "test",
        Modified = DateTime.UtcNow
    };

    private static PersonStatus S(Guid personId, string kindName, string? note, DateTime openUtc, bool isActive, short seq = 0)
    {
        var kind = K(kindName);
        return new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = kind.Id,
            StatusKind = kind,
            OpenDate = openUtc,
            Sequence = seq,
            Note = note,
            IsActive = isActive,
            Author = "test",
            Modified = DateTime.UtcNow
        };
    }

    private IRenderedComponent<PersonCard> Render(Guid id, Person? person, PersonStatus? active = null, IReadOnlyList<PersonStatus>? history = null)
    {
        _persons.Reset();
        _statuses.Reset();
        _toast.Reset();

        _persons.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

        _persons.Setup(s => s.UpdateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        _statuses.Setup(s => s.GetActiveAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(active);

        _statuses.Setup(s => s.GetHistoryAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(history ?? []);

        return RenderComponent<PersonCard>(ps => ps.Add(p => p.Id, id));
    }

    // =============== TESTS ===============

    [Fact(DisplayName = "Init: відображає дані картки після завантаження")]
    public void Init_Renders_Person_Data()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var cut = Render(id, person);

        cut.Markup.Contains(person.FullName);
        cut.Markup.Contains(person.Rank);
        cut.Markup.Contains(person.Rnokpp);
        cut.Markup.Contains(person.BZVP);
        cut.Markup.Contains(person.Callsign!);
    }

    [Fact(DisplayName = "BeginEdit: перемикає в режим редагування і показує форму")]
    public void BeginEdit_Shows_EditForm()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var cut = Render(id, person);

        cut.Find("button.btn-warning").Click(); // «Редагувати»

        Assert.NotNull(cut.Find("form#person-edit-form"));
        Assert.Contains("РНОКПП", cut.Markup);
    }

    [Fact(DisplayName = "CancelEdit: повертає у режим перегляду")]
    public void CancelEdit_Returns_ViewMode()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var cut = Render(id, person);
        cut.Find("button.btn-warning").Click();      // Edit
        cut.Find("button.btn-warning").Click(); // Cancel

        Assert.DoesNotContain("form#person-edit-form", cut.Markup);
    }

    [Fact(DisplayName = "Back: кнопка 'Назад' навігує на /persons")]
    public void Back_Navigates_To_List()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = Render(id, person);

        cut.Find("button.btn-success").Click(); // «Назад»
        Assert.EndsWith("/persons", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Active status: показує назву/примітку; історія рахує лише IsActive=true")]
    public void ActiveStatus_Shown_History_Counts_Only_Active()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        // Активний X @ 00:00 з нотаткою
        var sx = S(id, "X", "Нотатка X", new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc), isActive: true);

        // Неактивний Y @ 01:00 (не має рахуватись ні як поточний, ні в лічильник)
        var sy = S(id, "Y", "Нотатка Y", new DateTime(2025, 9, 1, 1, 0, 0, DateTimeKind.Utc), isActive: false);

        // History: і активний, і неактивний
        var history = new List<PersonStatus> { sx, sy };

        var cut = Render(id, person, active: sx, history: history);

        // Показ назви активного статусу
        Assert.Contains("Поточний статус", cut.Markup);
        Assert.Contains("X", cut.Markup);

        // Показ примітки саме активного
        Assert.Contains("Нотатка", cut.Markup);
        Assert.Contains("Нотатка X", cut.Markup);
        Assert.DoesNotContain("Нотатка Y", cut.Markup);

        // Лічильник історії — тільки валідні (IsActive=true) → 1
        // Шукаємо бейдж поруч із кнопкою "Переглянути історію"
        var badge = cut.Find("button.btn.btn-sm.btn-primary span.badge");
        Assert.Equal("1", badge.TextContent.Trim());
    }

    [Fact(DisplayName = "NotFound: якщо особи нема — компонент не падає і показує '—' у заголовку")]
    public void NotFound_DoesNotCrash_ShowsDash()
    {
        var id = Guid.NewGuid();

        var cut = Render(id, person: null, active: null, history: []);
        // У заголовку поруч із «Картка:» має бути тире
        Assert.Contains("Картка:", cut.Markup);
        Assert.Contains("—", cut.Markup);
    }
}
