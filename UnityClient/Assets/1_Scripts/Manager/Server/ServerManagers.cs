using System;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_SERVER
using System.IO;
#endif

namespace FPS.Manager.Server
{
	[DefaultExecutionOrder(-100)]
	public class ServerManagers : MonoBehaviour
	{
		private static ServerManagers instance;
		public static ServerManagers Instance { get => instance; }

		void Awake()
		{
			Init();

#if !UNITY_EDITOR && UNITY_SERVER
			var sw = new StreamWriter(Console.OpenStandardOutput());
			sw.AutoFlush = true;
			Console.SetOut(sw);

			Debug.unityLogger.logEnabled = true;
			Application.runInBackground = true;
			Application.logMessageReceived += OnLog;
#endif
		}

		private void Init()
		{
			if (null == instance)
			{
				instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
			else
			{
				Destroy(this.gameObject);
			}

			dedi.Init();
		}

		private void Update()
		{
			dedi.OnUpdate();
		}

		private void OnDestroy()
		{
			dedi.Shutdown();

#if !UNITY_EDITOR && UNITY_SERVER
			Application.logMessageReceived -= OnLog;
#endif
		}

		private static AuthManager auth = new AuthManager();
		private static LobbyManager lobby = new LobbyManager();

#if !UNITY_EDITOR && UNITY_SERVER
		private static UDPNetworkTransport dedi = new DediServerManager();
#else
		private static UDPNetworkTransport dedi = new DediClientManager();
#endif

		public static AuthManager Auth => auth;
		public static LobbyManager Lobby => lobby;
		public static UDPNetworkTransport Dedi => dedi;

		#region Utils
		private void OnLog(string msg, string stackTrace, LogType type)
		{
			Console.Out.Flush();
		}
		#endregion
	}
}
