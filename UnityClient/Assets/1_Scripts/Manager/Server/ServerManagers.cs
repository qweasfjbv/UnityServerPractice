using UnityEngine;

namespace Practice.Manager.Server
{
	[DefaultExecutionOrder(-100)]
	public class ServerManagers : MonoBehaviour
	{
		private static ServerManagers instance;
		public static ServerManagers Instance { get => instance; }

		void Awake()
		{
			Init();
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
		}

		private static AuthManager auth = new AuthManager();
		private static LobbyManager lobby = new LobbyManager();

#if UNITY_SERVER
		private static UDPNetworkTransport dedi = new DediServerManager();
#else
		private static UDPNetworkTransport dedi = new DediClientManager();
#endif

		public static AuthManager Auth => auth;
		public static LobbyManager Lobby => lobby;
		public static UDPNetworkTransport Dedi => dedi;
	}
}
