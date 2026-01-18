//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// OptimisticConcurrencyException
//-----------------------------------------------------------------------------

namespace eRaven.Exceptions;

public sealed class OptimisticConcurrencyException(Guid aggregateId, long expectedVersion, long actualVersion) : Exception($"Concurrency conflict for {aggregateId}. Expected version {expectedVersion}, actual {actualVersion}.")
{
    public Guid AggregateId { get; } = aggregateId;
    public long ExpectedVersion { get; } = expectedVersion;
    public long ActualVersion { get; } = actualVersion;
}