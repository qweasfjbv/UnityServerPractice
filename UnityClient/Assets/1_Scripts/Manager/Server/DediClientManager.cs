using Practice.Controller;
using Practice.Utils;
using System;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Practice.Manager.Server
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
			new Thread(() =>
			{
				while (true)
				{
					Send(serverEP, Serializer.Serialize<long>(PacketType.C2S_Ping, NetworkTimer.NowMs()));
					Thread.Sleep(1000);
				}
			}).Start();

			Debug.Log("DediClient Init");
		}

		public override void OnUpdate()
		{
			base.OnUpdate();

			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				Send(serverEP, Serializer.Serialize<int>(PacketType.C2S_Ping, 1));
			}
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			PacketType type = (PacketType)packet.data[0];
			switch (type)
			{
				case PacketType.S2C_Pong:
					Debug.Log("Ping Latency : " + (NetworkTimer.NowMs() - Serializer.Deserialize<long>(out _, packet.data)));
					break;
				case PacketType.S2C_Snapshot:
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
