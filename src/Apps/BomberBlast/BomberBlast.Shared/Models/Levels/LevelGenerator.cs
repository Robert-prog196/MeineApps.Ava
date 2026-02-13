using BomberBlast.Models.Entities;

namespace BomberBlast.Models.Levels;

/// <summary>
/// Generates levels based on NES Bomberman progression
/// </summary>
public static class LevelGenerator
{
    /// <summary>
    /// Generate a level configuration for a specific level number
    /// </summary>
    public static Level GenerateLevel(int levelNumber)
    {
        var level = new Level
        {
            Number = levelNumber,
            Name = $"Stage {levelNumber}",
            TimeLimit = 200,
            Seed = levelNumber * 12345 // Deterministic seed
        };

        // Check for bonus level (every 5th level)
        if (levelNumber % 5 == 0 && levelNumber <= 50)
        {
            ConfigureBonusLevel(level, levelNumber);
            return level;
        }

        // Configure based on level range
        ConfigureEnemies(level, levelNumber);
        ConfigurePowerUps(level, levelNumber);
        ConfigureBlockDensity(level, levelNumber);

        // Boss music for Pontan stages
        if (levelNumber >= 36)
        {
            level.MusicTrack = "boss";
        }

        return level;
    }

    /// <summary>
    /// Generate an arcade mode level
    /// </summary>
    public static Level GenerateArcadeLevel(int wave)
    {
        var level = new Level
        {
            Number = wave,
            Name = $"Wave {wave}",
            TimeLimit = Math.Max(120, 200 - wave * 5), // Decreasing time
            Seed = wave * 54321 + Environment.TickCount,
            BlockDensity = Math.Min(0.7f, 0.4f + wave * 0.02f)
        };

        // Progressively harder enemies
        ConfigureArcadeEnemies(level, wave);
        ConfigureArcadePowerUps(level, wave);

        return level;
    }

    private static void ConfigureEnemies(Level level, int levelNumber)
    {
        level.Enemies.Clear();

        // Enemy progression (reduced counts for 11x9 GBA-style grid)
        switch (levelNumber)
        {
            case >= 1 and <= 5:
                // Tutorial: Ballom only (2-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ballom, Count = 2 + levelNumber / 2 });
                break;

            case >= 6 and <= 10:
                // Introduce Onil (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ballom, Count = 1 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Onil, Count = 2 + (levelNumber - 6) / 2 });
                break;

            case >= 11 and <= 15:
                // Onil + Doll (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Onil, Count = 2 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Doll, Count = 1 + (levelNumber - 11) / 2 });
                break;

            case >= 16 and <= 20:
                // Doll + Minvo (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Doll, Count = 1 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Minvo, Count = 2 + (levelNumber - 16) / 2 });
                break;

            case >= 21 and <= 25:
                // Minvo + Kondoria (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Minvo, Count = 2 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Kondoria, Count = 1 + (levelNumber - 21) / 2 });
                break;

            case >= 26 and <= 30:
                // Kondoria + Ovapi (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Kondoria, Count = 1 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ovapi, Count = 2 + (levelNumber - 26) / 2 });
                break;

            case >= 31 and <= 35:
                // Ovapi + Pass (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ovapi, Count = 2 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pass, Count = 1 + (levelNumber - 31) / 2 });
                break;

            case >= 36 and <= 40:
                // Pass + Pontan (3-4 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pass, Count = 2 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pontan, Count = 1 + (levelNumber - 36) / 3 });
                break;

            case >= 41 and <= 50:
                // Mixed chaos (4-5 enemies)
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Minvo, Count = 1 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Kondoria, Count = 1 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pass, Count = 1 + (levelNumber - 41) / 3 });
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pontan, Count = 1 });
                break;
        }
    }

    private static void ConfigurePowerUps(Level level, int levelNumber)
    {
        level.PowerUps.Clear();

        // Always at least one power-up
        // Guaranteed useful power-ups early on

        if (levelNumber <= 5)
        {
            // Easy levels: Bomb Up and Fire
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.BombUp });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
        }
        else if (levelNumber <= 15)
        {
            // Mid-early: Add Speed
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.BombUp });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Speed });
        }
        else if (levelNumber <= 25)
        {
            // Mid: Kick + Spezialfähigkeiten
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Speed });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Kick });
            // Ab Level 20: Skull als Risiko-Element
            if (levelNumber >= 20)
                level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Skull });
        }
        else if (levelNumber <= 35)
        {
            // Mid-late: Wall pass + Line-Bomb + Skull
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Wallpass });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.LineBomb });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Skull });
        }
        else
        {
            // Late game: Power-Bomb + Survival
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.PowerBomb });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Flamepass });
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Skull });
        }
    }

    private static void ConfigureBlockDensity(Level level, int levelNumber)
    {
        // Start with more open levels, get denser over time
        level.BlockDensity = 0.35f + (levelNumber / 50f) * 0.25f;
    }

    private static void ConfigureBonusLevel(Level level, int levelNumber)
    {
        level.IsBonusLevel = true;
        level.Name = $"Bonus Stage {levelNumber / 5}";
        level.TimeLimit = 45; // Shorter time for smaller grid
        level.BlockDensity = 0.5f;

        // Bonus levels spawn weak enemies (reduced for 11x9 grid)
        level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ballom, Count = 6 });

        // Power-ups (reduced for smaller grid)
        level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.BombUp });
        level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Fire });
        level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Speed });
        level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Mystery });
    }

    private static void ConfigureArcadeEnemies(Level level, int wave)
    {
        level.Enemies.Clear();

        // Progressively add harder enemies (reduced for 11x9 grid)
        int baseCount = 2 + wave / 3;  // Max ~5 enemies
        baseCount = Math.Min(baseCount, 5);

        if (wave <= 3)
        {
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ballom, Count = baseCount });
        }
        else if (wave <= 6)
        {
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Ballom, Count = 1 });
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Onil, Count = baseCount - 1 });
        }
        else if (wave <= 10)
        {
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Onil, Count = 1 });
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Doll, Count = 1 });
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Minvo, Count = baseCount - 2 });
        }
        else
        {
            // Chaos mode (still limited to ~5 enemies)
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Minvo, Count = 1 });
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Kondoria, Count = 1 });
            level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pass, Count = 1 });
            if (wave >= 15)
            {
                level.Enemies.Add(new EnemySpawn { Type = EnemyType.Pontan, Count = 1 });
            }
        }
    }

    private static void ConfigureArcadePowerUps(Level level, int wave)
    {
        level.PowerUps.Clear();

        // PowerUp-Pool nach Wave-Fortschritt (Skull erst ab Wave 5, maximal 1)
        var pool = new List<PowerUpType>
        {
            PowerUpType.BombUp, PowerUpType.Fire, PowerUpType.Speed,
            PowerUpType.Wallpass, PowerUpType.Detonator, PowerUpType.Bombpass,
            PowerUpType.Flamepass, PowerUpType.Mystery, PowerUpType.Kick
        };
        if (wave >= 5) pool.Add(PowerUpType.LineBomb);
        if (wave >= 8) pool.Add(PowerUpType.PowerBomb);

        var random = new Random(wave * 11111);
        int powerUpCount = Math.Max(1, 4 - wave / 5);
        bool skullAdded = false;

        for (int i = 0; i < powerUpCount; i++)
        {
            var type = pool[random.Next(pool.Count)];
            level.PowerUps.Add(new PowerUpPlacement { Type = type });
        }

        // Ab Wave 5: 40% Chance auf einen Skull (maximal 1)
        if (wave >= 5 && !skullAdded && random.NextDouble() < 0.4)
        {
            level.PowerUps.Add(new PowerUpPlacement { Type = PowerUpType.Skull });
        }
    }
}
