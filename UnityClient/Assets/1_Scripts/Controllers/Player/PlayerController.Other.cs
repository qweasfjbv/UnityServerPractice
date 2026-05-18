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


		public NetworkPlayerState GetNetworkPlayerState(int localId)
		{
			NetworkPlayerState state;
			state.localId = localId;
			state.position = curPlayerState.position;
			state.velocity = curPlayerState.velocity;
			state.tick = curPlayerState.tick;
			state.isGrounded = curPlayerState.isGrounded;

			return state;
		}

		public void UpdateState(NetworkPlayerState state)
		{
			transform.position = state.position;
		}
	}
}
