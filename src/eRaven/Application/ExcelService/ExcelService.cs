//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ExcelService
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace eRaven.Application.ExcelService;

/// <summary>
/// Сервіс для експорту звітів в ексель
/// </summary>
/// <param name="db"></param>
public sealed class ExcelService : IExcelService
{
    public Task<Stream> ExportAsync<T>(IEnumerable<T> items, CancellationToken ct = default)
    {
        items ??= [];

        var props = GetMappableProps(typeof(T)); // лишаємо твою логіку вибору властивостей
                                                 // якщо треба поважати Display(Order) – це можна зробити тут:
                                                 // props = props.OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue)
                                                 //              .ThenBy(p => p.Name)
                                                 //              .ToArray();

        var wb = new XLWorkbook();

        // назва аркуша: Display(Name) на типі T -> Type.Name; санітизуємо під Excel (<=31 символ, без :\/?*[])
        var sheetName = typeof(T).GetCustomAttribute<DisplayAttribute>()?.Name ?? typeof(T).Name;
        var ws = wb.Worksheets.Add(SanitizeSheetName(sheetName));

        // Заголовки з Display(Name) на властивостях (fallback: Property.Name)
        for (int c = 0; c < props.Length; c++)
        {
            var header = props[c].GetCustomAttribute<DisplayAttribute>()?.Name ?? props[c].Name;
            ws.Cell(1, c + 1).Value = header;
        }

        // Рядки
        int r = 2;
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            for (int c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(item);
                var cell = ws.Cell(r, c + 1);

                if (val is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                }
                else
                {
                    cell.Value = val?.ToString() ?? string.Empty;
                }
            }
            r++;
        }

        var lastRow = Math.Max(1, r - 1);

        // Стилі: вертикальне вирівнювання + перенос тексту для всієї використаної області
        var used = ws.Range(1, 1, lastRow, Math.Max(1, props.Length));
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        used.Style.Alignment.WrapText = true;

        // Фіксуємо перший рядок (шапку)
        ws.SheetView.FreezeRows(1);

        // Автофільтр (ставимо, якщо є хоча б 1 рядок даних під шапкою)
        if (lastRow >= 2)
            ws.Range(1, 1, lastRow, props.Length).SetAutoFilter();

        // Автопідбір ширин
        ws.Columns(1, props.Length).AdjustToContents();

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return Task.FromResult<Stream>(ms);

        // --- helpers ---
        static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var ch in invalid) name = name.Replace(ch, ' ');
            return name.Length <= 31 ? name : name.Substring(0, 31);
        }
    }

    public async Task<(List<T> Rows, List<string> Errors)> ImportAsync<T>(Stream xlsx, CancellationToken ct = default) where T : new()
    {
        if (xlsx is null) throw new ArgumentNullException(nameof(xlsx));

        // 1) Буферизуємо у пам’ять — це обходить заборону sync-читань BrowserFileStream
        using var ms = new MemoryStream();
        await xlsx.CopyToAsync(ms, ct);
        ms.Position = 0;

        // 2) Працюємо вже з MemoryStream
        using var wb = new XLWorkbook(ms); // можна XLEventTracking.Disabled для швидкості
        var ws = wb.Worksheets.Worksheet(1);

        var rows = new List<T>();
        var errors = new List<string>();

        var used = ws.RangeUsed();
        if (used is null) return (rows, errors);

        int headerRow = used.RangeAddress.FirstAddress.RowNumber;
        var header = ws.Row(headerRow);

        var props = GetMappableProps(typeof(T));
        var synonyms = GetSynonymsForType(typeof(T));
        var colMap = new Dictionary<int, PropertyInfo>();

        foreach (var cell in header.CellsUsed())
        {
            var norm = NormalizeHeader(cell.GetString());

            // 1) точна відповідність імені властивості
            var prop = props.FirstOrDefault(p => NormalizeHeader(p.Name) == norm);

            // 2) точна відповідність синонімів
            if (prop is null && synonyms.Count > 0)
            {
                foreach (var kv in synonyms)
                {
                    if (NormalizeHeader(kv.Key) == norm || kv.Value.Any(s => NormalizeHeader(s) == norm))
                    {
                        prop = props.FirstOrDefault(p => p.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                        if (prop != null) break;
                    }
                }
            }

            // 3) часткові входження (last resort)
            if (prop is null)
            {
                prop = props.FirstOrDefault(p => norm.Contains(NormalizeHeader(p.Name)));
                if (prop is null && synonyms.Count > 0)
                {
                    foreach (var kv in synonyms)
                    {
                        if (norm.Contains(NormalizeHeader(kv.Key)) || kv.Value.Any(s => norm.Contains(NormalizeHeader(s))))
                        {
                            prop = props.FirstOrDefault(p => p.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                            if (prop != null) break;
                        }
                    }
                }
            }

            if (prop != null && !colMap.Values.Contains(prop))
                colMap[cell.Address.ColumnNumber] = prop;
        }

        int firstDataRow = headerRow + 1;
        foreach (var rr in used.RowsUsed().Where(r => r.RowNumber() >= firstDataRow))
        {
            ct.ThrowIfCancellationRequested();

            var inst = new T();

            foreach (var kv in colMap)
            {
                var col = kv.Key;
                var prop = kv.Value;
                var cell = rr.Cell(col);

                try
                {
                    var value = ConvertCell(cell, prop.PropertyType);
                    // якщо null і властивість nullable — норм; інакше залишаємо дефолт
                    if (value != null || IsNullable(prop))
                        prop.SetValue(inst, value);
                    else if (!prop.PropertyType.IsValueType)
                        prop.SetValue(inst, null);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rr.RowNumber()}, Col {col} → {prop.Name}: {ex.Message}");
                }
            }

            rows.Add(inst);
        }

        return (rows, errors);
    }

    // ================= helpers =================

    private static PropertyInfo[] GetMappableProps(Type t) =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
         .Where(p => p.CanRead && p.CanWrite && IsScalar(p.PropertyType))
         .ToArray();

    private static bool IsScalar(Type t)
    {
        var nt = Nullable.GetUnderlyingType(t) ?? t;
        if (nt.IsEnum) return true;

        return nt == typeof(string)
            || nt == typeof(Guid)
            || nt == typeof(DateTime)
            || nt == typeof(bool)
            || nt == typeof(byte) || nt == typeof(short) || nt == typeof(int) || nt == typeof(long)
            || nt == typeof(float) || nt == typeof(double) || nt == typeof(decimal);
    }

    private static bool IsNullable(PropertyInfo p) =>
        Nullable.GetUnderlyingType(p.PropertyType) != null || !p.PropertyType.IsValueType;

    private static object? ConvertCell(IXLCell cell, Type targetType)
    {
        var isNullable = Nullable.GetUnderlyingType(targetType) != null;
        var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (cell.IsEmpty())
            return isNullable ? null : GetDefault(t);

        // DateTime
        if (t == typeof(DateTime))
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            var s = cell.GetString();
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
            if (DateTime.TryParse(s, out dt))
                return dt;

            throw new FormatException("Invalid DateTime");
        }

        // Guid
        if (t == typeof(Guid))
        {
            var s = cell.GetString().Trim();
            return Guid.Parse(s);
        }

        // bool
        if (t == typeof(bool))
        {
            if (cell.DataType == XLDataType.Boolean) return cell.GetBoolean();

            var s = cell.GetString().Trim();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1" || s == "так" || s == "yes")
                return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0" || s == "ні" || s == "no")
                return false;

            throw new FormatException("Invalid Boolean");
        }

        // enum
        if (t.IsEnum)
        {
            var s = cell.GetString().Trim();
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumInt))
                return Enum.ToObject(t, enumInt);

            return Enum.Parse(t, s, ignoreCase: true);
        }

        // integer-like
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
        {
            var dbl = cell.TryGetValue<double>(out var num)
                ? num
                : double.Parse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture);

            if (t == typeof(int)) return (int)Math.Round(dbl);
            if (t == typeof(long)) return (long)Math.Round(dbl);
            if (t == typeof(short)) return (short)Math.Round(dbl);
            if (t == typeof(byte)) return (byte)Math.Round(dbl);
        }

        // decimals/floats
        if (t == typeof(decimal))
        {
            if (cell.TryGetValue<decimal>(out var dec)) return dec;
            return decimal.Parse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        if (t == typeof(double))
        {
            if (cell.TryGetValue<double>(out var d)) return d;
            return double.Parse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        if (t == typeof(float))
        {
            if (cell.TryGetValue<double>(out var d2)) return (float)d2;
            return float.Parse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        
        // string
        if (t == typeof(string))
            return cell.GetString()?.Trim();

        // fallback
        return cell.GetString();
    }

    private static object? GetDefault(Type t) =>
        t.IsValueType ? Activator.CreateInstance(t) : null;

    private static string NormalizeHeader(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim();
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        s = Regex.Replace(s, @"\s+", " ");
        s = s.ToLowerInvariant();
        s = s.Replace("’", "").Replace("'", "").Replace("`", "").Replace("\"", "");
        s = s.Replace(" ", "").Replace("_", "");
        return s;
    }

    private static Dictionary<string, string[]> GetSynonymsForType(Type t)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (t == typeof(PositionUnit))
        {
            dict["Code"] = ["індекс", "код", "index", "code"];
            dict["ShortName"] = ["посада", "должность", "роль", "shortname", "short"];
            dict["OrgPath"] = ["шлях", "структура", "підрозділ", "підрозділ/посада", "посада/ієрархія", "orgpath", "path"];
            // за потреби можна додати: dict["IsActived"] = ["активний", "isactive", "enabled"];
        }
        else if (t == typeof(Person))
        {
            // Скаляри з нашої моделі Person:
            // Rnokpp, Rank, LastName, FirstName, MiddleName, BZVP, Weapon, Callsign,
            // PositionUnitId (Guid?), StatusKindId (int)
            dict["Rnokpp"] = ["іпн", "рнокпп", "rnokpp", "inn", "taxid"];
            dict["Rank"] = ["звання", "звание", "rank"];
            dict["LastName"] = ["прізвище", "фамилия", "surname", "last", "lastname"];
            dict["FirstName"] = ["ім'я", "имя", "firstname", "first", "name"];
            dict["MiddleName"] = ["по батькові", "отчество", "middlename", "middle"];
            dict["BZVP"] = ["бзвп"];
            dict["Weapon"] = ["зброя", "оружие", "weapon"];
            dict["Callsign"] = ["позивний", "позывной", "callsign", "call sign"];

            // УВАГА: тут очікується GUID посади (PositionUnit.Id). Якщо треба імпортувати за назвою — це окрема логіка з довідником.
            dict["PositionUnitId"] = ["id посади", "посада id", "unitid", "positionunitid", "position id", "posada id"];

            // Якщо у файлі дають саме ID/код статусу — мапимо сюди.
            // Імпорт за назвою статусу можливий тільки з доступом до довідника (не в цьому сервісі).
            dict["StatusKindId"] = ["статус", "status", "statusid", "status kind id", "код статусу", "код статуса"];
        }

        return dict;
    }
}