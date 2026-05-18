using FPS.Controller;
using FPS.Manager.Game;
using FPS.Utils;
using System;
using System.Net;
using System.Threading;
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
		private IPEndPoint serverEP;

		public Action<PlayerState> OnGetSnapshotAction { get; set; }

		public override void Init()
		{
			base.Init();

			// TODO - PORT will be changed by GameServerManager
			serverEP = new IPEndPoint(IPAddress.Parse(Constants.IP_ADDR), Constants.PORT_DEDI);
			
			// Send Ping per 1000ms
			new Thread(() =>
			{
				while (true)
				{
					Send(serverEP, Serializer.Serialize<long>(PacketType.C2S_Ping, NetworkTimer.NowMs()));
					Thread.Sleep(1000);
				}
			}).Start();

			Send(serverEP, Serializer.Serialize(PacketType.Spawn));
			Debug.Log("DediClient Init");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			PacketType type = (PacketType)packet.data[0];
			switch (type)
			{
				case PacketType.S2C_Pong:
					{
						// Debug.Log("Ping Latency : " + (NetworkTimer.NowMs() - Serializer.Deserialize<long>(out _, packet.data)));
					}
					break;
				case PacketType.S2C_Snapshot:
					{
						PlayerState snapshot = Serializer.Deserialize<PlayerState>(out _, packet.data);
						GameManagerEx.Instance.UpdatePlayerState(snapshot);
					}
					break;
				case PacketType.S2C_StateUpdate:
					{
						NetworkPlayerState playerState = Serializer.Deserialize<NetworkPlayerState>(out _, packet.data);
						GameManagerEx.Instance.UpdatePlayerState(playerState);
					}
					break;
				case PacketType.Spawn:
					{
						int localId = Serializer.Deserialize<int>(out _, packet.data);
						GameManagerEx.Instance.SpawnPlayerObject(localId);
					}
					break;
				case PacketType.Init:
					{
						GameManagerEx.Instance.LocalId = Serializer.Deserialize<int>(out _, packet.data);
					}
					break;
			}
		}

		public override void Send(IPEndPoint destEP, byte[] payload)
		{
			if (destEP == null) destEP = serverEP;
			base.Send(destEP, payload);
		}
	}
}
