using FPS.Manager.Game;
using FPS.Manager.Server;
using FPS.Systems;
using FPS.UI;
using FPS.Utils;
using FPS.Weapons;
using System.Runtime.InteropServices;
using TMPro;
using TMPro.EditorUtilities;
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
		public Vector3 muzzleDir;			  
		public Vector3 muzzlePos;			  
		public Vector2 move;
		public Vector2 lookDir;
		public int tick;
		public int recentlyReceivedTick;
		public bool isJump;
		public bool isCrouch;
		public bool isFired;
		public bool isReload;
	}

	/// <summary>
	/// Server -> Client
	/// Player State Info for Synchronization
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PlayerState
	{
		public NetworkWeaponState weaponState;
		public Vector3 position;
		public Vector3 velocity;
		public int tick;
		public bool isGrounded;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct NetworkPlayerState
	{
		public PlayerState playerState;
		public Vector2 lookDir;
		public int localId;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PlayerAnimParams
	{
		public Vector2 speed;
		public float input;
		public float pitch;
		public bool isAim;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SpawnData
	{
		public int localId;
		public int startTick;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FireHitResult
	{
		public int shooterId;
		public int targetId;
		public Vector3 hitPoint;
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
		[SerializeField] private Transform cameraBoom;
		[SerializeField] private Transform targetCamera;
		[SerializeField] private GameObject followerPrefab;

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

		[Header("----------View Params----------")]
		[SerializeField] private float mouseSensitivity;
		[SerializeField] private float viewPitchLimit;
		[SerializeField] private float rotationSpeed;

		[Header("----------Animation Params----------")]
		[SerializeField] private float footOffset;

		[Header("----------Debug----------")]
		[SerializeField, ReadOnly] private GunBase currentWeapon = null;
		[SerializeField] private GameObject testPrefab;
		[SerializeField] private TextMeshProUGUI ammoTMP;
		[SerializeField] private float lag;

		private bool isReady = false;
		private PlayerControllerType controllerType = PlayerControllerType.None;

		private PlayerState curPlayerState = default;
		private WeaponState curWeaponState = default;

		private PlayerInput[] inputBuffer = new PlayerInput[Constants.BUFFER_SIZE];
		private PlayerState[] stateBuffer = new PlayerState[Constants.BUFFER_SIZE];
		private WeaponState[] weaponBuffer = new WeaponState[Constants.BUFFER_SIZE];

		private int currentTick = 0;
		private Vector2 currentLookDir = Vector2.zero;

		private float timer = 0f;

		private GameObject followerObject = null;

		public void SetAsOtherPlayer()
		{
			controllerType = PlayerControllerType.Other;
			targetCamera.gameObject.SetActive(false);
		}

		// Server Tick Synchronization
		public void SetAsClientPlayer(int startTick)
		{
			currentTick = startTick;
		}

		private void Awake()
		{
			animator = GetComponent<Animator>();

			Cursor.visible = false;

			if (ServerManagers.Dedi is DediServerManager server)
			{
				controllerType = PlayerControllerType.Server;
			}

			if (ServerManagers.Dedi is DediClientManager client)
			{
				controllerType = PlayerControllerType.Client;
				(ServerManagers.Dedi as DediClientManager).OnGetFireHitResult += ((fireResult) =>
				{
					if (GameManagerEx.Instance.LocalId == fireResult.shooterId)
					{
						// HACK
						UIManager.Instance.PlayHitUI(0);
					}
				});
			}

			// HACK
			curWeaponState.ammoInMagazine = currentWeapon.Spec.MagazineSize;
			curWeaponState.reserveAmmo = currentWeapon.Spec.MaxAmmo;
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
					OtherPlayerUpdate();
					break;
				default:
					Debug.LogError("Wrong Controller Type");
					break;
			}

			// ammoTMP.text = $"{curWeaponState.ammoInMagazine} / {curWeaponState.reserveAmmo}";
		}

		private void OnAnimatorIK(int layerIndex)
		{
			if (currentWeapon == null) return;
			if (currentWeapon.LeftHandTarget == null) return;
			if (currentWeapon.RightHandTarget == null) return;

			animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);

			animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
			animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

			if (!curPlayerState.weaponState.isReloading)
			{
				animator.SetIKPosition(AvatarIKGoal.LeftHand, currentWeapon.LeftHandTarget.position);
				animator.SetIKRotation(AvatarIKGoal.LeftHand, currentWeapon.LeftHandTarget.rotation);
			}
			
			animator.SetIKPosition(AvatarIKGoal.RightHand, currentWeapon.RightHandTarget.position);
			animator.SetIKRotation(AvatarIKGoal.RightHand, currentWeapon.RightHandTarget.rotation);
		}

		/** Common Logic **/
		private PlayerInput GetInput(int tick)
		{
			PlayerInput input;

			input.tick = tick;
			input.recentlyReceivedTick = GameManagerEx.Instance.RecentlyReceivedTick;
			input.move = Managers.Input.IA.Player.Move.ReadValue<Vector2>();
			input.isJump = Managers.Input.IA.Player.Jump.IsPressed();
			input.isCrouch = Managers.Input.IA.Player.Crouch.IsPressed();
			input.isFired = Managers.Input.IA.Player.Fire.IsPressed();
			input.isReload = Managers.Input.IA.Player.Reload.IsPressed();

			input.lookDir = currentLookDir;
			input.muzzleDir = Vector3.zero;
			input.muzzlePos = Vector3.zero;

			return input;
		}

		private PlayerState Simulate(PlayerState state, PlayerInput input, float dt)
		{
			state.isGrounded = CheckGround(state, out _);

			Vector2 lookDir = new Vector2(Mathf.Atan2(targetCamera.forward.x, targetCamera.forward.z) * Mathf.Rad2Deg, -Mathf.Asin(targetCamera.forward.y) * Mathf.Rad2Deg);

			// Accel/Friction
			Vector3 wishDir = Quaternion.Euler(0f, lookDir.x, 0f) * new Vector3(input.move.x, 0, input.move.y);
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

		private void ApplyState(in PlayerState state)
		{
			transform.position = state.position;
		}



		private void ApplyAnimParams(in PlayerInput input, in PlayerState state, in WeaponState weaponState)
		{
			PlayerAnimParams animParams;
			animParams.input = input.move.sqrMagnitude;
			animParams.speed.x = Mathf.Abs(state.velocity.x) / maxRunSpeed;
			animParams.speed.y = Mathf.Abs(state.velocity.z) / maxRunSpeed;
			animParams.pitch = -Mathf.Clamp(currentLookDir.y - weaponState.recoilState.recoilOffset.x, -viewPitchLimit, viewPitchLimit) / viewPitchLimit * .5f + .5f;
			animParams.isAim = true;

			animator.SetFloat("input", animParams.input);

			animator.SetFloat("speedX", animParams.speed.x);
			animator.SetFloat("speedY", animParams.speed.y);
			animator.SetFloat("speed", Mathf.Abs(animParams.speed.x * animParams.speed.y * maxRunSpeed * 1.4f));

			animator.SetFloat("pitch", animParams.pitch);

			if (weaponState.lastFiredTick == input.tick) animator.SetTrigger("isShoot");
			if (!weaponBuffer[(currentTick-1).ToIndex()].isReloading && weaponState.isReloading) animator.SetTrigger("isReload");
		}

		private int IncreaseTick()
		{
			currentTick = (currentTick + 1);
			return currentTick;
		}
	}
}