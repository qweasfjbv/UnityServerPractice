
namespace Practice.Utils
{
	public enum PacketType : byte
	{
		S2C_Snapshot = 1,

		C2S_Ping = 100,
		C2S_Input,
	}
}
