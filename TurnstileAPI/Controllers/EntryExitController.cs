using AutoMapper;
using ExcelDataReader;
using InoosterTurnstileAPI.Data;
using InoosterTurnstileAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Packaging;
using Npgsql;
using SqlHandler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics.Metrics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace InoosterTurnstileAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class EntryExitController : Controller
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public EntryExitController(DataContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        //Seçii ay/yılda bulunan kullanıcıları listeleme
        [HttpGet]
        public async Task<ActionResult> GetUsers(int year, int month, int selection)
        {
            try
            { 
                var listUser = _context.entryexits
                    .Where(u => u.date_time.Year == year && u.date_time.Month == month)
                    .Select(u => new
                    {
                        u.cardid,
                        u.users,
                    })
                    .Distinct()
                    .OrderBy(u => u.users)
                    .ToList();
                return Ok(listUser);
            }
            catch (Exception ex)
            {
                return Ok(ex);
            }
        }

        //Seçili kullanıcı ve ay/yıla göre çalışma günleri ve sürelerini listeler.
        [HttpGet]
        public async Task<IActionResult> GetData(int cardid, int year, int month)
        {
            try
            {
                //FormattableString
                string query = $@"
                       ;with entrysall as
                       (
                       	select
                       		e.""cardid""
                       		,e.""users""
                       		,min(e.""date_time"") as min_datetime		
                       		,date_part('year', e.""date_time"") as EntryYear
                       		,date_part('month', e.""date_time"") as EntryMonth
                       		,date_part('day', e.""date_time"") as EntryDay 
                       	from ""entryexits"" as e
                       	where 
                       		(e.""address"" = 'TURNİKE  1[Giriş]' or e.""address"" = 'TURNİKE  2[Giriş]')
                       		and EXTRACT (YEAR FROM e.""date_time"") = {year}
                       		and EXTRACT (MONTH FROM e.""date_time"") = {month}
                       	group by 
                       		e.""cardid""
                       		,e.""users""
                       		,date_part('year', e.""date_time"")
                       		,date_part('month', e.""date_time"")
                       		,date_part('day', e.""date_time"")
                       	order by ""min_datetime""
                       ),
                       exitmax as
                       (
                       	select 
                       		e.""cardid""
                       		,e.""users""
                       		,ea.""min_datetime""
                    		,max(e.""date_time"") as max_exit
                    	from ""entryexits"" e
                    		inner join entrysall ea
                    			on ea.""cardid"" = e.""cardid""
                    	where (e.""address"" = 'TURNİKE 1[Çıkış]' or e.""address"" = 'TURNİKE 2[Çıkış]')
                    		and e.""date_time"" < ea.""min_datetime"" + interval '20 hour'
                    		and ea.""min_datetime"" < e.""date_time""
                    	group by 
                    		e.""cardid""
                    		,e.""users""
                    		,ea.""min_datetime""
                    )
                    select 
                    	em.""cardid""
                    	,em.""users""
                    	,em.""min_datetime"" as entrytime
                    	,em.""max_exit"" as exittime
                    	,em.""max_exit"" - em.""min_datetime"" as worktime
                    from ""exitmax"" as em
                    where em.""cardid"" = {cardid}
                    order by em.""min_datetime""";
                              
                var _connection = _configuration.GetConnectionString("DefaultConnection");
                using var con = new NpgsqlConnection(_connection);
                con.Open();
                
                using var cmd = new NpgsqlCommand(query, con);

                NpgsqlDataReader result = cmd.ExecuteReader();
                List<worktimes> list = new List<worktimes>();

                while (result.Read())
                {
                    list.Add(
                        new worktimes
                        {
                            cardid = Convert.ToInt32(result["cardid"]),
                            users = result["users"].ToString(),
                            
                            entrytime = Convert.ToDateTime(result["entrytime"]),
                            exittime = Convert.ToDateTime(result["exittime"]),
                            worktime = TimeSpan.Parse(result["worktime"].ToString()),
                        }
                    );
                }

                con.Close();
                result.Close();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        //Kullanıcının Aylık Ortalama verisi
        [HttpGet]
        public async Task<IActionResult> GetAvgDataMonth(int cardid, int year, int month)
        {
            string query = $@"
                        ;with entrysall as
                        (
                        	select
                        		e.cardid
                        		,e.users
                        		,min(e.date_time) as min_datetime		
                        		,date_part('year', e.date_time) as EntryYear
                        		,date_part('month', e.date_time) as EntryMonth
                        		,date_part('day', e.date_time) as EntryDay 
                        	from entryexits as e
                        	where 
                        		(e.address = 'TURNİKE  1[Giriş]' or e.address = 'TURNİKE  2[Giriş]')
                        		and EXTRACT (YEAR FROM e.date_time) = {year}
                        		and EXTRACT (MONTH FROM e.date_time) = {month}
                        	group by 
                        		e.cardid
                        		,e.users
                        		,date_part('year', e.date_time)
                        		,date_part('month', e.date_time)
                        		,date_part('day', e.date_time)
                        	order by min_datetime
                        ),
                        exitmax as
                        (
                        	select 
                        		e.cardid
                        		,e.users
                        		,ea.min_datetime
                        		,max(e.date_time) as max_exit
                        	from entryexits e
                        		inner join entrysall ea
                        			on ea.cardid = e.cardid
                        	where (e.address = 'TURNİKE 1[Çıkış]' or e.address = 'TURNİKE 2[Çıkış]')
                        		and e.date_time < ea.min_datetime + interval '20 hour'
                        		and ea.min_datetime < e.date_time
                        	group by 
                        		e.cardid
                        		,e.users
                        		,ea.min_datetime
                        ),
                        worktime_select_users as
                        (
                        	select 
                        		em.cardid
                        		,em.users
                        		,em.min_datetime as entrytime
                        		,em.max_exit as exittime
                        		,em.max_exit - em.min_datetime as worktime
                        	from exitmax as em
                        	where em.cardid = {cardid}
                        	order by em.min_datetime
                        )
                        select 
                        	wsu.""cardid""
                        	,wsu.""users""
                        	,to_char(
                        		(
                        			avg(
                        					(
                        						(extract(hour from wsu.entrytime) * 3600) +
                        						(extract(minute from wsu.entrytime) * 60) + 
                        						(extract(second from wsu.entrytime))
                        					)
                        				) || 'second'
                        		)::interval, 'HH24:MI:SS'
                        	) as avg_entry
                        	,to_char(
                        		(
                        			(				
                        				avg(
                        						(
                        							(extract(hour from wsu.entrytime) * 3600) +
                        							(extract(minute from wsu.entrytime) * 60) + 
                        							(extract(second from wsu.entrytime))
                        						)
                        					)
                        				+
                        				avg(
                        					(
                        						(extract(hour from wsu.worktime) * 3600) +
                        						(extract(minute from wsu.worktime) * 60) + 
                        						(extract(second from wsu.worktime))
                        					)				
                        				)
                        			)|| 'second'
                        		)::interval, 'HH24:MI:SS'
                        	) as avg_exit
                        	,to_char(
                        		(
                        			avg(
                        					(
                        						(extract(hour from wsu.worktime) * 3600) +
                        						(extract(minute from wsu.worktime) * 60) + 
                        						(extract(second from wsu.worktime))
                        					)
                        				) || 'second'
                        		)::interval, 'HH24:MI:SS'
                        	) as avg_work_time
                        	,round(
                        		sum(
                        				(extract(hour from wsu.worktime) * 3600) +
                        				(extract(minute from wsu.worktime) * 60) + 
                        				(extract(second from wsu.worktime))
                        		)/3600 
                        	) as total_working_hours
                        	,count(wsu.*) as total_day
                        from  worktime_select_users as wsu
                        group by 
                        	wsu.""cardid""
                        	,wsu.""users""
                        ";
                       
            var _connection = _configuration.GetConnectionString("DefaultConnection");
            using var con = new NpgsqlConnection(_connection);
            con.Open();

            using var cmd = new NpgsqlCommand(query, con);

            NpgsqlDataReader result = cmd.ExecuteReader();
            List<avg_worktime> list = new List<avg_worktime>();

            while (result.Read())
            {
                list.Add(
                    new avg_worktime
                    {
                        cardid = Convert.ToInt32(result["cardid"]),
                        users = result["users"].ToString(),

                        avg_entry = TimeSpan.Parse(result["avg_entry"].ToString()),
                        avg_exit = TimeSpan.Parse(result["avg_exit"].ToString()),
                        avg_work_time = TimeSpan.Parse(result["avg_work_time"].ToString()),
                        total_work = Convert.ToInt32(result["total_working_hours"]),
                        total_day = Convert.ToInt32(result["total_day"])
                    }
                );
            }
            con.Close();
            result.Close();

            return Ok(list);
        }

        // 1 ay içerisinde hafta hafta ortalama veri
        [HttpGet]
        public async Task<IActionResult> GetAvgDataWeek(int cardid, int year, int month)
        {
            //BREAK POINT KULLANMA!!!!!!!!!!!
            try
            {
                DateTime dayOfMonth = new DateTime();
                DateTime sundayOfMonth = new DateTime();

                int c = 0;
                string changeDayOfMonth;
                string changeSundayOfMonth;

                // Seçilin ayın toplam gün sayısı
                var dates = new List<DateTime>();
                for (var date = new DateTime(year, month, 1); date.Month == month; date = date.AddDays(1))
                {
                    dates.Add(date);
                }

                //connection DB
                var _connection = _configuration.GetConnectionString("DefaultConnection");
                List<weekworktime> list = new List<weekworktime>();
                using (NpgsqlConnection con = new NpgsqlConnection(_connection))
                {
                    //Hafaların sayılması
                    for (int i = 1; i <= dates.Count; i++)
                    {
                        dayOfMonth = new DateTime(year, month, i);

                        //pazar gününü bul
                        sundayOfMonth = dayOfMonth.AddDays((DayOfWeek.Sunday + 7 - dayOfMonth.DayOfWeek) % 7);

                        //Eğer seçilen pazar gününün Ayı  saçili Ay'dan büyükse seçili ayın son günü tanımlanır.
                        if (sundayOfMonth.Month > dayOfMonth.Month)
                        {
                            sundayOfMonth = dates[(DateTime.DaysInMonth(year, month)) - 1];
                            //Son günün değerini alır.
                            //dates[(DateTime.DaysInMonth(year, month)) - 1]
                        }

                        //DB'de date tipini düzenleme
                        changeDayOfMonth = dayOfMonth.ToString("yyyy-MM-dd");
                        changeSundayOfMonth = sundayOfMonth.ToString("yyyy-MM-dd");

                        con.Open();

                        //Kullanıcının her haftaki ortalamasının alınması.
                        if (dayOfMonth.Day == 1 || dayOfMonth.DayOfWeek.ToString() == "Monday")
                        {
                            string query = $@"
                                ;with entrysall as 
                                ( 
                                	select 
                                		e.cardid 
                                		,e.users 
                                		,min(e.date_time) as min_datetime 
                                		,date_part('year', e.date_time) as EntryYear 
                                		,date_part('month', e.date_time) as EntryMonth 
                                		,date_part('day', e.date_time) as EntryDay 
                                	from entryexits as e 
                                	where  
                                		(e.address = 'TURNİKE  1[Giriş]' or e.address = 'TURNİKE  2[Giriş]') 
                                		and date_part('year', e.date_time) = {year} 
                                		and date_part('month', e.date_time) = {month} 
                                	group by  
                                		e.cardid 
                                		,e.users 
                                		,date_part('year', e.date_time) 
                                		,date_part('month', e.date_time) 
                                		,date_part('day', e.date_time) 
                                	order by min_datetime 
                                ), 
                                exitmax as 
                                ( 
                                	select  
                                		e.cardid 
                                		,e.users 
                                		,ea.min_datetime 
                                		,max(e.date_time) as max_exit 
                                	from entryexits e 
                                		inner join entrysall ea 
                                			on ea.cardid = e.cardid 
                                	where (e.address = 'TURNİKE 1[Çıkış]' or e.address = 'TURNİKE 2[Çıkış]') 
                                		and e.date_time < ea.min_datetime + interval '20 hour' 
                                		and ea.min_datetime < e.date_time 
                                	group by  
                                		e.cardid 
                                		,e.users 
                                		,ea.min_datetime 
                                ), 
                                week_worktime_select_users as 
                                ( 
                                	select 
                                		em.cardid 
                                		,em.users 
                                		,em.min_datetime as entrytime 
                                		,em.max_exit as exittime 
                                		,em.max_exit - em.min_datetime as worktime 
                                	from exitmax as em 
                                	where em.cardid = {cardid} 
                                		and em.min_datetime between '{changeDayOfMonth}' and '{changeSundayOfMonth}' 
                                	order by em.min_datetime 
                                ) 
                                select  
                                	w.cardid 
                                	,w.users 
                                	,to_char( 
                                		( 
                                			avg( 
                                					( 
                                						(extract(hour from w.entrytime) * 3600) + 
                                						(extract(minute from w.entrytime) * 60) +  
                                						(extract(second from w.entrytime)) 
                                					) 
                                				) || 'second' 
                                		)::interval, 'HH24:MI:SS' 
                                	) as week_avg_entry 
                                	,to_char( 
                                		( 
                                			avg( 
                                					( 
                                						(extract(hour from w.exittime) * 3600) + 
                                						(extract(minute from w.exittime) * 60) +  
                                						(extract(second from w.exittime)) 
                                					) 
                                				) || 'second' 
                                		)::interval, 'HH24:MI:SS' 
                                	) as week_avg_exit 
                                	,to_char( 
                                		( 
                                			avg( 
                                					( 
                                						(extract(hour from w.worktime) * 3600) + 
                                						(extract(minute from w.worktime) * 60) +  
                                						(extract(second from w.worktime)) 
                                					) 
                                				) || 'second' 
                                		)::interval, 'HH24:MI:SS' 
                                	) as avg_work_time 
                                	,round( 
                                		sum( 
                                				(extract(hour from w.worktime) * 3600) + 
                                				(extract(minute from w.worktime) * 60) + 
                                				(extract(second from w.worktime)) 
                                		)/3600  
                                	) as week_total_working_hours 
                                	,count(w.*) as week_total_day 
                                from week_worktime_select_users w 
                                group by 
                                	w.cardid 
                                	,w.users 
                            ";

                            using (var cmd = new NpgsqlCommand(query, con))
                            {
                                c++;
                                NpgsqlDataReader result = cmd.ExecuteReader();
                                while (result.Read())
                                {
                                    list.Add(
                                        new weekworktime
                                        {
                                            cardid = Convert.ToInt32(result["cardid"]),
                                            users = result["users"].ToString(),

                                            week_avg_entry = TimeSpan.Parse(result["week_avg_entry"].ToString()),
                                            week_avg_exit = TimeSpan.Parse(result["week_avg_exit"].ToString()),
                                            avg_work_time = TimeSpan.Parse(result["avg_work_time"].ToString()),

                                            week_total_working_hours = Convert.ToInt32(result["week_total_working_hours"]),
                                            week_total_day = Convert.ToInt32(result["week_total_day"]),

                                            firstdayofweek = dayOfMonth,
                                            lastdayofweek = sundayOfMonth,
                                            numberofweeks = c,
                                        });
                                }
                                result.Close();
                            }                            
                        }
                        con.Close();
                    }
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        // POST: api/EntryExit
        [HttpPost("uploaddata")]
        public async Task<IActionResult> Post(IFormFile file)
        {
            //!!!!!!!!!
            //Kesinlikle olmalı
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            #region Save Folder
            var originalFileName = Path.GetFileName(file.FileName);
            string path = @"~/../ExcelDatas/";
            var pathFile = Path.Combine(path, originalFileName);
            using (FileStream stream = System.IO.File.Create(pathFile))
            {
                await file.CopyToAsync(stream);
            }
            #endregion

            //Excel'i DB'e aktarma
            using (FileStream stream = System.IO.File.Open(pathFile, FileMode.Open, FileAccess.Read))
            {
                //Okuyucunun oluşturulması
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (data) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    DataTableCollection table = result.Tables;
                    DataTable resultTable = table["Sheet1"];

                    #region Change Column Name
                    resultTable.Columns["ID"].ColumnName = "id";
                    resultTable.Columns["Card ID"].ColumnName = "cardid";
                    resultTable.Columns["Worker No."].ColumnName = "workno";
                    resultTable.Columns["Users"].ColumnName = "users";
                    resultTable.Columns["Department"].ColumnName = "department";
                    resultTable.Columns["Date/Time"].ColumnName = "date_time";
                    resultTable.Columns["Address"].ColumnName = "address";
                    resultTable.Columns["Allowable"].ColumnName = "allowable";
                    resultTable.Columns["Tanimlama"].ColumnName = "tanimlama";

                    //var tarih = resultTable.Columns[5].ColumnName;
                    #endregion

                    #region Change Column Type
                    DataTable dtClone = resultTable.Clone();


                    dtClone.Columns["id"].DataType = typeof(int);
                    dtClone.Columns["cardid"].DataType = typeof(int);

                    dtClone.Columns["workno"].DataType = typeof(Int32);
                    dtClone.Columns["allowable"].DataType = typeof(int);

                    dtClone.Columns["users"].DataType = typeof(string);
                    dtClone.Columns["department"].DataType = typeof(string);
                    dtClone.Columns["address"].DataType = typeof(string);
                    dtClone.Columns["tanimlama"].DataType = typeof(string);

                    dtClone.Columns["date_time"].DataType = typeof(DateTime);
                    #endregion

                    //Yapılan değişikliklerin kaydedilmesi
                    foreach (DataRow row in resultTable.Rows)
                    {
                        dtClone.ImportRow(row);
                    }

                    //Veri Karşılaştırma
                    //Aynı dosya bir daha yüklenmesini engelleme.
                    var id = Convert.ToInt32(dtClone.Rows[0]["id"]);
                    var userLists = await _context.entryexits.FindAsync(id);
                    if (userLists == null)
                    {
                        List<GetEntryExit> getEntryExitsList = new List<GetEntryExit>();
                        for (int i = 0; i < dtClone.Rows.Count; i++)
                        {
                            GetEntryExit getEntryExit = new GetEntryExit();

                            getEntryExit.id = Convert.ToInt32(dtClone.Rows[i]["id"]);
                            getEntryExit.cardid = Convert.ToInt32(dtClone.Rows[i]["cardid"]);

                            //WorkNo değeri DbNull tipinde geliyor. Null şekilde DB'ye atılmadı.
                            //getEntryExit.WorkNo = Convert.ToInt32(dtClone.Rows[i]["WorkNo"]);
                            getEntryExit.workno = Convert.ToInt32(Convert.IsDBNull(dtClone.Rows[i]["workno"]));

                            getEntryExit.users = dtClone.Rows[i]["users"].ToString();
                            getEntryExit.department = dtClone.Rows[i]["department"].ToString();

                            getEntryExit.date_time = Convert.ToDateTime(dtClone.Rows[i]["date_time"]);
                            //UTCzone

                            getEntryExit.address = dtClone.Rows[i]["address"].ToString();
                            getEntryExit.allowable = Convert.ToInt32(dtClone.Rows[i]["allowable"]);
                            getEntryExit.tanimlama = dtClone.Rows[i]["tanimlama"].ToString();

                            getEntryExitsList.Add(getEntryExit);
                        }

                        await _context.AddRangeAsync(_mapper.Map<List<GetEntryExit>>(getEntryExitsList));
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return Ok("Bu dosya veritabanında ekli. Lütfen başka bir dosya seçiniz.");
                    }
                }
            }
            return Ok($"Received file {file.FileName} with size in bytes {file.Length}");
        }

        //Deneme fonksiyonu
        [HttpPost("deneme")]
        public async Task<IActionResult> PostData()
        {
            try
            {
                //PrimaryKey ile filtreleme yapmk için find kullanılır.
                var userLists = await _context.entryexits.FindAsync(4232003);
                if (userLists == null)
                {
                    return Ok("not data");
                }

                return Ok(userLists.id);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);

            }
            //return Ok("sa");
        }
    }
}