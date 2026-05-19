using FPS.SO;
using UnityEngine;

namespace FPS.Weapons
{
	/// <summary>
	/// 
	/// Base Class of Gun Weapon
	/// 
	/// Including Logics ( not about anims, only logics )
	/// - Basic Actions ( Fire, Reload, Rebound, Zoom ... )
	/// 
	/// </summary>
	public abstract class GunBase : MonoBehaviour
    {
		[Header("GunBase")]
		[SerializeField] private GunSpec spec;
		[SerializeField] private Transform rightHandTarget;
		[SerializeField] private Transform leftHandTarget;
		[SerializeField] private Transform magazineTarget;
		[SerializeField] private Transform muzzlePosition;
		[SerializeField] private ParticleSystem muzzleFlash; // HACK
		
		public GunSpec Spec => spec;
		public Transform LeftHandTarget => leftHandTarget;
		public Transform RightHandTarget => rightHandTarget;
		public Transform MagazineTarget => magazineTarget;
		public Vector3 MuzzlePos => muzzlePosition.position;

		public void DoFireFX()
		{
			muzzleFlash.Play();
		}

    }
}