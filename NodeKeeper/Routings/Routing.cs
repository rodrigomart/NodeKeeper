using NodeKeeper.Forwarders;
using NodeKeeper.Dns;

using System.Threading.Tasks;


namespace NodeKeeper.Routings
{
	internal class Routing : IRouting
	{
		DnsInfo dnsInfo;
		
		public string Listener { private set; get; }

		public string Name { private set; get; }


		public Routing(string name, string listener, string host) {
			dnsInfo = DnsResolve.Resolve(host);
			Listener = listener;
			Name = name;
		}


		public async Task<IForwarder> GetForwarderAsync()
		{
			return new TcpForwarder(Name, dnsInfo);
		}
	};
};