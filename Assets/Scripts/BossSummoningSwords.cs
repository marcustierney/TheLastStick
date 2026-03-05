using System.Collections;
using UnityEngine;
public class BossSummoningSwords : MonoBehaviour
{
    public GameObject fallingSwordPrefab;   
    public float arenaLeftEdge = 15f;     
    public float arenaRightEdge = 30f;     
    public float swordSpawnHeight = 8f;    
    public int swordCount = 30;             
    public float timeBetweenSwords = 0.15f; 
    public float chargeUpDuration = 2f;     
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void TriggerSummonAttack()
    {
        StartCoroutine(SummonAttackSequence());
    }

    private IEnumerator SummonAttackSequence()
    {
        if (animator != null)
            animator.SetBool("isCharging", true);

        yield return new WaitForSeconds(chargeUpDuration);

        if (animator != null)
            animator.SetBool("isCharging", false);

        float arenaWidth = arenaRightEdge - arenaLeftEdge;
        float spacing = arenaWidth / (swordCount - 1); 
        float[] positions = new float[swordCount];
        for (int i = 0; i < swordCount; i++)
        {
            positions[i] = arenaLeftEdge + spacing * i;
        }
        ShuffleArray(positions); 

        for (int i = 0; i < swordCount; i++)
        {
            SpawnFallingSword(positions[i]);
            yield return new WaitForSeconds(timeBetweenSwords);
        }
    }

    private void SpawnFallingSword(float xPosition)
    {
        GameObject swordObj = Instantiate(fallingSwordPrefab, new Vector3(xPosition, swordSpawnHeight, 0f), Quaternion.identity);
        RainingSword sword = swordObj.GetComponent<RainingSword>();
        if (sword != null)
        {
            sword.targetX = xPosition;
            sword.spawnY = swordSpawnHeight;
            sword.StartFall();
        }
    }
    private void ShuffleArray(float[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            float temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}