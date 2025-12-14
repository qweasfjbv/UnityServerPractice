using UnityEngine;

namespace Practice.Manager.Game
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

		public static SceneManagerEx Scene => scene;
	}
}