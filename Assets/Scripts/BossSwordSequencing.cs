using System.Collections;
using UnityEngine;

public class BossSwordSequencing : MonoBehaviour
{
    public BossFloatingSwords swordLeft;
    public BossFloatingSwords swordMiddle;
    public BossFloatingSwords swordRight;
    public float timeBetweenSequences = 10f;  
    public float timeBetweenDives = 0.6f;    
    public float horizontalSpread = 4f;    
    private bool isActive = false;

    private void Start()
    {
        swordLeft.Init(-horizontalSpread, 0f);
        swordMiddle.Init(0f, 1.2f);        
        swordRight.Init(horizontalSpread, 2.4f);
        ActivateSwords();
    }

    public void ActivateSwords()
    {
        if (isActive) return;
        isActive = true;
        StartCoroutine(SwordSequenceLoop());
    }

    public void DeactivateSwords()
    {
        isActive = false;
        StopAllCoroutines();
    }

    private IEnumerator SwordSequenceLoop()
    {
        yield return new WaitForSeconds(2f);
        while (isActive)
        {
            yield return StartCoroutine(DiveSequence());
            yield return new WaitForSeconds(timeBetweenSequences);
        }
    }

    private IEnumerator DiveSequence()
    {
        StartCoroutine(swordLeft.Dive());
        yield return new WaitForSeconds(timeBetweenDives);
        StartCoroutine(swordMiddle.Dive());
        yield return new WaitForSeconds(timeBetweenDives);
        StartCoroutine(swordRight.Dive());
        yield return new WaitForSeconds(timeBetweenDives);
    }
}