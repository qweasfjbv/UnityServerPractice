using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Systems;
using FPS.Utils;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
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

			curWeaponState = WeaponSystem.SimulateWeapon(currentWeapon, curWeaponState, input,
				new CameraContext
				{
					camPosition = targetCamera.position,
					camForward = targetCamera.forward,
					range = 60 
				}
				, out FireResult fireResult);
			weaponBuffer[inputTick] = curWeaponState;

			HandleTestFireFX(fireResult);

			ApplyState(curPlayerState);
			ApplyServerView(input);

			curPlayerState.weaponState = curWeaponState.ToNetworkState();
			ServerManagers.Dedi.Send(localId, Serializer.Serialize<PlayerState>(PacketType.S2C_Snapshot, curPlayerState));
		}

		private void ApplyServerView(in PlayerInput input)
		{
			transform.rotation = Quaternion.Euler(0f, input.lookDir.x, 0f);
			cameraBoom.localRotation = Quaternion.Euler(input.lookDir.y, 0f, 0f);
		}

		public NetworkPlayerState GetNetworkPlayerState(int localId)
		{
			NetworkPlayerState state;
			Debug.Log(localId + " : " + "CURPLAYER POS : " + curPlayerState.position);
			state.localId = localId;
			state.position = curPlayerState.position;
			state.velocity = curPlayerState.velocity;
			state.tick = curPlayerState.tick;
			state.isGrounded = curPlayerState.isGrounded;

			return state;
		}
	}
}