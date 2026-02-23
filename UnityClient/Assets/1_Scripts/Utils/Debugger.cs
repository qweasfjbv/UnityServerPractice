using UnityEngine;

namespace FPS.Utils
{
	public static class Debugger
	{
		public static void DrawCapsule(Vector3 bottom, Vector3 top, float radius)
		{
			Color tmpColor = Gizmos.color;
			Gizmos.color = Color.red;

			Gizmos.DrawWireSphere(bottom, radius);
			Gizmos.DrawWireSphere(top, radius);

			Gizmos.DrawLine(
				bottom + Vector3.forward * radius,
				top + Vector3.forward * radius
			);
			Gizmos.DrawLine(
				bottom + Vector3.back * radius,
				top + Vector3.back * radius
			);
			Gizmos.DrawLine(
				bottom + Vector3.left * radius,
				top + Vector3.left * radius
			);
			Gizmos.DrawLine(
				bottom + Vector3.right * radius,
				top + Vector3.right * radius
			);

			Gizmos.color = tmpColor;
		}
	}
}
