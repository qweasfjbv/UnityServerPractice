using FPS.Controller;
using FPS.Manager.Game;
using FPS.Utils;
using System;
using System.Net;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;

namespace FPS.Manager.Server
{
	/// <summary>
	/// 
	/// Manager to communicate with Dedicated Server (Unity Headless)
	/// 
	/// - UDP-Based Connection
	/// - Send Pong, Player Input ...
	/// - Recv Ping, Player State ...
	/// 
	/// </summary>
	public class DediClientManager : UDPNetworkTransport
	{
		private PeerConnection serverConnection;

		public Action<PlayerState> OnGetSnapshotAction { get; set; }
		public Action<FireHitResult> OnGetFireHitResult { get; set; }

		public override void Init()
		{
			base.Init();

			// TODO - PORT will be changed by GameServerManager
			var serverEP = new IPEndPoint(IPAddress.Parse(Constants.IP_ADDR), Constants.PORT_DEDI);
			serverConnection = new PeerConnection(localSocket, serverEP, -1);

			// Send Ping per 1000ms
			new Thread(() =>
			{
				while (true)
				{
					Send(null, ChannelMode.Unreliable, PacketType.Ping, new EmptyPayload());
					Thread.Sleep(10);
				}
			}).Start();
			
			Send(null, ChannelMode.Reliable, PacketType.Spawn, new EmptyPayload());
			Debug.Log("DediClient Init");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			serverConnection.OnUpdate(TimeSpan.FromMilliseconds(100));
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			var header = serverConnection.ProcessPacket(packet.data, out var readyPackets);
			var payloadSpan = packet.data.AsSpan().Slice(Serializer.HEADER_SIZE);

			foreach (var (packetHeader, payload) in readyPackets)
			{
				Dispatch(packetHeader, payload);
			}
		}

		private void Dispatch(in PacketHeader header, ReadOnlySpan<byte> payloadSpan)
		{
			switch (header.type)
			{
				case PacketType.Pong:	Debug.Log("Pong Received"); break;
				case PacketType.Spawn:
					SpawnData data = Serializer.ReadPayload<SpawnData>(payloadSpan);
					GameManagerEx.Instance.SpawnPlayerObject(data);
					break;
				case PacketType.Init:
					GameManagerEx.Instance.MyLocalId = Serializer.ReadPayload<int>(payloadSpan);
					break;
				default:
					HandleData(header, payloadSpan);
					break;
			}
		}
		private void HandleData(in PacketHeader header, ReadOnlySpan<byte> payloadSpan)
		{
			switch (header.type)
			{
				case PacketType.S2C_Snapshot:
					{
						PlayerState snapshot = Serializer.ReadPayload<PlayerState>(payloadSpan);
						GameManagerEx.Instance.UpdatePlayerState(snapshot);
					}
					break;
				case PacketType.S2C_StateUpdate:
					{
						NetworkPlayerState playerState = Serializer.ReadPayload<NetworkPlayerState>(payloadSpan);
						GameManagerEx.Instance.UpdatePlayerState(playerState);
					}
					break;
				case PacketType.S2C_HitResult:
					{
						FireHitResult hitResult = Serializer.ReadPayload<FireHitResult>(payloadSpan);
						OnGetFireHitResult?.Invoke(hitResult);
					}
					break;
			}
		}

		public override void Send<T>(IPEndPoint destEP, ChannelMode channel, PacketType type, in T payload)
		{
			if (serverConnection == null)
			{
				Debug.LogWarning("Server Endpoint doesn't exist.");
				return;
			}

			if (destEP != null && destEP != serverConnection.RemoteEndPoint)
			{
				Debug.LogWarning("[DediClinetManager] - wrong destination.");
				return;
			}

			serverConnection.Send(channel, type, payload);
		}
	}
}
