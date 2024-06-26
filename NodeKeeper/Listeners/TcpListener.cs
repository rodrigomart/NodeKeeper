using NodeKeeper.Forwarders;
using NodeKeeper.Dns;

using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System;


namespace NodeKeeper.Listeners
{
	public class TcpListener : IListener
	{
		readonly CancellationTokenSource cancellationTokenSource;

		readonly CancellationToken cancellationToken;

		readonly Socket socket;

		readonly Task task;


		public event AcceptAction OnAccept;

		public event ClosedAction OnClosed;

		public event OpenedAction OnOpened;


		public DnsInfo DnsInfo { private set; get; }

		public string Name { private set; get; }


		public TcpListener(string name, string host) {
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;

			socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			DnsInfo = DnsResolve.Resolve(host);
			Name = name;

			task = InitNetworkTask();
		}


		public void Close()
		{
			cancellationTokenSource.Cancel();
		}


		private Task InitNetworkTask()
		{
			return Task.Factory.StartNew(async () =>
			{
				try
				{
					socket.Bind(DnsInfo.EndPoint);
					socket.Listen(100);

					Console.WriteLine($"Listener {DnsInfo} started");

					while (!cancellationToken.IsCancellationRequested)
					{
						var remoteSocket = await socket.AcceptAsync();

						var remoteIpEndPoint = remoteSocket.RemoteEndPoint;

						var remoteDnsInfo = new DnsInfo
						{
							EndPoint = remoteIpEndPoint as IPEndPoint,
							Address = (remoteIpEndPoint as IPEndPoint).Address,
							Port = (remoteIpEndPoint as IPEndPoint).Port,
							Protocol = DnsInfo.Protocol,
							Hostname = DnsInfo.Hostname,
							Path = DnsInfo.Path,
							SSL = DnsInfo.SSL
						};

						var forwarder = new TcpForwarder(Name, remoteSocket, remoteDnsInfo);
						forwarder.OnClosed += Console.WriteLine;
						await OnAccept(forwarder);
					}
				}

				catch (Exception ex)
				{
					Console.WriteLine(ex);
					OnClosed(ex.Message);
					return;
				}

				OnClosed("Closed");
			});
		}
	};
};