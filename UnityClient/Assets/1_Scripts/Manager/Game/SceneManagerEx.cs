using UnityEngine.SceneManagement;

namespace Practice.Manager.Game
{
	public class SceneManagerEx
	{
		public void ChangeScene(string sceneName)
		{
			SceneManager.LoadScene(sceneName);
		}
	}
}