using System;
using Unity.Collections.LowLevel.Unsafe;

namespace FPS.Utils
{
	public static unsafe class Serializer
	{
		public static readonly int HEADER_SIZE = sizeof(PacketHeader);

		public static void WriteHeader(in PacketHeader header, Span<byte> destination)
		{
			if (destination.Length < HEADER_SIZE)
				throw new ArgumentException("Buffer too small for header");

			fixed (byte* destPtr = destination)
			fixed (PacketHeader* srcPtr = &header)
			{
				UnsafeUtility.MemCpy(destPtr, srcPtr, HEADER_SIZE);
			}
		}
		public static void WritePayload<T>(in T payload, Span<byte> destination) where T : unmanaged
		{
			int size = sizeof(T);
			if (destination.Length < size)
				throw new ArgumentException("Buffer too small for paylaod");

			fixed (byte* destPtr = destination)
			fixed(T* srcPtr = &payload)
			{
				UnsafeUtility.MemCpy(destPtr, srcPtr, size);
			}
		}
		public static int Serialize<T>(in PacketHeader header, in T payload, Span<byte> destination) where T : unmanaged
		{
			int payloadSize = sizeof(T);
			int totalSize = HEADER_SIZE + payloadSize;

			if (destination.Length < totalSize)
				throw new ArgumentException("Buffer too small for header + payload");

			WriteHeader(header, destination);
			WritePayload<T>(payload, destination.Slice(HEADER_SIZE, payloadSize));

			return totalSize;
		}

		public static PacketHeader ReadHeader(ReadOnlySpan<byte> source)
		{
			if (source.Length < HEADER_SIZE)
				throw new ArgumentException("Buffer too small to contain header");

			PacketHeader header;
			fixed (byte* srcPtr = source)
			{
				UnsafeUtility.MemCpy(&header, srcPtr, HEADER_SIZE);
			}
			return header;
		}
		public static T ReadPayload<T>(ReadOnlySpan<byte> source) where T : unmanaged
		{
			int size = sizeof(T);
			if (source.Length < size)
				throw new ArgumentException("Buffer too small to contain payload");

			T payload;
			fixed(byte* srcPtr = source)
			{
				UnsafeUtility.MemCpy(&payload, srcPtr, HEADER_SIZE);
			}
			return payload;
		}
		public static T Deserialize<T>(ReadOnlySpan<byte> source, out PacketHeader header) where T : unmanaged
		{
			int payloadSize = sizeof(T);
			int totalSize = HEADER_SIZE + payloadSize;

			if (source.Length < totalSize)
				throw new ArgumentException("Buffer too small to contain header + payload");

			header = ReadHeader(source);
			return ReadPayload<T>(source.Slice(HEADER_SIZE, payloadSize));
		}
	}
}