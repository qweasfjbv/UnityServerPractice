using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Systems;
using FPS.Utils;
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

			int index = tick % Constants.BUFFER_SIZE;

			curPlayerState = Simulate(curPlayerState, input, Constants.TICK_DT);

			curPlayerState.tick = input.tick;
			inputBuffer[index] = input;
			stateBuffer[index] = curPlayerState;

			curWeaponState = WeaponSystem.SimulateWeapon(currentWeapon, curWeaponState, input, 
				new CameraContext
				{
					camPosition = targetCamera.position,
					camForward = targetCamera.forward,
					range = 60
				}
				, out FireResult fireResult);
			weaponBuffer[index] = curWeaponState;

			HandleTestFireFX(fireResult);

			ApplyState(curPlayerState);
			ApplyClientView(input, curWeaponState);
			ApplyAnimParams(input, curPlayerState, curWeaponState, out PlayerAnimParams animParams);

			ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_Input, input));
			ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_AnimParam, animParams));
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
		}

		private void OnGetSnapshot(PlayerState state)
		{
			PlayerState simulateState = state;
			NetworkWeaponState weaponState = state.weaponState;

			int tick = (state.tick + 1) % Constants.BUFFER_SIZE;

			while (tick != (currentTick + 1) % Constants.BUFFER_SIZE)
			{
				simulateState = Simulate(simulateState, inputBuffer[tick], Constants.TICK_DT);
				weaponState = WeaponSystem.SimulateWeaponSimple(currentWeapon, weaponState, inputBuffer[tick]);
				tick = (tick + 1) % Constants.BUFFER_SIZE;
			}

			Reconcile(simulateState);
			ReconcileWeapon(weaponState);
			ApplyState(curPlayerState);
			stateBuffer[currentTick] = curPlayerState;
		}

		// Hybrid Reconciliation
		private void Reconcile(PlayerState rewind)
		{
			Vector3 localPos = curPlayerState.position;
			Vector3 serverPos = rewind.position;

			float error = Vector3.Distance(localPos, serverPos);

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
		}

		private void HandleTestFireFX(in FireResult result)
		{
			if (!result.isFired) return;

			Ray ray = new Ray(result.origin, result.direction);
			Vector3 targetPoint;

			if (Physics.Raycast(ray, out RaycastHit hit, 60/*HACK*/))
			{
				targetPoint = hit.point;
			}
			else
			{
				targetPoint = ray.GetPoint(60);
			}

			// HACK - TEST
			Debug.Log("DEBUG : " + transform.position + ", " + transform.rotation);
			Debug.Log("target : " + targetCamera.position + ", " + targetCamera.forward);
			Debug.Log(result.tick + " : TARGET POINT : " + targetPoint);
			Instantiate(testPrefab, targetPoint, Quaternion.identity);
		}
	}
}