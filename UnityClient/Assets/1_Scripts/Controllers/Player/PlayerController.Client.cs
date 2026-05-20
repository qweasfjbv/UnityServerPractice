using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Systems;
using FPS.UI;
using FPS.Utils;
using System.Collections;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void ClientPlayerUpdate()
		{
			timer += Time.deltaTime;

			while (timer >= Constants.TICK_DT)
			{
				Tick();
				timer -= Constants.TICK_DT;
			}
		}

		private void Tick()
		{
			int tick = IncreaseTick();

			PlayerInput input = GetInput(tick);
			input.tick = tick;

			int index = tick.ToIndex();

			curPlayerState = Simulate(curPlayerState, input, Constants.TICK_DT);

			curPlayerState.tick = input.tick;
			inputBuffer[index] = input;
			stateBuffer[index] = curPlayerState;

			curWeaponState = WeaponSystem.SimulateWeapon(this, currentWeapon, curWeaponState, input, 
				new CameraContext
				{
					camPosition = targetCamera.position,
					camForward = targetCamera.forward,
					range = 60
				}
				, out FireResult fireResult);
			curPlayerState.weaponState = curWeaponState.ToNetworkState();
			weaponBuffer[index] = curWeaponState;

			input.muzzlePos = fireResult.origin;
			input.muzzleDir = fireResult.direction;

			HandleTestFireFX(fireResult);

			ApplyState(curPlayerState);
			ApplyClientView(input, curWeaponState);
			ApplyAnimParams(input, curPlayerState, curWeaponState);

			StartCoroutine(SendInputAfterLag(lag, input));
			// ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_Input, input));
		}


		private IEnumerator SendInputAfterLag(float lag, PlayerInput input)
		{
			yield return new WaitForSeconds(lag);
			ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_Input, input));
		}

		private void ApplyClientView(in PlayerInput input, in WeaponState weaponState)
		{
			Vector2 mouseDelta = Managers.Input.IA.Player.Look.ReadValue<Vector2>();

			currentLookDir.x += mouseDelta.x * mouseSensitivity;
			currentLookDir.y -= mouseDelta.y * mouseSensitivity;

			currentLookDir.y = Mathf.Clamp(currentLookDir.y - weaponState.recoilState.pitchKickVelocity * Constants.TICK_DT, -viewPitchLimit, viewPitchLimit);

			float finalPitch = currentLookDir.y - weaponState.recoilState.recoilOffset.y;
			transform.rotation = Quaternion.Euler(0f, currentLookDir.x + weaponState.recoilState.recoilOffset.x, 0f);
			cameraBoom.localRotation = Quaternion.Euler(finalPitch, 0f, 0f);

			UIManager.Instance.UpdateAmmoText(curWeaponState.ammoInMagazine, curWeaponState.reserveAmmo);
		}

		public void OnGetSnapshot(PlayerState state)
		{
			PlayerState simulateState = state;
			NetworkWeaponState weaponState = state.weaponState;

			int tick = state.tick + 1;

			while (tick <= currentTick)
			{
				simulateState = Simulate(simulateState, inputBuffer[tick.ToIndex()], Constants.TICK_DT);
				weaponState = WeaponSystem.SimulateWeaponSimple(currentWeapon, weaponState, inputBuffer[tick.ToIndex()]);
				tick++;
			}

			if (followerObject == null)
			{
				followerObject = Instantiate(followerPrefab);
				followerObject.GetComponent<PlayerController>().SetAsOtherPlayer();
			}

			NetworkPlayerState networkState = default;
			networkState.playerState = state;
			networkState.lookDir = inputBuffer[state.tick.ToIndex()].lookDir;
			followerObject.GetComponent<PlayerController>().UpdateState(networkState);

			Reconcile(simulateState);
			ReconcileWeapon(weaponState);
			ApplyState(curPlayerState);
			stateBuffer[currentTick.ToIndex()] = curPlayerState;
		}

		// Hybrid Reconciliation
		private void Reconcile(PlayerState rewind)
		{
			Vector3 localPos = curPlayerState.position;
			Vector3 serverPos = rewind.position;
			float error = Vector3.Distance(localPos, serverPos);

			curPlayerState.position = rewind.position;
			curPlayerState.velocity = rewind.velocity;
			return;

			// Large Error -> TP
			if (error > TELEPORT)
			{
				curPlayerState = rewind;
				return;
			}

			// Medium Error -> Snap
			if (error > SNAP_DIST)
			{
				curPlayerState.position = serverPos;
				curPlayerState.velocity = rewind.velocity;
				return;
			}

			// Small Error -> Lerp
			curPlayerState.position = Vector3.Lerp(
				localPos,
				serverPos,
				SMOOTH_RATE
			);

			curPlayerState.velocity = Vector3.Lerp(
				curPlayerState.velocity,
				rewind.velocity,
				SMOOTH_RATE
			);
		}

		private void ReconcileWeapon(NetworkWeaponState weaponState)
		{
			curWeaponState.ammoInMagazine = weaponState.ammoInMagazine;
			curWeaponState.reserveAmmo = weaponState.reserveAmmo;
			curWeaponState.lastFiredTick = weaponState.lastFiredTick;
			curWeaponState.reloadEndTick = weaponState.reloadEndTick;
			curWeaponState.isReloading = weaponState.isReloading;

			curPlayerState.weaponState = curWeaponState.ToNetworkState();
		}

		// HACK - for Test
		private void HandleTestFireFX(in FireResult result)
		{
			if (!result.isFired) return;
			currentWeapon.DoFireFX();
			RaycastHit[] hits = Physics.RaycastAll(
							result.origin,
							result.direction,
							60f,
							LayerMask.GetMask("Player", "Wall", "Obstacles", "Floor"),
							QueryTriggerInteraction.Collide
							);

			System.Array.Sort(hits, (a, b) =>
			a.distance.CompareTo(b.distance));

			RaycastHit hit;
			foreach (var h in hits)
			{
				var target =
					h.collider.GetComponentInParent<PlayerController>();

				if (target == null)
					continue;

				if (target.gameObject == this.gameObject)
					continue;

				Debug.Log("Client Hit! : " + target.transform.position + ", " + result.origin + ", " + result.direction);
				hit = h;
			}

		}
	}
}