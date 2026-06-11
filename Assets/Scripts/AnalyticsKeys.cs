/// <summary>
/// UGS custom event names and parameter keys / death_cause values (snake_case for payloads).
/// </summary>
public static class AnalyticsKeys
{
    public const string EventPlayerDeath = "player_death";
    public const string EventLevelCompleted = "level_completed";
    public const string EventLevelTimeSpent = "level_time_spent";
    public const string EventCoinsCollected = "coins_collected";
    public const string EventUpgradeTaken = "upgrade_taken";
    public const string EventTutorialSignRead = "tutorial_sign_read";
    public const string EventTutorialSignSkipped = "tutorial_sign_skipped";
    public const string EventDeviceProfile = "device_profile";
    public const string EventPerformanceSnapshot = "performance_snapshot";

    public const string ParamLevelName = "level_name";
    public const string ParamTimeSeconds = "time_seconds";
    public const string ParamSpeedrunTimeSeconds = "speedrun_time_seconds";
    public const string ParamDeathsThisRun = "deaths_this_run";
    public const string ParamCoinsThisRun = "coins_this_run";
    public const string ParamFinalHealth = "final_health";
    public const string ParamLevelTwoFallRestarts = "level_two_fall_restarts";
    public const string ParamDeathCause = "death_cause";
    public const string ParamBossHealth = "boss_health";
    public const string ParamBossMaxHealth = "boss_max_health";

    public const string ParamUpgradeId = "upgrade_id";
    public const string ParamUpgradeName = "upgrade_name";
    public const string ParamSignId = "sign_id";

    public const string DeathCauseUnknown = "unknown";
    public const string DeathCauseEnemySword = "enemy_sword";
    public const string DeathCauseSpider = "spider";
    public const string DeathCauseBossSlam = "boss_slam";
    public const string DeathCauseBossTwoDash = "boss_two_dash";
    public const string DeathCauseToxicSpit = "toxic_spit";
    public const string DeathCauseRock = "rock";
    public const string DeathCauseToxicPuddle = "toxic_puddle";
    public const string DeathCauseFlyingSword = "flying_sword";
    public const string DeathCauseBossFloatingSwords = "boss_floating_swords";
    public const string DeathCauseRainingSword = "raining_sword";
    public const string DeathCauseShieldEnemy = "shield_enemy";
    public const string DeathCauseRangeStudent = "range_student";
    public const string DeathCauseDojoStudent = "dojo_student";
    public const string DeathCauseSpiderEnemy = "spider_enemy";
    public const string DeathCauseToxicSpider = "toxic_spider";
    public const string DeathCauseBossThrownSword = "boss_thrown_sword";

    public const string UpgradeIdShopSpeed = "shop_speed";
    public const string UpgradeIdShopDamage = "shop_damage";
    public const string UpgradeIdShopHealth = "shop_health";

    public const string SceneTutorial = "Tutorial";
    public const string SceneLevelOne = "LevelOne";
    public const string SceneLevelOneBoss = "LevelOneBoss";
    public const string SceneLevelTwo = "LevelTwo";
    public const string SceneLevelTwoBoss = "LevelTwoBoss";
    public const string SceneLevelThree = "LevelThree";

    public static bool IsBossFightScene(string sceneName)
    {
        return sceneName == SceneLevelOneBoss || sceneName == SceneLevelTwoBoss;
    }
}
