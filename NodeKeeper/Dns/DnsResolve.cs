using NetDns = System.Net.Dns;

using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;


namespace NodeKeeper.Dns
{
	public static class DnsResolve
	{
		public static DnsInfo Resolve(string url){
			var dnsInfo = new DnsInfo {};

			var protocolIndex = url.IndexOf("//", StringComparison.Ordinal);

			var hostSearch = protocolIndex > 0 ? url.Substring(protocolIndex + 2) : url;

			if (url.LastIndexOf('/') > protocolIndex + 2)
			{
				dnsInfo.Path = hostSearch.Substring(hostSearch.IndexOf('/'));
				hostSearch   = hostSearch.Substring(0, hostSearch.IndexOf('/'));
			}

			ushort specifiedPort = 9000;

			var portIndex = hostSearch.IndexOf(':');
			if(portIndex > 0)
			{
				specifiedPort = Convert.ToUInt16(hostSearch.Split(':')[1].Trim());
				hostSearch    = hostSearch.Substring(0, portIndex);
			}

			if (protocolIndex > 0)
			{
				var protocol = url.Substring(0, protocolIndex).ToLower().Replace(":", "");

				switch(protocol){
					case "https":
						dnsInfo.Protocol = Protocol.http;
						dnsInfo.SSL      = true;
						break;
					case "http":
						dnsInfo.Protocol = Protocol.http;
						dnsInfo.SSL      = false;
						break;

					case "tcps":
						dnsInfo.Protocol = Protocol.tcp;
						dnsInfo.SSL      = true;
						break;
					case "tcp":
						dnsInfo.Protocol = Protocol.tcp;
						dnsInfo.SSL      = false;
						break;

					case "udps":
						dnsInfo.Protocol = Protocol.udp;
						dnsInfo.SSL      = true;
						break;
					case "udp":
						dnsInfo.Protocol = Protocol.udp;
						dnsInfo.SSL      = false;
						break;

					case "wss":
						dnsInfo.Protocol = Protocol.ws;
						dnsInfo.SSL      = true;
						break;
					case "ws":
						dnsInfo.Protocol = Protocol.ws;
						dnsInfo.SSL      = false;
						break;

					default:
						dnsInfo.Protocol = Protocol.Unknown;
						dnsInfo.SSL      = false;
						break;
				}
			}

			if (specifiedPort > 0u) dnsInfo.Port = specifiedPort;
			else specifiedPort = (ushort)(dnsInfo.SSL ? 443 : 80);

			if (hostSearch.Split('.').Length == 4)
			{
				dnsInfo.Address  = IPAddress.Parse(hostSearch);
				dnsInfo.Hostname = hostSearch;
			}

			else
			{
				var addresses = NetDns.GetHostAddresses(hostSearch);

				dnsInfo.Address  = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
				dnsInfo.EndPoint = new IPEndPoint(dnsInfo.Address, dnsInfo.Port);
				dnsInfo.Hostname = hostSearch;
			}

			return dnsInfo;
		}


		public static async Task<DnsInfo> ResolveAsync(string url)
		{return await Task<DnsInfo>.Factory.StartNew(() => Resolve(url));}
	};
};