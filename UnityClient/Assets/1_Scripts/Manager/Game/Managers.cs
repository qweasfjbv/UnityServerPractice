using UnityEngine;

namespace FPS.Manager.Game
{
	[DefaultExecutionOrder(-100)]
	public class Managers : MonoBehaviour
	{
		private static Managers instance;
		public static Managers Instance { get => instance; }

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

		private static SceneManagerEx scene = new SceneManagerEx();
		private static InputManager input = new InputManager();

		public static SceneManagerEx Scene => scene;
		public static InputManager Input => input;	
	}
}