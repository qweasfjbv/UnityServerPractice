using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Weapons;
using System.Runtime.InteropServices;
using Unity.Collections;
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
		// CSP
		private const int BUFFER_SIZE = 1024;
		private const float TICK_DT = 1f / 60f;

		// Reconciliation
		const float SNAP_DIST = 0.3f;
		const float TELEPORT = 2.0f;
		const float SMOOTH_RATE = 0.08f;

		// Collision
		private const float HEIGHT = 3.6f;
		private const float RADIUS = 0.3f;
		private const float SKIN_WIDTH = 0.01f;
		private const float GROUND_DETECT_DIST = 0.3f;

		private Animator animator;

		[Header("----------Bindings----------")]
		[SerializeField] private Transform targetCamera;

		[Header("----------Collision Layers----------")]
		[SerializeField] private LayerMask collisionMask;
		[SerializeField] private LayerMask groundMask;

		[Header("----------Physics Params----------")]
		[SerializeField] private float maxWalkSpeed;
		[SerializeField] private float maxRunSpeed;
		[SerializeField] private float maxFallSpeed;
		[SerializeField] private float walkAccel;
		[SerializeField] private float groundFriction;
		[SerializeField] private float airAccel;
		[SerializeField] private float airFriction;
		[SerializeField] private float gravity;
		[SerializeField] private float jumpForce;

		[Header("----------Debug----------")]
		[SerializeField, ReadOnly] private GunBase currentWeapon = null;

		private bool isReady = false;
		private PlayerControllerType controllerType = PlayerControllerType.None;

		private PlayerState curState;
		private PlayerInput[] inputBuffer = new PlayerInput[BUFFER_SIZE];
		private PlayerState[] stateBuffer = new PlayerState[BUFFER_SIZE];

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

		private void OnAnimatorIK(int layerIndex)
		{
			if (currentWeapon == null) return;
			if (currentWeapon.LeftHandTarget == null) return;

			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

			animator.SetIKPosition(AvatarIKGoal.LeftHand, currentWeapon.LeftHandTarget.position);
			animator.SetIKRotation(AvatarIKGoal.LeftHand, currentWeapon.LeftHandTarget.rotation);
		}

		/** Common Logic **/
		private PlayerInput GetInput(int tick)
		{
			PlayerInput input;

			input.tick = tick;
			input.move = Managers.Input.IA.Player.Move.ReadValue<Vector2>();
			input.isJump = Managers.Input.IA.Player.Jump.IsPressed();
			input.isCrouch = Managers.Input.IA.Player.Crouch.IsPressed();

			Vector3 dir = targetCamera.forward;
			input.yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
			input.pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;

			return input;
		}

		private PlayerState Simulate(PlayerState state, PlayerInput input, float dt)
		{
			state.isGrounded = CheckGround(state, out _);

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

			// Velocity Limits
			Vector2 horz = new Vector2(state.velocity.x, state.velocity.z);
			if (horz.magnitude > maxWalkSpeed)
			{
				horz = horz.normalized * maxWalkSpeed;
			}
			float vert = Mathf.Clamp(state.velocity.y, -maxFallSpeed, maxFallSpeed);
			state.velocity = new Vector3(horz.x, vert, horz.y);

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

				Vector3 bottom = state.position + Vector3.up * RADIUS;
				Vector3 top = bottom + Vector3.up * (HEIGHT - RADIUS * 2);

				if (Physics.CapsuleCast(
					bottom,
					top,
					RADIUS,
					remaining.normalized,
					out RaycastHit hit,
					remaining.magnitude + SKIN_WIDTH,
					collisionMask,
					QueryTriggerInteraction.Ignore))
				{
					float moveDist = Mathf.Max(0, hit.distance - SKIN_WIDTH);
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

		private bool CheckGround(PlayerState state, out float groundY)
		{
			Vector3 origin = transform.position + Vector3.up * 0.1f;

			if (Physics.Raycast(
				origin,
				Vector3.down,
				out RaycastHit hit,
				GROUND_DETECT_DIST,
				groundMask))
			{
				groundY = hit.point.y;
				return true;
			}

			groundY = 0f;
			return false;
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

		private int IncreaseTick()
		{
			currentTick = (currentTick + 1) % BUFFER_SIZE;
			return currentTick;
		}
	}
}