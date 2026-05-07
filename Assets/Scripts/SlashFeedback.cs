using System.Collections;
using UnityEngine;

public class SlashFeedback : MonoBehaviour
{
    public Sprite[] slashSprites = new Sprite[4];
    public SpriteRenderer overlayRenderer;

    private Coroutine hideRoutine;

    public void PlaySlash(int comboIndex)
    {
        if (overlayRenderer == null || slashSprites == null || slashSprites.Length == 0)
        {
            return;
        }

        int i = Mathf.Clamp(comboIndex, 0, slashSprites.Length - 1);
        overlayRenderer.sprite = slashSprites[i];
        overlayRenderer.enabled = true;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay(0.12f));
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (overlayRenderer != null)
        {
            overlayRenderer.enabled = false;
        }

        hideRoutine = null;
    }
}
