//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanServiceTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PlanServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper = new();
    private readonly AppDbContext _db;
    private readonly PlanService _svc;
    private readonly CancellationToken _ct = default;

    public PlanServiceTests()
    {
        _db = _helper.Db;
        _svc = new PlanService(_db);
    }

    public void Dispose() => _helper.Dispose();

    // ---------------------- helpers ----------------------

    private static PlanParticipantSnapshot Snap(Guid? pid = null, string fn = "Іванов І.І.", string rnokpp = "1111111111")
        => new()
        {
            Id = Guid.NewGuid(),
            PlanElementId = Guid.Empty,        // встановить сервіс при копіюванні
            PersonId = pid ?? Guid.NewGuid(),
            FullName = fn,
            Rnokpp = rnokpp,
            Rank = "сержант",
            PositionSnapshot = "Командир відділення",
            Weapon = "АК-74",
            Callsign = "Сокіл",
            StatusKindId = 1,
            StatusKindCode = "30",
            Author = "tester"
        };

    private static PlanElement El(PlanType type, DateTime whenUtc, string? loc = "локація", string? grp = "група", string? tool = "екіпаж", string? note = null, params PlanParticipantSnapshot[] participants)
        => new()
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.Empty,              // поставить сервіс
            Type = type,
            EventAtUtc = whenUtc,             // вже в UTC
            Location = loc,
            GroupName = grp,
            ToolType = tool,
            Note = note,
            Author = "tester",
            Participants = participants?.ToList() ?? []
        };

    private static DateTime Utc(int y, int M, int d, int h = 0, int m = 0)
        => new(y, M, d, h, m, 0, DateTimeKind.Utc);

    // ====================== tests: Create ======================

    [Fact(DisplayName = "CreateAsync: створює план з елементами і їх учасниками")]
    public async Task Create_CreatesPlan_WithElementsAndParticipants()
    {
        // Arrange
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "P-100",
            State = PlanState.Open,
            PlanElements =
            [
                El(PlanType.Dispatch, Utc(2025,9,10,12), participants: [Snap()]),
                El(PlanType.Return,   Utc(2025,9,11, 8), participants: [Snap(), Snap()])
            ]
        };

        // Act
        var saved = await _svc.CreateAsync(vm, _ct);

        // Assert
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("P-100", saved.PlanNumber);
        Assert.Equal(PlanState.Open, saved.State);

        var reloaded = await _db.Plans
            .Include(p => p.PlanElements)
            .ThenInclude(pe => pe.Participants)
            .FirstAsync(p => p.Id == saved.Id, _ct);

        Assert.Equal(2, reloaded.PlanElements.Count);
    }

    [Fact(DisplayName = "CreateAsync: відхиляє порожній номер плану")]
    public async Task Create_Rejects_EmptyPlanNumber()
    {
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "   ",
            PlanElements = [El(PlanType.Dispatch, Utc(2025, 9, 10), participants: [Snap()])]
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(vm, _ct));
    }

    [Fact(DisplayName = "CreateAsync: відхиляє порожній список елементів")]
    public async Task Create_Rejects_NoElements()
    {
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "P-empty",
            PlanElements = []
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CreateAsync(vm, _ct));
    }

    [Fact(DisplayName = "CreateAsync: відхиляє елемент без жодного учасника")]
    public async Task Create_Rejects_ElementWithoutParticipants()
    {
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "P-err1",
            PlanElements = [El(PlanType.Dispatch, Utc(2025, 9, 10))] // 0 учасників
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CreateAsync(vm, _ct));
    }

    // ====================== tests: Queries ======================

    [Fact(DisplayName = "GetAllPlansAsync: повертає відсортовано за RecordedUtc DESC")]
    public async Task GetAllPlans_Returns_InRecordedDesc()
    {
        // Arrange
        var p1 = new Plan { Id = Guid.NewGuid(), PlanNumber = "A", RecordedUtc = Utc(2025, 9, 10, 10) };
        var p2 = new Plan { Id = Guid.NewGuid(), PlanNumber = "B", RecordedUtc = Utc(2025, 9, 10, 12) };
        var p3 = new Plan { Id = Guid.NewGuid(), PlanNumber = "C", RecordedUtc = Utc(2025, 9, 10, 11) };
        _db.AddRange(p1, p2, p3);
        await _db.SaveChangesAsync(_ct);

        // Act
        var list = (await _svc.GetAllPlansAsync(_ct)).ToList();

        // Assert
        Assert.Equal([ "B", "C", "A" ], [.. list.Select(x => x.PlanNumber)]);
    }

    [Fact(DisplayName = "GetByIdAsync: повертає план з елементами та учасниками")]
    public async Task GetById_Returns_Plan_WithGraph()
    {
        // Arrange: створюємо план через сервіс, щоби мати коректну структуру
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "P-graph",
            PlanElements =
            [
                El(PlanType.Dispatch, Utc(2025,9,10,12), participants: [Snap(), Snap()]),
            ]
        };
        var saved = await _svc.CreateAsync(vm, _ct);

        // Act
        var found = await _svc.GetByIdAsync(saved.Id, _ct);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("P-graph", found!.PlanNumber);
        Assert.Single(found.PlanElements);
        Assert.Equal(2, found.PlanElements.First().Participants.Count);
    }

    [Fact(DisplayName = "GetByIdAsync: повертає null для відсутнього Id")]
    public async Task GetById_Returns_Null_IfMissing()
    {
        var missing = await _svc.GetByIdAsync(Guid.NewGuid(), _ct);
        Assert.Null(missing);
    }

    // ====================== tests: Update ======================

    [Fact(DisplayName = "UpdateIfOpenAsync: замінює всі елементи та їх учасників цілком")]
    public async Task UpdateIfOpen_Replaces_EntireGraph()
    {
        // Arrange: початковий план
        var initial = await _svc.CreateAsync(new CreatePlanViewModel
        {
            PlanNumber = "P-upd",
            PlanElements =
            [
                El(PlanType.Dispatch, Utc(2025,9,10,12), participants: [Snap()])
            ]
        }, _ct);

        // Incoming (нові дані)
        var incoming = new Plan
        {
            Id = initial.Id,
            PlanNumber = "P-upd-NEW",
            State = PlanState.Open,      // сервіс дозволяє лише відкриті
            Author = "editor",
            // Повністю новий набір елементів:
            PlanElements =
            [
                El(PlanType.Return, Utc(2025,9,11,8), "Локація X", "Група X", "Тип X", participants: [Snap(), Snap()]),
                El(PlanType.Dispatch, Utc(2025,9,12,9), "Локація Y", "Група Y", "Тип Y", participants: [Snap()])
            ]
        };

        // Act
        var ok = await _svc.UpdateIfOpenAsync(incoming, _ct);

        // Assert
        Assert.True(ok);

        var reloaded = await _db.Plans
            .Include(p => p.PlanElements)
            .ThenInclude(e => e.Participants)
            .FirstAsync(p => p.Id == initial.Id, _ct);

        Assert.Equal("P-upd-NEW", reloaded.PlanNumber);
        Assert.Equal(2, reloaded.PlanElements.Count);
        Assert.Equal([2, 1], [.. reloaded.PlanElements.OrderBy(x => x.EventAtUtc).Select(x => x.Participants.Count)]);
    }

    [Fact(DisplayName = "UpdateIfOpenAsync: відхиляє редагування закритого плану")]
    public async Task UpdateIfOpen_Throws_WhenClosed()
    {
        // Arrange
        var saved = await _svc.CreateAsync(new CreatePlanViewModel
        {
            PlanNumber = "P-closed",
            PlanElements = [El(PlanType.Dispatch, Utc(2025, 9, 10), participants: [Snap()])]
        }, _ct);

        // закриваємо
        var plan = await _db.Plans.FirstAsync(p => p.Id == saved.Id, _ct);
        plan.State = PlanState.Close;
        await _db.SaveChangesAsync(_ct);

        var incoming = new Plan
        {
            Id = saved.Id,
            PlanNumber = "P-closed-NEW",
            State = PlanState.Open,
            PlanElements = [El(PlanType.Return, Utc(2025, 9, 11), participants: [Snap()])]
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.UpdateIfOpenAsync(incoming, _ct));
    }

    // ====================== tests: Delete ======================

    [Fact(DisplayName = "DeleteIfOpenAsync: видаляє відкритий план разом з ієрархією")]
    public async Task DeleteIfOpen_DeletesPlan_AndGraph()
    {
        // Arrange
        var saved = await _svc.CreateAsync(new CreatePlanViewModel
        {
            PlanNumber = "P-del",
            PlanElements =
            [
                El(PlanType.Dispatch, Utc(2025,9,10,12), participants: [Snap(), Snap()])
            ]
        }, _ct);

        // Act
        var ok = await _svc.DeleteIfOpenAsync(saved.Id, _ct);

        // Assert
        Assert.True(ok);
        Assert.Null(await _db.Plans.FirstOrDefaultAsync(p => p.Id == saved.Id, _ct));
        Assert.False(await _db.PlanElements.AnyAsync(pe => pe.PlanId == saved.Id, _ct));
        Assert.False(await _db.PlanParticipantSnapshots.AnyAsync(_ct));
    }

    [Fact(DisplayName = "DeleteIfOpenAsync: відхиляє видалення закритого плану")]
    public async Task DeleteIfOpen_Throws_WhenClosed()
    {
        // Arrange
        var saved = await _svc.CreateAsync(new CreatePlanViewModel
        {
            PlanNumber = "P-del-closed",
            PlanElements = [El(PlanType.Dispatch, Utc(2025, 9, 10), participants: [Snap()])]
        }, _ct);

        var plan = await _db.Plans.FirstAsync(p => p.Id == saved.Id, _ct);
        plan.State = PlanState.Close;
        await _db.SaveChangesAsync(_ct);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.DeleteIfOpenAsync(saved.Id, _ct));
    }
}
