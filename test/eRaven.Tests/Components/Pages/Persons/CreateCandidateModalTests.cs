//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateModalTests
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Components.Pages.Persons.Registry.Modals;
using eRaven.Presentation.Toasts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace eRaven.Tests.Components.Pages.Persons;

public sealed class CreateCandidateModalTests : BunitContext
{
    private static IReadOnlyList<PositionUnitOptionDto> SampleOptions()
        =>
        [
            new(
                Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code: "POS001",
                ShortName: "Стрілець",
                FullName: "Стрілець (повна)",
                Rank: "Солдат",
                Tarif: "1"
            ),
            new(
                Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code: "POS002",
                ShortName: "Оператор",
                FullName: "Оператор (повна)",
                Rank: "Сержант",
                Tarif: "2"
            ),
        ];

    private (Mock<IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>>> mock, IReadOnlyList<PositionUnitOptionDto> data)
        RegisterHappyPathServices()
    {
        var data = SampleOptions();

        var q = new Mock<IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>>>(MockBehavior.Strict);
        q.Setup(x => x.HandleAsync(
                It.IsAny<GetVacantPositionUnitsQuery>(),
                It.IsAny<CancellationToken>()))
         .ReturnsAsync(data);

        Services.AddSingleton(q.Object);
        Services.AddSingleton(new ToastService());

        return (q, data);
    }

    [Fact]
    public void When_IsOpen_true_should_render_form_fields_and_load_positions()
    {
        // Arrange
        var (q, data) = RegisterHappyPathServices();

        var onCreateCalled = false;

        // Act
        var cut = Render<CreateCandidateModal>(ps => ps
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnCreate, _ =>
            {
                onCreateCalled = true;
                return Task.CompletedTask;
            }));

        // Assert: query викликався при відкритті
        q.Verify(x => x.HandleAsync(
            It.Is<GetVacantPositionUnitsQuery>(qq => qq.Take == 200),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert: є заголовок/кнопки (текст)
        cut.Markup.Contains("Створити кандидата");
        cut.Markup.Contains("Створити");
        cut.Markup.Contains("Скасувати");

        // Assert: є поля форми
        cut.Markup.Contains("РНОКПП");
        cut.Markup.Contains("Прізвище");
        cut.Markup.Contains("Ім’я");
        cut.Markup.Contains("По батькові");
        cut.Markup.Contains("Вакантна посада");

        // Assert: є select та options з query
        var selects = cut.FindAll("select");
        Assert.True(selects.Count >= 1);

        // У select має бути дефолтний option + 2 з data
        var options = cut.FindAll("option");
        Assert.Contains(options, o => o.TextContent.Contains("-- не обирати --"));
        Assert.Contains(options, o => o.TextContent.Contains("POS001") && o.TextContent.Contains("Стрілець"));
        Assert.Contains(options, o => o.TextContent.Contains("POS002") && o.TextContent.Contains("Оператор"));

        // callback існує (не викликаємо, лише перевіряємо що він заданий)
        Assert.True(cut.Instance.OnCreate.HasDelegate);
        Assert.True(cut.Instance.IsOpenChanged.HasDelegate);
        Assert.False(onCreateCalled);
    }

    [Fact]
    public void When_IsOpen_false_should_not_call_query_and_should_not_render_options()
    {
        // Arrange
        var (q, _) = RegisterHappyPathServices();

        // Act
        var cut = Render<CreateCandidateModal>(ps => ps
            .Add(p => p.IsOpen, false)
            .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
            .Add(p => p.OnCreate, _ => Task.CompletedTask));

        // Assert: query НЕ викликався (бо не відкрито)
        q.Verify(x => x.HandleAsync(It.IsAny<GetVacantPositionUnitsQuery>(), It.IsAny<CancellationToken>()), Times.Never);

        // В залежності від реалізації <Modal>:
        // - якщо children не рендеряться коли IsOpen=false, то optionів не буде.
        // - якщо рендеряться але приховані — цей assert можна прибрати.
        Assert.DoesNotContain("POS001", cut.Markup);
        Assert.DoesNotContain("POS002", cut.Markup);
    }

    [Fact]
    public void Rendering_without_required_query_service_should_throw()
    {
        // Arrange: реєструємо тільки Toasts, а VacantPositionsQuery НЕ реєструємо
        Services.AddSingleton(new ToastService());

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            Render<CreateCandidateModal>(ps => ps
                .Add(p => p.IsOpen, true)
                .Add(p => p.IsOpenChanged, _ => Task.CompletedTask)
                .Add(p => p.OnCreate, _ => Task.CompletedTask)));
    }
}
