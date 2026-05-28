using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSummoningSwords : MonoBehaviour
{
    public GameObject fallingSwordPrefab;
    public float arenaLeftEdge = -20f;
    public float arenaRightEdge = 45f;
    public float swordSpawnHeight = 8f;
    public float chargeUpDuration = 2f;

    [Header("Lane Pattern")]
    public int laneCount = 10;
    public int safeLaneSpan = 2;
    public int wavesPerAttack = 6;
    public float timeBetweenWaves = 0.7f;
    public float laneSpawnStagger = 0.03f;
    public float minimumLaneGap = 2.2f;
    public float extraWaveBuffer = 0.1f;

    [Header("Visual Tuning")]
    public float swordScaleMultiplier = 1.35f;

    public System.Action<bool> onChargingChanged;
    public System.Action onSwordsDescending;
    private bool isAttackRunning = false;

    public void TriggerSummonAttack()
    {
        if (isAttackRunning)
        {
            return;
        }

        StartCoroutine(SummonAttackSequence());
    }

    private IEnumerator SummonAttackSequence()
    {
        isAttackRunning = true;
        onChargingChanged?.Invoke(true);
        yield return new WaitForSeconds(chargeUpDuration);

        onChargingChanged?.Invoke(false);

        List<float> lanePositions = BuildLanePositions();
        if (lanePositions.Count == 0)
        {
            isAttackRunning = false;
            yield break;
        }

        int clampedSafeLaneSpan = Mathf.Clamp(safeLaneSpan, 1, lanePositions.Count - 1);
        float waveDelay = ComputeWaveDelay();

        for (int wave = 0; wave < Mathf.Max(1, wavesPerAttack); wave++)
        {
            onSwordsDescending?.Invoke();

            int safeStartLane = Random.Range(0, lanePositions.Count - clampedSafeLaneSpan + 1);

            for (int lane = 0; lane < lanePositions.Count; lane++)
            {
                bool inSafeGap = lane >= safeStartLane && lane < safeStartLane + clampedSafeLaneSpan;
                if (inSafeGap)
                {
                    continue;
                }

                SpawnFallingSword(lanePositions[lane]);

                if (laneSpawnStagger > 0f)
                {
                    yield return new WaitForSeconds(laneSpawnStagger);
                }
            }

            if (wave < wavesPerAttack - 1 && waveDelay > 0f)
            {
                yield return new WaitForSeconds(waveDelay);
            }
        }

        isAttackRunning = false;
    }

    private void SpawnFallingSword(float xPosition)
    {
        GameObject swordObj = Instantiate(fallingSwordPrefab, new Vector3(xPosition, swordSpawnHeight, 0f), Quaternion.identity);

        if (swordScaleMultiplier > 0f)
        {
            swordObj.transform.localScale *= swordScaleMultiplier;
        }

        DownwardRainingSword sword = swordObj.GetComponent<DownwardRainingSword>();
        if (sword != null)
        {
            sword.targetX = xPosition;
            sword.spawnY = swordSpawnHeight;
            sword.StartFall();
        }
        else
        {
            Debug.LogWarning($"Spawned sword at {xPosition} does not have DownwardRainingSword component!");
        }
    }

    private List<float> BuildLanePositions()
    {
        float arenaWidth = arenaRightEdge - arenaLeftEdge;
        if (arenaWidth <= 0f)
        {
            return new List<float>();
        }

        float clampedMinimumGap = Mathf.Max(0.1f, minimumLaneGap);
        int maxLaneCountFromGap = Mathf.Max(2, Mathf.FloorToInt(arenaWidth / clampedMinimumGap) + 1);
        int clampedLaneCount = Mathf.Clamp(laneCount, 2, maxLaneCountFromGap);

        float spacing = arenaWidth / (clampedLaneCount - 1);
        List<float> lanePositions = new List<float>(clampedLaneCount);
        for (int i = 0; i < clampedLaneCount; i++)
        {
            lanePositions.Add(arenaLeftEdge + spacing * i);
        }

        return lanePositions;
    }

    private float ComputeWaveDelay()
    {
        float baseDelay = Mathf.Max(0f, timeBetweenWaves);
        if (fallingSwordPrefab == null)
        {
            return baseDelay;
        }

        RainingSword rainSword = fallingSwordPrefab.GetComponent<RainingSword>();
        if (rainSword == null)
        {
            return baseDelay;
        }

        float minimumDelay = Mathf.Max(0f, rainSword.warningDuration + Mathf.Max(0f, extraWaveBuffer));
        return Mathf.Max(baseDelay, minimumDelay);
    }
}