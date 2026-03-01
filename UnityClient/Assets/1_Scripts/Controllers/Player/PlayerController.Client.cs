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

			curState = Simulate(curState, input, TICK_DT);
			stateBuffer[index] = curState;
			ApplyState(curState);

			ServerManagers.Dedi.Send(null, Serializer.Serialize<PlayerInput>(PacketType.C2S_Input, input));
		}

		private void OnGetSnapshot(PlayerState state)
		{
			PlayerState simulateState = stateBuffer[state.tick];
			for (int i = state.tick + 1; i <= currentTick % BUFFER_SIZE; i++)
			{
				simulateState = Simulate(simulateState, inputBuffer[i], TICK_DT);
			}

			Reconcile(simulateState);
			ApplyState(curState);
			stateBuffer[currentTick] = curState;
		}

		// Hybrid Reconciliation
		private void Reconcile(PlayerState rewind)
		{
			Vector3 localPos = curState.position;
			Vector3 serverPos = rewind.position;

			float error = Vector3.Distance(localPos, serverPos);

			// Large Error -> TP
			if (error > TELEPORT)
			{
				curState = rewind;
				return;
			}

			// Medium Error -> Snap
			if (error > SNAP_DIST)
			{
				curState.position = serverPos;
				curState.velocity = rewind.velocity;
				return;
			}

			// Small Error -> Lerp
			curState.position = Vector3.Lerp(
				localPos,
				serverPos,
				SMOOTH_RATE
			);

			curState.velocity = Vector3.Lerp(
				curState.velocity,
				rewind.velocity,
				SMOOTH_RATE
			);
		}
	}
}