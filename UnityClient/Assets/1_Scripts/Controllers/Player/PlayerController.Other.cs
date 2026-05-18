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

		public void UpdateState(NetworkPlayerState state)
		{
			Debug.Log("UPDATE! : " + state.position);
			transform.position = state.position;
		}
	}
}
