using HtmlAgilityPack;

namespace edenorte_scrap.Extensions;

public class TableSpanExtension
{
    /// <summary>
    /// Processes a given table collapsing rowspan and colspan into unique cells to aid in parsing tables.
    /// </summary>
    /// <param name="tableNode">Should be a HtmlNode of a table type. This function doesn't mutate this param. It is cloned and a modified copied is returned.</param>
    /// <returns>Returns the processed table as a newly cloned HtmlNode object.</returns>
    public HtmlNode? ProcessTable(in HtmlNode? tableNode)
    {
        if (tableNode == null)
        {
            return null;
        }

        if (!tableNode.Name.Equals("table"))
        {
            return tableNode;
        }

        var ret = tableNode.Clone();

        var rows = ret.SelectNodes(".//tr");

        // Calculate the maximum number of rows and columns currently in the table after colspan rebuilding
        var numCols = rows
            .Select(row => row.SelectNodes(".//td|.//th"))
            .Max(a => a.Count);

        // Build ColSpans
        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td|.//th");

            for (var colIndex = 0; colIndex < cells.Count; colIndex++)
            {
                var cell = cells[colIndex];

                var colspan = cell.GetAttributeValue("colspan", 0);

                if (colspan > 0)
                {
                    cell.Attributes["colspan"]?.Remove();

                    for (var i = 1; i < colspan; i++)
                    {
                        if (colIndex + i >= numCols)
                        {
                            continue;
                        }

                        var newCell = HtmlNode.CreateNode(cell.OuterHtml);

                        row.InsertAfter(newCell, cell);
                    }
                }
            }
        }

        // Calculate the maximum number of rows and columns currently in the table after colspan rebuilding
        var numRows = rows.Count;
        numCols = rows
            .Select(row => row.SelectNodes(".//td|.//th"))
            .Max(a => a.Count);

        // Build RowSpans
        for (var colIndex = 0; colIndex < numCols; colIndex++)
        {
            for (var rowIndex = 0; rowIndex < numRows; rowIndex++)
            {
                var row = rows[rowIndex];

                var cells = row.SelectNodes(".//td|.//th");

                var cell = cells[colIndex];

                var rowspan = cell.GetAttributeValue("rowspan", 0);

                if (rowspan > 0)
                {
                    cell.Attributes["rowspan"]?.Remove();

                    for (var i = 1; i < rowspan; i++)
                    {
                        if (rowIndex + i >= rows.Count)
                        {
                            continue;
                        }

                        var subRow = rows[rowIndex + i];
                        var subRowCells = subRow.SelectNodes(".//td|.//th");

                        var newCell = HtmlNode.CreateNode(cell.OuterHtml);

                        var targetCellIndex = Math.Min(subRowCells.Count - 1, colIndex);
                        var targetCell = subRowCells[targetCellIndex];

                        if (colIndex > subRowCells.Count - 1)
                        {
                            subRow.InsertAfter(newCell, targetCell);
                        }
                        else
                        {
                            subRow.InsertBefore(newCell, targetCell);
                        }
                    }
                }
            }
        }

        return ret;
    }

    public static string[,] ToArray(in HtmlNode tableNode)
    {
        var rows = tableNode.SelectNodes(".//tr");
        if (rows != null)
        {
            var numRows = rows.Count;
            var numCols = rows.Select(row => row.SelectNodes(".//td|.//th")).Select(cols => cols?.Count ?? 0).Prepend(0).Max();

            var ret = new string[numCols, numRows];

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];

                var cols = row.SelectNodes(".//td|.//th");
                if (cols != null)
                {
                    for (var colIndex = 0; colIndex < cols.Count; colIndex++)
                    {
                        var col = cols[colIndex];

                        ret[colIndex, rowIndex] = col.InnerText;
                    }
                }


            }

            return ret;
        }


        return new string[0, 0];


    }
}