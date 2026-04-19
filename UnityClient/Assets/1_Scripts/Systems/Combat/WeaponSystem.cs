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
		public bool isFired;
		public int tick;

		public Vector3 origin;
		public Vector3 direction;
	}

	public struct WeaponState
	{
		public RecoilState recoilState;
		public int lastFiredTick;
		public int ammo;
		public bool isFiredThisTick;
	}

	public struct RecoilState
	{
		public float pitchKickVelocity;
		public Vector2 recoilOffset;
		public Vector2 recoilVelocity;
	}

	public static class WeaponSystem
	{
		public static WeaponState SimulateWeapon(
			GunBase currentWeapon,
			WeaponState state,
			PlayerInput input,
			CameraContext cameraCtx,
			out FireResult fireResult)
		{
			fireResult = default;

			int tickBetweenShots = Mathf.RoundToInt(Constants.TICK_RATE / currentWeapon.Spec.FireRate);

			bool canFire = input.isFired
				&& ((input.tick < state.lastFiredTick ? input.tick + Constants.BUFFER_SIZE : input.tick) - state.lastFiredTick) >= tickBetweenShots
				&& state.ammo > 0;

			var adjustedInput = input;
			adjustedInput.isFired = canFire;

			if (canFire)
			{
				fireResult = new FireResult
				{
					isFired = true,
					origin = currentWeapon.MuzzlePos,
					direction = CalculateWeaponDir(currentWeapon.MuzzlePos, cameraCtx),
					tick = input.tick
				};

				state.ammo--;
				state.lastFiredTick = input.tick;
			}

			state.isFiredThisTick = canFire;
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
