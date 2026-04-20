using FPS.Manager.Server;
using FPS.Systems;
using FPS.Utils;
using System.Net;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void ServerPlayerUpdate()
		{

		}

		// TODO - Process Edge Case
		private void OnGetInput(IPEndPoint clientEP, PlayerInput input)
		{
			curPlayerState = Simulate(curPlayerState, input, Constants.TICK_DT);

			curPlayerState.tick = input.tick;
			inputBuffer[input.tick] = input;
			stateBuffer[input.tick] = curPlayerState;

			curWeaponState = WeaponSystem.SimulateWeapon(currentWeapon, curWeaponState, input,
				new CameraContext
				{
					camPosition = targetCamera.position,
					camForward = targetCamera.forward,
					range = 60 
				}
				, out FireResult fireResult);
			weaponBuffer[input.tick] = curWeaponState;

			HandleTestFireFX(fireResult);

			ApplyState(curPlayerState);
			ApplyServerView(input);

			curPlayerState.weaponState.ammoInMagazine = curWeaponState.ammoInMagazine;
			curPlayerState.weaponState.reserveAmmo = curWeaponState.reserveAmmo;
			curPlayerState.weaponState.lastFiredTick = curWeaponState.lastFiredTick;

			ServerManagers.Dedi.Send(clientEP, Serializer.Serialize<PlayerState>(PacketType.S2C_Snapshot, curPlayerState));
		}

		private void ApplyServerView(in PlayerInput input)
		{
			transform.rotation = Quaternion.Euler(0f, input.lookDir.x, 0f);
			cameraBoom.localRotation = Quaternion.Euler(input.lookDir.y, 0f, 0f);
		}
	}
}