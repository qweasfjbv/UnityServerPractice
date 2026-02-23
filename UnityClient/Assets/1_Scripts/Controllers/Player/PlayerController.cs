using FPS.Manager.Game;
using FPS.Manager.Server;
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
		public bool isRun;
		public bool isCrouch;

		public float yaw;
		public float pitch;
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

	public enum PlayerControllerType
	{
		None = 0,
		Server = 1,
		Client,
		Other,
	}

    [RequireComponent(typeof(Animator))]
    public partial class PlayerController : MonoBehaviour
    {
		private const int BUFFER_SIZE = 1024;
		private const float TICK_DT = 1f / 60f;

		private Animator animator;

		[Header("----------Bindings----------")]
		[SerializeField] private Transform targetCamera;

		[Header("----------Collision Layers----------")]
		[SerializeField] private LayerMask collisionMask;
		[SerializeField] private LayerMask groundMask;

		[Header("----------Physics Params----------")]
		[SerializeField] private float maxWalkSpeed;
		[SerializeField] private float maxRunSpeed;
		[SerializeField] private float walkAccel;
		[SerializeField] private float groundFriction;
		[SerializeField] private float airAccel;
		[SerializeField] private float airFriction;
		[SerializeField] private float gravity;
		[SerializeField] private float jumpForce;

		private bool isReady = false;
		private PlayerControllerType controllerType = PlayerControllerType.None;

		private PlayerState curState;
		private PlayerInput[] inputBuffer = new PlayerInput[BUFFER_SIZE];
		private PlayerState[] stateBuffer = new PlayerState[BUFFER_SIZE];

		private const float height = 3.6f;
		private const float radius = 0.3f;
		private const float skinWidth = 0.01f;

		private int currentTick = 0;
		private float timer = 0f;

		private void Awake()
		{
			animator = GetComponent<Animator>();

			if (ServerManagers.Dedi is DediServerManager server)
			{
				controllerType = PlayerControllerType.Server;
				server.OnGetInputAction += OnGetInput;
			}

			if (ServerManagers.Dedi is DediClientManager client)
			{
				controllerType = PlayerControllerType.Client;
				client.OnGetSnapshotAction += OnGetSnapshot;
			}
		}

		private void Update()
		{
			switch (controllerType)
			{
				case PlayerControllerType.Server:
					ServerPlayerUpdate();
					break;
				case PlayerControllerType.Client:
					ClientPlayerUpdate();
					break;
				case PlayerControllerType.Other:
					break;
				default:
					Debug.LogError("Wrong Controller Type");
					break;
			}
		}

		/** Common Logic **/
		private PlayerInput GetInput(int tick)
		{
			PlayerInput input;

			input.tick = tick;
			input.move = Managers.Input.IA.Player.Move.ReadValue<Vector2>();
			input.isJump = Managers.Input.IA.Player.Jump.IsPressed();
			input.isRun = Managers.Input.IA.Player.Run.IsPressed();
			input.isCrouch = Managers.Input.IA.Player.Crouch.IsPressed();

			Vector3 dir = targetCamera.forward;
			input.yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
			input.pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;

			return input;
		}

		private PlayerState Simulate(PlayerState state, PlayerInput input, float dt)
		{
			// Ground Check (임시)
			state.isGrounded = CheckGround(state);

			// Accel/Friction
			Vector3 wishDir = new Vector3(input.move.x, 0, input.move.y);
			float accel = state.isGrounded ? walkAccel : airAccel;
			float friction = state.isGrounded ? groundFriction : airFriction;

			state.velocity += wishDir * accel * dt;

			Vector3 horizontalVel = new Vector3(
				state.velocity.x,
				0,
				state.velocity.z
			);

			float speed = horizontalVel.magnitude;

			if (speed > 0.0001f)
			{
				float drop = speed * friction * dt;

				float newSpeed = Mathf.Max(speed - drop, 0);
				horizontalVel *= newSpeed / speed;

				state.velocity.x = horizontalVel.x;
				state.velocity.z = horizontalVel.z;
			}

			// Jump
			if (state.isGrounded && input.isJump)
				state.velocity.y = jumpForce;

			// Gravity
			state.velocity += Vector3.down * gravity * dt;

			// Collision Move
			MoveWithCollision(ref state, dt);

			return state;
		}

		private void MoveWithCollision(ref PlayerState state, float dt)
		{
			Vector3 velocity = state.velocity;
			Vector3 remaining = velocity * dt;

			for (int i = 0; i < 3; i++)
			{
				if (remaining.magnitude < 0.0001f) break;

				Vector3 bottom = state.position + Vector3.up * radius;
				Vector3 top = bottom + Vector3.up * (height - radius * 2);

				if (Physics.CapsuleCast(
					bottom,
					top,
					radius,
					remaining.normalized,
					out RaycastHit hit,
					remaining.magnitude + skinWidth,
					collisionMask,
					QueryTriggerInteraction.Ignore))
				{
					float moveDist = Mathf.Max(0, hit.distance - skinWidth);
					state.position += remaining.normalized * moveDist;

					remaining = Vector3.ProjectOnPlane(remaining - (remaining.normalized * moveDist), hit.normal);

					state.velocity = Vector3.ProjectOnPlane(state.velocity, hit.normal);
				}
				else
				{
					state.position += remaining;
					break;
				}
			}
		}

		private bool CheckGround(PlayerState state)
		{
			return true;
			// TODO
		}

		private void ApplyState(PlayerState state)
		{
			transform.position = state.position;

			animator.SetFloat("speed", state.velocity.magnitude / maxRunSpeed * 1.4f);
			animator.SetFloat("speedX", state.velocity.x / maxRunSpeed);
			animator.SetFloat("speedY", state.velocity.z / maxRunSpeed);
			// TODO
			// - Apply state to player
		}
	}
}