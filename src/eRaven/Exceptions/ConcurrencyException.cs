//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConcurrencyException
//-----------------------------------------------------------------------------

namespace eRaven.Exceptions;

public sealed class ConcurrencyException(string message) : Exception(message)
{
}
