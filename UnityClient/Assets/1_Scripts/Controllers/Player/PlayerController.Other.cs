using FPS.Utils;
using UnityEngine;

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
			transform.rotation = Quaternion.Euler(0f, state.lookDir.x, 0f);

			PlayerAnimParams animParams;
			animParams.speed.x = Mathf.Abs(state.playerState.velocity.x) / maxRunSpeed;
			animParams.speed.y = Mathf.Abs(state.playerState.velocity.z) / maxRunSpeed;
			animParams.pitch = -Mathf.Clamp(state.lookDir.y , -viewPitchLimit, viewPitchLimit) / viewPitchLimit * .5f + .5f;
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
