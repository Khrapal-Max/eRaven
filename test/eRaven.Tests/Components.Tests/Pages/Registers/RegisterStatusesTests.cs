//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// RegisterStatusesRenderTests (simple rendering: empty & filled)
//-----------------------------------------------------------------------------

using Bunit;
using eRaven.Components.Pages.Registers;
using eRaven.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Registers;

public sealed class RegisterStatusesRenderTests : TestContext
{
    private readonly Mock<IPersonStatusService> _svc = new();
    private readonly Mock<Blazored.Toast.Services.IToastService> _toast = new(MockBehavior.Loose);

    public RegisterStatusesRenderTests()
    {
        Services.AddSingleton(_svc.Object);
        Services.AddSingleton(_toast.Object);
    }

    // ---------- helpers ----------

    private static Person P(string ln, string fn, string? mn, string rnokpp) => new()
    {
        Id = Guid.NewGuid(),
        LastName = ln,
        FirstName = fn,
        MiddleName = mn,
        Rnokpp = rnokpp
    };

    private static PersonStatus S(Person p, string statusName, string? note, DateTime openUtc, DateTime modifiedUtc, bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            Person = p,
            StatusKindId = 1,
            StatusKind = new StatusKind { Id = 1, Name = statusName, IsActive = true },
            OpenDate = openUtc,
            Modified = modifiedUtc,
            Note = note,
            IsActive = isActive,
            Sequence = 0,
            Author = "tester"
        };

    // ---------- tests ----------

    [Fact(DisplayName = "Init: порожній набір → показує текст про порожній реєстр")]
    public void Empty_Shows_EmptyMessage()
    {
        // Arrange
        _svc.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var cut = RenderComponent<RegisterStatuses>();

        // Assert (чекаємо асинхронне завантаження)
        cut.WaitForAssertion(() =>
            Assert.Contains("Регістр порожній або нічого не знайдено.", cut.Markup));
    }

    [Fact(DisplayName = "Init: заповнений набір → відображає таблицю з даними та чекбокс у стовпці 'Стан'")]
    public void Filled_Renders_Table_With_Rows_And_Toggle()
    {
        // Arrange
        var p1 = P("Іваненко", "Іван", "Іванович", "1111111111");
        var p2 = P("Петренко", "Петро", null, "2222222222");

        var t1 = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2025, 9, 2, 10, 30, 0, DateTimeKind.Utc);

        var items = new[]
        {
            S(p1, "В районі", "note-1", t1, t1.AddHours(2), true),
            S(p2, "В БР",     "note-2", t2, t2.AddMinutes(15), false),
        };

        _svc.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var cut = RenderComponent<RegisterStatuses>();

        // Assert (чекаємо асинхронне завантаження)
        cut.WaitForAssertion(() =>
        {
            // Хедер
            Assert.Contains("Статус", cut.Markup);
            Assert.Contains("Дата події", cut.Markup);
            Assert.Contains("Примітка", cut.Markup);
            Assert.Contains("Особа", cut.Markup);
            Assert.Contains("Автор", cut.Markup);
            Assert.Contains("Дата запису", cut.Markup);
            Assert.Contains("Стан", cut.Markup);

            // Рядок 1
            Assert.Contains(p1.FullName, cut.Markup);
            Assert.Contains(p1.Rnokpp, cut.Markup);
            Assert.Contains("В районі", cut.Markup);
            Assert.Contains("note-1", cut.Markup);
            Assert.Contains(t1.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);          // OpenDate
            Assert.Contains(t1.AddHours(2).ToLocalTime().ToString("dd.MM.yyyy HH:mm"), cut.Markup); // Modified

            // Рядок 2
            Assert.Contains(p2.FullName, cut.Markup);
            Assert.Contains(p2.Rnokpp, cut.Markup);
            Assert.Contains("В БР", cut.Markup);
            Assert.Contains("note-2", cut.Markup);
            Assert.Contains(t2.ToLocalTime().ToString("dd.MM.yyyy"), cut.Markup);
            Assert.Contains(t2.AddMinutes(15).ToLocalTime().ToString("dd.MM.yyyy HH:mm"), cut.Markup);

            // Чекбокси у стовпці "Стан" присутні (TransitionToggle рендерить <input type=checkbox>)
            var checkboxes = cut.FindAll("input.form-check-input[type=checkbox]");
            Assert.True(checkboxes.Count >= 2);
        });
    }
}
