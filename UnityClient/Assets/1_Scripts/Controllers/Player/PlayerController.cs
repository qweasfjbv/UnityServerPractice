using System.Runtime.InteropServices;
using UnityEngine;

namespace Practice.Controller
{
	/// <summary>
	/// Client -> Server
	/// Player Input Info for Client Side Prediction
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct PlayerInput
	{
		public int tick;
		public Vector2 move;
		public bool isJump;
		public float yaw;
	}

	/// <summary>
	/// Server -> Client
	/// Player State Info for Synchronization
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct PlayerState
	{
		public int tick;
		public Vector3 position;
		public Vector3 velocity;
		public bool isGrounded;
	}

    [RequireComponent(typeof(Animator))]
    public partial class PlayerController : MonoBehaviour
    {
		private const int BUFFER_SIZE = 1024;

		[Header("----------Bindings----------")]
		[SerializeField] private Transform targetCamera;

		[Header("----------Physics Params----------")]
		[SerializeField] private float groundAccel;
		[SerializeField] private float airAccel;
		[SerializeField] private float gravity;
		[SerializeField] private float jumpForce;


        private Animator animator;

		private PlayerState curState;

		private PlayerInput[] inputBuffer = new PlayerInput[BUFFER_SIZE];
		private PlayerState[] stateBuffer = new PlayerState[BUFFER_SIZE];

		private int currentTick = 0;

		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		private void Update()
		{
			
		}

		private void FixedUpdate()
		{
			int tick = currentTick++;

			PlayerInput input = GetInput();
			input.tick = tick;

			int index = tick % BUFFER_SIZE;

			inputBuffer[index] = input;

			curState = Simulate(curState, input, Time.fixedDeltaTime);
			stateBuffer[index] = curState;

			// SendToServer(input);
		}

		/** Common Logic **/
		private PlayerInput GetInput()
		{
			return new PlayerInput();
		}

		private PlayerState Simulate(PlayerState state, PlayerInput input, float dt)
		{
			Vector3 wishDir = Vector3.zero; // TODO
			float accel = state.isGrounded ? groundAccel : airAccel;

			state.velocity += wishDir * accel * dt;

			if (state.isGrounded && input.isJump)
				state.velocity.y = jumpForce;

			state.velocity += Vector3.down * gravity * dt;
			state.position += state.velocity * dt;

			return state;
		}

	}
}