namespace SQL_Web_Management.Domain.Models
{
	public class TableColumnInfo
	{
		public string ColumnName { get; set; } = string.Empty;
		public string DataType {  get; set; } = string.Empty;
		public int? MaxLength { get; set; }
		public bool IsNullable { get; set; }
		public bool IsPrimaryKey { get; set; }
	}
}
