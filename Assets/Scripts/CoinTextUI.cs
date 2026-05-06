using TMPro;
using UnityEngine;

public class CoinTextUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    private void Awake()
    {
        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (CoinManager.Instance != null && coinText != null)
        {
            CoinManager.Instance.SetCoinText(coinText);
        }
    }

    private System.Collections.IEnumerator Start()
    {
        // Ensure CoinManager has had a chance to initialize across scene loads.
        yield return null;

        if (coinText == null)
        {
            coinText = GetComponent<TMP_Text>();
        }

        if (CoinManager.Instance != null && coinText != null)
        {
            CoinManager.Instance.SetCoinText(coinText);
        }
    }
}
