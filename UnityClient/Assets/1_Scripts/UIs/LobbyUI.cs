using Practice.Manager.Server;
using UnityEngine;
using UnityEngine.UI;

namespace Practice.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Button profileButton;

		private void Awake()
		{
			profileButton.onClick.AddListener(() =>
			{
				ServerManagers.Auth.SendProfileRequest();
			});
		}

		int count = 0;
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				ServerManagers.Lobby.SendString("HELLO" + count++);
			}
		}

	}
}