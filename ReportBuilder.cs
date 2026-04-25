using System.Text;

/// <summary>
/// Построитель отчётов с использованием паттерна Fluent Interface.
/// </summary>
public class ReportBuilder
{
    private readonly DatabaseManager _db;
    private string _sql = string.Empty;
    private string _title = string.Empty;
    private string[] _headers = Array.Empty<string>();
    private int[] _widths = Array.Empty<int>();
    private bool _numbered = false;
    private string _footer = string.Empty;

    public ReportBuilder(DatabaseManager db) => _db = db;

    public ReportBuilder Query(string sql) { _sql = sql; return this; }
    public ReportBuilder Title(string title) { _title = title; return this; }
    public ReportBuilder Header(params string[] headers) { _headers = headers; return this; }
    public ReportBuilder ColumnWidths(params int[] widths) { _widths = widths; return this; }
    public ReportBuilder Numbered() { _numbered = true; return this; }
    public ReportBuilder Footer(string label) { _footer = label; return this; }

    public string Build()
    {
        var (_, rows) = _db.ExecuteQuery(_sql);
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(_title))
        {
            sb.AppendLine($"== {_title} ==");
            sb.AppendLine();
        }

        int colCount = Math.Min(_headers.Length, _widths.Length);
        var widths = new int[colCount];
        Array.Copy(_widths, widths, colCount);

        int numWidth = _numbered ? 5 : 0;

        if (_numbered) sb.Append("№".PadRight(numWidth));
        for (int i = 0; i < colCount; i++)
            sb.Append(_headers[i].PadRight(widths[i]));
        sb.AppendLine();

        int totalWidth = numWidth + widths.Sum();
        sb.AppendLine(new string('-', totalWidth));

        for (int r = 0; r < rows.Count; r++)
        {
            if (_numbered) sb.Append((r + 1).ToString().PadRight(numWidth));
            for (int c = 0; c < rows[r].Length && c < colCount; c++)
                sb.Append(rows[r][c].PadRight(widths[c]));
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(_footer))
        {
            sb.AppendLine(new string('-', totalWidth));
            sb.AppendLine($"{_footer}: {rows.Count}");
        }

        return sb.ToString();
    }

    public void Print() => Console.WriteLine(Build());
}