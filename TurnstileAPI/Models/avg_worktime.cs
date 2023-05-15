using System;
namespace InoosterTurnstileAPI.Models
{
	public class avg_worktime
	{
		public int cardid { get; set; }
		public string users { get; set; }
		public TimeSpan avg_entry { get; set; }
		public TimeSpan avg_exit { get; set; }
		public TimeSpan avg_work_time { get; set; }
		public int total_work { get; set; }
		public int total_day { get; set; }

	}
}

