using Practice.Controller;
using Practice.Utils;
using System.Collections.Generic;
using System.Net;

namespace Practice.Manager.Server
{
	public class ClientConnection
	{
		public IPEndPoint endPoint;

		// Network
		public int lastRecvTick;
		public float lastRecvTime;

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
		private Dictionary<IPEndPoint, ClientConnection> clients = new();

		protected override void HandlePacket(in UdpPacket packet)
		{
			if (!clients.TryGetValue(packet.sender, out var client))
			{
				client = new ClientConnection
				{
					endPoint = packet.sender,
					isConnected = true
				};
				clients.Add(packet.sender, client);
			}

			PacketType type = (PacketType)packet.data[0];
			switch (type)
			{
				case PacketType.C2S_Pong:
					break;
				case PacketType.C2S_Input:
					break;
			}
		}
	}
}
