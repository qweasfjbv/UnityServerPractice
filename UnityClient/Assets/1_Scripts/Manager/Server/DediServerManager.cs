using FPS.Controller;
using FPS.Manager.Game;
using FPS.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
		public bool isConnected = true;
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
		public Action<int, PlayerInput> OnGetInputAction { get; set; }

		public override void Init()
		{
			base.Init();

			Debug.Log("DediServer Init");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var senderPair in clients)
			{
				ClientConnection sender = senderPair.Value;

				GameObject playerObject = GameManagerEx.Instance.GetPlayerObject(sender.localId);
				if (playerObject == null) continue;

				byte[] payload = Serializer.Serialize(
					PacketType.S2C_StateUpdate,
					playerObject.GetComponent<PlayerController>().GetNetworkPlayerState(sender.localId)
				);

				foreach (var receiverPair in clients)
				{
					ClientConnection receiver = receiverPair.Value;

					if (!receiver.isConnected)
						continue;

					if (receiver.localId == sender.localId)
						continue;

					Send(receiver.endPoint, payload);
				}
			}
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

				Send(packet.sender, Serializer.Serialize<int>(PacketType.Init, client.localId));
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
						OnGetInputAction?.Invoke(client.localId, input);
					}
					break;
				case PacketType.Spawn:
					{
						int newPlayerId = client.localId;
						byte[] spawnPacket = Serializer.Serialize(PacketType.Spawn, newPlayerId);
						GameManagerEx.Instance.SpawnPlayerObjectOnServer(newPlayerId);

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

		public override void Send(int localId, byte[] payload)
		{
			IPEndPoint? endPoint = clients
				.FirstOrDefault(kvp => kvp.Value.localId == id)
				.Key;

			if (endPoint == null) return;
			Send(endPoint, payload);
		}
	}
}
