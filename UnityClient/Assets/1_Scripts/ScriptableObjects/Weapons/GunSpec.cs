using UnityEngine;

namespace FPS.SO
{
	[System.Serializable]
	public class RecoilProfile
	{
		[SerializeField] private float damping;
		[SerializeField] private float recovery;
		[SerializeField] private float permanentRatio;  // kick 보존율
		[SerializeField] private float pitchKick;
		[SerializeField] private float yawKick;


		public float Damping => damping;
		public float Recovery => recovery;
		public float PermanentRatio => permanentRatio;
		public float RecoverableRatio => 1 - permanentRatio;
		public float PitchKick => pitchKick;
		public float YawKick => yawKick;
	}

	[CreateAssetMenu(fileName = "GunSpec", menuName = "WeaponSpec/GunSpec")]
	public class GunSpec : ScriptableObject
	{
		[SerializeField] private float damage;		// 총알 당 데미지
		[SerializeField] private float fireRate;	// 초당 발사 수
		[SerializeField] private float reloadTime;	// 재장전 시간
		[SerializeField] private int magazineSize;	// 탄창 크기
		[SerializeField] private int maxAmmo;       // 총알 최대 개수
		[SerializeField] private RecoilProfile recoilPrefile;

		public float Damage => damage;
		public float FireRate => fireRate;
		public float ReloadTime => reloadTime;
		public int MagazineSize => magazineSize;
		public int MaxAmmo => maxAmmo;
		public RecoilProfile RecoilProfile => recoilPrefile;
	}
}