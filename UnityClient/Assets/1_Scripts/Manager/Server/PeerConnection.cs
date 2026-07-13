using FPS.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;

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

		private readonly Dictionary<ushort, (PacketHeader header, byte[] payload)> reorderBuffer = new();
		private ushort expectedSequence;
		private bool hasExpected;

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
			expectedSequence = 0;

			hasReceivedAny = false;
			hasExpected = false;
		}

		// 채널에 따라 패킷 처리 및 처리해야하는 패킷 반환
		public List<(PacketHeader header, byte[] payload)> OnPacketReceived(PacketHeader header, ReadOnlySpan<byte> payload)
		{
			bool isDuplicate = IsDuplicate(header.sequence);
			UpdateReceiveState(header.sequence);

			var result = new List<(PacketHeader, byte[])>();

			if (!IsReliable)
			{
				result.Add((header, payload.ToArray()));
				return result;
			}

			if (isDuplicate)
				return result;

			if (!IsOrdered)
			{
				result.Add((header, payload.ToArray()));
				return result;
			}

			if (!hasExpected)
			{
				expectedSequence = header.sequence;
				hasExpected = true;
			}

			reorderBuffer[header.sequence] = (header, payload.ToArray());
			while (reorderBuffer.TryGetValue(expectedSequence, out var entry))
			{
				result.Add(entry);
				reorderBuffer.Remove(expectedSequence);
				expectedSequence++;
			}
			return result;
		}

		private void UpdateReceiveState(ushort seq)
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

		public void ScanAndResend(Socket socket, IPEndPoint remoteEP, TimeSpan interval, int maxRetry)
		{
			var now = DateTime.UtcNow;
			List<ushort> toDrop = null;

			foreach (var kv in pendingAcks)
			{
				var entry = kv.Value;
				if (now - entry.sentAt < interval) continue;

				if (entry.retryCount >= maxRetry)
				{
					(toDrop ??= new List<ushort>()).Add(kv.Key); // 포기 (연결 끊김 처리는 상위에서)
					continue;
				}

				socket.SendTo(entry.data, 0, entry.length, SocketFlags.None, remoteEP);
				entry.sentAt = now;
				entry.retryCount++;
			}

			if (toDrop != null)
				foreach (var seq in toDrop) pendingAcks.Remove(seq);
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

		private bool IsDuplicate(ushort seq)
		{
			if (!hasReceivedAny) return false;
			if (seq == LastReceivedSequence) return true;			// 정확히 재전송된 최신 패킷
			if (IsNewer(seq, LastReceivedSequence)) return false;   // 새 것 -> 중복 X

			int diff = (ushort)(LastReceivedSequence - seq);
			if (diff >= 1 && diff <= 32)
				return (AckBitfield & (1u << (diff - 1))) != 0;		// 비트가 이미 있으면 중복
			return false;
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
		
		// 주기적으로 호출 필요
		public void OnUpdate(TimeSpan resendInterval, int maxRetry = 10)
		{
			foreach (var channelState in channels)
			{
				if (!channelState.IsReliable) continue;
				channelState.ScanAndResend(socket, remoteEndPoint, resendInterval, maxRetry);
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
		public PacketHeader ProcessPacket(ReadOnlySpan<byte> data, out List<(PacketHeader header, byte[] payload)> readyPackets)
		{
			var header = Serializer.ReadHeader(data);
			var channelState = channels[(byte)header.channel];
			var payload = data.Slice(Serializer.HEADER_SIZE);

			readyPackets = channelState.OnPacketReceived(header, payload);
			channelState.OnAckReceived(header.ack, header.ackBitfield);

			return header;
		}

	}
}
