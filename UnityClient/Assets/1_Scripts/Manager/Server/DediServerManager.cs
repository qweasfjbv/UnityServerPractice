using FPS.Controller;
using FPS.Utils;
using System;
using System.Collections.Concurrent;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

namespace FPS.Manager.Server
{
	public class ClientConnection
	{
		public IPEndPoint endPoint;

		public int localId;

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

		public int id = 1;
		public Action<IPEndPoint, PlayerInput> OnGetInputAction { get; set; }

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
					localId = id++,
					isConnected = true,
				};
				clients.TryAdd(packet.sender, client);
			}

			PacketType type = (PacketType)packet.data[0];

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
						PlayerInput input = Serializer.Deserialize<PlayerInput>(out _, packet.data);
						OnGetInputAction.Invoke(packet.sender, input);
					}
					break;
				case PacketType.Spawn:
					{
						int newPlayerId = client.localId;
						byte[] spawnPacket = Serializer.Serialize(PacketType.Spawn, newPlayerId);

						foreach (var kv in clients)
						{
							Send(kv.Key, spawnPacket);
						}

						foreach (var kv in clients)
						{
							ClientConnection other = kv.Value;

							if (other.localId == newPlayerId)
								continue;

							byte[] existingPlayerSpawnPacket = Serializer.Serialize(
								PacketType.Spawn,
								other.localId
							);

							Send(client.endPoint, existingPlayerSpawnPacket);
						}
					}
					break;
			}
		}

		public override void Send(IPEndPoint destEP, byte[] payload)
		{
			if (destEP == null) return;
			base.Send(destEP, payload);
		}
	}
}
