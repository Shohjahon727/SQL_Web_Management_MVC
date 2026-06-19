namespace SQL_Web_Management.Models.ViewModels
{
	public class WorkspaceViewModel
	{
		public int ConnectionId { get; set; }
		public string ConnectionName { get; set; } = string.Empty;
		public string Server { get; set; } = string.Empty;
		public string Database { get; set; } = string.Empty;
	}
}
