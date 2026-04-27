using UnityEngine;
using TMPro;

public class LoadingDotsTMP : MonoBehaviour
{
    public TMP_Text loadingText;
    private float timer = 0f;
    private int dotCount = 0;

    public float interval = 0.5f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            dotCount = (dotCount + 1) % 4;

            loadingText.text = "Loading" + new string('.', dotCount);
        }
    }
}