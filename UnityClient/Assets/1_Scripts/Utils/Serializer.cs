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

		public static T Deserialize<T>(byte[] buffer) where T : unmanaged
		{
			T data;
			fixed (byte* ptr = buffer)
			{
				UnsafeUtility.MemCpy(&data, ptr, sizeof(T));
			}
			return data;
		}
	}
}
