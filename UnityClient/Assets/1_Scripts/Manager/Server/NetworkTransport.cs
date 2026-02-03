using Practice.Utils;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Practice.Manager.Server
{
	/// <summary>
	/// 
	/// Base class of NetworkTransportManagers
	/// 
	/// </summary>
	public abstract class UDPNetworkTransport
	{
		protected UdpClient udp;
		protected Thread recvThread;
		protected bool isRunning;

		protected IPEndPoint localEP;
		protected ConcurrentQueue<UdpPacket> recvQueue = new();

		// Deserialize, Switch packetType, Update game state ...
		public virtual void Init()
		{
			localEP = new IPEndPoint(IPAddress.Any, Constants.PORT_DEDI);

			udp = new UdpClient(localEP);

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
				Debug.Log("Handle Packet!");
				HandlePacket(packet);
			}
		}
		public virtual void Shutdown()
		{
			isRunning = false;
			recvThread?.Abort();
			udp?.Close();
		}

		protected abstract void HandlePacket(in UdpPacket packet);

		protected void ReceiveLoop()
		{
			try
			{
				while (true)
				{
					IPEndPoint sender = null;
					UdpPacket packet;
					packet.data = udp.Receive(ref sender);
					packet.sender = sender;

					recvQueue.Enqueue(packet);
				}
			}
			catch (Exception e)
			{
				Debug.Log($"UDP Receive stopped : {e}");
			}
		}
		protected void Send(IPEndPoint destEP, byte[] payload)
		{
			udp.Send(payload, payload.Length, destEP);
		}
	}
}
