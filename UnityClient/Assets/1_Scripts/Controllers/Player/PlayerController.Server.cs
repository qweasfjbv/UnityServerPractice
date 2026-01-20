using UnityEngine;

namespace Practice.Controller
{
	public partial class PlayerController
	{
		private void OnReceiveInput(PlayerInput input)
		{
			curState = Simulate(curState, input, Time.fixedDeltaTime);

		}
	}
}