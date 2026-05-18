using FPS.Controller;
using System.Collections.Generic;
using UnityEngine;

namespace FPS.Manager.Game
{
	/// <summary>
	/// 
	/// Manager for managing GameState
	/// 
	/// - Send/Recv Msgs with LobbyServer
	/// - Update game states ( player positions, map states ... )
	/// 
	/// </summary>
	public class GameManagerEx : MonoBehaviour
    {
		public static GameManagerEx Instance => instance;
		private static GameManagerEx instance = null;

		void Awake()
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

		[SerializeField] private GameObject playerPrefab;

		private Dictionary<int, GameObject> playerObjects = new();

		public void SpawnPlayerObject(int id)
		{
			if (playerObjects.ContainsKey(id)) return;
			playerObjects.Add(id, Instantiate(playerPrefab, Vector3.zero, Quaternion.identity));
		}

		public void UpdatePlayerState(int id, PlayerState state)
		{

		}

		public void UpdatePlayerState(int id, PlayerAnimParams animParams)
		{

		}
    }
}