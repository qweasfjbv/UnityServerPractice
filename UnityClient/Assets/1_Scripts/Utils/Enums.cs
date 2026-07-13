
namespace FPS.Utils
{
	public enum ChannelMode : byte
	{
		Unreliable,
		Reliable,
		ReliableOrdered
	}

	public enum PacketType : byte
	{
		S2C_Snapshot = 1,		
		S2C_StateUpdate,
		S2C_HitResult,
		
		C2S_Input = 100,

		Init = 200,
		Spawn,
		Ping, Pong,
		Ack
	}
}
