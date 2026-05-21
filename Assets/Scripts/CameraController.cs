using UnityEngine;

[DefaultExecutionOrder(0)]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool findPlayerByTag = true;

    [Header("Follow")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 0.7f, -10f);
    [SerializeField] private float smoothTimeX = 0.15f;
    [SerializeField] private float smoothTimeYUp = 0.25f;
    [SerializeField] private float smoothTimeYDown = 0.08f;
    [SerializeField] private float facingLookahead = 1.2f;
    [SerializeField] private float facingLookaheadSmoothTime = 0.12f;

    [Header("Boss Lookahead")]
    [SerializeField] private bool enableBossShift;
    [SerializeField] private string shiftTargetTag = "CameraShift";
    [SerializeField] private float maxShift = 3f;
    [SerializeField] private float influenceDistance = 6f;
    [SerializeField] private float shiftSmoothTime = 0.2f;

    [Header("Trauma Shake")]
    [SerializeField] private float damageTrauma = 0.35f;
    [SerializeField] private float landingTrauma = 0.12f;
    [SerializeField] private float landingMinFallSpeed = 4f;
    [SerializeField] private float maxShakeOffset = 0.4f;
    [SerializeField] private float traumaDecayPerSecond = 1.2f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Landing Bump")]
    [SerializeField] private float landingBumpYOffset = 0.15f;
    [SerializeField] private float bumpDecayPerSecond = 4f;

    [Header("Dash Zoom")]
    [SerializeField] private float defaultOrthographicSize = 5f;
    [SerializeField] private float dashOrthographicSize = 4.25f;
    [SerializeField] private float zoomSmoothTime = 0.05f;

    [Header("Scene Fade In")]
    [SerializeField] private bool enableSceneFadeIn = true;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private bool fadeUseUnscaledTime = true;

    private Camera cam;
    private float sceneFadeAlpha;
    private Movement movement;
    private float trauma;
    private float bumpOffsetY;
    private float currentLookahead;
    private float lookaheadVelocity;
    private float zoomVelocity;
    private float velocityX;
    private float velocityY;
    private float velocityZ;
    private float shiftVelocityX;
    private Vector3 followPosition;
    private bool followInitialized;

    public float LandingMinFallSpeed => landingMinFallSpeed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultOrthographicSize = cam.orthographicSize;
        }

        if (target == null && findPlayerByTag)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null)
        {
            movement = target.GetComponent<Movement>();
        }

        if (transform.parent != null && target != null && transform.IsChildOf(target))
        {
            transform.SetParent(null, true);
        }

        if (target != null)
        {
            SnapFollowToTarget();
        }
        else
        {
            followPosition = transform.position;
            followInitialized = true;
        }

        sceneFadeAlpha = enableSceneFadeIn && fadeInDuration > 0f ? 1f : 0f;
    }

    private void Update()
    {
        UpdateSceneFadeIn();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateFollow();
        ApplyBossShift();
        UpdateTraumaAndBump();
        ApplyShakeOffset();
        UpdateDashZoom();
    }

    public void OnPlayerDamaged()
    {
        AddTrauma(damageTrauma);
    }

    public void OnPlayerLanded(float impactSpeed)
    {
        if (impactSpeed < landingMinFallSpeed)
        {
            return;
        }

        AddTrauma(landingTrauma);
        float impactFactor = Mathf.Clamp01((impactSpeed - landingMinFallSpeed) / 12f);
        bumpOffsetY = Mathf.Max(bumpOffsetY, landingBumpYOffset * impactFactor);
    }

    public void OnDashStarted()
    {
        // Zoom handled continuously while dashing via movement state.
    }

    public void OnDashEnded()
    {
        // Zoom handled continuously while dashing via movement state.
    }

    private void SnapFollowToTarget()
    {
        float lookaheadTarget = 0f;
        if (movement != null)
        {
            lookaheadTarget = movement.IsFacingRight ? facingLookahead : -facingLookahead;
        }

        currentLookahead = lookaheadTarget;
        lookaheadVelocity = 0f;
        followPosition = target.position + baseOffset + Vector3.right * currentLookahead;
        transform.position = followPosition;
        velocityX = velocityY = velocityZ = 0f;
        shiftVelocityX = 0f;
        followInitialized = true;
    }

    private void UpdateFollow()
    {
        float lookaheadTarget = 0f;
        if (movement != null)
        {
            lookaheadTarget = movement.IsFacingRight ? facingLookahead : -facingLookahead;
        }

        currentLookahead = Mathf.SmoothDamp(
            currentLookahead,
            lookaheadTarget,
            ref lookaheadVelocity,
            facingLookaheadSmoothTime);

        Vector3 desired = target.position + baseOffset + Vector3.right * currentLookahead;

        if (!followInitialized)
        {
            followPosition = desired;
            followInitialized = true;
            velocityX = velocityY = velocityZ = 0f;
        }

        float smoothY = target.position.y > followPosition.y ? smoothTimeYUp : smoothTimeYDown;

        followPosition.x = Mathf.SmoothDamp(followPosition.x, desired.x, ref velocityX, smoothTimeX);
        followPosition.y = Mathf.SmoothDamp(followPosition.y, desired.y, ref velocityY, smoothY);
        followPosition.z = Mathf.SmoothDamp(followPosition.z, desired.z, ref velocityZ, smoothTimeX);

        transform.position = followPosition;
    }

    private void ApplyBossShift()
    {
        if (!enableBossShift)
        {
            return;
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag(shiftTargetTag);
        float totalInfluence = 0f;

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 targetPos = targets[i].transform.position;
            float dist = Vector2.Distance(target.position, targetPos);
            float magnitude = Mathf.Clamp01(1f - (dist / influenceDistance));
            magnitude = magnitude * magnitude * (3f - 2f * magnitude);
            float direction = Mathf.Sign(target.position.x - targetPos.x);
            totalInfluence += magnitude * direction;
        }

        totalInfluence = Mathf.Clamp(totalInfluence, -1f, 1f);
        float desiredShiftX = maxShift * totalInfluence;
        float newX = Mathf.SmoothDamp(
            transform.position.x,
            followPosition.x + desiredShiftX,
            ref shiftVelocityX,
            shiftSmoothTime);

        Vector3 pos = transform.position;
        pos.x = newX;
        transform.position = pos;
        followPosition.x = newX;
    }

    private void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    private void UpdateTraumaAndBump()
    {
        float dt = Time.deltaTime;
        trauma = Mathf.Max(0f, trauma - traumaDecayPerSecond * dt);
        bumpOffsetY = Mathf.MoveTowards(bumpOffsetY, 0f, bumpDecayPerSecond * dt);
    }

    private void ApplyShakeOffset()
    {
        float shakeScale = trauma * trauma * maxShakeOffset;
        float seed = Time.time * shakeFrequency;
        float offsetX = (Mathf.PerlinNoise(seed, 0.1f) - 0.5f) * 2f * shakeScale;
        float offsetY = (Mathf.PerlinNoise(0.2f, seed) - 0.5f) * 2f * shakeScale + bumpOffsetY;

        Vector3 pos = transform.position;
        pos.x += offsetX;
        pos.y += offsetY;
        transform.position = pos;
    }

    private void UpdateDashZoom()
    {
        if (cam == null)
        {
            return;
        }

        float targetSize = movement != null && movement.IsDashing ? dashOrthographicSize : defaultOrthographicSize;
        float dt = Time.timeScale > 0f && Time.timeScale < 1f ? Time.unscaledDeltaTime : Time.deltaTime;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
    }

    private void UpdateSceneFadeIn()
    {
        if (sceneFadeAlpha <= 0f)
        {
            return;
        }

        float dt = fadeUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        sceneFadeAlpha = Mathf.MoveTowards(sceneFadeAlpha, 0f, dt / fadeInDuration);
    }

    private void OnGUI()
    {
        if (sceneFadeAlpha <= 0f)
        {
            return;
        }

        Color previousGuiColor = GUI.color;
        GUI.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeColor.a * sceneFadeAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousGuiColor;
    }
}
