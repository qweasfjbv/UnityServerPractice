using FPS.Controller;
using FPS.Manager.Game;
using FPS.Utils;
using System;
using System.Collections.Concurrent;
using System.Net;
using UnityEngine;

namespace FPS.Manager.Server
{
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
		private readonly ConcurrentDictionary<string, int> endpointToLocalID = new();   // IPEndPoint.ToString() -> localID
		private readonly ConcurrentDictionary<int, PeerConnection> peers = new();       // localID -> PeerConnection

		private int idProvider = 1;

		private int serverTick = 0;
		public int ServerTick => serverTick;

		public override void Init()
		{
			base.Init();

			Debug.Log("DediServer Init");
		}

		private float timer = 0f;
		public override void OnUpdate()
		{
			base.OnUpdate();

			timer += Time.deltaTime;

			while (timer >= Constants.TICK_DT)
			{
				serverTick++;
				BroadcastOtherSnapshot();
				timer -= Constants.TICK_DT;
			}
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			PeerConnection client = null;
			if (!TryGetPeer(packet.sender, out var peer))
			{
				Debug.LogWarning($"[DediServerManager] Unknown Peer: {packet.sender}");

				client = new PeerConnection(localSocket, packet.sender, idProvider++);
				peers.TryAdd(client.LocalID, client);
				endpointToLocalID.TryAdd(client.RemoteEndPoint.ToString(), client.LocalID);

				Send(client.LocalID, ChannelMode.Reliable, PacketType.Init, client.LocalID);
			}

			var header = peer.ProcessPacket(packet.data);

			switch (header.type)
			{
				case PacketType.Ack:
					// NOTHING TO DO
					break;
				case PacketType.Ping:
					peer.Send(header.channel, PacketType.Pong, new EmptyPayload());
					break;
				case PacketType.Spawn:
					{
						SpawnData spawndata;
						spawndata.startTick = serverTick;
						spawndata.localId = peer.LocalID;

						GameManagerEx.Instance.SpawnPlayerObjectOnServer(spawndata);

						foreach (var kv in peers)
						{
							kv.Value.Send(ChannelMode.Reliable, PacketType.Spawn, spawndata);
						}

						foreach (var kv in peers)
						{
							var other = kv.Value;

							if (other.LocalID == spawndata.localId)
								continue;

							spawndata.localId = other.LocalID;
							Send(peer.RemoteEndPoint, ChannelMode.Reliable, PacketType.Spawn, spawndata);
						}
					}
					break;
				default:
					HandleData(peer, header, packet.data.AsSpan().Slice(Serializer.HEADER_SIZE));
					break;
			}
		}

		private void HandleData(PeerConnection peer, in PacketHeader header, ReadOnlySpan<byte> payloadSpan)
		{
			switch (header.type)
			{
				case PacketType.C2S_Input:
					{
						PlayerInput input = Serializer.ReadPayload<PlayerInput>(payloadSpan);
						GameManagerEx.Instance.OnGetPlayerInput(peer.LocalID, input);
					}
					break;
			}
		}

		private void BroadcastOtherSnapshot()
		{
			foreach (var kv1 in peers)
			{
				var sender = kv1.Value;

				GameObject playerObject = GameManagerEx.Instance.GetPlayerObject(sender.LocalID);
				if (playerObject == null) continue;

				foreach (var kv2 in peers)
				{
					var receiver = kv2.Value;

					if (receiver.LocalID == sender.LocalID)
						continue;

					Send(receiver.RemoteEndPoint, ChannelMode.Unreliable, PacketType.S2C_StateUpdate, 
						playerObject.GetComponent<PlayerController>().GetNetworkPlayerState(sender.LocalID));
				}
			}
		}

		private bool TryGetPeer(IPEndPoint sender, out PeerConnection peer)
		{
			string key = sender.ToString();

			if (!endpointToLocalID.TryGetValue(key, out int localId))
			{
				peer = null;
				return false;
			}

			if (!peers.TryGetValue(localId, out peer))
			{
				return false;
			}

			return true;
		}

		private bool TryGetLocalId(IPEndPoint sender, out int localId)
		{
			if (!TryGetPeer(sender, out PeerConnection peer))
			{
				localId = -1;
				return false;
			}

			localId = peer.LocalID;
			return true;
		}


		public void Send<T>(int localId, ChannelMode channel, PacketType type, in T payload)
			where T: unmanaged
		{
			if (!peers.TryGetValue(localId, out PeerConnection peer))
			{
				Debug.LogWarning("[DediServerManager] - localID doesn't exist.");
				return;
			}

			peer.Send<T>(channel, type, payload);
		}

		public override void Send<T>(IPEndPoint destEP,
			ChannelMode channel,
			PacketType type,
			in T payload)
		{
			if (destEP == null)	// null -> broadcast
			{
				foreach (var kv in peers)
					kv.Value.Send(channel, type, payload);

				return;
			}

			if (!TryGetPeer(destEP, out PeerConnection peer))
			{
				return;
			}

			peer.Send<T>(channel, type, payload);
		}
	}
}
