using UnityEngine;
using TMPro;

public class LoadingDotsTMP : MonoBehaviour
{
    public TMP_Text loadingText;
    public float interval = 0.5f;

    private float timer;
    private int dotCount;
    private string lastBase;

    private void Update()
    {
        if (loadingText == null) return;

        string base_ = GetBase(loadingText.text);
        if (base_ == null) return;

        if (base_ != lastBase)
        {
            lastBase = base_;
            dotCount = 0;
            timer = 0f;
        }

        timer += Time.deltaTime;
        if (timer < interval) return;

        timer = 0f;
        dotCount = (dotCount + 1) % 4;
        loadingText.text = lastBase + new string('.', dotCount);
    }

    private static string GetBase(string text)
    {
        if (text.StartsWith("Saving", System.StringComparison.Ordinal))  return "Saving";
        if (text.StartsWith("Loading", System.StringComparison.Ordinal)) return "Loading";
        return null;
    }
}