using NodeKeeper.Forwarders;
using NodeKeeper.Listeners;
using NodeKeeper.Routings;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;


namespace NodeKeeper
{
	public delegate Task AcceptAction(IForwarder forwarder);

	public delegate Task BufferAction(byte[] buffer, int offset, int length);

	public delegate void ClosedAction(string reason);

	public delegate void OpenedAction();


	public class ProxyService
	{
		readonly HashSet<IListener> listeners = new HashSet<IListener>();

		readonly HashSet<IRouting> routings = new HashSet<IRouting>();


		public void AddListener(IListener listener)
		{
			if (!listeners.Contains(listener))
			{
				listener.OnAccept += AttachAsync;
				listeners.Add(listener);
			}
		}

		public void RemoveListener(IListener listener)
		{
			if (listeners.Contains(listener)) {
				listeners.Remove(listener);
				listener.Close();
			}
		}


		public void AddRouting(IRouting routing)
		{
			if (!routings.Contains(routing))
			{ routings.Add(routing); }
		}

		public void RemoveRouting(IRouting routing)
		{
			if (routings.Contains(routing))
			{ routings.Remove(routing); }
		}


		public void Close()
		{
			foreach(IListener listener in listeners)
			{
				listener.Close();
			}

			listeners.Clear();
			routings.Clear();
		}


		private async Task AttachAsync(IForwarder forwarder) {
			foreach (var routing in routings) {
				if (routing.Listener != forwarder.Name) continue;

				Console.WriteLine($"Register route to client {forwarder.DnsInfo}");

				var insideForwarder = await routing.GetForwarderAsync();

				insideForwarder.OnBuffer += async (buffer, offset, length) => {
					Console.WriteLine($"Outside forward {forwarder.DnsInfo}");
					await forwarder.SendAsync(buffer, offset, length);
				};

				forwarder.OnBuffer += async (buffer, offset, length) => {
					Console.WriteLine($"Inside forward {insideForwarder.DnsInfo}");
					await insideForwarder.SendAsync(buffer, offset, length);
				};

				insideForwarder.OnClosed += (reason) => forwarder.Close();

				forwarder.OnClosed += (reason) => insideForwarder.Close();
			}

			forwarder.OnOpened += () => { };

			forwarder.OnClosed += (reason) => { };
		}
	};
};