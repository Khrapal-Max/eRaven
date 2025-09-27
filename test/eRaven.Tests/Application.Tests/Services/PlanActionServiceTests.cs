// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanActionServiceTests
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services
{
    public class PlanActionServiceTests : IDisposable
    {
        private readonly SqliteDbHelper _dbh;
        private readonly PlanActionService _svc;

        public PlanActionServiceTests()
        {
            _dbh = new SqliteDbHelper();
            _svc = new PlanActionService(_dbh.Factory);
        }

        public void Dispose() => GC.SuppressFinalize(this);

        // ---------- helpers ----------

        private async Task SeedPersonAsync(Guid id)
        {
            var rnokpp = $"T{id:N}"[..10];

            _dbh.Db.Persons.Add(new Person
            {
                Id = id,
                Rnokpp = rnokpp
            });
            await _dbh.Db.SaveChangesAsync();
        }

        private static PlanAction NewPlanAction(
            Guid? personId = null,
            DateTime? atUtc = null,
            MoveType move = MoveType.Dispatch,
            ActionState state = ActionState.PlanAction)
        {
            var pid = personId ?? Guid.NewGuid();
            var dt = atUtc ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            return new PlanAction
            {
                Id = Guid.NewGuid(),
                PersonId = pid,
                // Person навігацію свідомо НЕ встановлюємо — сервіс працює лише з FK

                PlanActionName = " R-001/24 ", // навмисно з пробілами — перевіряємо, що сервіс це НЕ трімить
                EffectiveAtUtc = dt,
                ToStatusKindId = 11,           // сервіс у CreateAsync обнуляє це поле
                Order = "  SHOULD_BE_NULL_AFTER_CREATE ", // сервіс у CreateAsync обнуляє це поле
                ActionState = state,

                MoveType = move,
                Location = "  Sector B  ",
                GroupName = "  Alpha  ",
                CrewName = "  Crew-1  ",
                Note = "  Note here  ",

                Rnokpp = "1234567890",
                FullName = "ПІБ Тест",
                RankName = "Сержант",
                PositionName = "Відділення",
                BZVP = "БЗ-42",
                Weapon = "АК",
                Callsign = "Сокіл",
                StatusKindOnDate = "В строю 21.09.2025 10:30"
            };
        }

        private async Task SeedAsync(params PlanAction[] actions)
        {
            _dbh.Db.PlanActions.AddRange(actions);
            await _dbh.Db.SaveChangesAsync();
        }

        // ---------- tests ----------

        // ------------------------ GetActiveDispatchOnDateAsync ------------------------

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: включає тих, чия остання дія ≤ atUtc — Dispatch; виключає тих, хто повернувся")]
        public async Task GetActiveDispatchOnDateAsync_Basic_Include_Dispatch_Exclude_Returned()
        {
            // Arrange
            var p1 = Guid.NewGuid(); // тільки Dispatch → має бути
            var p2 = Guid.NewGuid(); // Dispatch, але потім Return до дати → не має бути
            var p3 = Guid.NewGuid(); // тільки Return → не має бути

            await SeedPersonAsync(p1);
            await SeedPersonAsync(p2);
            await SeedPersonAsync(p3);

            var d = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);

            await SeedAsync(
                // p1: відряджений і не повернувся
                NewPlanAction(personId: p1, atUtc: d.AddDays(1), move: MoveType.Dispatch, state: ActionState.PlanAction),

                // p2: відряджений, але ПОВЕРНУВСЯ до дати звіту
                NewPlanAction(personId: p2, atUtc: d.AddDays(5), move: MoveType.Dispatch, state: ActionState.PlanAction),
                NewPlanAction(personId: p2, atUtc: d.AddDays(23), move: MoveType.Return, state: ActionState.PlanAction),

                // p3: тільки Return
                NewPlanAction(personId: p3, atUtc: d.AddDays(10), move: MoveType.Return, state: ActionState.PlanAction)
            );

            var atUtc = new DateTime(2025, 09, 24, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var res = await _svc.GetActiveDispatchOnDateAsync(atUtc);

            // Assert
            var list = res.ToList();
            Assert.Single(list);                 // лише p1
            Assert.Equal(p1, list[0].PersonId);  // p1 в результаті
        }

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: на більш ранню дату — до Return — особа ще в Dispatch")]
        public async Task GetActiveDispatchOnDateAsync_Person_In_Dispatch_Before_Return_Date()
        {
            // Arrange
            var p = Guid.NewGuid();
            await SeedPersonAsync(p);

            var d = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);

            await SeedAsync(
                NewPlanAction(personId: p, atUtc: d.AddDays(5), move: MoveType.Dispatch, state: ActionState.PlanAction),
                NewPlanAction(personId: p, atUtc: d.AddDays(23), move: MoveType.Return, state: ActionState.PlanAction)
            );

            var atUtc = new DateTime(2025, 09, 22, 12, 0, 0, DateTimeKind.Utc); // ДО повернення

            // Act
            var res = await _svc.GetActiveDispatchOnDateAsync(atUtc);

            // Assert
            var list = res.ToList();
            Assert.Single(list);
            Assert.Equal(p, list[0].PersonId);
            Assert.Equal(MoveType.Dispatch, list[0].MoveType);
        }

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: однаковий час — пріоритет ApprovedOrder над PlanAction")]
        public async Task GetActiveDispatchOnDateAsync_Prefers_ApprovedOrder_Over_PlanAction_At_Same_Time()
        {
            // Arrange
            var p = Guid.NewGuid();
            await SeedPersonAsync(p);

            var t = new DateTime(2025, 09, 10, 10, 0, 0, DateTimeKind.Utc);

            await SeedAsync(
                // Обидві — Dispatch, той самий час; має обрати ApprovedOrder
                NewPlanAction(personId: p, atUtc: t, move: MoveType.Dispatch, state: ActionState.PlanAction),
                NewPlanAction(personId: p, atUtc: t, move: MoveType.Dispatch, state: ActionState.ApprovedOrder)
            );

            // Act
            var res = await _svc.GetActiveDispatchOnDateAsync(t);

            // Assert
            var item = Assert.Single(res);
            Assert.Equal(p, item.PersonId);
            Assert.Equal(ActionState.ApprovedOrder, item.ActionState); // пріоритет
            Assert.Equal(MoveType.Dispatch, item.MoveType);
        }

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: сортування Location → Group → Crew → FullName (asc, case-insensitive)")]
        public async Task GetActiveDispatchOnDateAsync_Sorts_As_Specified()
        {
            // Arrange
            var p1 = Guid.NewGuid();
            var p2 = Guid.NewGuid();
            var p3 = Guid.NewGuid();
            var p4 = Guid.NewGuid();

            await SeedPersonAsync(p1);
            await SeedPersonAsync(p2);
            await SeedPersonAsync(p3);
            await SeedPersonAsync(p4);

            var t = new DateTime(2025, 09, 15, 0, 0, 0, DateTimeKind.Utc);

            // Робимо так, щоб остання дія для кожного — саме Dispatch
            var a1 = NewPlanAction(personId: p1, atUtc: t.AddMinutes(-5), move: MoveType.Dispatch, state: ActionState.ApprovedOrder);
            a1.Location = "Bravo"; a1.GroupName = "A"; a1.CrewName = "X"; a1.FullName = "Марко";

            var a2 = NewPlanAction(personId: p2, atUtc: t.AddMinutes(-4), move: MoveType.Dispatch, state: ActionState.ApprovedOrder);
            a2.Location = "Alpha"; a2.GroupName = "B"; a2.CrewName = "Y"; a2.FullName = "Андрій";

            var a3 = NewPlanAction(personId: p3, atUtc: t.AddMinutes(-3), move: MoveType.Dispatch, state: ActionState.ApprovedOrder);
            a3.Location = "Alpha"; a3.GroupName = "A"; a3.CrewName = "Z"; a3.FullName = "Богдан";

            var a4 = NewPlanAction(personId: p4, atUtc: t.AddMinutes(-2), move: MoveType.Dispatch, state: ActionState.ApprovedOrder);
            a4.Location = "Alpha"; a4.GroupName = "A"; a4.CrewName = "Y"; a4.FullName = "Віктор";

            await SeedAsync(a1, a2, a3, a4);

            // Act
            var res = (await _svc.GetActiveDispatchOnDateAsync(t.AddMinutes(1))).ToList();

            // Assert: за Location asc → Group asc → Crew asc → FullName asc
            Assert.Equal(4, res.Count);
            Assert.Collection(res,
                x => { Assert.Equal("Alpha", x.Location); Assert.Equal("A", x.GroupName); Assert.Equal("Y", x.CrewName); Assert.Equal("Віктор", x.FullName); },
                x => { Assert.Equal("Alpha", x.Location); Assert.Equal("A", x.GroupName); Assert.Equal("Z", x.CrewName); Assert.Equal("Богдан", x.FullName); },
                x => { Assert.Equal("Alpha", x.Location); Assert.Equal("B", x.GroupName); Assert.Equal("Y", x.CrewName); Assert.Equal("Андрій", x.FullName); },
                x => { Assert.Equal("Bravo", x.Location); Assert.Equal("A", x.GroupName); Assert.Equal("X", x.CrewName); Assert.Equal("Марко", x.FullName); }
            );
        }

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: включає події з часом, що дорівнює atUtc (inclusive)")]
        public async Task GetActiveDispatchOnDateAsync_Inclusive_Boundary_AtUtc()
        {
            // Arrange
            var p = Guid.NewGuid();
            await SeedPersonAsync(p);

            var at = new DateTime(2025, 09, 20, 12, 30, 0, DateTimeKind.Utc);
            await SeedAsync(
                NewPlanAction(personId: p, atUtc: at, move: MoveType.Dispatch, state: ActionState.PlanAction)
            );

            // Act
            var res = await _svc.GetActiveDispatchOnDateAsync(at);

            // Assert
            var item = Assert.Single(res);
            Assert.Equal(p, item.PersonId);
            Assert.Equal(at, item.EffectiveAtUtc);
            Assert.Equal(MoveType.Dispatch, item.MoveType);
        }

        [Fact(DisplayName = "GetActiveDispatchOnDateAsync: коли подій немає — повертає порожньо")]
        public async Task GetActiveDispatchOnDateAsync_Empty_When_No_Items()
        {
            // Arrange
            var at = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var res = await _svc.GetActiveDispatchOnDateAsync(at);

            // Assert
            Assert.NotNull(res);
            Assert.Empty(res);
        }


        [Fact]
        public async Task CreateAsync_Should_Insert_With_Trims_And_Nulls()
        {
            // Arrange
            var personId = Guid.NewGuid();
            await SeedPersonAsync(personId); // FK
            var pa = NewPlanAction(personId: personId);

            // Act
            var created = await _svc.CreateAsync(pa);

            // Assert
            var fromDb = await _dbh.Db.PlanActions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(created.Id, fromDb!.Id);

            // Trim’и: сервіс трімить Location/GroupName/CrewName/Note
            Assert.Equal("Sector B", fromDb.Location);
            Assert.Equal("Alpha", fromDb.GroupName);
            Assert.Equal("Crew-1", fromDb.CrewName);
            Assert.Equal("Note here", fromDb.Note);

            // Політика null’ів у CreateAsync
            Assert.Null(fromDb.Order);
            Assert.Null(fromDb.ToStatusKindId);

            // Інші поля залишилися як задано
            Assert.Equal(pa.EffectiveAtUtc, fromDb.EffectiveAtUtc);
            Assert.Equal(pa.ActionState, fromDb.ActionState);
            Assert.Equal(pa.MoveType, fromDb.MoveType);
            Assert.Equal(pa.PlanActionName, fromDb.PlanActionName); // сервіс не трімить
            Assert.Equal(personId, fromDb.PersonId);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Up_To_150_Sorted_By_Date_Desc()
        {
            // Arrange
            var personId = Guid.NewGuid();
            await SeedPersonAsync(personId);
            var start = DateTime.SpecifyKind(new DateTime(2025, 09, 01, 0, 0, 0), DateTimeKind.Utc);

            var many = new List<PlanAction>();
            // 200 записів для однієї особи
            for (int i = 0; i < 200; i++)
            {
                many.Add(NewPlanAction(
                    personId: personId,
                    atUtc: start.AddMinutes(i), // зростаюча дата
                    move: i % 2 == 0 ? MoveType.Dispatch : MoveType.Return));
            }
            await SeedAsync([.. many]);

            // Act
            var result = await _svc.GetByIdAsync(personId, limit: 150);

            // Assert
            var list = result.ToList();
            Assert.Equal(150, list.Count);

            // Сортування DESC: перший — найпізніший
            var expectedTop = start.AddMinutes(199);
            Assert.Equal(expectedTop, list[0]!.EffectiveAtUtc);

            // останній у вибірці має бути 199 - 149 = 50
            var expectedBottom = start.AddMinutes(50);
            Assert.Equal(expectedBottom, list[^1]!.EffectiveAtUtc);
        }

        [Fact]
        public async Task ApproveAsync_Should_Set_State_To_Approved_And_Set_Order()
        {
            // Arrange
            var personId = Guid.NewGuid();
            await SeedPersonAsync(personId);
            var pa = NewPlanAction(personId: personId, state: ActionState.PlanAction);
            await SeedAsync(pa);

            var vm = new ApprovePlanActionViewModel
            {
                Id = pa.Id,
                Order = "БР-77/25"
            };

            // Act
            var updated = await _svc.ApproveAsync(vm);

            // Assert
            Assert.Equal(pa.Id, updated.Id);
            Assert.Equal(ActionState.ApprovedOrder, updated.ActionState);
            Assert.Equal("БР-77/25", updated.Order);

            // Перевірка фактичного стану в БД
            var fromDb = await _dbh.Db.PlanActions.AsNoTracking().FirstAsync(x => x.Id == pa.Id);
            Assert.Equal(ActionState.ApprovedOrder, fromDb.ActionState);
            Assert.Equal("БР-77/25", fromDb.Order);
        }

        [Fact]
        public async Task ApproveAsync_Should_Throw_If_Not_In_PlanAction_State()
        {
            // Arrange
            var personId = Guid.NewGuid();
            await SeedPersonAsync(personId);
            var pa = NewPlanAction(personId: personId, state: ActionState.ApprovedOrder);
            await SeedAsync(pa);

            var vm = new ApprovePlanActionViewModel
            {
                Id = pa.Id,
                Order = "БР-1/25"
            };

            // Act
            Task<PlanAction> act() => _svc.ApproveAsync(vm);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>((Func<Task<PlanAction>>)act);
        }

        [Fact]
        public async Task DeleteAsync_Should_Remove_Entity_And_Return_True()
        {
            // Arrange
            var personId = Guid.NewGuid();
            await SeedPersonAsync(personId);
            var pa = NewPlanAction(personId: personId, state: ActionState.PlanAction);
            await SeedAsync(pa);

            // Act
            var ok = await _svc.DeleteAsync(pa.Id);

            // Assert
            Assert.True(ok);
            var exists = await _dbh.Db.PlanActions.AsNoTracking().AnyAsync(x => x.Id == pa.Id);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_False_If_Not_Exists()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var ok = await _svc.DeleteAsync(id);

            // Assert
            Assert.False(ok);
        }
    }
}
