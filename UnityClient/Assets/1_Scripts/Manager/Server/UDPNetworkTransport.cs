using FPS.Utils;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace FPS.Manager.Server
{
	/// <summary>
	/// 
	/// Base class of NetworkTransportManagers
	/// 
	/// </summary>
	public abstract class UDPNetworkTransport
	{
		protected Socket localSocket;
		protected IPEndPoint localEP;
		
		protected bool isRunning;
		protected Thread recvThread;
		protected ConcurrentQueue<UdpPacket> recvQueue = new();

		// Deserialize, Switch packetType, Update game state ...
		public virtual void Init()
		{
			localEP = new IPEndPoint(IPAddress.Any, Constants.PORT_DEDI);
			localSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			recvThread = new Thread(ReceiveLoop);
			recvThread.IsBackground = true;
			isRunning = true;
			recvThread.Start();
		}
		public virtual void OnUpdate()
		{
			// Process packet on main thread
			while (recvQueue.TryDequeue(out UdpPacket packet))
			{
				HandlePacket(packet);
			}
		}
		public virtual void Shutdown()
		{
			isRunning = false;
			recvThread?.Abort();
			localSocket?.Close();
		}

		public abstract void Send<T>(IPEndPoint destEP,
			ChannelMode channel,
			PacketType type,
			in T payload) where T : unmanaged;

		protected abstract void HandlePacket(in UdpPacket packet);

		protected void ReceiveLoop()
		{
			byte[] buffer = new byte[2048];
			EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			
			while (isRunning)
			{
				try
				{
					int received = localSocket.ReceiveFrom(buffer, ref sender);

					byte[] data = new byte[received];
					Array.Copy(buffer, data, received);

					recvQueue.Enqueue(new UdpPacket
					{
						data = data,
						sender = (IPEndPoint)sender
					});
				}
				catch (SocketException)
				{
					if (!isRunning) break;
					// Temporal Err - continue
				}
				catch (Exception e)
				{
					Debug.Log($"UDP Receive stopped : {e}");
					break;
				}
			}
		}
	}
}
