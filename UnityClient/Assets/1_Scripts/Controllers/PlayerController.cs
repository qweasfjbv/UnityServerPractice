using UnityEngine;

namespace Practice.Controller
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
		[Header("----------Bindings----------")]
		[SerializeField] private Transform targetCamera;

        private Animator animator;
        private Rigidbody rigidbody;
        private CapsuleCollider collider;

		private void Awake()
		{
			animator = GetComponent<Animator>();
            rigidbody = GetComponent<Rigidbody>();
            collider = GetComponent<CapsuleCollider>();
		}

		private void Update()
		{
			
		}

		private void InitCamera()
		{

		}

		private void Move()
		{

		}

		private void Jump()
		{

		}

		private void Fire()
		{

		}

	}
}