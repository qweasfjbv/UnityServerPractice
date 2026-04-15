using FPS.Controller;
using FPS.SO;
using FPS.Utils;
using UnityEngine;

namespace FPS.Systems
{
	public struct WeaponState
	{
		public RecoilState recoilState;
		public int lastFiredTick;
	}

	public struct RecoilState
	{
		public Vector2 recoilOffset;
		public Vector2 recoilVelocity;
		public int shotsFired;
	}

	public static class WeaponSystem
	{
		public static WeaponState SimulateWeapon(
			WeaponState state, 
			PlayerInput input, 
			GunSpec spec,
			float dt)
		{
			int tickBetweenShots = Mathf.RoundToInt(Constants.TICK_RATE / spec.FireRate);

			bool canFire = input.isFired
				&& (input.tick - state.lastFiredTick) >= tickBetweenShots;

			var adjustedInput = input;
			adjustedInput.isFired = canFire;

			if (canFire)
				state.lastFiredTick = input.tick;

			state.recoilState = SimulateRecoil(state.recoilState, adjustedInput, spec.RecoilProfile, dt);
			return state;
		}

		public static RecoilState SimulateRecoil(
			RecoilState state,
			PlayerInput input,
			RecoilProfile profile,
			float dt
			)
		{
			// Pattern-based recoil
			if (input.isFired)
			{
				float pitchKick = profile.GetPitchKick(state.shotsFired);
				float yawKick = profile.GetYawKick(state.shotsFired);

				state.recoilVelocity += new Vector2(pitchKick, yawKick);
				state.shotsFired++;
			}

			// recoil damping
			state.recoilVelocity = Vector2.Lerp(state.recoilVelocity, Vector2.zero, profile.Damping * dt);
			state.recoilOffset += state.recoilVelocity * dt;
			
			// recovery
			state.recoilOffset = Vector2.Lerp(state.recoilOffset, Vector2.zero, profile.Recovery * dt);
			return state;
		}
	}
}
