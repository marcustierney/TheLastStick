UGS Analytics — repo-specific plan

Current state





[Packages/manifest.json](Packages/manifest.json): com.unity.services.analytics 6.3.0 present; lockfile pulls **com.unity.services.core** (needed for UnityServices.InitializeAsync). **com.unity.modules.adaptiveperformance** is listed (optional later for thermal/CPU/GPU hints on supported devices — not required for v1 snapshot).



No UnityServices / AnalyticsService usage in [Assets/Scripts](Assets/Scripts) yet — greenfield wiring.



Player death: [Assets/Scripts/UpdateHealth.cs](Assets/Scripts/UpdateHealth.cs) — TakeDamage → Die() → DeathSequenceRoutine() (death UI overlay, not a separate death scene).



Coins: [Assets/Scripts/CoinManager.cs](Assets/Scripts/CoinManager.cs) — DontDestroyOnLoad, persistent wallet; **AddCoins** is the right choke point for “coins gained this session” (increment a session counter, not SpendCoins).



Shop upgrades: [Assets/Scripts/ShopUI.cs](Assets/Scripts/ShopUI.cs) → CoinManager.TryBuySpeedUpgrade / TryBuyDamageUpgrade / TryBuyHealthUpgrade after successful purchase.



Tutorial signs: [Assets/Scripts/Sign.cs](Assets/Scripts/Sign.cs) — Interact() already sets IsInteracted; add a stable sign_id here.



Level progression (where to hang **level_completed** / flush before loads):





Boss 1: [Assets/Scripts/BossController.cs](Assets/Scripts/BossController.cs) Die() → SceneTransition.SetPendingNextScene("LevelTwo", …) + LoadScene("LoadingScreen").



Doors: [Assets/Scripts/DoorTriggerScript.cs](Assets/Scripts/DoorTriggerScript.cs) — boss door / LevelOneDoor (tutorial exit → LevelOne).



Async loads: [Assets/Scripts/SceneTransition.cs](Assets/Scripts/SceneTransition.cs) — LoadSceneAsync before activation (optional flush point when allowSceneActivation = true if you want one central place for loading-screen transitions).



Level 2 “finished” (product rule): For now, Level 2 is complete when boss 2 is killed — fire **level_completed** from [BossTwoController.Die](Assets/Scripts/BossTwoController.cs) using SceneManager.GetActiveScene().name (expected **LevelTwoBoss**). Die() today only Destroys the boss; add level_completed (+ time/coins/deaths/final_health from LevelRunStats, flush) there. If you later add scene load / credits, keep analytics on the same Die() path (or shared win helper) so gameplay and telemetry stay aligned.



Boss HP for tuning: [BossHealth.cs](Assets/Scripts/BossHealth.cs) exposes Health / MaxHealth (floats). Boss 1 wires this on the same object as [BossController](Assets/Scripts/BossController.cs); boss 2 uses [BossTwoController](Assets/Scripts/BossTwoController.cs)’s serialized bossHealth reference.

flowchart LR
  subgraph init [Startup]
    Bootstrap[UgsAnalyticsBootstrap]
    Bootstrap --> UnityServicesInit
    UnityServicesInit --> AnalyticsStart
  end
  subgraph session [Per level scene]
    Tracker[LevelRunAnalytics or static tracker]
    Tracker --> timer[time_seconds]
    Tracker --> deaths[deaths_this_run]
    Tracker --> coins[coins_this_run]
    Tracker --> falls[level_two_fall_restarts]
  end
  ResetPosition --> falls
  UpdateHealth --> player_death
  UpdateHealth --> session
  CoinManager --> coins
  completion[Door / Boss1Die / Boss2Die] --> level_completed
  completion --> session



1. Setup (code + Editor)







Task



Detail





Package



Already satisfied; no manifest change unless you want to pin com.unity.services.core explicitly.





Editor



Edit → Project Settings → Services: link Project ID (manual).





Bootstrap



New script e.g. [Assets/Scripts/UgsAnalyticsBootstrap.cs](Assets/Scripts/UgsAnalyticsBootstrap.cs): DontDestroyOnLoad root, async void Start() or coroutine: await UnityServices.InitializeAsync(); then AnalyticsService.Instance.StartDataCollection(); guard with try/catch + “already initialized” so duplicates safe. Place component on first scene that always runs (typically [Assets/Scenes/MainMenu.unity](Assets/Scenes/MainMenu.unity) or [LoadingScreen.unity](Assets/Scenes/LoadingScreen.unity) — whichever actually loads first in File → Build Settings).





Shared helpers



New [Assets/Scripts/AnalyticsKeys.cs](Assets/Scripts/AnalyticsKeys.cs): const event names + death_cause strings to avoid typos. New thin wrapper e.g. GameAnalytics.cs static methods: CustomData(...), FlushIfReady() that no-op if services not initialized (safe from editor hot-reload / missing config).



2. Per-level session state (replaces placing LevelTimer on every scene)

Single DontDestroyOnLoad listener (can live on same object as bootstrap or small LevelRunStats):





Subscribe SceneManager.sceneLoaded.



Maintain whitelist of analytics gameplay scenes (names from repo: Tutorial, LevelOne, LevelOneBoss, LevelTwo, LevelTwoBoss, LevelThree, transitions optional — decide whether transitions count as separate level_name or exclude them from timer).



On entering a tracked scene: reset levelStartTime, deathsThisRun, coinsThisRun, **levelTwoFallRestarts** (and optionally snapshot scene name).



Provide: RegisterCoinPickup(int amount), RegisterDeath(), RegisterLevelTwoFallReset() (see subsection below), GetElapsedSeconds(), CurrentDeaths, CurrentCoinsThisRun, CurrentLevelTwoFallRestarts.

Level 2 — pit fall / soft restart count

What happens today: [Assets/Scenes/LevelTwo.unity](Assets/Scenes/LevelTwo.unity) uses [ResetPosition](Assets/Scripts/ResetPosition.cs): on player collision, position resets (soft reset / checkpoint), not a full LoadScene of LevelTwo. [PauseManager.Restart()](Assets/Scripts/PauseManager.cs) reloads **Tutorial** and clears progress — that is a full run reset, not “restart Level 2”; do not treat it as Level 2 fall analytics unless you change that flow later.

What to track: Each time the player hits a ResetPosition pit while SceneManager.GetActiveScene().name == "LevelTwo", increment a session counter level_two_fall_restarts (name matches “had to redo the segment” intent).

Where to wire: At the start of [ResetPosition.OnCollisionEnter2D](Assets/Scripts/ResetPosition.cs) after confirming Player tag, call LevelRunStats.RegisterLevelTwoFallReset() (no-op if scene ≠ LevelTwo).

How to emit:





Recommended: Add level_two_fall_restarts (int) to the same payloads where you already send level_name for **LevelTwo** exits — e.g. level_completed, level_time_spent, coins_collected, and player_death when level_name is LevelTwo — so dashboards get one row per exit with the total count for that attempt.



Optional: Also fire a lightweight per-incident event e.g. level_two_fall_reset with { "level_name": "LevelTwo" } each time if you want a raw event stream (more events, easier funnels).

Constants: Add event/param keys to [AnalyticsKeys](Assets/Scripts/AnalyticsKeys.cs) (level_two_fall_restarts, optional level_two_fall_reset).



**level_time_spent + coins_collected on exit:** call from:





[UpdateHealth.Die](Assets/Scripts/UpdateHealth.cs) (death path) — after computing duration; include player_death with death_cause and boss snapshot when in a boss fight (see section 3).



Each level-complete path (below) — fire level_completed and also level_time_spent / coins_collected if you want parity with your table (your table says time/coins on “level exits”; death is an exit too).

Call **FlushIfReady()** immediately before **SceneManager.LoadScene / LoadSceneAsync** in edited call sites ([DoorTriggerScript](Assets/Scripts/DoorTriggerScript.cs), [BossController](Assets/Scripts/BossController.cs), [MainMenu](Assets/Scripts/MainMenu.cs), [PauseManager](Assets/Scripts/PauseManager.cs), etc.) — fewer misses than only one central place, because loads are scattered today.



3. Death + death_cause + boss HP (boss fights)





Extend [UpdateHealth.TakeDamage(float damage, Vector2 hitSourcePosition)](Assets/Scripts/UpdateHealth.cs) with optional string deathCause = null (overload or default param). When damage would kill, store last non-null cause in a field; Die() passes death_cause to player_death (default unknown if never set).



Increment deathsThisRun in Die() (session tracker).



Roll out causes incrementally at damage sites under [Assets/Scripts](Assets/Scripts) (grep playerHealth.TakeDamage / health.TakeDamage): e.g. [EnemySwordHitbox](Assets/Scripts/EnemySwordHitbox.cs), [BossController](Assets/Scripts/BossController.cs) slam zones, [BossTwoController](Assets/Scripts/BossTwoController.cs) dash, [ToxicSpitProjectile](Assets/Scripts/ToxicSpitProjectile.cs), [RockProjectile](Assets/Scripts/RockProjectile.cs), etc. Use AnalyticsKeys constants per enemy/boss attack.

Boss HP when the player loses (boss scenes)

When the player dies and the active scene is a boss fight (at minimum **LevelOneBoss** and **LevelTwoBoss** — keep a small whitelist in AnalyticsKeys or LevelRunStats), resolve the active boss’s [BossHealth](Assets/Scripts/BossHealth.cs) and add to the same player_death payload (lowercase keys per UGS convention):





boss_health — current BossHealth.Health (e.g. Mathf.RoundToInt or raw float; stay consistent across events).



boss_max_health — BossHealth.MaxHealth.

Resolution strategy: Prefer a single Object.FindFirstObjectByType<BossHealth>() in boss scenes (there should be one boss health bar target); if you ever have multiple instances, switch to a serialized reference on a tiny BossFightContext in each boss scene or tag the boss root.

Non-boss scenes: Omit both keys (or omit player_death extras only — do not send placeholder zeros unless dashboards require it).

Add parameter name constants to [AnalyticsKeys](Assets/Scripts/AnalyticsKeys.cs).

Note: [ResetPosition](Assets/Scripts/ResetPosition.cs) does not kill the player; in **LevelTwo** it increments pit fall / soft restart analytics (see section 2, “Level 2 — pit fall”). Use death_cause: "fall" on TakeDamage only if you add lethal pit damage later.



4. Coins this run





In [CoinManager.AddCoins](Assets/Scripts/CoinManager.cs), after updating wallet, call LevelRunStats.RegisterCoinPickup(amount) (no-op if not in tracked scene). Do not count coins on menu-only loads.



5. level_completed

Fire level_completed with level_name, time_seconds, deaths_this_run, coins_this_run, final_health (Mathf.RoundToInt or raw float — match dashboard preference; your spec says raw HP). When level_name is **LevelTwo, also include **level_two_fall_restarts (session pit count from the platforming scene). For **LevelTwoBoss** completion, level_two_fall_restarts is usually 0 unless you carry over stats cross-scene (default: omit on LevelTwoBoss or send 0 — pick one and document).

Concrete hooks:







Location



Semantics





[BossController.Die](Assets/Scripts/BossController.cs)



Boss 1 defeated → **level_completed** with level_name = active scene (**LevelOneBoss**).





[BossTwoController.Die](Assets/Scripts/BossTwoController.cs)



Boss 2 defeated → **level_completed** with level_name = **LevelTwoBoss** — canonical “Level 2 finished” for analytics right now.





[DoorTriggerScript](Assets/Scripts/DoorTriggerScript.cs)



Boss door from LevelOne / LevelTwo → segment / reach boss; LevelOneDoor → tutorial exit → LevelOne. Still fire level_completed per scene name if you want per-segment funnels; do not treat LevelTwo door alone as full Level 2 completion (boss kill is).

Then FlushIfReady() before scene load.



6. upgrade_taken





In [ShopUI.cs](Assets/Scripts/ShopUI.cs) after each successful TryBuy* (speed / damage / health): upgrade_id e.g. shop_speed, shop_damage, shop_health; upgrade_name from existing UI labels or fixed strings; level_name = SceneManager.GetActiveScene().name.



**upgrade_used_in_level:** v1 can equal level_name (same scene as shop) or persist first-use in PlayerPrefs when relevant gameplay first runs — only add second if you need the spec literally; otherwise document simplification.



7. Tutorial signs





[Sign.cs](Assets/Scripts/Sign.cs): add [SerializeField] string signId (inspector per sign). On successful Interact(), fire tutorial_sign_read with sign_id.



Skipped signs: e.g. small [TutorialSignAnalytics.cs](Assets/Scripts/TutorialSignAnalytics.cs) in Tutorial scene only: Awake finds all Sign, or each Sign registers to static list on OnEnable, unregisters OnDisable. When leaving Tutorial (sceneLoaded and new scene ≠ Tutorial, previous == Tutorial), for each sign with !IsInteracted emit tutorial_sign_skipped. Avoid double-fire with a one-shot flag per session.



8. Hardware + performance snapshot (session)

Goal: One non-PII-ish hardware fingerprint + one lightweight perf sample per session (or first gameplay scene), via custom events — same UGS pipeline as the rest of the plan. Full Editor Profiler captures are not shipped to production players; this is aggregated telemetry, not a downloadable trace.

8a. Device / hardware (device_profile or session_hardware)

After UnityServices.InitializeAsync + StartDataCollection succeed (same bootstrap as section 1), build one dictionary from **UnityEngine.SystemInfo** (all keys snake_case). Useful fields (trim if you hit UGS payload limits — split into a second event if needed):





device_model, device_type, operating_system, operating_system_family



processor_type, processor_count, system_memory_size



graphics_device_name, graphics_device_type, graphics_device_vendor, graphics_memory_size, graphics_shader_level, max_texture_size



supports_compute_shaders, supports_instancing



screen_width, screen_height, screen_refresh_rate (where available)



unity_version, application_version (Application.version)

Privacy / stores: Treat as device analytics; align with your privacy policy / platform data-safety disclosures (typically OK for aggregate tuning, avoid tying to account IDs unless product/legal approves).

8b. Performance snapshot (performance_snapshot)

v1 (no extra packages): After a short warm-up (e.g. 1–2s unscaled after first scene where timeScale is 1), sample for N seconds (e.g. 5s):





FPS: from Time.unscaledDeltaTime — record fps_avg, fps_min (and optionally approximate fps_1pct_low by tracking worst frames).



Settings: quality_level index + name (QualitySettings.names), vsync (QualitySettings.vsyncCount / OnDemandRendering if used), target_frame_rate (Application.targetFrameRate).



Context: scene_name at sample start, gpu_skinning (SystemInfo.supportsGpuSkinning) if useful.

Optional upgrade path: Add **com.unity.profiling.recorder** (Profiler Recorder) or **FrameTimingManager**-based CPU/GPU ms if you need deeper GPU/CPU frame breakdown and confirm target platforms support it — not in [Packages/manifest.json](Packages/manifest.json) today.

Optional mobile: Project already references **com.unity.modules.adaptiveperformance**; a later iteration could attach thermal/bottleneck hints on supported hardware.

Where to implement: Extend [UgsAnalyticsBootstrap](Assets/Scripts/UgsAnalyticsBootstrap.cs) (or sibling SessionTelemetry.cs) with a coroutine: init → device_profile event → wait → sample window → performance_snapshot → FlushIfOnce optional.



9. QA





Play Mode + Unity Dashboard / Debug Event Inspector (package-dependent).



Verify init order: bootstrap scene runs before first CustomData.



Test: death (including boss fight deaths with boss_health / boss_max_health), door, boss1 die, boss2 die → level_completed on LevelTwoBoss, shop buy, sign read, tutorial leave with unread signs, LevelTwo pit falls on LevelTwo exit payloads, **device_profile + performance_snapshot** once per session on device.



Implementation order (adjusted to this repo)





Bootstrap + GameAnalytics helper + AnalyticsKeys + session device_profile + performance_snapshot (after init).



LevelRunStats + scene whitelist + CoinManager hook + UpdateHealth death events + exit events (level_time_spent, coins_collected).



death_cause overload + highest-traffic damage call sites + boss HP snapshot on death in boss scenes.



level_completed on BossController.Die + BossTwoController.Die + DoorTriggerScript; boss HP on player_death in boss scenes.



ShopUI upgrade events.



Sign IDs + tutorial skipped logic.



Level 2 falls: ResetPosition → LevelRunStats; add level_two_fall_restarts to LevelTwo exit payloads.



QA pass + flush audit on remaining LoadScene call sites if any missed; verify device + perf events on target hardware (Editor vs device).

