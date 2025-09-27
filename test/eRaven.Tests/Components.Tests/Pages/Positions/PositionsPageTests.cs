//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionsTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Components.Pages.Positions;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Positions;

public class PositionsPageTests : TestContext
{
    [Fact]
    public void Renders_With_DI_And_Defaults()
    {
        var svc = new Mock<IPositionService>();
        svc.Setup(s => s.GetPositionsAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync([]);
        Services.AddSingleton(svc.Object);
        Services.AddSingleton(new Mock<IExcelService>().Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IValidator<CreatePositionUnitViewModel>>().Object);

        var cut = RenderComponent<PositionsPage>();

        // просто переконуємося, що змонтувалось і бачимо тулбар
        cut.Markup.Contains("Створити");                       // кнопка
        cut.Markup.Contains("Пошук: код / коротка");           // плейсхолдер
        svc.Verify(s => s.GetPositionsAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Shows_Empty_Text_When_No_Items()
    {
        var svc = new Mock<IPositionService>();
        svc.Setup(s => s.GetPositionsAsync(false, It.IsAny<CancellationToken>()))
           .ReturnsAsync([]);
        Services.AddSingleton(svc.Object);
        Services.AddSingleton(new Mock<IExcelService>().Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IValidator<CreatePositionUnitViewModel>>().Object);

        var cut = RenderComponent<PositionsPage>();

        cut.Markup.Contains("Список посад порожній.");
    }

    [Fact]
    public void Renders_Rows_And_Vacant_Label()
    {
        var withPerson = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "A1",
            ShortName = "Посада 1",
            SpecialNumber = "11-111",
            OrgPath = "X/Y",
            IsActived = true,
            CurrentPerson = new Person
            {
                Id = Guid.NewGuid(),
                FirstName = "Іван",
                LastName = "Тест",
                StatusKindId = 1
                // FullName не чіпаємо — MapToVm зшиє Іван + Тест
            }
        };

        var vacant = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "B2",
            ShortName = "Посада 2",
            SpecialNumber = "22-222",
            OrgPath = "X/Z",
            IsActived = true,
            CurrentPerson = null
        };

        var svc = new Mock<IPositionService>();
        svc.Setup(s => s.GetPositionsAsync(false, It.IsAny<CancellationToken>()))
           .ReturnsAsync([withPerson, vacant]);
        Services.AddSingleton(svc.Object);
        Services.AddSingleton(new Mock<IExcelService>().Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IValidator<CreatePositionUnitViewModel>>().Object);

        var cut = RenderComponent<PositionsPage>();

        // є рядки
        cut.Markup.Contains("Посада 1");
        cut.Markup.Contains("Посада 2");

        // зайнята — бачимо ПІБ
        cut.Markup.Contains("Іван Тест");
        // вакантна — "Вакантна"
        cut.Markup.Contains("Вакантна");
    }

    [Fact]
    public void Filter_Matches_By_Multiple_Fields()
    {
        var list = new[]
        {
        new PositionUnit { ShortName = "Командир", Code = "KMD", SpecialNumber = "12-345" },
        new PositionUnit { ShortName = "Стрілець", Code = "STR", SpecialNumber = "99-000" }
    };

        var f1 = PositionsUi.Filter(list, "KMD");
        Assert.Single(f1); // збіг по Code

        var f2 = PositionsUi.Filter(list, "стріле");
        Assert.Single(f2); // збіг лише по ShortName "Стрілець"

        var f3 = PositionsUi.Filter(list, "12-345");
        Assert.Single(f3); // збіг по SpecialNumber
    }

    [Fact]
    public void MapToVm_Sets_Vacant_Label_When_No_Person()
    {
        var vacant = new PositionUnit { ShortName = "Оператор", CurrentPerson = null };
        var vm = PositionsUi.MapToVm(vacant);

        Assert.Equal("Вакантна", vm.CurrentPersonFullName);
    }

    [Fact]
    public void MapToVm_Uses_FullName_When_Person_Exists()
    {
        var p = new Person { FirstName = "Олег", LastName = "Коваль", StatusKindId = 1 };

        var occupied = new PositionUnit { ShortName = "Оператор", CurrentPerson = p };
        var vm = PositionsUi.MapToVm(occupied);

        Assert.Equal("Коваль Олег", vm.CurrentPersonFullName);
    }

    [Fact]
    public void Transform_Sorts_By_Code_Then_ShortName()
    {
        var list = new[]
        {
            new PositionUnit { Code = "B", ShortName = "Бета" },
            new PositionUnit { Code = "A", ShortName = "Альфа" },
            new PositionUnit { Code = "A", ShortName = "Арена" },
        };

        var vms = PositionsUi.Transform(list, null);
        Assert.Equal(["A", "A", "B"], vms.Select(x => x.Code));
        Assert.Equal(["Альфа", "Арена", "Бета"], vms.Select(x => x.ShortName));
    }
}
