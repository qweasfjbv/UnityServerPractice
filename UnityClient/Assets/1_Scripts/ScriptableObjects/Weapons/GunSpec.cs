using UnityEngine;

namespace Practice.SO
{
	[CreateAssetMenu(fileName = "GunSpec", menuName = "WeaponSpec/GunSpec")]
	public class GunSpec : ScriptableObject
	{
		[SerializeField] private float damage;
		[SerializeField] private float fireRate;
		[SerializeField] private float reloadTime;
		[SerializeField] private int maxAmmo;

		[SerializeField] private float reboundStrength;
	}
}