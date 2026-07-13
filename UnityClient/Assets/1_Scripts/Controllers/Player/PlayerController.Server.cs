using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Systems;
using FPS.Utils;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private int serverTickOffset = 0;

		private void ServerPlayerUpdate()
		{

		}

		// TODO - Process Edge Case
		public void OnGetInput(int localId, PlayerInput input)
		{
			int inputTick = input.tick.ToIndex();
			curPlayerState = Simulate(curPlayerState, input, Constants.TICK_DT);

			curPlayerState.tick = input.tick;
			inputBuffer[inputTick] = input;
			stateBuffer[inputTick] = curPlayerState;

			curWeaponState = WeaponSystem.SimulateWeapon(this, currentWeapon, curWeaponState, input,
				new CameraContext
				{
					camPosition = targetCamera.position,
					camForward = targetCamera.forward,
					range = 60 
				}
				, out FireResult fireResult);
			weaponBuffer[inputTick] = curWeaponState;

			if (fireResult.isFired)
			{
				if (GameManagerEx.Instance.LagCompensationRaycast(
					input.recentlyReceivedTick,
					input.muzzlePos,
					input.muzzleDir,
					60f,
					localId,
					out RaycastHit hit))
				{
					var target = hit.collider.GetComponentInParent<PlayerController>();
					if (target != null)
					{
						FireHitResult hitResult;
						hitResult.shooterId = localId;
						hitResult.targetId = GameManagerEx.Instance.GetLocalId(target.transform);
						hitResult.hitPoint = hit.point;
						ServerManagers.Dedi.Send(null, ChannelMode.Unreliable, PacketType.S2C_HitResult, hitResult);
					}
				}
			}

			ApplyState(curPlayerState);
			ApplyServerView(input);
			
			curPlayerState.weaponState = curWeaponState.ToNetworkState();
#if !UNITY_EDITOR && UNITY_SERVER
			(ServerManagers.Dedi as DediServerManager).Send(localId, ChannelMode.Reliable, PacketType.S2C_Snapshot, curPlayerState);
#endif
		}

		private void ApplyServerView(in PlayerInput input)
		{
			transform.rotation = Quaternion.Euler(0f, input.lookDir.x, 0f);
			cameraBoom.localRotation = Quaternion.Euler(input.lookDir.y, 0f, 0f);
		}

		public NetworkPlayerState GetNetworkPlayerState(int localId)
		{
			NetworkPlayerState state;

			state.localId = localId;
			state.lookDir = inputBuffer[(curPlayerState.tick - 1).ToIndex()].lookDir;

			state.playerState.weaponState = curWeaponState.ToNetworkState();
			state.playerState.position = curPlayerState.position;
			state.playerState.velocity = curPlayerState.velocity;
			state.playerState.tick = curPlayerState.tick;
			state.playerState.isGrounded = curPlayerState.isGrounded;

			return state;
		}

		public bool TryGetHistoricalState(int tick, out PlayerState state)
		{
			// HACK - 최근 20 tick 까지는 확인
			for (int i = 0; i < 20; i++)
			{
				int index = tick.ToIndex();
				state = stateBuffer[index];

				if(state.tick == tick) return true;
				tick--;
			}

			state = default;
			return false;
		}
	}
}