using System;
using System.Data.Entity;
namespace TonMonitorBot
{
	public class TonMonitorContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Wallet> Wallets { get; set; }

	}
}

