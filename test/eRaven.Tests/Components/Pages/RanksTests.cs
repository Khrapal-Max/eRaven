//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RanksTests -> Ranks
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Ranks;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.RankRepository;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages;

public class RanksPageTests : BunitContext
{
    private readonly Mock<IRankRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IValidator<Rank>> _validator = new(MockBehavior.Strict);

    public RanksPageTests()
    {
        Services.AddSingleton(_repo.Object);
        Services.AddSingleton(_validator.Object);
    }

    [Fact]
    public void Render_WhenRepositoryReturnsItems_ShouldRenderTableRows_AndAddButton()
    {
        // Arrange
        var items = new List<Rank>
        {
            new() { Id = Guid.NewGuid(), Title = "лейтенант", Priority = 13, IsActived = true },
            new() { Id = Guid.NewGuid(), Title = "капітан", Priority = 15, IsActived = true },
        };

        _repo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(items);

        // Act
        var cut = Render<Ranks>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Довідник звань", cut.Markup);
            Assert.Contains("лейтенант", cut.Markup);
            Assert.Contains("капітан", cut.Markup);

            // кнопка на тулбарі (до відкриття модалки вона одна)
            var addBtn = cut.FindAll("button")
                .Where(x => x.TextContent.Trim() == "Додати")
                .ToList();

            Assert.Single(addBtn);

            // footer завжди має бути
            Assert.Contains("Бізнес-правила:", cut.Markup);
        });
    }

    [Fact]
    public void Render_WhenRepositoryReturnsEmpty_ShouldRenderEmptyState_AndFooter()
    {
        // Arrange
        _repo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        // Act
        var cut = Render<Ranks>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Записи відсутні.", cut.Markup);
            Assert.Contains("Бізнес-правила:", cut.Markup);
        });
    }

    [Fact]
    public void AddButton_Click_ShouldOpenCreateModal()
    {
        // Arrange
        _repo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var cut = Render<Ranks>();

        // Act
        cut.WaitForAssertion(() => Assert.Contains("Записи відсутні.", cut.Markup));

        // до відкриття модалки кнопка "Додати" є одна
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Додати").Click();

        // Assert: модалка повинна зʼявитись
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Додати звання", cut.Markup);
            Assert.Contains("Назва звання", cut.Markup);
            Assert.Contains("Пріоритет", cut.Markup);
            Assert.Contains("modal-backdrop", cut.Markup);
        });
    }

    [Fact]
    public void DeleteButton_Click_ShouldOpenConfirmModal()
    {
        // Arrange
        var rank = new Rank { Id = Guid.NewGuid(), Title = "майор", Priority = 16, IsActived = true };

        _repo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([rank]);

        var cut = Render<Ranks>();

        // Act
        cut.WaitForAssertion(() => Assert.Contains("майор", cut.Markup));

        // кнопка "Видалити" у рядку таблиці (поки confirm не відкритий — вона одна)
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Видалити").Click();

        // Assert: ConfirmModal має відкритися і показати текст
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Видалення звання в довіднику", cut.Markup);
            Assert.Contains("Видалити звання", cut.Markup);
            Assert.Contains("майор", cut.Markup);
        });
    }

    [Fact]
    public void ConfirmDelete_Click_ShouldCallRepositoryDeactivate_AndReload()
    {
        // Arrange
        var id = Guid.NewGuid();
        var rank = new Rank { Id = id, Title = "майор", Priority = 16, IsActived = true };

        _repo.SetupSequence(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([rank])
             .ReturnsAsync([new Rank { Id = id, Title = "майор", Priority = 16, IsActived = false }]);

        _repo.Setup(x => x.DeActivatedRankAsync(id, It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var cut = Render<Ranks>();

        // Act: відкрити ConfirmModal
        cut.WaitForAssertion(() => Assert.Contains("майор", cut.Markup));
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Видалити").Click();

        // натиснути Confirm у модалці (кнопка в modal-footer)
        cut.WaitForAssertion(() => Assert.Contains("Видалення звання в довіднику", cut.Markup));
        var modal = cut.Find(".modal");
        cut.FindAll(".modal .modal-footer button")
           .Single(x => x.TextContent.Trim() == "Видалити")
           .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _repo.Verify(x => x.DeActivatedRankAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        });
    }

    [Fact]
    public void CreateModal_SubmitValid_ShouldCallAdd_AndReload_AndCloseModal()
    {
        // Arrange
        _repo.SetupSequence(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync([])
             .ReturnsAsync([new() { Id = Guid.NewGuid(), Title = "нове звання", Priority = 5, IsActived = true }]);

        _repo.Setup(x => x.ActiveTitleExistsAsync("нове звання", It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        _repo.Setup(x => x.AddRankAsync(It.IsAny<Rank>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<Rank>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var cut = Render<Ranks>();

        // Act: відкрити модалку (до відкриття вона одна)
        cut.WaitForAssertion(() => Assert.Contains("Записи відсутні.", cut.Markup));
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Додати").Click();

        // дочекайся, що модалка реально відрендерилась
        cut.WaitForAssertion(() => Assert.Contains("Додати звання", cut.Markup));

        // IMPORTANT: інпути шукаємо ВСЕРЕДИНІ .modal і ПІСЛЯ того як модалка вже є
        cut.WaitForAssertion(() =>
        {
            var modalInputs = cut.FindAll(".modal input.form-control");
            Assert.True(modalInputs.Count >= 2);
        });

        // рефетч щоразу (не тримаємо старі IElement)
        cut.FindAll(".modal input.form-control")[0].Change("нове звання");
        cut.FindAll(".modal input.form-control")[1].Change("5");

        // натиснути Create у модалці
        cut.FindAll(".modal .modal-footer button")
           .Single(x => x.TextContent.Trim() == "Додати")
           .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _repo.Verify(x => x.ActiveTitleExistsAsync("нове звання", It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(x => x.AddRankAsync(It.IsAny<Rank>(), It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            Assert.Contains("нове звання", cut.Markup);
            Assert.DoesNotContain("modal-backdrop", cut.Markup);
        });
    }
}
