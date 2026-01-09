//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IEffectiveDatedEvent
//-----------------------------------------------------------------------------

namespace eRaven.Domain;

public interface IEffectiveDatedEvent
{
    DateOnly EffectiveDate { get; }
}
