using Practice.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System;

namespace Practice.Manager.Server
{
	/// <summary>
	/// 
	/// Manager for Dedicated Server (Unity Headless Build)
	/// 
	/// - UDP-Based Connection
	/// - Send/Recv Player Input/State (for Client Side Prediction)
	/// 
	/// </summary>
	public class HeadlessManager
	{
		public enum PacketType : byte
		{
			Ping = 1,
			Input = 2,
			Snapshot = 3,
		}

		private UdpClient udpClient;
		private IPEndPoint serverEP;
		private Thread recvThread;

		private ConcurrentQueue<byte[]> recvQueue = new();

		private int localPort = 0;

		public void Init()
		{
			udpClient = new UdpClient(localPort);
			serverEP = new IPEndPoint(IPAddress.Parse(Constants.IP_ADDR), Constants.PORT_DEDI);

			recvThread = new Thread(ReceiveLoop);
			recvThread.IsBackground = true;
			recvThread.Start();
		}

		private void Shutdown()
		{
			recvThread?.Abort();
			udpClient?.Close();
		}

		public void Send(byte[] payload)
		{
			udpClient.Send(payload, payload.Length, serverEP);
		}

		private void ReceiveLoop()
		{
			try
			{
				while (true)
				{
					IPEndPoint remote = null;
					byte[] data = udpClient.Receive(ref remote);
					recvQueue.Enqueue(data);
				}
			}
			catch(Exception e)
			{
				Debug.Log($"UDP Receive stopped : {e}");
			}
		}

		private void HandlePacket(byte[] data)
		{
			PacketType type = (PacketType)data[0];

			switch (type)
			{
				case PacketType.Ping:
					break;
				case PacketType.Input:
					break;
				case PacketType.Snapshot:
					break;
			}
		}
	}
}
