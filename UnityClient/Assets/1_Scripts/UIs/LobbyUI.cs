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

		
	}
}