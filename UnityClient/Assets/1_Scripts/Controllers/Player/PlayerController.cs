using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Utils;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FPS.Controller
{
	/// <summary>
	/// Client -> Server
	/// Player Input Info for Client Side Prediction
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PlayerInput
	{
		public int tick;
		public Vector2 move;
		public bool isJump;
		public bool isCrouch;
		public float yaw;
	}

	/// <summary>
	/// Server -> Client
	/// Player State Info for Synchronization
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PlayerState
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
		private const float TICK_DT = 1f / 60f;

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
		private float timer = 0f;

		private void Awake()
		{
			animator = GetComponent<Animator>();

			if (ServerManagers.Dedi is DediServerManager server) server.OnGetInputAction += OnGetInput;
			if (ServerManagers.Dedi is DediClientManager client) client.OnGetSnapshotAction += OnGetSnapshot;
		}

		private void Update()
		{
#if !UNITY_EDITOR && UNITY_SERVER
			ServerPlayerUpdate();
#else
			ClientPlayerUpdate();
#endif
		}

		/** Common Logic **/
		private PlayerInput GetInput(int tick)
		{
			PlayerInput input;

			input.tick = tick;
			input.move = Managers.Input.IA.Player.Move.ReadValue<Vector2>();
			input.isJump = Managers.Input.IA.Player.Jump.IsPressed();
			input.isCrouch = Managers.Input.IA.Player.Crouch.IsPressed();

			// TODO 
			// input.yaw = 

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

		private void ApplyState(PlayerState state)
		{
			// TODO
			// - Apply state to player
		}
	}
}