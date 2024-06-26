using NodeKeeper.Dns;

using System.Threading.Tasks;


namespace NodeKeeper.Forwarders
{
	public interface IForwarder
	{
		event BufferAction OnBuffer;

		event ClosedAction OnClosed;

		event OpenedAction OnOpened;


		DateTime Timestamp { get; }

		TimeSpan Elapsed { get; }

		DnsInfo DnsInfo { get; }

		string Listener { get; }

		string Name { get; }


		void Close();


		Task SendAsync(byte[] buffer, int offset, int length);
	};
};