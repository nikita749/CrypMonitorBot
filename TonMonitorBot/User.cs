using System;
namespace TonMonitorBot
{
	public class User
	{

		public long id { set; get; }

		public string username { get; set; }

		public string firstName { get; set; }

		public string secondName { get; set; }

		public List<Wallet> wallets { get; set; }
	}
}

