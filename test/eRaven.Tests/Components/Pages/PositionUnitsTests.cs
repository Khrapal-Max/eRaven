//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitsTests -> PositionUnits
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.RositionUnits;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Excel;
using eRaven.Infrastructure.Repositories.PositionUnitRepository;
using eRaven.Infrastructure.Repositories.RankRepository;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Pages;

public class PositionUnitsTests : BunitContext
{
    private readonly Mock<IValidator<PositionUnit>> _validator = new(MockBehavior.Strict);
    private readonly Mock<IPositionUnitRepository> _posRepo = new(MockBehavior.Strict);
    private readonly Mock<IRankRepository> _rankRepo = new(MockBehavior.Strict);
    private readonly Mock<IPositionUnitExcelService> _excel = new(MockBehavior.Strict);

    public PositionUnitsTests()
    {
        Services.AddSingleton(_validator.Object);
        Services.AddSingleton(_posRepo.Object);
        Services.AddSingleton(_rankRepo.Object);
        Services.AddSingleton(_excel.Object);
    }

    [Fact]
    public void OnInitialized_ShouldLoadData_AndRenderRows()
    {
        // Arrange
        var units = new List<PositionUnit>
        {
            new() { Id = Guid.NewGuid(), Number = 2, Code = "B", ShortName = "S2", FullName = "F2", SpecialNumber="002", Rank="R2", Tarif="1", IsActived = true },
            new() { Id = Guid.NewGuid(), Number = 1, Code = "A", ShortName = "S1", FullName = "F1", SpecialNumber="001", Rank="R1", Tarif="1", IsActived = false },
        };

        _posRepo.Setup(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(units);

        // Act
        var cut = Render<PositionUnits>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _posRepo.Verify(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Є таблиця/контент (залежить від Table-компонента, але хоча б дані в markup)
            Assert.Contains("Довідник посад", cut.Markup);
            Assert.Contains("A", cut.Markup);
            Assert.Contains("B", cut.Markup);
        });

        _posRepo.VerifyAll();
    }

    [Fact]
    public void Load_ShouldSort_ActiveFirst_ThenByNumber()
    {
        // Arrange: спеціально хаотично
        var u1 = new PositionUnit { Id = Guid.NewGuid(), Number = 5, Code = "C", ShortName = "S", FullName = "F", SpecialNumber = "003", Rank = "R", Tarif = "1", IsActived = true };
        var u2 = new PositionUnit { Id = Guid.NewGuid(), Number = 1, Code = "A", ShortName = "S", FullName = "F", SpecialNumber = "001", Rank = "R", Tarif = "1", IsActived = false };
        var u3 = new PositionUnit { Id = Guid.NewGuid(), Number = 2, Code = "B", ShortName = "S", FullName = "F", SpecialNumber = "002", Rank = "R", Tarif = "1", IsActived = true };

        _posRepo.Setup(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([u1, u2, u3]);

        // Act
        var cut = Render<PositionUnits>();

        // Assert: після сортування має бути: активні (Number 2,5) потім неактивні (Number 1)
        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;

            var idxB = markup.IndexOf(">B<", StringComparison.Ordinal);
            var idxC = markup.IndexOf(">C<", StringComparison.Ordinal);
            var idxA = markup.IndexOf(">A<", StringComparison.Ordinal);

            Assert.True(idxB >= 0 && idxC >= 0 && idxA >= 0);
            Assert.True(idxB < idxC); // 2 перед 5
            Assert.True(idxC < idxA); // активні перед неактивними
        });

        _posRepo.VerifyAll();
    }

    [Fact]
    public void AddButton_Click_ShouldLoadRanks_AndOpenCreateModal()
    {
        // Arrange
        _posRepo.Setup(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var ranks = new List<Rank>
        {
            new() { Id = Guid.NewGuid(), Title = "Сержант", Priority = 2, IsActived = true },
            new() { Id = Guid.NewGuid(), Title = "Солдат", Priority = 1, IsActived = true },
            new() { Id = Guid.NewGuid(), Title = "Солдат", Priority = 99, IsActived = true }, // дубль -> Distinct
            new() { Id = Guid.NewGuid(), Title = "Неактивний", Priority = 0, IsActived = false }
        };

        _rankRepo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ranks);

        var cut = Render<PositionUnits>();

        // Act
        cut.WaitForAssertion(() => Assert.Contains("Записи відсутні", cut.Markup));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Додати").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _rankRepo.Verify(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Модалка створення має відкритися
            Assert.Contains("Додати посаду", cut.Markup);

            // Список звань: активні, відсортовані по Priority, без дублів
            // Перевіримо що "Солдат" зустрічається, а "Неактивний" — ні
            Assert.Contains("Солдат", cut.Markup);
            Assert.Contains("Сержант", cut.Markup);
            Assert.DoesNotContain("Неактивний", cut.Markup);
        });

        _posRepo.VerifyAll();
        _rankRepo.VerifyAll();
    }

    [Fact]
    public void CreateAsync_ValidModel_ShouldCallRepoChecks_Add_AndReload()
    {
        // Arrange
        _posRepo.SetupSequence(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]) // initial load
                .ReturnsAsync([
                    new PositionUnit
                {
                    Id = Guid.NewGuid(),
                    Number = 1,
                    Code = "A1",
                    ShortName = "S",
                    FullName = "F",
                    SpecialNumber = "001",
                    Rank = "Солдат",
                    Tarif = "1",
                    IsActived = true
                }
                ]); // reload after create

        _rankRepo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([
                     new Rank { Id = Guid.NewGuid(), Title = "Солдат", Priority = 1, IsActived = true }
                 ]);

        // ✅ важливо: правильна перегрузка
        _validator.Setup(v => v.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new ValidationResult());

        _posRepo.Setup(x => x.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        _posRepo.Setup(x => x.ActiveNumberExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        _posRepo.Setup(x => x.AddPositionUnitAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var cut = Render<PositionUnits>();

        // open create modal
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Додати").Click();
        cut.WaitForAssertion(() => Assert.Contains("Додати посаду", cut.Markup));

        // fill form строго по name як у DOM
        cut.Find(".modal input[type='number'][name='CreateModel.Number']").Change("1");

        // Code із пробілами -> CreateAsync.Trim()
        cut.Find(".modal input[name='CreateModel.Code']").Change(" A1 ");
        cut.Find(".modal input[name='CreateModel.ShortName']").Change("S");
        cut.Find(".modal input[name='CreateModel.FullName']").Change("F");
        cut.Find(".modal input[name='CreateModel.SpecialNumber']").Change("001");
        cut.Find(".modal input[name='CreateModel.Tarif']").Change("1");

        // ✅ Rank: НЕ клікаємо dropdown-toggle (нема blazor handler)
        // Клікаємо одразу item у dropdown-menu (в нього є blazor:onclick)
        cut.FindAll(".modal .dropdown-menu button")
           .Single(b => b.TextContent.Trim() == "Солдат")
           .Click();

        // Act
        cut.FindAll(".modal .modal-footer button")
           .Single(b => b.TextContent.Trim() == "Додати")
           .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _validator.Verify(v => v.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);

            _posRepo.Verify(x => x.CodeExistsAsync("A1", It.IsAny<CancellationToken>()), Times.Once);
            _posRepo.Verify(x => x.ActiveNumberExistsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _posRepo.Verify(x => x.AddPositionUnitAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);

            // було 2 завантаження списку: initial + після успішного create
            _posRepo.Verify(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            Assert.Contains("A1", cut.Markup);
        });

        _validator.VerifyAll();
        _rankRepo.VerifyAll();
        _posRepo.VerifyAll();
    }

    [Fact]
    public void CreateAsync_DuplicateCode_ShouldShowError_AndNotAdd()
    {
        // Arrange
        _posRepo.Setup(x => x.GetAllPositionUnitsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        _rankRepo.Setup(x => x.GetAllRanksAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([
                     new Rank
                 {
                     Id = Guid.NewGuid(),
                     Title = "Солдат",
                     Priority = 1,
                     IsActived = true
                 }
                 ]);

        // ✅ FIX: Setup на ValidateAsync(T instance, CancellationToken)
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _posRepo.Setup(x => x.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // 👈 дубль коду

        var cut = Render<PositionUnits>();

        // open create modal
        cut.FindAll("button")
           .Single(b => b.TextContent.Trim() == "Додати")
           .Click();

        cut.WaitForAssertion(() => Assert.Contains("Додати посаду", cut.Markup));

        // fill form
        cut.Find(".modal input[type='number'][name='CreateModel.Number']").Change("1");
        cut.Find(".modal input[name='CreateModel.Code']").Change("A1");
        cut.Find(".modal input[name='CreateModel.ShortName']").Change("S");
        cut.Find(".modal input[name='CreateModel.FullName']").Change("F");
        cut.Find(".modal input[name='CreateModel.SpecialNumber']").Change("001");
        cut.Find(".modal input[name='CreateModel.Tarif']").Change("1");

        // rank: НЕ клікаємо toggle, клікаємо item з blazor handler
        cut.FindAll(".modal .dropdown-menu button")
           .Single(b => b.TextContent.Trim() == "Солдат")
           .Click();

        // Act
        cut.FindAll(".modal .modal-footer button")
           .Single(b => b.TextContent.Trim() == "Додати")
           .Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            _validator.Verify(v => v.ValidateAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Once);

            _posRepo.Verify(x => x.CodeExistsAsync("A1", It.IsAny<CancellationToken>()), Times.Once);
            _posRepo.Verify(x => x.ActiveNumberExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _posRepo.Verify(x => x.AddPositionUnitAsync(It.IsAny<PositionUnit>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.Contains("Посада з таким кодом вже існує", cut.Markup);
            Assert.Contains("Додати посаду", cut.Markup);
        });

        _validator.VerifyAll();
        _rankRepo.VerifyAll();
        _posRepo.VerifyAll();
    }
}