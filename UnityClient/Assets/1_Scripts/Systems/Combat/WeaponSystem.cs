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
		public float pitchKickVelocity;
		public Vector2 recoilOffset;
		public Vector2 recoilVelocity;
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
				&& ((input.tick < state.lastFiredTick ? input.tick + Constants.BUFFER_SIZE : input.tick) - state.lastFiredTick) >= tickBetweenShots;


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
			if (input.isFired)
			{
				System.Random rng = new System.Random(input.tick);
				Vector2 totalKick = new Vector2(profile.PitchKick, (float)(rng.NextDouble() * 2.0 - 1.0) * profile.YawKick);
				
				state.pitchKickVelocity += totalKick.x * profile.PermanentRatio;
				state.recoilVelocity += new Vector2(totalKick.x * profile.RecoverableRatio, totalKick.y);
			}

			// recoil damping
			state.pitchKickVelocity = Mathf.Lerp(state.pitchKickVelocity, 0, profile.Damping * dt);
			state.recoilVelocity = Vector2.Lerp(state.recoilVelocity, Vector2.zero, profile.Damping * dt);
			state.recoilOffset += state.recoilVelocity * dt;

			// recovery
			state.pitchKickVelocity = Mathf.Lerp(state.pitchKickVelocity, 0f, profile.Recovery * dt);
			state.recoilOffset = Vector2.Lerp(state.recoilOffset, Vector2.zero, profile.Recovery * dt);
			return state;
		}
	}
}
