using FPS.Controller;
using System.Collections;
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

		private int recentlyReceivedTick = 0;
		public int RecentlyReceivedTick => recentlyReceivedTick;

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

		public void SpawnPlayerObjectOnServer(in SpawnData data)
		{
			if (playerObjects.ContainsKey(data.localId)) return;

			GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
			playerObject.name = $"Player {data.localId}";
			playerObjects.Add(data.localId, playerObject);
		}

		public void SpawnPlayerObject(in SpawnData data)
		{
			if (playerObjects.ContainsKey(data.localId)) return;

			StartCoroutine(WaitForInit(data.localId, data.startTick));
		}

		private IEnumerator WaitForInit(int id, int startTick)
		{
			yield return new WaitUntil(() => localId != -1);

			GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
			if (localId != id) playerObject.GetComponent<PlayerController>().SetAsOtherPlayer();
			else playerObject.GetComponent<PlayerController>().SetAsClientPlayer(startTick);
			playerObjects.Add(id, playerObject);
		}

		public GameObject GetPlayerObject(int id)
		{
			if (!playerObjects.ContainsKey(id)) return null;
			return playerObjects[id];
		}

		public void OnGetPlayerInput(int localId, PlayerInput input)
		{
			if (!playerObjects.TryGetValue(localId, out GameObject playerObject)) return;

			playerObject.GetComponent<PlayerController>().OnGetInput(localId, input);
		}

		public void UpdatePlayerState(PlayerState state)
		{
			if (!playerObjects.TryGetValue(localId, out GameObject playerObject)) return;

			playerObject.GetComponent<PlayerController>().OnGetSnapshot(state);
		}
		public void UpdatePlayerState(NetworkPlayerState state)
		{
			if(!playerObjects.TryGetValue(state.localId, out GameObject playerObject)) return;

			recentlyReceivedTick = state.playerState.tick;
			playerObject.GetComponent<PlayerController>().UpdateState(state);
		}

		public void UpdatePlayerState(int id, PlayerAnimParams animParams)
		{

		}

		public bool LagCompensationRaycast(
			int rewindTick,
			Vector3 origin,
			Vector3 direction,
			float distance,
			int shooterLocalId,
			out RaycastHit hit)
		{
			// Current State Backup
			Dictionary<int, Vector3> backupPositions = new();

			// Rewind
			foreach (var kv in playerObjects)
			{
				if (kv.Key == shooterLocalId)
					continue;

				if (kv.Value.GetComponent<PlayerController>()
					.TryGetHistoricalState(rewindTick, out PlayerState pastState))
				{
					backupPositions[kv.Key] = kv.Value.transform.position;
					kv.Value.transform.position = pastState.position;
				}
			}

			RaycastHit[] hits = Physics.RaycastAll(
				origin,
				direction,
				distance,
				LayerMask.GetMask("Player", "Wall"),
				QueryTriggerInteraction.Collide
				);

			// Restore
			foreach (var kv in backupPositions)
			{
				playerObjects[kv.Key].transform.position = kv.Value;
			}


			System.Array.Sort(hits, (a, b) =>
			a.distance.CompareTo(b.distance));

			foreach (var h in hits)
			{
				var target =
					h.collider.GetComponentInParent<PlayerController>();

				if (target == null)
					continue;

				if (target.gameObject == playerObjects[shooterLocalId])
					continue;

				hit = h;
				return true;
			}

			hit = default;
			return false;
		}
	}
}