// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanServiceTests — інтеграційні тести сервісу на SQLite :memory:
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PlanServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper;
    private readonly PlanService _svc;

    public PlanServiceTests()
    {
        _helper = new SqliteDbHelper();
        _svc = new PlanService(_helper.Db);
    }

    public void Dispose() => _helper.Dispose();

    // --------------------------- helpers ---------------------------

    private static DateTime Utc(int y, int M, int d, int h, int m)
        => new(y, M, d, h, m, 0, DateTimeKind.Utc);

    private async Task<Person> SeedPersonAsync(string name = "Тест", string rnokpp = "1234567890")
    {
        var p = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = name,
            LastName = name,
            MiddleName = name,
            Rnokpp = rnokpp,
            Rank = "рядовий",
            Weapon = "АК",
            Callsign = "Тест"
            // PositionUnit/StatusKind можна не задавати — сервіс це допускає
        };

        _helper.Db.Persons.Add(p);
        await _helper.Db.SaveChangesAsync();
        return p;
    }

   
}
