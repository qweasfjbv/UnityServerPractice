using UnityEngine.SceneManagement;

namespace FPS.Manager.Game
{
	public class SceneManagerEx
	{
		public void ChangeScene(string sceneName)
		{
			SceneManager.LoadScene(sceneName);
		}
	}
}