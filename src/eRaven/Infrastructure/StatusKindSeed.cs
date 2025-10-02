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
    private static readonly DateTime SeedTs = new(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);

    public static readonly StatusKind[] AllStatusKind =
    [
        new() { Id = 1,  Name = "Рекрут",              Code = "нб",    Order = 0,   IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 2,  Name = "В районі",            Code = "30",    Order = 10,   IsActive = true, Author="system", Modified = SeedTs },

        new() { Id = 3,  Name = "В БР",                Code = "100",   Order = 100,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 4,  Name = "В БТГр",              Code = "100",   Order = 100,  IsActive = true, Author="system", Modified = SeedTs },

        new() { Id = 5,  Name = "Розпорядження",       Code = "РОЗПОР",Order = 40,   IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 6,  Name = "Переміщення",         Code = "нб",    Order = 50,   IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 7,  Name = "Звільнення",          Code = "нб",    Order = 50,   IsActive = true, Author="system", Modified = SeedTs },

        new() { Id = 8,  Name = "Відрядження",         Code = "ВДР",   Order = 80,   IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 9,  Name = "Фахове навчання",     Code = "ВДР",   Order = 80,   IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 10,  Name = "Відпустка",           Code = "В",     Order = 90,   IsActive = true, Author="system", Modified = SeedTs },

        new() { Id = 11, Name = "Проходження ВЛК",     Code = "Л_Х",   Order = 120,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 12, Name = "Направлення на МСЕК", Code = "Л_Х",   Order = 120,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 13, Name = "Лікування по хворобі",Code = "Л_Х",   Order = 120,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 14, Name = "Лікування по пораненню", Code = "Л_Б",Order = 130,  IsActive = true, Author="system", Modified = SeedTs },

        new() { Id = 15, Name = "Безвісті",            Code = "БВ",    Order = 170,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 16, Name = "Полон",               Code = "П",     Order = 175,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 17, Name = "Арешт",               Code = "А",     Order = 178,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 18, Name = "СЗЧ",                 Code = "СЗЧ",   Order = 180,  IsActive = true, Author="system", Modified = SeedTs },
        new() { Id = 19, Name = "Загибель",            Code = "200",   Order = 190,  IsActive = true, Author="system", Modified = SeedTs },
    ];


    public static List<StatusTransition> GetStatus()
    {
        var id = 1;
        var edges = new List<StatusTransition>();
        var seen = new HashSet<(int from, int to)>();

        void Edge(int from, params int[] tos)
        {
            foreach (var t in tos)
            {
                if (from == t) continue;                  // без самопереходів
                var key = (from, t);
                if (seen.Add(key))                        // додаємо лише унікальні
                {
                    edges.Add(new StatusTransition
                    {
                        Id = id++,
                        FromStatusKindId = from,
                        ToStatusKindId = t
                    });
                }
            }
        }

        // В районі -> дозволено все (крім самого себе)
        Edge(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);

        // В БР (2) -> тільки: В районі, Медичні, Безвісті, Полон, Загибель, СЗЧ
        Edge(2, 1, 12, 13, 14, 15, 16, 18);

        // В БТГр (3) -> як В БР (ВАЖЛИВО: було 2, має бути 3)
        Edge(3, 1, 12, 13, 14, 15, 16, 18);

        // Переміщення/Звільнення/Розпорядження -> тільки повернення "В районі"
        Edge(4, 1);
        Edge(5, 1);
        Edge(6, 1);

        // Відрядження/Фах. навчання/Відпустка -> "В районі", СЗЧ
        Edge(7, 1, 18);
        Edge(8, 1, 18);
        Edge(9, 1, 18);

        // Медичні (ВЛК, МСЕК, хвороба, поранення) -> "В районі", "Розпорядження", СЗЧ
        Edge(10, 1, 6, 18);
        Edge(11, 1, 6, 18);
        Edge(12, 1, 6, 18);
        Edge(13, 1, 6, 18);

        // Безвісті/Полон/СЗЧ/Арешт -> "В районі", "Розпорядження"
        Edge(14, 1, 6);
        Edge(15, 1, 6);
        Edge(18, 1, 6);
        Edge(17, 1, 6);

        // Загибель -> "Розпорядження"/Звільнення
        Edge(16, 5, 6);

        return edges;
    }
}
