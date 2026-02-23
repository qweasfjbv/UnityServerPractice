
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void OtherPlayerUpdate()
		{
			timer += Time.deltaTime;

			while(timer >= TICK_DT)
			{
				Tick();
				timer -= TICK_DT;
			}
		}
		
	}
}
