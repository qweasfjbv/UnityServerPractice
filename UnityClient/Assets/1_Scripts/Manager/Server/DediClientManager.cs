using Practice.Utils;
using System.Net;

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
			serverEP = new IPEndPoint(IPAddress.Parse(Constants.IP_ADDR), Constants.PORT_DEDI);
		}

		protected override void HandlePacket(in UdpPacket packet)
		{
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
