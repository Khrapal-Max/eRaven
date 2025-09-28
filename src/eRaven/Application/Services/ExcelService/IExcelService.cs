//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IExcelService
//-----------------------------------------------------------------------------

namespace eRaven.Application.Services.ExcelService;

/// <summary>
/// Сервіс експорту звітів в ексель
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Експорт даних в ексель файл. Т тип
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="ct"></param>
    /// <returns>Stream(<see cref="Stream"/>)</returns>
    Task<Stream> ExportAsync<T>(IEnumerable<T> items, CancellationToken ct = default);

    /// <summary>
    /// Імпотр даних з файла. Зв'язаний з ExcelImporter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xlsx"></param>
    /// <param name="ct"></param>
    /// <returns>List T</returns>
    Task<(List<T> Rows, List<string> Errors)> ImportAsync<T>(Stream xlsx, CancellationToken ct = default) where T : new();
}
