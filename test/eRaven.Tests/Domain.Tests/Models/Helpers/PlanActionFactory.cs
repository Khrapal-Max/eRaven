//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionFactory
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models.Helpers;

internal class PlanActionFactory
{
    public static PlanAction CreateValid(
            Guid? id = null,
            Guid? personId = null,
            MoveType move = MoveType.Dispatch,
            DateTime? effectiveUtc = null)
    {
        // Arrange (factory): підготовка валідного екземпляра
        var nowUtc = effectiveUtc ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var pid = personId ?? Guid.NewGuid();

        var pa = new PlanAction
        {
            Id = id ?? Guid.NewGuid(),
            PersonId = pid,
            Person = new Person { Id = pid },  // заглушка, щоб не тягнути весь домен
            PlanActionName = "R-001/24",
            EffectiveAtUtc = nowUtc,
            ToStatusKindId = 1,
            Order = null,
            ActionState = ActionState.PlanAction,

            MoveType = move,
            Location = "Сектор Б",
            GroupName = "Група Альфа",
            CrewName = "Екіпаж 1",
            Note = "Попередня розвідка",

            Rnokpp = "1234567890",
            FullName = "Іваненко Іван Іванович",
            RankName = "Сержант",
            PositionName = "Відділення Звʼязку",
            BZVP = "БЗ-42",
            Weapon = "АК-74",
            Callsign = "Сокіл",
            StatusKindOnDate = "В строю 21.09.2025 10:30"
        };

        return pa;
    }
}
