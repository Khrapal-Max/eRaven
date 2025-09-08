//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonCardTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Configuration;
using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class PersonCardTests : TestContext
{
    private readonly Mock<IPersonService> _svc = new();
    private readonly Mock<IToastService> _toast = new(MockBehavior.Loose);

    public PersonCardTests()
    {
        // DI
        Services.AddSingleton(_svc.Object);
        Services.AddSingleton(_toast.Object);

        // Валідація для EditForm (FluentValidationValidator в розмітці)
        Services.AddScoped<IValidator<EditPersonViewModel>, EditPersonViewModelValidator>();
    }

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

    private IRenderedComponent<PersonCard> Render(Guid id, Person? person)
    {
        _svc.Reset();
        _toast.Reset();

        _svc.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(person);

        // UpdateAsync — за замовчуванням OK
        _svc.Setup(s => s.UpdateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return RenderComponent<PersonCard>(ps => ps.Add(p => p.Id, id));
    }

    // =============== ТЕСТИ ===============

    [Fact(DisplayName = "Init: відображає дані картки після завантаження")]
    public void Init_Renders_Person_Data()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var cut = Render(id, person);

        // після OnInitializedAsync — _initialLoading=false, відрисовано дані
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

        // клік "Редагувати"
        cut.Find("button.btn-warning").Click();

        // є форма з id="person-edit-form"
        Assert.NotNull(cut.Find("form#person-edit-form"));
        // є інпут по РНОКПП
        Assert.Contains("РНОКПП", cut.Markup);
    }

    [Fact(DisplayName = "CancelEdit: повертає у режим перегляду")]
    public void CancelEdit_Returns_ViewMode()
    {
        var id = Guid.NewGuid();
        var person = P(id);

        var cut = Render(id, person);
        cut.Find("button.btn-warning").Click();      // Edit
        cut.Find("button.btn-outline-dark").Click(); // Cancel

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
}
