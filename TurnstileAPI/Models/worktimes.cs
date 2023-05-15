using System;
namespace InoosterTurnstileAPI.Models
{
	public class worktimes
	{
		public int cardid { get; set; }
		public string? users { get; set; }

		public DateTime? entrytime { get; set; }
		public DateTime? exittime { get; set; }
		public TimeSpan? worktime { get; set; }

	}
}

