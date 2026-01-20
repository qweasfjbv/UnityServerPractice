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
		}

		private static AuthManager auth = new AuthManager();
		private static LobbyManager lobby = new LobbyManager();
		private static HeadlessManager headless = new HeadlessManager();

		public static AuthManager Auth => auth;
		public static LobbyManager Lobby => lobby;
		public static HeadlessManager Headless => headless;
	}
}
