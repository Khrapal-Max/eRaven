//-----------------------------------------------------------------------------
// Components/Pages/Persons/Modals/PersonCreateModal.razor.cs
//-----------------------------------------------------------------------------
// PersonCreateModaltests
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons.Modals;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public class PersonCreateModalTests : TestContext
{
    private readonly Mock<IPersonService> _svc = new(MockBehavior.Strict);
    private readonly Mock<IToastService> _toast = new(MockBehavior.Loose);

    public PersonCreateModalTests()
    {
        Services.AddSingleton(_svc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddTransient<IValidator<CreatePersonViewModel>, CreatePersonViewModelValidator>();
        Services.AddTransient<FluentValidationValidator>();
    }

    private IRenderedComponent<PersonCreateModal> RenderModal() => RenderComponent<PersonCreateModal>();

    private static void TypeIntoInputs(IRenderedComponent<PersonCreateModal> cut, CreatePersonViewModel m)
    {
        var inputs = cut.FindAll(".modal-body input");
        inputs[0].Change(m.LastName);
        inputs[1].Change(m.FirstName);
        inputs[2].Change(m.MiddleName);
        inputs[3].Change(m.Rnokpp);
        inputs[4].Change(m.Rank);
        inputs[5].Change(m.Callsign);
        inputs[6].Change(m.BZVP);
        inputs[7].Change(m.Weapon);
    }

    private static Task FillValidModelAsync(IRenderedComponent<PersonCreateModal> cut)
    => cut.InvokeAsync(() =>
    {
        var m = cut.Instance.Model;
        m.LastName = "Козак";
        m.FirstName = "Василь";
        m.MiddleName = "";                // опційно
        m.Rnokpp = "0123456789";
        m.Rank = "сержант";
        m.Callsign = "";
        m.BZVP = "є";
        m.Weapon = "";
    });

    [Fact(DisplayName = "Open(): модал показується з порожньою моделлю")]
    public async Task Open_ShowsModal()
    {
        var cut = RenderModal();

        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.IsOpen);
            Assert.Contains("Створення картки особи", cut.Markup);
        });
    }

    [Fact(DisplayName = "Cancel: закриває модал і викликає OnClose(false)")]
    public async Task Cancel_ClosesModal_RaisesOnCloseFalse()
    {
        var cut = RenderModal();
        bool? closeArg = null;
        cut.SetParametersAndRender(ps => ps.Add(p => p.OnClose, EventCallback.Factory.Create<bool>(this, v => closeArg = v)));

        await cut.InvokeAsync(() => cut.Instance.Open());
        cut.Find("button.btn-warning").Click(); // «Скасувати»

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsOpen);
            Assert.Equal(false, closeArg);
        });
    }

    [Fact(DisplayName = "Submit: невалідна форма -> CreateAsync НЕ викликається")]
    public async Task Submit_Invalid_DoesNotCallService()
    {
        var cut = RenderModal();
        await cut.InvokeAsync(() => cut.Instance.Open());

        // заповнюємо ТІЛЬКИ прізвище та імʼя — RNOKPP/Rank/BZVP відсутні => форма невалідна
        await cut.InvokeAsync(() =>
        {
            var m = cut.Instance.Model;
            m.LastName = "Козак";
            m.FirstName = "Василь";
        });

        cut.Find("form").Submit();

        _svc.Verify(s => s.CreateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Submit: валідна форма -> CreateAsync з нормалізованими значеннями, OnCreated")]
    public async Task Submit_Valid_CallsService_Normalizes_RaisesOnCreated()
    {
        var created = new Person { Id = Guid.NewGuid(), LastName = "Козак", FirstName = "Василь" };
        _svc.Setup(s => s.CreateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var cut = RenderModal();
        await cut.InvokeAsync(() => cut.Instance.Open());

        Person? createdArg = null;
        cut.SetParametersAndRender(ps => ps.Add(p => p.OnCreated,
            EventCallback.Factory.Create<Person>(this, p => createdArg = p)));

        // заповнюємо модель (з пробілами — перевіримо нормалізацію)
        await cut.InvokeAsync(() =>
        {
            var m = cut.Instance.Model;
            m.LastName = "  Козак  ";
            m.FirstName = "  Василь ";
            m.MiddleName = "  Степанович  ";
            m.Rnokpp = "0123456789";
            m.Rank = "  сержант ";
            m.Callsign = "  Сокіл  ";
            m.BZVP = "  є  ";
            m.Weapon = "  АК-74  ";
        });

        cut.Find("form").Submit();

        _svc.Verify(s => s.CreateAsync(
            It.Is<Person>(p =>
                p.Id != Guid.Empty &&
                p.LastName == "Козак" &&
                p.FirstName == "Василь" &&
                p.MiddleName == "Степанович" &&
                p.Rnokpp == "0123456789" &&
                p.Rank == "сержант" &&
                p.BZVP == "є" &&
                p.Callsign == "Сокіл" &&
                p.Weapon == "АК-74"
            ),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(createdArg);
        Assert.Equal(created.Id, createdArg!.Id);

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsOpen));
    }
}