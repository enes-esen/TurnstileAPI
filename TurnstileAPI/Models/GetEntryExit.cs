using System;
using System.ComponentModel.DataAnnotations;

namespace InoosterTurnstileAPI.Models
{
	public class GetEntryExit
	{
		[Key]
		public int? id { get; set; }
		public int cardid { get; set; }
		public int? workno { get; set; }
        public int allowable { get; set; }

		public string users { get; set; }
		public string department { get; set; }
		public string address { get; set; }
        public string tanimlama { get; set; }

		public DateTime date_time { get; set; }
    }
}

