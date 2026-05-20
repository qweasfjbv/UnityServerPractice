using System.Collections;
using TMPro;
using UnityEngine;

namespace FPS.UI
{
	public class UIManager : MonoBehaviour
	{
		public static UIManager Instance => instance;
		private static UIManager instance = null;

		void Awake()
		{
			if (null == instance)
			{
				instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
			else
			{
				Destroy(this.gameObject);
			}
		}

		[SerializeField] private GameObject aimUI;
		[SerializeField] private TextMeshProUGUI ammoText;
		[SerializeField] private GameObject hitTextPrefab;

		public void PlayHitUI(float damage)
		{
			RectTransform aimRect = aimUI.GetComponent<RectTransform>();
			RectTransform parentRect = aimRect.parent as RectTransform;

			Vector2 startPos = aimRect.anchoredPosition + new Vector2(0f, 40f);

			GameObject go = Instantiate(hitTextPrefab, parentRect);
			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchoredPosition = startPos;

			TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
			text.text = "HIT!";
			Debug.Log("PlayHitUI");

			StartCoroutine(HitTextCoroutine(rect, text));
		}

		private IEnumerator HitTextCoroutine(RectTransform rect, TextMeshProUGUI text)
		{
			float duration = 0.6f;
			float elapsed = 0f;

			Vector2 start = rect.anchoredPosition;
			Vector2 end = start + new Vector2(0f, 50f);

			Color startColor = text.color;
			Color endColor = startColor;
			endColor.a = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				// 부드러운 보간
				t = Mathf.SmoothStep(0f, 1f, t);

				rect.anchoredPosition = Vector2.Lerp(start, end, t);
				text.color = Color.Lerp(startColor, endColor, t);

				yield return null;
			}

			Destroy(rect.gameObject);
		}

		public void UpdateAmmoText(int ammoInMagazine, int totalAmmo)
		{
			ammoText.text = $"{ammoInMagazine} / {totalAmmo}";
		}

	}
}
