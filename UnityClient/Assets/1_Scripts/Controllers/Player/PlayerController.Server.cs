using FPS.Manager.Server;
using FPS.Utils;
using System.Net;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void ServerPlayerUpdate()
		{

		}

		private void OnGetInput(IPEndPoint clientEP, PlayerInput input)
		{
			curState = Simulate(curState, input, Time.fixedDeltaTime);
			inputBuffer[input.tick] = input;
			stateBuffer[input.tick] = curState;
			ApplyState(curState);

			ServerManagers.Dedi.Send(clientEP, Serializer.Serialize<PlayerState>(PacketType.S2C_Snapshot, curState));
		}
	}
}