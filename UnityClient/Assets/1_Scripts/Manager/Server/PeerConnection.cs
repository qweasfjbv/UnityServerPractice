using FPS.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityEngine.SocialPlatforms;

namespace FPS.Manager.Server
{
	public class RetransmitEntry
	{
		public byte[] data;          // 
		public int length;           // 
		public DateTime sentAt;      // last sent time
		public int retryCount;       // Try counts
	}

	public class ChannelState
	{
		public bool IsReliable { get; set; }
		public bool IsOrdered { get; set; }

		// For Sending
		private ushort localSequence = 0;
		public ushort NextSequence() => localSequence++;
		private readonly Dictionary<ushort, RetransmitEntry> pendingAcks = new();

		// For Receiving
		public ushort LastReceivedSequence { get; private set; }
		private bool hasReceivedAny = false;
		private uint ackBitField = 0;
		public uint AckBitfield => ackBitField;

		public ChannelState(ChannelMode mode)
		{
			switch (mode)
			{
				case ChannelMode.Unreliable:
					IsReliable = false;
					IsOrdered = false;
					break;
				case ChannelMode.Reliable:
					IsReliable = true;
					IsOrdered = false;
					break;
				case ChannelMode.ReliableOrdered:
					IsReliable = true;
					IsOrdered = true;
					break;
			}

			localSequence = 0;
			ackBitField = 0;
			hasReceivedAny = false;
		}

		public void OnPacketReceived(ushort seq)
		{
			if (!hasReceivedAny)
			{
				hasReceivedAny = true;
				LastReceivedSequence = seq;
				ackBitField = 0;
				return;
			}

			if (IsNewer(seq, LastReceivedSequence))
			{
				// 새 시퀀스가 최신 -> 비트필드를 밀고, 이전 LastReceived 위치를 1로 표시
				int shift = (ushort)(seq - LastReceivedSequence);

				if (shift >= 32) 
					ackBitField = 0;
				else 
					ackBitField = (ackBitField << shift) | (1u << (shift - 1));

				LastReceivedSequence = seq;
			}
			else
			{
				// 과거 시퀀스가 뒤늦게 도착 -> 해당 비트만 표시
				int diff = (ushort)(LastReceivedSequence - seq);
				if (diff >= 1 && diff <= 32)
				{
					ackBitField |= (1u << (diff - 1));
				}
			}
		}

		public void RegisterForRetransmit(ushort sequence, ReadOnlySpan<byte> data)
		{
			var buffer = new byte[data.Length];
			data.CopyTo(buffer);
			pendingAcks[sequence] = new RetransmitEntry
			{
				data = buffer,
				length = data.Length,
				sentAt = DateTime.UtcNow,
				retryCount = 0,
			};
		}

		public void OnAckReceived(ushort ackedSequence, uint ackBitfield)
		{
			pendingAcks.Remove(ackedSequence);

			for (int i = 0; i < 32; i++)
			{
				if ((ackBitfield & (1u << i)) != 0)
					pendingAcks.Remove((ushort)(ackedSequence - i - 1));
			}
		}

		public static bool IsNewer(ushort a, ushort b) => (ushort)(a - b) < (ushort.MaxValue / 2  + 1);
	}

	public class PeerConnection
	{
		private readonly Socket socket;
		private readonly IPEndPoint remoteEndPoint;
		private readonly ChannelState[] channels;

		public IPEndPoint RemoteEndPoint => remoteEndPoint;
		public int LocalID { get; private set; }

		public PeerConnection(Socket socket, IPEndPoint remoteEndPoint, int localId)
		{
			this.socket = socket;
			this.remoteEndPoint = remoteEndPoint;
			this.LocalID = localId;

			int enumCounts = Enum.GetNames(typeof(ChannelMode)).Length;
			channels = new ChannelState[enumCounts];
			for (int i = 0; i < enumCounts; i++)
			{
				channels[i] = new ChannelState((ChannelMode)i);
			}
		}

		// Send udp packet with payload
		public void Send<T>(ChannelMode channel, PacketType type, in T payload) where T : unmanaged
		{
			var channelState = channels[(byte)channel];

			var header = new PacketHeader
			{
				type = type,
				channel = channel,
				sequence = channelState.NextSequence(),
				ack = channelState.LastReceivedSequence,
				ackBitfield = channelState.AckBitfield
			};

			byte[] rented = ArrayPool<byte>.Shared.Rent(Serializer.HEADER_SIZE + Unsafe.SizeOf<T>());
			try
			{
				int size = Serializer.Serialize(header, payload, rented.AsSpan());
				
				if(channelState.IsReliable)
				{
					channelState.RegisterForRetransmit(header.sequence, rented.AsSpan(0, size));
				}

				socket.SendTo(rented, 0, size, SocketFlags.None, remoteEndPoint);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(rented);
			}
		}

		// Process Received packet's header
		public PacketHeader ProcessPacket(ReadOnlySpan<byte> data)
		{
			var header = Serializer.ReadHeader(data);
			var channelState = channels[(byte)header.channel];

			channelState.OnPacketReceived(header.sequence);
			channelState.OnAckReceived(header.ack, header.ackBitfield);

			return header;
		}
	}
}
