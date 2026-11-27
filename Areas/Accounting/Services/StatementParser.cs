using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Accounting_Register_BE.Areas.Accounting.Services
{
    /// <summary>How the columns of a bank CSV map to statement fields. Column indexes are zero-based.</summary>
    public class CsvMapping
    {
        public bool HasHeader { get; set; }
        public int DateColumn { get; set; }
        /// <summary>One or more columns joined with " - " (e.g. NAB "Transaction Type" + "Transaction Details").</summary>
        public List<int> DescriptionColumns { get; set; } = new();
        public int? ReferenceColumn { get; set; }
        /// <summary>"single": one signed amount column. "split": separate money-out (debit) and money-in (credit) columns.</summary>
        public string AmountMode { get; set; } = "single";
        public int? AmountColumn { get; set; }
        public int? DebitColumn { get; set; }
        public int? CreditColumn { get; set; }
        public int? BalanceColumn { get; set; }
        /// <summary>.NET date format (e.g. "dd/MM/yyyy"). Null = auto-detect, day-first (Australian) formats preferred.</summary>
        public string? DateFormat { get; set; }
        /// <summary>For banks that export withdrawals as positive numbers in a single column.</summary>
        public bool InvertSign { get; set; }
    }

    public record ParsedLine(int RowNumber, DateOnly Date, string Description, string? Reference, decimal Amount, decimal? Balance, string? ExternalId);

    public record ParseError(int RowNumber, string Message);

    public record ParseResult(List<ParsedLine> Lines, List<ParseError> Errors);

    public record StatementPreview(string Format, List<string>? Headers, List<List<string>> Rows, int RowCount, CsvMapping? SuggestedMapping, List<ParsedLine> Sample, int ParsedCount, List<ParseError> Errors);

    public static class StatementParser
    {
        public const int MaxFileBytes = 5 * 1024 * 1024;

        private static readonly string[] DayFirstFormats =
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd",
            "dd MMM yyyy", "d MMM yyyy", "dd MMM yy", "d MMM yy", "dd-MMM-yyyy", "d-MMM-yyyy", "dd-MMM-yy",
            "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-ddTHH:mm:ss",
        };

        public static string ReadText(byte[] bytes)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        public static string DetectFormat(string fileName, string content)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext is ".ofx" or ".qfx") return "ofx";
            var head = content.Length > 2000 ? content[..2000] : content;
            if (head.Contains("<OFX>", StringComparison.OrdinalIgnoreCase) || head.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase)) return "ofx";
            return "csv";
        }

        // ---------------------------------------------------------------- preview

        public static StatementPreview Preview(string format, string content, CsvMapping? savedMapping, bool force = false)
        {
            if (format == "ofx")
            {
                var ofx = ParseOfx(content);
                return new StatementPreview("ofx", null, new(), ofx.Lines.Count, null, ofx.Lines.Take(10).ToList(), ofx.Lines.Count, ofx.Errors.Take(20).ToList());
            }

            var rows = ReadCsv(content);
            var mapping = savedMapping != null && (force || IsMappingCompatible(savedMapping, rows)) ? savedMapping : SuggestMapping(rows);
            var headers = mapping.HasHeader && rows.Count > 0 ? rows[0] : null;
            var dataRows = mapping.HasHeader ? rows.Skip(1).ToList() : rows;
            var parsed = ParseCsv(rows, mapping);
            return new StatementPreview("csv", headers, dataRows.Take(10).ToList(), dataRows.Count, mapping, parsed.Lines.Take(10).ToList(), parsed.Lines.Count, parsed.Errors.Take(20).ToList());
        }

        private static bool IsMappingCompatible(CsvMapping m, List<List<string>> rows)
        {
            if (rows.Count == 0) return false;
            var width = rows.Max(r => r.Count);
            var cols = new List<int?> { m.DateColumn, m.ReferenceColumn, m.AmountColumn, m.DebitColumn, m.CreditColumn, m.BalanceColumn };
            cols.AddRange(m.DescriptionColumns.Select(x => (int?)x));
            return cols.All(c => c is null || c < width) && ParseCsv(rows, m).Lines.Count > 0;
        }

        /// <summary>Guesses the mapping from header names, or from the cell contents when there is no header row.</summary>
        public static CsvMapping SuggestMapping(List<List<string>> rows)
        {
            var m = new CsvMapping();
            if (rows.Count == 0) return m;

            var width = rows.Max(r => r.Count);
            var probe = rows.Skip(1).Take(30).ToList();
            if (probe.Count == 0) probe = rows.Take(1).ToList();

            var first = rows[0];
            m.HasHeader = rows.Count > 1 && !first.Any(c => TryParseDate(c, null, out _)) && probe.Any(r => r.Any(c => TryParseDate(c, null, out _)));
            var data = m.HasHeader ? rows.Skip(1).Take(30).ToList() : rows.Take(30).ToList();

            bool IsDateCol(int i) => data.Count(r => i < r.Count && TryParseDate(r[i], null, out _)) >= Math.Max(1, data.Count * 0.8);
            bool IsNumCol(int i) => data.Count(r => i < r.Count && (string.IsNullOrWhiteSpace(r[i]) || TryParseAmount(r[i], out _))) >= data.Count * 0.9
                                    && data.Any(r => i < r.Count && TryParseAmount(r[i], out _));

            int? Find(params string[] names) => FindHeader(false, names);
            int? FindExact(params string[] names) => FindHeader(true, names);
            int? FindHeader(bool exactOnly, string[] names)
            {
                if (!m.HasHeader) return null;
                for (var i = 0; i < first.Count; i++)
                {
                    var h = first[i].Trim().ToLowerInvariant();
                    if (names.Any(n => h == n)) return i;
                }
                if (exactOnly) return null;
                for (var i = 0; i < first.Count; i++)
                {
                    var h = first[i].Trim().ToLowerInvariant();
                    if (names.Any(n => h.Contains(n))) return i;
                }
                return null;
            }

            var date = Find("date", "fecha", "transaction date", "posted date", "posting date");
            m.DateColumn = date ?? Enumerable.Range(0, width).FirstOrDefault(IsDateCol);

            var debit = Find("debit amount", "debit", "withdrawal", "withdrawals", "money out", "cargo");
            var credit = Find("credit amount", "credit", "deposit", "deposits", "money in", "abono");
            var amount = FindExact("amount", "importe", "monto") ?? (debit is not null && credit is not null ? null : Find("amount", "importe", "monto"));
            var balance = Find("balance", "saldo");
            var reference = Find("reference", "ref", "serial", "cheque", "referencia");
            var description = Find("transaction details", "narrative", "description", "details", "descripción", "descripcion", "memo", "payee");

            var numeric = Enumerable.Range(0, width).Where(i => i != m.DateColumn && IsNumCol(i)).ToList();

            if (amount is null && debit is not null && credit is not null)
            {
                m.AmountMode = "split";
                m.DebitColumn = debit;
                m.CreditColumn = credit;
            }
            else
            {
                m.AmountMode = "single";
                m.AmountColumn = amount ?? numeric.FirstOrDefault(i => i != balance);
            }

            // Without a header, a second numeric column after the amount is almost always the running balance (CommBank).
            m.BalanceColumn = balance ?? (m.HasHeader ? null : numeric.Where(i => i != m.AmountColumn).Cast<int?>().LastOrDefault());
            m.ReferenceColumn = reference;

            if (description is not null)
            {
                m.DescriptionColumns = new() { description.Value };
                var type = Find("transaction type", "type", "tipo");
                if (type is not null && type != description && m.HasHeader) m.DescriptionColumns.Insert(0, type.Value);
            }
            else
            {
                var used = new HashSet<int?> { m.DateColumn, m.AmountColumn, m.DebitColumn, m.CreditColumn, m.BalanceColumn, m.ReferenceColumn };
                var best = Enumerable.Range(0, width)
                    .Where(i => !used.Contains(i) && !numeric.Contains(i))
                    .OrderByDescending(i => data.Average(r => i < r.Count ? r[i].Length : 0))
                    .Cast<int?>().FirstOrDefault();
                if (best is not null) m.DescriptionColumns = new() { best.Value };
            }

            return m;
        }

        // ---------------------------------------------------------------- CSV

        public static ParseResult ParseCsv(string content, CsvMapping mapping) => ParseCsv(ReadCsv(content), mapping);

        public static ParseResult ParseCsv(List<List<string>> rows, CsvMapping m)
        {
            var lines = new List<ParsedLine>();
            var errors = new List<ParseError>();

            for (var r = m.HasHeader ? 1 : 0; r < rows.Count; r++)
            {
                var row = rows[r];
                var rowNo = r + 1;
                string Cell(int? i) => i is int idx && idx >= 0 && idx < row.Count ? row[idx].Trim() : string.Empty;

                if (row.All(string.IsNullOrWhiteSpace)) continue;

                if (!TryParseDate(Cell(m.DateColumn), m.DateFormat, out var date))
                {
                    errors.Add(new ParseError(rowNo, $"Fecha no válida: \"{Cell(m.DateColumn)}\""));
                    continue;
                }

                decimal amount;
                if (m.AmountMode == "split")
                {
                    var outOk = TryParseAmount(Cell(m.DebitColumn), out var moneyOut);
                    var inOk = TryParseAmount(Cell(m.CreditColumn), out var moneyIn);
                    if (!outOk && !inOk)
                    {
                        errors.Add(new ParseError(rowNo, "Sin monto de cargo ni de abono"));
                        continue;
                    }
                    amount = Math.Abs(inOk ? moneyIn : 0) - Math.Abs(outOk ? moneyOut : 0);
                }
                else if (!TryParseAmount(Cell(m.AmountColumn), out amount))
                {
                    errors.Add(new ParseError(rowNo, $"Monto no válido: \"{Cell(m.AmountColumn)}\""));
                    continue;
                }
                if (m.InvertSign) amount = -amount;
                if (amount == 0) continue;

                decimal? balance = TryParseAmount(Cell(m.BalanceColumn), out var b) ? b : null;
                var description = string.Join(" - ", m.DescriptionColumns.Select(i => Cell(i)).Where(s => s.Length > 0));
                if (description.Length == 0) description = "(sin descripción)";
                var reference = Cell(m.ReferenceColumn);

                lines.Add(new ParsedLine(rowNo, date, Truncate(description, 500), reference.Length > 0 ? Truncate(reference, 100) : null, Math.Round(amount, 2), balance, null));
            }

            return new ParseResult(lines, errors);
        }

        /// <summary>RFC 4180-ish reader: quoted fields, escaped quotes, newlines inside quotes; delimiter auto-detected.</summary>
        public static List<List<string>> ReadCsv(string content)
        {
            var delimiter = DetectDelimiter(content);
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < content.Length; i++)
            {
                var c = content[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == delimiter) { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                    row.Add(field.ToString()); field.Clear();
                    if (row.Any(s => s.Length > 0)) rows.Add(row);
                    row = new List<string>();
                }
                else field.Append(c);
            }
            row.Add(field.ToString());
            if (row.Any(s => s.Length > 0)) rows.Add(row);
            return rows;
        }

        private static char DetectDelimiter(string content)
        {
            var firstLine = content.Split('\n').FirstOrDefault(l => l.Trim().Length > 0) ?? string.Empty;
            var counts = new Dictionary<char, int> { [','] = 0, [';'] = 0, ['\t'] = 0, ['|'] = 0 };
            var inQuotes = false;
            foreach (var c in firstLine)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (!inQuotes && counts.ContainsKey(c)) counts[c]++;
            }
            var best = counts.OrderByDescending(kv => kv.Value).First();
            return best.Value > 0 ? best.Key : ',';
        }

        // ---------------------------------------------------------------- OFX / QFX

        private static readonly Regex StmtTrn = new(@"<STMTTRN>(.*?)(</STMTTRN>|(?=<STMTTRN>)|(?=</BANKTRANLIST>))", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>Handles both SGML (OFX 1.x, unclosed tags) and XML (OFX 2.x) files.</summary>
        public static ParseResult ParseOfx(string content)
        {
            var lines = new List<ParsedLine>();
            var errors = new List<ParseError>();
            var n = 0;
            foreach (Match match in StmtTrn.Matches(content))
            {
                n++;
                var block = match.Groups[1].Value;
                var dt = Tag(block, "DTPOSTED");
                var amt = Tag(block, "TRNAMT");
                if (dt is null || dt.Length < 8 || !DateOnly.TryParseExact(dt[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    errors.Add(new ParseError(n, $"Fecha no válida: \"{dt}\""));
                    continue;
                }
                if (amt is null || !decimal.TryParse(amt.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    errors.Add(new ParseError(n, $"Monto no válido: \"{amt}\""));
                    continue;
                }
                if (amount == 0) continue;
                var name = Tag(block, "NAME");
                var memo = Tag(block, "MEMO");
                var description = string.Join(" - ", new[] { name, memo }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
                if (description.Length == 0) description = Tag(block, "TRNTYPE") ?? "(sin descripción)";
                var reference = Tag(block, "CHECKNUM") ?? Tag(block, "REFNUM");
                lines.Add(new ParsedLine(n, date, Truncate(System.Net.WebUtility.HtmlDecode(description), 500), reference is null ? null : Truncate(reference, 100), Math.Round(amount, 2), null, Tag(block, "FITID") is { } f ? Truncate(f, 100) : null));
            }
            return new ParseResult(lines, errors);
        }

        private static string? Tag(string block, string tag)
        {
            var m = Regex.Match(block, $@"<{tag}>([^<\r\n]*)", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var v = m.Groups[1].Value.Trim();
            return v.Length == 0 ? null : v;
        }

        // ---------------------------------------------------------------- helpers

        public static bool TryParseDate(string? s, string? format, out DateOnly date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            var formats = string.IsNullOrWhiteSpace(format) ? DayFirstFormats : new[] { format };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
            {
                date = DateOnly.FromDateTime(dt);
                return dt.Year is > 1900 and < 2200;
            }
            return false;
        }

        public static bool TryParseAmount(string? s, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim().ToUpperInvariant();
            var negative = false;
            if (t.EndsWith("DR")) { negative = true; t = t[..^2]; }
            else if (t.EndsWith("CR")) t = t[..^2];
            t = t.Trim();
            if (t.StartsWith('(') && t.EndsWith(')')) { negative = true; t = t[1..^1]; }
            t = t.Replace("$", "").Replace("AUD", "").Replace(",", "").Replace(" ", "").Replace(" ", "");
            if (t.EndsWith('-')) { negative = true; t = t[..^1]; }
            if (t.Length == 0 || !Regex.IsMatch(t, @"^[+-]?\d+(\.\d+)?$")) return false;
            if (!decimal.TryParse(t, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)) return false;
            if (negative) value = -Math.Abs(value);
            return true;
        }

        /// <summary>
        /// Duplicate-detection key. Uses the bank id when present; otherwise date + amount + description + how many
        /// identical lines came before it in the same file, so two genuine identical purchases on one day both import,
        /// while re-uploading an overlapping statement does not.
        /// </summary>
        public static List<string> DedupeKeys(IEnumerable<ParsedLine> lines)
        {
            var seen = new Dictionary<string, int>();
            var keys = new List<string>();
            foreach (var l in lines)
            {
                string raw;
                if (!string.IsNullOrEmpty(l.ExternalId)) raw = "fit|" + l.ExternalId;
                else
                {
                    var baseKey = $"{l.Date:yyyy-MM-dd}|{l.Amount.ToString("0.00", CultureInfo.InvariantCulture)}|{Regex.Replace(l.Description.ToUpperInvariant(), @"\s+", " ").Trim()}";
                    seen[baseKey] = seen.TryGetValue(baseKey, out var c) ? c + 1 : 0;
                    raw = baseKey + "|" + seen[baseKey];
                }
                keys.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))));
            }
            return keys;
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    }
}
