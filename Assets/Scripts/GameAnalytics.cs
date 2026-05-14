using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

/// <summary>
/// Thin wrapper over UGS Analytics CustomEvent; no-ops until data collection is active.
/// </summary>
public static class GameAnalytics
{
    public static bool IsDataCollectionActive { get; private set; }

    public static void SetDataCollectionActive(bool active)
    {
        IsDataCollectionActive = active;
    }

    public static void RecordCustom(string eventName, IReadOnlyDictionary<string, object> parameters)
    {
        if (!IsDataCollectionActive || string.IsNullOrEmpty(eventName))
        {
            return;
        }

        try
        {
            CustomEvent customEvent = new CustomEvent(eventName);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> kv in parameters)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                    {
                        continue;
                    }

                    AddParameter(customEvent, kv.Key, kv.Value);
                }
            }

            AnalyticsService.Instance.RecordEvent(customEvent);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameAnalytics] RecordCustom failed: {ex.Message}");
        }
    }

    public static void FlushIfReady()
    {
        if (!IsDataCollectionActive)
        {
            return;
        }

        try
        {
            AnalyticsService.Instance.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameAnalytics] Flush failed: {ex.Message}");
        }
    }

    static void AddParameter(CustomEvent customEvent, string key, object value)
    {
        switch (value)
        {
            case string s:
                customEvent[key] = s;
                return;
            case int i:
                customEvent[key] = i;
                return;
            case long l:
                customEvent[key] = l;
                return;
            case float f:
                customEvent[key] = f;
                return;
            case double d:
                customEvent[key] = d;
                return;
            case bool b:
                customEvent[key] = b;
                return;
            case DateTime dt:
                customEvent[key] = dt;
                return;
            default:
                if (value is IConvertible convertible)
                {
                    customEvent[key] = convertible.ToDouble(null);
                }

                return;
        }
    }
}
