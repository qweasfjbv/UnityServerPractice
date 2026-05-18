using FPS.Systems;
using FPS.Utils;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Windows;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void OtherPlayerUpdate()
		{
			timer += Time.deltaTime;

			while (timer >= Constants.TICK_DT)
			{
				timer -= Constants.TICK_DT;
			}
		}

		// HACK 테스트용
		public void UpdateState(NetworkPlayerState state)
		{
			transform.position = state.playerState.position;

			if (state.lookDir.sqrMagnitude > 0.0001f)
			{
				float angle = Mathf.Atan2(state.lookDir.y, state.lookDir.x) * Mathf.Rad2Deg;
				transform.rotation = Quaternion.Euler(0f, 0f, angle);
			}

			PlayerAnimParams animParams;
			animParams.speed.x = state.playerState.velocity.x / maxRunSpeed;
			animParams.speed.y = state.playerState.velocity.z / maxRunSpeed;
			animParams.pitch = -Mathf.Clamp(currentLookDir.y , -viewPitchLimit, viewPitchLimit) / viewPitchLimit * .5f + .5f;
			animParams.isAim = true;

			animator.SetFloat("input", state.playerState.velocity.sqrMagnitude);

			animator.SetFloat("speedX", animParams.speed.x);
			animator.SetFloat("speedY", animParams.speed.y);
			animator.SetFloat("speed", animParams.speed.x * animParams.speed.y * maxRunSpeed * 1.4f);

			animator.SetFloat("pitch", animParams.pitch);

			// if (weaponState.lastFiredTick == input.tick) animator.SetTrigger("isShoot");
			// if (!weaponBuffer[(currentTick - 1).ToIndex()].isReloading && weaponState.isReloading) animator.SetTrigger("isReload");
		}
	}
}
