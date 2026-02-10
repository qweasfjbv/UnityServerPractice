using FPS.Manager.Server;
using UnityEngine;
using UnityEngine.UI;

namespace FPS.UI
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

		}

	}
}