using Unity.Collections.LowLevel.Unsafe;

namespace Practice.Utils
{
	public static unsafe class Serializer
	{
		public static byte[] Serialize<T>(T data) where T : unmanaged
		{
			int size = sizeof(T);
			byte[] buffer = new byte[size];

			fixed (byte* ptr = buffer)
			{
				UnsafeUtility.MemCpy(ptr, &data, size);
			}

			return buffer;
		}
		public static byte[] Serialize<T>(PacketType type, T data) where T : unmanaged
		{
			int size = sizeof(T);
			byte[] buffer = new byte[size + 1];

			buffer[0] = (byte)type;
			fixed (byte* ptr = buffer)
			{
				UnsafeUtility.MemCpy(ptr + 1, &data, size);
			}

			return buffer;
		}

		public static T Deserialize<T>(byte[] buffer) where T : unmanaged
		{
			T data;
			fixed (byte* ptr = buffer)
			{
				UnsafeUtility.MemCpy(&data, ptr, sizeof(T));
			}
			return data;
		}
		public static T Deserialize<T>(out PacketType type, byte[] buffer) where T : unmanaged
		{
			type = (PacketType)buffer[0];

			T data;
			fixed (byte* ptr = buffer)
			{
				UnsafeUtility.MemCpy(&data, ptr + 1, sizeof(T));
			}
			return data;
		}
	}
}
