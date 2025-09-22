//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentsPageTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
using eRaven.Application.Services.ExcelService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PositionAssignmentService;
using eRaven.Application.Services.PositionService;
using eRaven.Components.Pages.PositionAssignments;
using eRaven.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.PositionAssignments;

public class PositionAssignmentsPageTests : TestContext
{
    private readonly Mock<IPersonService> _personSvc = new();
    private readonly Mock<IPositionService> _positionSvc = new();
    private readonly Mock<IPositionAssignmentService> _assignSvc = new();
    private readonly Mock<IToastService> _toast = new();
    private readonly Mock<IExcelService> _excel = new();

    private readonly Person _p1;
    private readonly Person _p2;
    private readonly PositionUnit _posA;
    private readonly PositionUnit _posB;

    public PositionAssignmentsPageTests()
    {
        // ДАНІ
        _posA = new PositionUnit { Id = Guid.NewGuid(), Code = "A", ShortName = "Оператор", OrgPath = "Взвод", SpecialNumber = "111", IsActived = true };
        _posB = new PositionUnit { Id = Guid.NewGuid(), Code = "B", ShortName = "Снайпер", OrgPath = "Рота", SpecialNumber = "222", IsActived = true };

        _p1 = new Person
        {
            Id = Guid.NewGuid(),
            LastName = "Перший",
            FirstName = "Іван",
            Rnokpp = "1111111111",
            Rank = "солдат",
            PositionUnitId = null,
            PositionUnit = null
        };

        _p2 = new Person
        {
            Id = Guid.NewGuid(),
            LastName = "Другий",
            FirstName = "Петро",
            Rnokpp = "2222222222",
            Rank = "сержант",
            PositionUnitId = _posA.Id,
            PositionUnit = _posA
        };

        // Моки сервісів
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([_p1, _p2]);

        _positionSvc.Setup(s => s.GetPositionsAsync(true, It.IsAny<CancellationToken>()))
                    .ReturnsAsync([_posA, _posB]);

        // За замовчуванням — активне призначення тільки для p2
        _assignSvc.Setup(s => s.GetActiveAsync(_p1.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((PersonPositionAssignment?)null);

        _assignSvc.Setup(s => s.GetActiveAsync(_p2.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new PersonPositionAssignment
                  {
                      Id = Guid.NewGuid(),
                      PersonId = _p2.Id,
                      PositionUnitId = _posA.Id,
                      OpenUtc = DateTime.UtcNow.AddDays(-1),
                      PositionUnit = _posA,
                      ModifiedUtc = DateTime.UtcNow
                  });

        _assignSvc.Setup(s => s.GetHistoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        // DI
        Services.AddSingleton(_personSvc.Object);
        Services.AddSingleton(_positionSvc.Object);
        Services.AddSingleton(_assignSvc.Object);
        Services.AddSingleton(_toast.Object);
        Services.AddSingleton(_excel.Object);
    }

    private IRenderedComponent<PositionAssignmentsPage> RenderPage()
    {
        var cut = RenderComponent<PositionAssignmentsPage>();

        // Дочекаємось, поки дані підтягнуться і таблиця з'явиться
        cut.WaitForAssertion(() =>
        {
            // принаймні два ПІБ у таблиці
            cut.Markup.Contains("Перший Іван");
            cut.Markup.Contains("Другий Петро");
        }, timeout: TimeSpan.FromSeconds(2));

        return cut;
    }

    [Fact(DisplayName = "Initial render: показує список осіб з їх чинним станом посади")]
    public void Renders_Table_With_Persons()
    {
        var cut = RenderPage();

        // рядки з обома особами
        cut.Markup.Contains("Перший Іван");
        cut.Markup.Contains("Другий Петро");

        // посада у другого відображається (повна назва)
        Assert.Contains(_posA.FullName, cut.Markup);
        Assert.NotNull(cut.Instance.PersonService);
        Assert.NotNull(cut.Instance.PositionService);
        Assert.NotNull(cut.Instance.PositionAssignmentService);
        Assert.NotNull(cut.Instance.Toast);
    }

    [Fact(DisplayName = "Обрання особи без призначення → показується лише кнопка 'Призначити' і вона активна (є вільні посади)")]
    public void Select_Unassigned_Shows_Assign_Only_Enabled()
    {
        var cut = RenderPage();

        // клік по рядку p1 (без посади)
        var rowOfP1 = cut.FindAll("tbody tr").First(tr => tr.InnerHtml.Contains("Перший Іван"));
        rowOfP1.Click();

        // дочекаємось підвантаження активного призначення/історії
        cut.WaitForAssertion(() =>
        {
            // Кнопка 'Призначити' повинна бути
            var hasAssign = cut.Markup.Contains("bi bi-person-plus");
            Assert.True(hasAssign);

            // Кнопки 'Зняти' бути не повинно
            var hasUnassign = cut.Markup.Contains("bi bi-person-dash");
            Assert.False(hasUnassign);

            // Вільні посади є (posB), отже кнопка не disabled
            var btn = cut.Find("button.btn.btn-primary");
            Assert.False(btn.HasAttribute("disabled"));
        });
    }

    [Fact(DisplayName = "Обрання особи з активним призначенням → показується лише кнопка 'Зняти'")]
    public void Select_Assigned_Shows_Unassign_Only()
    {
        var cut = RenderPage();

        // клік по рядку p2 (із посадою)
        var rowOfP2 = cut.FindAll("tbody tr").First(tr => tr.InnerHtml.Contains("Другий Петро"));
        rowOfP2.Click();

        cut.WaitForAssertion(() =>
        {
            // Лише 'Зняти'
            var hasUnassign = cut.Markup.Contains("bi bi-person-dash");
            Assert.True(hasUnassign);

            var hasAssign = cut.Markup.Contains("bi bi-person-plus");
            Assert.False(hasAssign);
        });
    }
}
