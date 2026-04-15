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
			curPlayerState = Simulate(curPlayerState, input, Time.fixedDeltaTime);

			curPlayerState.tick = input.tick;
			inputBuffer[input.tick] = input;
			stateBuffer[input.tick] = curPlayerState;
			ApplyState(curPlayerState);

			ServerManagers.Dedi.Send(clientEP, Serializer.Serialize<PlayerState>(PacketType.S2C_Snapshot, curPlayerState));
		}
	}
}