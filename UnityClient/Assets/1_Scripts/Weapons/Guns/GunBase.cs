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
		
		public GunSpec Spec => spec;
		public Transform LeftHandTarget => leftHandTarget;
		public Transform RightHandTarget => rightHandTarget;

		protected int totalAmmo = 0;
		protected int currentAmmo = 0;

		protected bool isReloading = false;
		protected bool isFiring = false;

		protected bool IsAbleToFire => !isReloading && !isFiring;
		protected bool IsAbleToReload => !isReloading && !isFiring && currentAmmo != spec.MagazineSize && totalAmmo > 0;

		public virtual void InitWeapon()
		{
			totalAmmo = spec.MaxAmmo;
			currentAmmo = spec.MagazineSize;
		}

        public bool Fire()
		{
			if (!IsAbleToFire) return false;
			currentAmmo--;

			return true;
		}

		public bool TryReload()
		{
			if (!IsAbleToReload) return false;
			isReloading = true;

			return true;
		}

        public void OnReloadEnd()
		{
			int lack = Mathf.Min(spec.MagazineSize - currentAmmo, totalAmmo);
			currentAmmo += lack;
			totalAmmo -= lack;

			isReloading = false;
		}
    }
}