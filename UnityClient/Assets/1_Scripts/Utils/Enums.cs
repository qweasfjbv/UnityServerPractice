
namespace Practice.Utils
{
	public enum PacketType : byte
	{
		S2C_Ping = 1,
		S2C_Snapshot = 2,

		C2S_Pong = 100,
		C2S_Input,
	}
}
