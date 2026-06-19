namespace SQL_Web_Management.Application.Services
{
	public class SqlBatchSplitter
	{
		public static IReadOnlyList<string> SplitBatches(string sql)
		{
			var batches = new List<string>();
			var current = new System.Text.StringBuilder();

			foreach (var line in sql.Replace("\r\n", "\n").Split("\n"))
			{
				if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
				{
					var batch = current.ToString().Trim();
					if (!string.IsNullOrWhiteSpace(batch))
					{
						batches.Add(batch);
					}
					current.Clear();
					continue;
				}
				current.AppendLine(line);
			}

			var lastBatch = current.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(lastBatch))
			{
				batches.Add(lastBatch);
			}

			return batches.Count > 0 ? batches : [sql.Trim()];
		}
	}
}
