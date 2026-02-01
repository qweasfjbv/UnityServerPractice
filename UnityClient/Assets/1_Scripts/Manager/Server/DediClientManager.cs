using Practice.Utils;
using System.Net;
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

		public override void Init()
		{
			base.Init();
			Debug.Log("DediClient Init");
			// TODO - PORT will be changed by GameServerManager
			serverEP = new IPEndPoint(IPAddress.Parse(Constants.IP_ADDR), Constants.PORT_DEDI);
		}

		public override void OnUpdate()
		{
			base.OnUpdate();

			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				Send(serverEP, Serializer.Serialize<int>(PacketType.S2C_Ping, 1));
			}
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
			Debug.Log("Client Get Packet : ");
			PacketType type = (PacketType)packet.data[0];
			switch (type)
			{
				case PacketType.S2C_Ping:
					break;
				case PacketType.S2C_Snapshot:
					break;
			}
		}
	}
}
