//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateReservedDrawerTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.Catalogs.Ranks;
using eRaven.Application.DTOs;
using eRaven.Components.Pages.Persons.Registry.Drawers;
using eRaven.Presentation.Toasts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons;

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
        Services.AddSingleton(new ToastService());
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

        cut.Find("form#create-reserved-form");

        // ✅ тепер не select, а dropdown menu
        var menu = cut.Find("ul.dropdown-menu");
        var menuHtml = menu.OuterHtml;

        Assert.Contains("солдат", menuHtml);
        Assert.Contains("сержант", menuHtml);

        // опційно: перевірити, що кнопка-тоггл існує
        cut.Find("button.dropdown-toggle");
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

        const string formSel = "form#create-reserved-form";

        // act — кожна дія: Find(...) + event в одному InvokeAsync
        await cut.InvokeAsync(() => cut.FindAll($"{formSel} input.form-control")[0].Change("1234567890"));        // Rnokpp
        await cut.InvokeAsync(() => cut.FindAll($"{formSel} input.form-control")[1].Change("  Ivanov  "));        // LastName
        await cut.InvokeAsync(() => cut.FindAll($"{formSel} input.form-control")[2].Change("  Ivan  "));          // FirstName
        await cut.InvokeAsync(() => cut.FindAll($"{formSel} input.form-control")[3].Change("  Ivanovich  "));     // MiddleName

        // PositionSort (якщо є в формі)
        // Якщо в твоїй формі PositionSort інколи відсутній — загорни в try/catch або перевір FindAll().Any()
        await cut.InvokeAsync(() => cut.Find($"{formSel} input[type=number]").Change("10"));

        // Position (зазвичай останній текстовий інпут у цій формі)
        await cut.InvokeAsync(() =>
        {
            var all = cut.FindAll($"{formSel} input.form-control");
            all[^1].Change("  оператор  ");
        });

        // ✅ вибір рангу через dropdown-item (bootstrap JS не потрібен)
        await cut.InvokeAsync(() =>
        {
            // Підійде і для <button>, і для <a>, і для твого компонента, якщо клас dropdown-item на кореневому елементі
            var items = cut.FindAll($"{formSel} .dropdown-menu .dropdown-item");
            var soldier = items.Single(x => x.TextContent.Trim() == "солдат");
            soldier.Click();
        });

        // submit
        await cut.InvokeAsync(() => cut.Find(formSel).Submit());

        // assert
        Assert.NotNull(captured);
        Assert.Equal("1234567890", captured!.Rnokpp);
        Assert.Equal("Ivanov", captured.LastName);
        Assert.Equal("Ivan", captured.FirstName);
        Assert.Equal("Ivanovich", captured.MiddleName);

        Assert.Equal("солдат", captured.Rank);
        Assert.Equal("оператор", captured.Position);  // або як у тебе normalize

        Assert.False(isOpen);

        rankCatalog.Verify(x => x.GetActive(), Times.Once);
    }
}
