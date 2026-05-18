using FPS.Controller;
using FPS.SO;
using FPS.Utils;
using FPS.Weapons;
using UnityEngine;

namespace FPS.Systems
{
	public struct CameraContext
	{
		public Vector3 camPosition;
		public Vector3 camForward;
		public float range;
	}

	public struct FireResult
	{
		public Vector3 origin;
		public Vector3 direction;

		public bool isFired;
		public int tick;
	}

	public struct WeaponState
	{
		public RecoilState recoilState;
		public int lastFiredTick;			// Reconcile
		public int reserveAmmo;				// Reconcile
		public int ammoInMagazine;          // Reconcile
		public int reloadEndTick;			// Reconcile
		public bool isReloading;			// Reconcile
	}

	public struct NetworkWeaponState
	{
		public int lastFiredTick;
		public int reserveAmmo; 
		public int ammoInMagazine;
		public int reloadEndTick;         
		public bool isReloading;                 
	}

	public struct RecoilState
	{
		public float pitchKickVelocity;
		public Vector2 recoilOffset;
		public Vector2 recoilVelocity;
	}

	public static class WeaponSystem
	{
		public static NetworkWeaponState SimulateWeaponSimple(
			GunBase currentWeapon,
			NetworkWeaponState state,
			PlayerInput input)
		{
			int tickBetweenShots = Mathf.RoundToInt(Constants.TICK_RATE / currentWeapon.Spec.FireRate);
			
			/* Reload */
			bool canReload = input.isReload
				&& state.ammoInMagazine < currentWeapon.Spec.MagazineSize
				&& state.reserveAmmo > 0
				&& !state.isReloading;

			if (canReload)
			{
				state.reloadEndTick = input.tick + currentWeapon.Spec.ReloadTick;
				state.isReloading = true;
			}

			if (state.isReloading && input.tick > state.reloadEndTick)
			{
				int reloadAmount = Mathf.Min(currentWeapon.Spec.MagazineSize - state.ammoInMagazine, state.reserveAmmo);
				state.ammoInMagazine += reloadAmount;
				state.reserveAmmo -= reloadAmount;

				state.isReloading = false;
			}

			bool canFire = input.isFired
				&& (input.tick - state.lastFiredTick) >= tickBetweenShots
				&& state.ammoInMagazine > 0
				&& !state.isReloading;

			if (canFire)
			{
				state.ammoInMagazine--;
				state.lastFiredTick = input.tick;
			}

			return state;
		}

		public static WeaponState SimulateWeapon(
			GunBase currentWeapon,
			WeaponState state,
			PlayerInput input,
			CameraContext cameraCtx,
			out FireResult fireResult)
		{
			fireResult = default;

			int tickBetweenShots = Mathf.RoundToInt(Constants.TICK_RATE / currentWeapon.Spec.FireRate);

			/* Reload */
			bool canReload = input.isReload
				&& state.ammoInMagazine < currentWeapon.Spec.MagazineSize
				&& state.reserveAmmo > 0
				&& !state.isReloading;

            if (canReload)
			{
				state.reloadEndTick = input.tick + currentWeapon.Spec.ReloadTick;
				state.isReloading = true;
			}

            if (state.isReloading && input.tick > state.reloadEndTick)
			{
				int reloadAmount = Mathf.Min(currentWeapon.Spec.MagazineSize - state.ammoInMagazine, state.reserveAmmo);
				state.ammoInMagazine += reloadAmount;
				state.reserveAmmo -= reloadAmount;

				state.isReloading = false;
			}

			/* Fire */
			bool canFire = input.isFired
				&& (input.tick - state.lastFiredTick) >= tickBetweenShots
				&& state.ammoInMagazine > 0
				&& !state.isReloading;

			var adjustedInput = input;
			adjustedInput.isFired = canFire;

			if (canFire)
			{
				Debug.Log(input.tick + ", " + state.isReloading + ", " + state.reloadEndTick);

				fireResult = new FireResult
				{
					isFired = true,
					origin = currentWeapon.MuzzlePos,
					direction = CalculateWeaponDir(currentWeapon.MuzzlePos, cameraCtx),
					tick = input.tick
				};

				state.ammoInMagazine--;
				state.lastFiredTick = input.tick;
			}

			state.recoilState = SimulateRecoil(state.recoilState, adjustedInput, currentWeapon.Spec.RecoilProfile);
			return state;
		}

		public static RecoilState SimulateRecoil(
			RecoilState state,
			PlayerInput input,
			RecoilProfile profile
			)
		{
			if (input.isFired)
			{
				System.Random rng = new System.Random(input.tick);
				Vector2 totalKick = new Vector2((float)(rng.NextDouble() * 2.0 - 1.0) * profile.YawKick, profile.PitchKick);
				
				state.pitchKickVelocity += totalKick.y * profile.PermanentRatio;
				state.recoilVelocity += new Vector2(totalKick.x, totalKick.y * profile.RecoverableRatio);
			}

			// recoil damping
			state.pitchKickVelocity = Mathf.Lerp(state.pitchKickVelocity, 0, profile.Damping * Constants.TICK_DT);
			state.recoilVelocity = Vector2.Lerp(state.recoilVelocity, Vector2.zero, profile.Damping * Constants.TICK_DT);
			state.recoilOffset += state.recoilVelocity * Constants.TICK_DT;

			// recovery
			state.pitchKickVelocity = Mathf.Lerp(state.pitchKickVelocity, 0f, profile.Recovery * Constants.TICK_DT);
			state.recoilOffset = Vector2.Lerp(state.recoilOffset, Vector2.zero, profile.Recovery * Constants.TICK_DT);
			return state;
		}

		public static Vector3 CalculateWeaponDir(Vector3 position, CameraContext cameraCtx)
		{
			Ray camRay = new Ray(cameraCtx.camPosition, cameraCtx.camForward);
			Vector3 targetPoint;

			if (Physics.Raycast(camRay, out RaycastHit hit, cameraCtx.range))
			{
				targetPoint = hit.point;
			}
			else
			{
				targetPoint = camRay.GetPoint(cameraCtx.range);
			}

			return (targetPoint - position).normalized;
		}
	}
}
