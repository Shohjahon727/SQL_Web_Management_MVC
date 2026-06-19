using SQL_Web_Management.Domain.Enums;

namespace SQL_Web_Management.Domain.Models
{
	public class DbObjectNode
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public DbObjectType Type { get; set; }
		public string? Database {  get; set; }
		public string? Schema { get; set; }
		public bool HasChildren { get; set; }
		public List<DbObjectNode> Children { get; set; } = [];
	}
}
