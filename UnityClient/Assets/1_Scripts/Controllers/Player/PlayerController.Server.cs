using UnityEngine;

namespace Practice.Controller
{
	public partial class PlayerController
	{
		private void OnGetInput(PlayerInput input)
		{
			curState = Simulate(curState, input, Time.fixedDeltaTime);
			// TODO
			// - Simulate 
			// - and send player's Snapshot
		}
	}
}