//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Registry;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons.Registry;

public sealed class CreateReservedDrawerTests : BunitContext
{
    // Мінімальний валідатор саме для UI-моделі, щоб OnValidSubmit спрацьовував.
    private sealed class CreateReservedDtoValidator : AbstractValidator<CreateReservedDto>
    {
        public CreateReservedDtoValidator()
        {
            RuleFor(x => x.Rnokpp)
                .NotEmpty()
                .Length(10)
                .Matches(@"^\d{10}$");

            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty();
        }
    }

    [Fact]
    public void When_open_should_load_ranks_and_render_form()
    {
        // arrange
        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns(
            [
                new RankOption("солдат"),
                new RankOption("сержант"),
            ]);

        Services.AddSingleton(rankCatalog.Object);

        // ToastService — щоб інжект не падав
        Services.AddSingleton(new ToastService());

        // Валідатор для моделі форми
        Services.AddSingleton<IValidator<CreateReservedDto>>(new CreateReservedDtoValidator());

        var isOpen = true;

        // act
        var cut = Render<CreateReservedDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnCreate, _ => Task.CompletedTask)
        );

        // assert
        rankCatalog.Verify(x => x.GetActive(), Times.Once);

        // форма є і має id, який потрібен для submit-кнопки з FooterContent
        cut.Find("form#create-reserved-form");

        // rank options відрендерені
        var select = cut.Find("select");
        var html = select.OuterHtml;
        Assert.Contains("солдат", html);
        Assert.Contains("сержант", html);
    }

    [Fact]
    public async Task Submit_valid_form_should_invoke_OnCreate_and_close_drawer()
    {
        // arrange
        var rankCatalog = new Mock<IRankCatalog>(MockBehavior.Strict);
        rankCatalog.Setup(x => x.GetActive())
            .Returns(
            [
            new RankOption("солдат"),
            ]);

        Services.AddSingleton(rankCatalog.Object);
        Services.AddSingleton(new ToastService());
        Services.AddSingleton<IValidator<CreateReservedDto>>(new CreateReservedDtoValidator());

        var isOpen = true;
        CreateReservedDto? captured = null;

        var cut = Render<CreateReservedDrawer>(ps => ps
            .Add(p => p.IsOpen, isOpen)
            .Add(p => p.IsOpenChanged, v => isOpen = v)
            .Add(p => p.OnCreate, dto =>
            {
                captured = dto;
                return Task.CompletedTask;
            })
        );

        // act: кожен раз перевибираємо input-и заново
        await cut.InvokeAsync(() => cut.FindAll("input.form-control")[0].Change("1234567890"));   // Rnokpp
        await cut.InvokeAsync(() => cut.FindAll("input.form-control")[1].Change("  Ivanov  "));   // LastName
        await cut.InvokeAsync(() => cut.FindAll("input.form-control")[2].Change("  Ivan  "));     // FirstName
        await cut.InvokeAsync(() => cut.FindAll("input.form-control")[3].Change("  Ivanovich  "));// MiddleName

        await cut.InvokeAsync(() => cut.Find("select").Change("солдат"));

        // submit: найнадійніше — Submit() форми
        await cut.InvokeAsync(() => cut.Find("form#create-reserved-form").Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal("1234567890", captured!.Rnokpp);
        Assert.Equal("Ivanov", captured.LastName);
        Assert.Equal("Ivan", captured.FirstName);
        Assert.Equal("Ivanovich", captured.MiddleName);
        Assert.Equal("солдат", captured.Rank);

        Assert.False(isOpen); // IsOpenChanged(false) після успіху
    }
}
