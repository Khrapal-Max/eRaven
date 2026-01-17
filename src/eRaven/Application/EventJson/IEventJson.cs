//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IEventJson
//-----------------------------------------------------------------------------

namespace eRaven.Application.EventJson;

public interface IEventJson
{
    T? TryDeserialize<T>(string? json);
}
