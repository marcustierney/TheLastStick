using UnityEngine;

public class ToxicPoisonPuddle : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private float damagePerTick = 2f;
    [SerializeField] private float damageInterval = 0.4f;

    private float nextDamageTime;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        if (Time.time < nextDamageTime)
        {
            return;
        }

        UpdateHealth playerHealth = collision.GetComponent<UpdateHealth>();
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(damagePerTick, transform.position, AnalyticsKeys.DeathCauseToxicPuddle);
        nextDamageTime = Time.time + damageInterval;
    }
}