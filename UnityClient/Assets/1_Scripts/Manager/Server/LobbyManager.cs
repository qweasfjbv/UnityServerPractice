using Practice.Utils;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Practice.Manager.Server
{
	public class LobbyManager
	{
		private TcpClient client;
		private NetworkStream stream;

		private Queue<byte[]> sendQueue = new Queue<byte[]>();
		private bool isSending = false;
		object sendLock = new object();

		public async void ConnectLobbyServer()
		{
			client = new TcpClient();
			await client.ConnectAsync(Constants.IP_ADDR, Constants.PORT_LOBBY);
			stream = client.GetStream();

			Debug.Log("Connected");
			_ = ReceiveLoop();
		}
		public void JoinMatchQueue()
		{

		}

		public void LeaveMatchQueue()
		{

		}

		public void SendString(string msg)
		{
			if (client == null || !client.Connected || stream == null) return;

			byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
			EnqueueSend(data);
		}

		private void EnqueueSend(byte[] data)
		{
			lock (sendLock)
			{
				sendQueue.Enqueue(data);
				if (!isSending)
				{
					_ = SendLoop();
				}
			}
		}

		async Task SendLoop()
		{
			isSending = true;

			while (true)
			{
				byte[] data = null;

				lock (sendLock)
				{
					if (sendQueue.Count == 0)
					{
						isSending = false;
						return;
					}

					data = sendQueue.Dequeue();
				}

				try
				{
					await stream.WriteAsync(data, 0, data.Length);
				}
				catch (System.Exception e)
				{
					Debug.LogError("Send failed: " + e);
					Close();
					return;
				}
			}
		}

		private async Task ReceiveLoop()
		{
			byte[] buffer = new byte[4096];

			try
			{
				while (client.Connected)
				{
					int size = await stream.ReadAsync(buffer, 0, buffer.Length);
					if (size <= 0)
						break;

					string msg = Encoding.UTF8.GetString(buffer, 0, size);
					Debug.Log("RECV: " + msg);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("Recv failed: " + e);
			}

			Close();
		}

		public void Close()
		{
			Debug.Log("Disconnected");

			stream?.Close();
			client?.Close();
		}

	}
}