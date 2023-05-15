using System;
namespace InoosterTurnstileAPI.Models
{
	public class weekworktime
	{
		public int cardid { get; set; }
		public string users { get; set; }

		public TimeSpan week_avg_entry { get; set; }
		public TimeSpan week_avg_exit { get; set; }
		public TimeSpan avg_work_time { get; set; }

		public int week_total_working_hours { get; set; }
		public int week_total_day { get; set; }

		public DateTime firstdayofweek { get; set; }
		public DateTime lastdayofweek { get; set; }
		public int numberofweeks { get; set; }

	}
}

