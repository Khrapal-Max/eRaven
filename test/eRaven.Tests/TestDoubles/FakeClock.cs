using System;
using eRaven.Application.Services.Clock;

namespace eRaven.Tests.TestDoubles;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
