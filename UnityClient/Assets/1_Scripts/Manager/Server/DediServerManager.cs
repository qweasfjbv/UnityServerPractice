using Practice.Controller;
using Practice.Utils;
using System.Collections.Concurrent;
using System.Net;
using UnityEngine;

namespace Practice.Manager.Server
{
	public class ClientConnection
	{
		public IPEndPoint endPoint;

		// Network
		public long lastRecvTick;
		public long lastRecvTime;

		// Client Side Preidction
		public int lastProcessedInputTick;
		public PlayerInput lastInput;

		// Game State
		public PlayerState serverState;

		// Security
		public bool isConnected;
	}

	/// <summary>
	/// 
	/// Manager for Dedicated Server (Unity Headless Build)
	/// 
	/// - UDP-Based Connection
	/// - Send Ping, Player State ...
	/// - Recv Pong, Player Input ...
	/// 
	/// </summary>
	public class DediServerManager : UDPNetworkTransport
	{
		private ConcurrentDictionary<IPEndPoint, ClientConnection> clients = new();

		public override void Init()
		{
			base.Init();

			Debug.Log("DediServer Init");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			// TODO - Add all player when game starts
			ClientConnection client;
			if (!clients.TryGetValue(packet.sender, out client))
			{
				client = new ClientConnection
				{
					endPoint = packet.sender,
					isConnected = true
				};
				clients.TryAdd(packet.sender, client);
			}

			PacketType type = (PacketType)packet.data[0];
			Debug.Log(packet.sender.ToString() + " : " + type.ToString());

			switch (type)
			{
				case PacketType.C2S_Ping:
					{
						// Response Ping-Pong
						long clientTime = Serializer.Deserialize<long>(out _, packet.data);
						Send(packet.sender, Serializer.Serialize<long>(PacketType.S2C_Pong, clientTime));

						client.lastRecvTime = NetworkTimer.NowMs();
						client.lastRecvTick = NetworkTimer.NowTicks();
					}
					break;
				case PacketType.C2S_Input:
					{

					}
					break;
			}
		}
	}
}
