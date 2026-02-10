using FPS.Manager.Server;
using FPS.Utils;
using UnityEngine;

namespace FPS.Controller
{
	public partial class PlayerController
	{
		private void ClientPlayerUpdate()
		{
			timer += Time.deltaTime;

			while (timer >= TICK_DT)
			{
				Tick();
				timer -= TICK_DT;
			}
		}

		private void Tick()
		{
			int tick = currentTick++;

			PlayerInput input = GetInput(tick);
			input.tick = tick;

			int index = tick % BUFFER_SIZE;

			inputBuffer[index] = input;

			curState = Simulate(curState, input, Time.fixedDeltaTime);
			stateBuffer[index] = curState;
			ApplyState(curState);

			ServerManagers.Dedi.Send(null, Serializer.Serialize<PlayerInput>(PacketType.C2S_Input, input));
		}

		private void SendPlayerInputToServer(PlayerInput input)
		{

		}

		private void OnGetSnapshot(PlayerState state)
		{
			// TODO
			// - Check tick
			// - Re-simulate to current tick
			// - Reconciliation
		}
	}
}