using FPS.Systems;

namespace FPS.Utils
{
	public static class Extensions
	{
		public static int ToIndex(this int tick)
		{
			return tick % Constants.BUFFER_SIZE;
		}

		public static NetworkWeaponState ToNetworkState(this WeaponState state)
		{
			NetworkWeaponState retState;
			retState.lastFiredTick = state.lastFiredTick;
			retState.reserveAmmo = state.reserveAmmo;
			retState.ammoInMagazine = state.ammoInMagazine;
			retState.reloadEndTick = state.reloadEndTick;
			retState.isReloading = state.isReloading;

			return retState;
		}
	}
}
