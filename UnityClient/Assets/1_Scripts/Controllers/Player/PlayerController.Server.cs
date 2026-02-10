using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void ServerPlayerUpdate()
		{

		}

		private void OnGetInput(PlayerInput input)
		{
			curState = Simulate(curState, input, Time.fixedDeltaTime);
			// TODO
			// - Simulate 
			// - and send player's Snapshot
		}
	}
}