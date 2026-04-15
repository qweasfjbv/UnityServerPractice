using FPS.Utils;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void OtherPlayerUpdate()
		{
			timer += Time.deltaTime;

			while(timer >= Constants.TICK_DT)
			{
				Tick();
				timer -= Constants.TICK_DT;
			}
		}
		
	}
}
