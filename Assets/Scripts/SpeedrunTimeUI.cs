using TMPro;
using UnityEngine;

public class SpeedrunTimeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private bool showFinalTimeOnly;

    int _lastDisplayedSecond = -1;

    void Awake()
    {
        if (timeText == null)
        {
            timeText = GetComponent<TMP_Text>();
        }
    }

    void OnEnable()
    {
        if (timeText == null)
        {
            timeText = GetComponent<TMP_Text>();
        }

        if (showFinalTimeOnly)
        {
            RefreshFinal();
        }
        else
        {
            _lastDisplayedSecond = -1;
            RefreshLive();
        }
    }

    void Update()
    {
        if (!showFinalTimeOnly)
        {
            RefreshLive();
        }
    }

    void RefreshLive()
    {
        LevelRunStats stats = LevelRunStats.Instance;
        if (stats == null || timeText == null)
        {
            return;
        }

        if (!stats.IsSpeedrunActive)
        {
            if (!string.IsNullOrEmpty(timeText.text))
            {
                timeText.text = string.Empty;
            }

            _lastDisplayedSecond = -1;
            return;
        }

        int seconds = Mathf.FloorToInt(stats.GetSpeedrunSeconds());
        if (seconds == _lastDisplayedSecond)
        {
            return;
        }

        _lastDisplayedSecond = seconds;
        timeText.text = LevelRunStats.FormatMmSs(seconds);
    }

    void RefreshFinal()
    {
        LevelRunStats stats = LevelRunStats.Instance;
        if (stats == null || timeText == null)
        {
            return;
        }

        timeText.text = LevelRunStats.FormatMmSs(stats.GetSpeedrunSeconds());
    }
}
