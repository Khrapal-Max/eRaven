using System;

namespace eRaven.Application.Services.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
}
