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
            _dbh.Db.Persons.Add(new Person { Id = id });
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
