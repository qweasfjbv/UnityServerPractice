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

		private int localId = -1;
		public int LocalId { get => localId; set => localId = value; }

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

			GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
			if (localId != id) playerObject.GetComponent<PlayerController>().SetAsOtherPlayer();
			playerObjects.Add(id, playerObject);
		}

		public GameObject GetPlayerObject(int id)
		{
			if (!playerObjects.ContainsKey(id)) return null;
			return playerObjects[id];
		}

		public void UpdatePlayerState(PlayerState state)
		{
			if (!playerObjects.TryGetValue(localId, out GameObject playerObject)) return;

			playerObject.GetComponent<PlayerController>().OnGetSnapshot(state);
		}
		public void UpdatePlayerState(NetworkPlayerState state)
		{
			if(!playerObjects.TryGetValue(state.localId, out GameObject playerObject)) return;

			playerObject.GetComponent<PlayerController>().UpdateState(state);
		}

		public void UpdatePlayerState(int id, PlayerAnimParams animParams)
		{

		}
    }
}