using UnityEngine;

namespace Practice.Controller
{
	/// <summary>
	/// Client -> Server
	/// Player Input Info for Client Side Prediction
	/// </summary>
	struct PlayerInput
	{
		int tick;
		Vector2 move;
		bool isJump;
		float yaw;
	}

	/// <summary>
	/// Server -> Client
	/// Player State Info for Synchronization
	/// </summary>
	struct PlayerState
	{
		int tick;
		Vector3 position;
		Vector3 velocity;
		bool isGrounded;
	}

    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
		[Header("----------Bindings----------")]
		[SerializeField] private Transform targetCamera;

        private Animator animator;

		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		private void Update()
		{
			
		}

		/** Logic **/

		private PlayerState Simulate(PlayerState state, PlayerInput input, float dt)
		{
			return new PlayerState();
		}

	}
}