//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Seed
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Infrastructure;

internal static class Seed
{
    private static readonly DateTime SeedTimestamp = new(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    // ===============================
    // Статуси
    // ===============================
    public static readonly StatusKind[] AllStatusKind =
    [
        new() { Id = 1,  Name = "Рекрут",                   Code = "RECRUIT", Order = 0,   IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 2,  Name = "В районі",                 Code = "30",      Order = 10,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 3,  Name = "В БР",                     Code = "100",     Order = 100, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 4,  Name = "В БТГр",                   Code = "100",     Order = 100, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 5,  Name = "Розпорядження",            Code = "РОЗПОР",  Order = 40,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 6,  Name = "Переміщення",              Code = "нб",      Order = 50,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 7,  Name = "Звільнення",               Code = "нб",      Order = 50,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 8,  Name = "Відрядження",              Code = "ВДР",     Order = 80,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 9,  Name = "Фахове навчання",          Code = "ВДР",     Order = 80,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 10, Name = "Відпустка",                Code = "В",       Order = 90,  IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 11, Name = "Проходження ВЛК",          Code = "Л_Х",     Order = 120, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 12, Name = "Направлення на МСЕК",      Code = "Л_Х",     Order = 120, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 13, Name = "Лікування по хворобі",     Code = "Л_Х",     Order = 120, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 14, Name = "Лікування по пораненню",   Code = "Л_Б",     Order = 130, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 15, Name = "Безвісті",                 Code = "БВ",      Order = 170, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 16, Name = "Полон",                    Code = "П",       Order = 175, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 17, Name = "Арешт",                    Code = "А",       Order = 178, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 18, Name = "СЗЧ",                      Code = "СЗЧ",     Order = 180, IsActive = true, Author = "system", Modified = SeedTimestamp },
        new() { Id = 19, Name = "Загибель",                 Code = "200",     Order = 190, IsActive = true, Author = "system", Modified = SeedTimestamp },
    ];

    // ===============================
    // Переходи між статусами
    // ===============================
    public static List<StatusTransition> GetStatusTransitions()
    {
        var transitions = new List<StatusTransition>();
        var id = 1;

        void AddTransition(int fromId, params int[] toIds)
        {
            foreach (var toId in toIds)
            {
                if (fromId == toId) continue; // Без самопереходів

                transitions.Add(new StatusTransition
                {
                    Id = id++,
                    FromStatusKindId = fromId,
                    ToStatusKindId = toId
                });
            }
        }

        // Рекрут (1) -> тільки "В районі"
        AddTransition(1, 2);

        // В районі (2) -> всі статуси (крім себе і Рекрута)
        AddTransition(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19);

        // В БР (3) -> обмежені переходи
        AddTransition(3, 2, 11, 12, 13, 14, 15, 16, 18, 19);

        // В БТГр (4) -> як "В БР"
        AddTransition(4, 2, 11, 12, 13, 14, 15, 16, 18, 19);

        // Розпорядження (5) -> тільки "В районі"
        AddTransition(5, 2);

        // Переміщення (6) -> тільки "В районі"
        AddTransition(6, 2);

        // Звільнення (7) -> тільки "В районі"
        AddTransition(7, 2);

        // Відрядження (8) -> "В районі", СЗЧ
        AddTransition(8, 2, 18);

        // Фахове навчання (9) -> "В районі", СЗЧ
        AddTransition(9, 2, 18);

        // Відпустка (10) -> "В районі", СЗЧ
        AddTransition(10, 2, 18);

        // Медичні статуси (11-14) -> "В районі", "Розпорядження", СЗЧ
        AddTransition(11, 2, 5, 18);
        AddTransition(12, 2, 5, 18);
        AddTransition(13, 2, 5, 18);
        AddTransition(14, 2, 5, 18);

        // Безвісті (15) -> "В районі", "Розпорядження"
        AddTransition(15, 2, 5);

        // Полон (16) -> "В районі", "Розпорядження"
        AddTransition(16, 2, 5);

        // Арешт (17) -> "В районі", "Розпорядження"
        AddTransition(17, 2, 5);

        // СЗЧ (18) -> "В районі", "Розпорядження"
        AddTransition(18, 2, 5);

        // Загибель (19) -> "Розпорядження", Звільнення
        AddTransition(19, 5, 7);

        return transitions;
    }
}
