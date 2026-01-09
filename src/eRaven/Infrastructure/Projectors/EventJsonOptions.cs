//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EventJsonOptions
//-----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace eRaven.Infrastructure.Projectors;

public static class EventJsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
