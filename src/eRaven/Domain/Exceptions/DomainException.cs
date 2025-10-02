//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DomainException
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Exceptions;

public class DomainException(string message) : Exception(message)
{
}