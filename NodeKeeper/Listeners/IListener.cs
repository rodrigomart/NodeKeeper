using NodeKeeper.Dns;


namespace NodeKeeper.Listeners
{
	public interface IListener
	{
		event AcceptAction OnAccept;

		event ClosedAction OnClosed;

		event OpenedAction OnOpened;


		DnsInfo DnsInfo { get; }

		string Name { get; }


		void Close();
	};
};