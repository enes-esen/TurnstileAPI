using System;
using ExcelDataReader;

namespace InoosterTurnstileAPI.Controllers
{
	public class ReadExcel
	{
		public ReadExcel(string path)
		{
            //path = "";
            var stream = File.Open(path, FileMode.Open, FileAccess.Read);

        }


	}
}

