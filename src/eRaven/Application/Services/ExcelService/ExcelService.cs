//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ExcelService
//-----------------------------------------------------------------------------

using ClosedXML.Excel;
using eRaven.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace eRaven.Application.Services.ExcelService;

/// <summary>
/// Сервіс для експорту звітів в ексель
/// </summary>
public sealed class ExcelService : IExcelService
{
    public Task<Stream> ExportAsync<T>(IEnumerable<T> items, CancellationToken ct = default)
    {
        items ??= [];

        // Матеріалізуємо, щоб:
        //  - дізнатися довжини масивів (string[])
        //  - уникнути багаторазового перебирання
        var list = items.ToList();

        var wb = new XLWorkbook();

        // Назва аркуша: Display(Name) або назва типу; санітизуємо під Excel
        var sheetName = typeof(T).GetCustomAttribute<DisplayAttribute>()?.Name ?? typeof(T).Name;
        var ws = wb.Worksheets.Add(SanitizeSheetName(sheetName));

        // --- Виявляємо властивості ---
        // 1) скалярні (string, Guid, DateTime, числа, bool, enum)
        var props = GetMappableProps(typeof(T));

        // 2) масиви string[]
        var arrayProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType == typeof(string[]))
            .ToArray();

        // --- Шапка: спочатку скаляри, потім масиви (розгорнуті у 01..NN) ---
        int col = 1;

        // Словник: початкова колонка та довжина для кожного string[]-поля
        var arrayColumnStarts = new Dictionary<string, (int startCol, int length)>();

        // 1) Скалярні заголовки
        foreach (var p in props)
        {
            var header = p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name;
            var cell = ws.Cell(1, col++);
            cell.Value = header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }

        // 2) Масиви string[]: розгортаємо у 01..NN (NN = макс. довжина серед рядків)
        foreach (var ap in arrayProps)
        {
            int maxLen = 0;
            foreach (var row in list)
            {
                var arr = ap.GetValue(row) as string[] ?? [];
                if (arr.Length > maxLen) maxLen = arr.Length;
            }

            arrayColumnStarts[ap.Name] = (col, maxLen);

            if (maxLen > 0)
            {
                for (int i = 1; i <= maxLen; i++)
                {
                    var cell = ws.Cell(1, col++);
                    cell.Value = i.ToString("00");
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                }
            }
        }

        // --- Дані ---
        int r = 2;
        foreach (var item in list)
        {
            ct.ThrowIfCancellationRequested();

            int c = 1;

            // 1) Скалярні
            foreach (var p in props)
            {
                var val = p.GetValue(item);
                var cell = ws.Cell(r, c++);

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

            // 2) Масиви (string[])
            foreach (var ap in arrayProps)
            {
                var (startCol, length) = arrayColumnStarts[ap.Name];
                var arr = ap.GetValue(item) as string[] ?? [];

                for (int i = 0; i < length; i++)
                {
                    var cell = ws.Cell(r, startCol + i);
                    var s = (i < arr.Length) ? (arr[i] ?? string.Empty) : string.Empty;
                    cell.Value = s;
                }

                c = Math.Max(c, startCol + length);
            }

            r++;
        }

        var lastRow = Math.Max(1, r - 1);
        var lastCol = Math.Max(1, col - 1);

        // ==== Загальні стилі таблиці ====
        var used = ws.Range(1, 1, lastRow, lastCol);

        // Вирівнювання + переноси тексту
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        used.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        used.Style.Alignment.WrapText = true;

        // Рамки
        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        // «Зебра» з 3-го рядка (1 — шапка, 2 — перший рядок даних)
        if (lastRow >= 3)
        {
            for (int rr = 3; rr <= lastRow; rr += 2)
                ws.Range(rr, 1, rr, lastCol).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
        }

        // Фіксація шапки + автофільтр
        ws.SheetView.FreezeRows(1);
        if (lastRow >= 2)
            ws.Range(1, 1, lastRow, lastCol).SetAutoFilter();

        // Автопідбір ширин з обмеженнями
        for (int c = 1; c <= lastCol; c++)
        {
            var colRef = ws.Column(c);
            colRef.AdjustToContents();
            if (colRef.Width > 60) colRef.Width = 60; // максимум
            if (colRef.Width < 8) colRef.Width = 8;  // мінімум
        }

        // ===== Уніфікація висоти рядків даних =====
        if (lastRow >= 2)
        {
            // 1) Піджени висоту під контент, щоб врахувати переноси
            ws.Rows(2, lastRow).AdjustToContents();

            // 2) Знайди максимальну висоту серед рядків даних
            double maxDataHeight = 0;
            for (int rr2 = 2; rr2 <= lastRow; rr2++)
                if (ws.Row(rr2).Height > maxDataHeight) maxDataHeight = ws.Row(rr2).Height;

            // Мінімальна читабельна висота (≈18pt)
            if (maxDataHeight < 18) maxDataHeight = 18;

            // 3) Встанови однакову висоту всім рядкам даних
            for (int rr2 = 2; rr2 <= lastRow; rr2++)
                ws.Row(rr2).Height = maxDataHeight;

            // 4) Шапка — трохи нижча/звична
            ws.Row(1).AdjustToContents();
            if (ws.Row(1).Height < 18) ws.Row(1).Height = 18;
        }

        // Вивід у пам'ять
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return Task.FromResult<Stream>(ms);

        // --- локальний helper ---
        static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var ch in invalid) name = name.Replace(ch, ' ');
            return name.Length <= 31 ? name : name[..31];
        }
    }

    public async Task<(List<T> Rows, List<string> Errors)> ImportAsync<T>(Stream xlsx, CancellationToken ct = default) where T : new()
    {
        ArgumentNullException.ThrowIfNull(xlsx);

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

            if (prop != null && !colMap.ContainsValue(prop))
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
        [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite && IsScalar(p.PropertyType))];

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
            dict["SpecialNumber"] = ["ВОС"];
            dict["OrgPath"] = ["шлях", "структура", "підрозділ", "підрозділ/посада", "посада/ієрархія", "orgpath", "path"];
        }
        else if (t == typeof(Person))
        {
            dict["Rnokpp"] = ["іпн", "рнокпп", "rnokpp", "inn", "taxid"];
            dict["Rank"] = ["звання", "звание", "rank"];
            dict["LastName"] = ["прізвище", "фамилия", "surname", "last", "lastname"];
            dict["FirstName"] = ["ім'я", "имя", "firstname", "first", "name"];
            dict["MiddleName"] = ["по батькові", "отчество", "middlename", "middle"];
            dict["BZVP"] = ["бзвп"];
            dict["Weapon"] = ["зброя", "оружие", "weapon"];
            dict["Callsign"] = ["позивний", "позывной", "callsign", "call sign"];
            dict["PositionUnitId"] = ["id посади", "посада id", "unitid", "positionunitid", "position id", "posada id"];
            dict["StatusKindId"] = ["статус", "status", "statusid", "status kind id", "код статусу", "код статуса"];
        }
        else if (t == typeof(PersonStatus))
        {
            dict["PersonId"] = ["person id", "ід особи", "ідентифікатор особи", "id працівника", "worker id"];
            dict["StatusKindId"] = ["статус", "status", "statusid", "status kind id", "код статусу", "код статуса"];
            dict["OpenDate"] = ["fromdate", "from", "start", "open", "open date", "від", "з", "дата від", "відкрито"];
            dict["CloseDate"] = ["todate", "to", "end", "close", "close date", "до", "дата до", "закрито"];
            dict["Note"] = ["примітка", "замітка", "коментар", "note", "comment"];
            dict["Author"] = ["автор", "створив", "created by", "author"];
            dict["Modified"] = ["змінено", "modified", "updated at", "updated"];
            dict["Id"] = ["ід", "id", "row id"];
        }

        return dict;
    }
}
