
namespace FPS.Utils
{
	public enum PacketType : byte
	{
		S2C_Pong = 1,
		S2C_Snapshot,		
		S2C_StateUpdate,
		S2C_HitResult,
		
		C2S_Ping = 100,
		C2S_Input,
		C2S_AnimParam,

		Init = 200,
		Spawn,
	}
}
