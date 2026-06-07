namespace ProjectResourceManagement.Client;

public static class ConsoleTable
{
    public static void Print(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var rowList = rows.Select(row => row.ToList()).ToList();
        if (rowList.Count == 0)
        {
            Console.WriteLine("(no records found)");
            return;
        }

        var widths = new int[headers.Count];
        for (var index = 0; index < headers.Count; index++)
        {
            widths[index] = headers[index].Length;
        }

        foreach (var row in rowList)
        {
            for (var index = 0; index < headers.Count; index++)
            {
                var cell = index < row.Count ? row[index] : string.Empty;
                widths[index] = Math.Max(widths[index], cell.Length);
            }
        }

        PrintRow(headers, widths);
        PrintSeparator(widths);

        foreach (var row in rowList)
        {
            PrintRow(row, widths);
        }
    }

    private static void PrintRow(IReadOnlyList<string> cells, int[] widths)
    {
        for (var index = 0; index < widths.Length; index++)
        {
            var cell = index < cells.Count ? cells[index] : string.Empty;
            Console.Write($"| {cell.PadRight(widths[index])} ");
        }

        Console.WriteLine("|");
    }

    private static void PrintSeparator(int[] widths)
    {
        foreach (var width in widths)
        {
            Console.Write($"|-{new string('-', width)}-");
        }

        Console.WriteLine("|");
    }
}
