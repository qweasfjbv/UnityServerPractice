
namespace FPS.Utils
{
	public enum PacketType : byte
	{
		S2C_Pong = 1,
		S2C_Snapshot,

		C2S_Ping = 100,
		C2S_Input,
	}
}
