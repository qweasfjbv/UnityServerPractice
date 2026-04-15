namespace FPS.Utils
{
	public static class Constants
    {
        // 122.35.235.105
        public static readonly string IP_ADDR = "122.35.235.105";

        // TCP Port
        public static readonly int PORT_DB      = 61392;
        public static readonly int PORT_AUTH    = 61393;
        public static readonly int PORT_LOBBY   = 61394;

		// UDP Port
		public static readonly int PORT_DEDI    = 61397;

        // CSP
		public static readonly int TICK_RATE = 60;
		public static readonly float TICK_DT = 1f / TICK_RATE;

	}
}