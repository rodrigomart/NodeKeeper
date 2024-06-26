using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using Timer = System.Timers.Timer;

using NodeKeeper.Routings;
using NodeKeeper.Dns;

using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Net.Security;
using System.Net.Sockets;
using System.IO;
using System;


namespace NodeKeeper.Forwarders
{
	public class TcpForwarder : IForwarder
	{
		readonly CancellationTokenSource cancellationTokenSource;

		readonly CancellationToken cancellationToken;

		X509Certificate2Collection x509Certificate2Collection;

		X509Certificate2 x509Certificate2;

		readonly DnsInfo dnsInfo;

		SslStream sslStream;

		Stream stream;

		readonly Socket socket;

		readonly Timer timer;

		/// <summary>Tarefa.</summary>
		readonly Task task;


		public event BufferAction OnBuffer;

		public event ClosedAction OnClosed;

		public event OpenedAction OnOpened;


		public DateTime Timestamp { private set; get; }

		public TimeSpan Elapsed { private set; get; }

		public DnsInfo DnsInfo { private set; get; }

		public string Listener { private set; get; }

		public string Name { private set; get; }


		public TcpForwarder(string name, DnsInfo dnsInfo)
		{
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;

			socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			timer = new Timer(5000);
			timer.Elapsed += IdleControl;
			timer.AutoReset = false;
			timer.Start();

			DnsInfo = dnsInfo;
			Name = name;

			task = InitNetworkTask(true);
		}

		public TcpForwarder(string name, Socket socket, DnsInfo dnsInfo)
		{
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;

			this.socket = socket;

			timer = new Timer(5000);
			timer.Elapsed += IdleControl;
			timer.AutoReset = false;
			timer.Start();

			DnsInfo = dnsInfo;
			Name = name;

			task = InitNetworkTask(false);
		}


		public void Close()
		{
			cancellationTokenSource.Cancel();
		}


		public async Task SendAsync(byte[] buffer, int offset, int length)
		{
			try
			{
				await stream.WriteAsync(buffer, offset, length);
				await stream.FlushAsync();
			}

			catch
			{
				cancellationTokenSource.Cancel();
			}
		}


		private Task InitNetworkTask(bool connect)
		{
			return Task.Factory.StartNew(async () =>
			{
				try
				{
					if (connect)
					{
						var timeoutTask = Task.Delay(2000);
						var connectTask = socket.ConnectAsync(DnsInfo.EndPoint);

						if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
						{
							if (!socket.Connected)
							{
								OnClosed("Refused");
								return;
							}
						}
					}

					stream = new NetworkStream(socket, true);

					if (DnsInfo.SSL)
					{
						var certFile = Path.Combine(
							Directory.GetCurrentDirectory(),
							"Certificates", DnsInfo.Hostname
						);

						x509Certificate2 = new X509Certificate2(certFile + ".pfx");
						x509Certificate2Collection = new X509Certificate2Collection(x509Certificate2);

						sslStream = new SslStream(stream, false, RemoteCertificateValidation);

						if (connect)
						{
							await sslStream.AuthenticateAsClientAsync(
								DnsInfo.Hostname, x509Certificate2Collection, SslProtocols.Tls12, false
							);
						}

						else
						{
							await sslStream.AuthenticateAsServerAsync(
								x509Certificate2, true, SslProtocols.Tls12, true
							);
						}
					}

					var buffer = new byte[4096];
					while (!cancellationToken.IsCancellationRequested)
					{
						var size = await stream.ReadAsync(buffer, 0, buffer.Length);

						if (size <= 0)
						{
							cancellationTokenSource.Cancel();
							break;
						}

						RegistryActivity();
						
						await OnBuffer(buffer, 0, size);
					}
				}

				catch (Exception ex)
				{
					OnClosed(ex.Message);
					return;
				}

				OnClosed("Closed");
			});
		}


		private void IdleControl(object? sender, ElapsedEventArgs e)
		{
			cancellationTokenSource.Cancel();
		}

		private void RegistryActivity()
		{
			timer.Stop();
			timer.Start();
		}

		private bool RemoteCertificateValidation(
			object sender,
			X509Certificate certificate,
			X509Chain chain,
			SslPolicyErrors sslPolicyErrors
		)
		{
			if (sslPolicyErrors == SslPolicyErrors.None) return true;
			if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors) return true;
			return false;
		}
	};
};