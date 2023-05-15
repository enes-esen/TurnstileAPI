using System;
using InoosterTurnstileAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InoosterTurnstileAPI.Data
{
	public class DataContext : DbContext
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options)
		{
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

		//public DbSet<PostEntyExit> PostEntyExits { get; set; }
        public DbSet<GetEntryExit> entryexits { get; set; }

    }
}

