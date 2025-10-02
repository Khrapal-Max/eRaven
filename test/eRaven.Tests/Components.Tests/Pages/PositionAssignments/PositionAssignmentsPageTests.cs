//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentsPageTests
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using Bunit;
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
        // ТЕСТОВІ ДАНІ
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

        // Активне призначення лише для p2
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

        // дочекаймося першого завантаження
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Contains("Перший Іван");
            cut.Markup.Contains("Другий Петро");
        }, timeout: TimeSpan.FromSeconds(2));

        return cut;
    }

    [Fact(DisplayName = "Initial render: показує список осіб з їх чинним станом та DI підкладено")]
    public void Renders_Table_With_Persons()
    {
        var cut = RenderPage();

        // рядки з обома особами
        cut.Markup.Contains("Перший Іван");
        cut.Markup.Contains("Другий Петро");

        // посада у другого відображається (повна назва)
        Assert.Contains(_posA.FullName, cut.Markup);

        // DI
        Assert.NotNull(cut.Instance.PersonService);
        Assert.NotNull(cut.Instance.PositionService);
        Assert.NotNull(cut.Instance.PositionAssignmentService);
        Assert.NotNull(cut.Instance.Toast);
    }

    [Fact(DisplayName = "Обрання особи без призначення → показується лише кнопка 'Призначити' і вона активна (є вільні посади)")]
    public void Select_Unassigned_Shows_Assign_Only_Enabled()
    {
        var cut = RenderPage();

        // клік по рядку p1
        var rowOfP1 = cut.FindAll("tbody tr").First(tr => tr.InnerHtml.Contains("Перший Іван"));
        rowOfP1.Click();

        cut.WaitForAssertion(() =>
        {
            // Є іконка призначення
            Assert.Contains("bi bi-person-plus", cut.Markup);

            // Немає іконки зняття (взагалі не використовується тепер)
            Assert.DoesNotContain("bi bi-person-dash", cut.Markup);

            // Кнопка 'Призначити' не disabled (бо _posB вільна)
            var btn = cut.Find("button.btn.btn-primary");
            Assert.False(btn.HasAttribute("disabled"));
        });
    }

    [Fact(DisplayName = "Обрання особи з активним призначенням → теж показуємо лише 'Призначити' (для пере призначення)")]
    public void Select_Assigned_Still_Shows_Assign_Only()
    {
        var cut = RenderPage();

        // клік по p2 (є активна посада)
        var rowOfP2 = cut.FindAll("tbody tr").First(tr => tr.InnerHtml.Contains("Другий Петро"));
        rowOfP2.Click();

        cut.WaitForAssertion(() =>
        {
            // Лише 'Призначити'
            Assert.Contains("bi bi-person-plus", cut.Markup);
            Assert.DoesNotContain("bi bi-person-dash", cut.Markup);

            // Є вільна posB → кнопка активна
            var btn = cut.Find("button.btn.btn-primary");
            Assert.False(btn.HasAttribute("disabled"));
        });
    }

    [Fact(DisplayName = "Коли немає жодної вільної посади — кнопка 'Призначити' вимкнена для обраної особи")]
    public void Assign_Disabled_When_No_Free_Positions()
    {
        // Переналаштовуємо мок даних: обидві посади зайняті
        var p1WithPos = new Person
        {
            Id = _p1.Id,
            LastName = _p1.LastName,
            FirstName = _p1.FirstName,
            MiddleName = _p1.MiddleName,
            Rnokpp = _p1.Rnokpp,
            Rank = _p1.Rank,
            Callsign = _p1.Callsign,
            BZVP = _p1.BZVP,
            Weapon = _p1.Weapon,

            // оновлюємо зайняту посаду як треба для тесту
            PositionUnitId = _posB.Id,
            PositionUnit = _posB
        };

        _personSvc.Reset(); // скинемо старі сетапи, щоб явно задати нові
        _personSvc.Setup(s => s.SearchAsync(null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([p1WithPos, _p2]);

        // Активні призначення для обох (щоб сторінка підтягнула ActiveAssign)
        _assignSvc.Setup(s => s.GetActiveAsync(p1WithPos.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new PersonPositionAssignment
                  {
                      Id = Guid.NewGuid(),
                      PersonId = p1WithPos.Id,
                      PositionUnitId = _posB.Id,
                      OpenUtc = DateTime.UtcNow.AddDays(-2),
                      PositionUnit = _posB,
                      ModifiedUtc = DateTime.UtcNow
                  });

        // Рендеримо заново
        var cut = RenderComponent<PositionAssignmentsPage>();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Contains("Перший Іван");
            cut.Markup.Contains("Другий Петро");
        });

        // Клік по p1 (тепер він теж на посаді)
        var rowOfP1 = cut.FindAll("tbody tr").First(tr => tr.InnerHtml.Contains("Перший Іван"));
        rowOfP1.Click();

        cut.WaitForAssertion(() =>
        {
            // Кнопка призначення є, але має бути disabled (усі посади зайняті)
            var btn = cut.Find("button.btn.btn-primary");
            Assert.True(btn.HasAttribute("disabled"));
        });
    }
}
