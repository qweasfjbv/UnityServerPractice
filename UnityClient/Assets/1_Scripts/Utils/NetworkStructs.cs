using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Practice.Utils
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct NetVector3
	{
		public float x;
		public float y;
		public float z;

		public NetVector3(float x, float y, float z)
		{
			this.x = x; this.y = y; this.z = z;
		}

		public Vector3 ToUnity() => new Vector3(x, y, z);
		public static NetVector3 FromUnity(Vector3 v) => new NetVector3
		{
			x = v.X,
			y = v.Y,
			z = v.Z
		};
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct NetVector2
	{
		public float x;
		public float y;

		public NetVector2(float x, float y)
		{
			this.x = x; this.y = y;
		}

		public Vector2 ToUnity() => new Vector2(x, y);
		public static NetVector2 FromUnity(Vector2 v) => new NetVector2
		{
			x = v.X,
			y = v.Y
		};
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct UdpPacket
	{
		public byte[] data;
		public IPEndPoint sender;
	}

}
