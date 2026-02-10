using FPS.Manager.Game;
using FPS.Manager.Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FPS.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField usernameIF;
        [SerializeField] private TMP_InputField passwordIF;
        [SerializeField] private Button loginButton;

		private void Awake()
		{
			loginButton.onClick.AddListener(() =>
			{
				ServerManagers.Auth.SendLoginRequest(usernameIF.text, passwordIF.text, () => {
					Managers.Scene.ChangeScene("LobbyScene");
					ServerManagers.Lobby.ConnectLobbyServer();
					});
			});
		}

	}
}