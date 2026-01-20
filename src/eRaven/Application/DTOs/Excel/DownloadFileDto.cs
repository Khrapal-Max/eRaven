//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DownloadFileDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Excel;

public sealed record DownloadFileDto(
    string FileName,
    string ContentType,
    string Base64
);