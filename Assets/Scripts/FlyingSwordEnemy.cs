using UnityEngine;


public class FlyingSwordEnemy : MonoBehaviour
{
    public float speed = 3f;
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 8f;
    public float screenEdgePadding = 1.5f;
    public float yRandomRange = 2f;
    public float damage = 10f;
    public float maxPlayerLeadDistance = 100f;

    private Transform player;
    private Camera mainCamera;
    private Collider2D hitbox;
    private SpriteRenderer spriteRenderer;
    private float moveDirection;
    private bool isCrossingScreen;

    private void Awake()
    {
        if (GetComponent<DownwardRainingSword>() != null)
        {
            enabled = false;
            return;
        }

        hitbox = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (hitbox != null)
        {
            hitbox.isTrigger = true;
            hitbox.enabled = true;
        }

        SetVisible(false);
    }

    private void Start()
    {
        ResolveReferences();
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        if (!isCrossingScreen)
        {
            return;
        }

        transform.position += Vector3.right * (moveDirection * speed * Time.deltaTime);

        float despawnX = moveDirection > 0f ? GetScreenEdgeX(true) : GetScreenEdgeX(false);
        if ((moveDirection > 0f && transform.position.x >= despawnX) ||
            (moveDirection < 0f && transform.position.x <= despawnX))
        {
            ForceDespawn();
            return;
        }

        // Despawn if the player has moved too far ahead of the sword
        float playerLead = (player.position.x - transform.position.x) * moveDirection;
        if (playerLead > maxPlayerLeadDistance)
        {
            ForceDespawn();
        }
    }

    public void ForceDespawn()
    {
        isCrossingScreen = false;
        SetVisible(false);
    }

    private System.Collections.IEnumerator SpawnLoop()
    {
        while (true)
        {
            while (!ResolveReferences())
            {
                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            if (isCrossingScreen)
            {
                continue;
            }

            SpawnAtScreenEdge();
        }
    }

    private void SpawnAtScreenEdge()
    {
        bool spawnOnLeft = Random.value < 0.5f;
        moveDirection = spawnOnLeft ? 1f : -1f;

        float spawnX = spawnOnLeft ? GetScreenEdgeX(false) : GetScreenEdgeX(true);
        float spawnY = player.position.y + Random.Range(-yRandomRange, yRandomRange);
        transform.position = new Vector3(spawnX, spawnY, transform.position.z);

        SetVisible(true);
        SetRotation();
        isCrossingScreen = true;
    }

    private bool ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        return player != null && mainCamera != null;
    }

    private float GetScreenEdgeX(bool rightSide)
    {
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float padding = rightSide ? screenEdgePadding : -screenEdgePadding;
        return mainCamera.transform.position.x + (rightSide ? halfWidth : -halfWidth) + padding;
    }

    private void SetRotation()
    {
        float absX = Mathf.Abs(transform.localScale.x);
        float absY = Mathf.Abs(transform.localScale.y);

        if (moveDirection > 0f)
        {
            // Coming from the left (front) — flip both X and Y
            transform.localScale = new Vector3(-absX, -absY, transform.localScale.z);
            transform.rotation = new Quaternion(0f, 0f, 0.379558176f, -0.925167859f);
        }
        else
        {
            // Coming from the right (back) — flip X, then flip both X and Y
            transform.localScale = new Vector3(absX, -absY, transform.localScale.z);
            transform.rotation = new Quaternion(0f, 0f, 0.388801575f, 0.921321511f);
        }
    }

    private void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isCrossingScreen)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            UpdateHealth health = other.GetComponent<UpdateHealth>();
            if (health != null)
            {
                health.TakeDamage((int)damage, transform.position, AnalyticsKeys.DeathCauseFlyingSword);
            }
        }
    }
}