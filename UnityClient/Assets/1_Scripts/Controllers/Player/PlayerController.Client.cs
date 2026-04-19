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

			inputBuffer[index] = input;

			curPlayerState = Simulate(curPlayerState, input, Constants.TICK_DT);
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
			ApplyView(input, curWeaponState);
			ApplyAnimParams(input, curPlayerState, curWeaponState, out PlayerAnimParams animParams);

			ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_Input, input));
			ServerManagers.Dedi.Send(null, Serializer.Serialize(PacketType.C2S_AnimParam, animParams));
		}

		private void OnGetSnapshot(PlayerState state)
		{
			PlayerState simulateState = state;

			int tick = (state.tick + 1) % Constants.BUFFER_SIZE;

			while (tick != (currentTick + 1) % Constants.BUFFER_SIZE)
			{
				simulateState = Simulate(simulateState, inputBuffer[tick], Constants.TICK_DT);
				tick = (tick + 1) % Constants.BUFFER_SIZE;
			}

			Reconcile(simulateState);
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

			Instantiate(testPrefab, targetPoint, Quaternion.identity);
		}
	}
}