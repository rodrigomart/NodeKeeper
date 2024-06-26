using System.Net;


namespace NodeKeeper.Dns {
	public enum Protocol {
		Unknown,
		http,
		tcp,
		udp,
		ws
	};


	public sealed class DnsInfo {
		public Protocol   Protocol { get; set; } = Protocol.Unknown;


		public IPEndPoint EndPoint { get; set; } = new IPEndPoint(0L, 0);

		public IPAddress  Address  { get; set; } = IPAddress.None;

		public string     Hostname { get; set; } = "localhost";


		public string     Path     { get; set; } = "/";

		public int        Port     { get; set; } = 9000;


		public bool       SSL      { get; set; } = true;


		public override string ToString(){
			return string.Format(
				"{0}{1}://{2}:{3}",
				Protocol, (SSL)? "s" : "",
				Address, Port
			);
		}
	};
};