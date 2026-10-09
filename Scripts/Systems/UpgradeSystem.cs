using System;
using System.Collections.Generic;
using Godot;

//Define los tipos de upgrades que existen
public enum UpgradeType
{
    Art, //Mejora un arte
    Ability, //Mejora una habilidad
    Dash, //Mejora el dash
    Stat //Mejora un stat genérico (Vida, Velocidad de movimiento, etc)
}

//Un upgrade individual
public class Upgrade
{
    public string Id; //Identificador único, por ejemplo "fireball_damage"
    public UpgradeType Type;
    public Element ElementType = Element.Neutral;
    public bool IsInfinite; //Se puede tomar más de una vez?
    public int TimesApplied; //Cuantas veces lo tomó el jugador
    public int MaxAcquisitions; //Límite de cuantas veces puede agarrarse (0 = sin límite)

    //Función que modifica al jugador cuando se elige esta mejora
    public Action<Player, int> Apply;

    //Permite seleccionar una clave alternativa para descripciones dinámicas.
    public Func<int, string> GetDescriptionKey;
    public Func<int, object[]> GetDescriptionArguments;

    //Condición opcional, si es null el upgrade siempre estará disponible, si no es null, solo aparecerá si la condición devuelve true.
    public Func<Player, bool> Condition = null;

    //Ids de upgrades que se bloquean cuando este se toma
    public string[] Excludes = System.Array.Empty<string>();

    public string NameKey => $"upgrade.{Id}.name";
    public string DescriptionKey => $"upgrade.{Id}.description";

    public string GetLocalizedDescriptionKey(int times)
    {
        return GetDescriptionKey?.Invoke(times) ?? DescriptionKey;
    }

    public object[] GetLocalizedDescriptionArguments(int times)
    {
        return GetDescriptionArguments?.Invoke(times) ?? System.Array.Empty<object>();
    }
}

public class UpgradeSystem
{
    //Todos los upgrades disponibles en el juego
    private List<Upgrade> _allUpgrades = new();

    //Los upgrades que el jugador ya tiene
    private List<Upgrade> _acquiredUpgrades = new();

    //Ids de upgrades bloqueados por exclusión
    private HashSet<string> _excludedIds = new();

    private Player _player;
    private PlayerStats _stats;
    private Random _random = new();

    public IReadOnlyList<Upgrade> AcquiredUpgrades => _acquiredUpgrades;

    public UpgradeSystem(Player player, PlayerStats stats)
    {
        _player = player;
        _stats = stats;
        RegisterUpgrades();
    }

    //Genera una lista de 3 upgrades para mostrar al jugador
    public List<Upgrade> GetUpgradeChoices()
    {
        List<Upgrade> available = GetAvailableUpgrades();
        List<Upgrade> choices = new();

        //Elegimos 3 upgrades al azar sin repetir
        while (choices.Count < 3 && available.Count > 0)
        {
            int index = _random.Next(available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }

        return choices;
    }

    private List<Upgrade> GetAvailableUpgrades()
    {
        List<Upgrade> available = new();

        foreach (var upgrade in _allUpgrades)
        {
            //Los upgrades únicos solo aparecen si no fueron tomados
            if (!upgrade.IsInfinite && upgrade.TimesApplied > 0)
                continue;

            //Si es infinito pero tiene un límite máximo de veces (mayor a 0), verificamos no excederlo
            if (upgrade.IsInfinite && upgrade.MaxAcquisitions > 0 && upgrade.TimesApplied >= upgrade.MaxAcquisitions)
                continue;

            //Si tiene condición y no se cumple, no aparece
            if (upgrade.Condition != null && !upgrade.Condition(_player))
                continue;

            //Si está en la lista de excluidos, no aparece
            if (_excludedIds.Contains(upgrade.Id))
                continue;

            available.Add(upgrade);
        }

        return available;
    }

    //Aplica un upgrade al jugador
    public void ApplyUpgrade(Upgrade upgrade)
    {
        upgrade.TimesApplied++;
        upgrade.Apply(_player, upgrade.TimesApplied);

        if (!_acquiredUpgrades.Contains(upgrade))
            _acquiredUpgrades.Add(upgrade);

        //Bloquea todos los upgrades que este excluye
        foreach (var excludedId in upgrade.Excludes)
            _excludedIds.Add(excludedId);
    }

    public void ApplyUpgradeById(string id)
    {
        var upgrade = _allUpgrades.Find(u => u.Id == id);
        if (upgrade != null)
        {
            ApplyUpgrade(upgrade);
        }
    }

    public Upgrade FindUpgradeById(string id)
    {
        return _allUpgrades.Find(u => u.Id == id);
    }

    //Acá se registran todas las upgrades del juego
    private void RegisterUpgrades()
    {
        //-------- STATS BASE --------
        //Upgrades genéricos de stats (siempre disponibles)
        _allUpgrades.Add(new Upgrade
        {
            Id = "stat_health",
            Type = UpgradeType.Stat,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.MaxHealth += 20f;
                player?.Health.IncreaseMaxHealth(20f);
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "stat_speed",
            Type = UpgradeType.Stat,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.Speed += 15f;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "stat_damage",
            Type = UpgradeType.Stat,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.BonusDamage += 6f;
            }
        });

        //Upgrade escalable de regeneración de vida
        _allUpgrades.Add(new Upgrade
        {
            Id = "stat_regen",
            GetDescriptionArguments = (times) => new object[] { times },
            Type = UpgradeType.Stat,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.HealthRegen += 1;
            }
        });

        //-------- DASH --------
        //Invencibilidad durante el dash
        _allUpgrades.Add(new Upgrade
        {
            Id = "dash_invincible",
            Type = UpgradeType.Dash,
            IsInfinite = false,
            Apply = (player, times) =>
            {
                _stats.DashIsInvincible = true;
            }
        });

        //Reduce el cooldown del dash
        _allUpgrades.Add(new Upgrade
        {
            Id = "dash_cooldown",
            GetDescriptionArguments = (times) => new object[] { _stats.DashCooldown },
            Type = UpgradeType.Dash,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.DashCooldown = Mathf.Max(0.5f, _stats.DashCooldown - 0.2f);
            },
            Condition = (player) => _stats.DashCooldown > 0.5f,
        });

        //Upgrade único de arte (cambia su comportamiento)
        //Dispara en ráfaga secuencial en línea recta
        _allUpgrades.Add(new Upgrade
        {
            Id = "fireball_burst",
            ElementType = Element.Fire,
            GetDescriptionKey = (times) => _stats.FireballBurstCount == 0
                ? "upgrade.fireball_burst.first_description"
                : "upgrade.fireball_burst.description",
            GetDescriptionArguments = (times) => new object[] { _stats.FireballBurstCount },
            Type = UpgradeType.Art,
            IsInfinite = true,
            MaxAcquisitions = 9, //Primera vez +2, luego 8 veces +1 = 10 proyectiles máximo
            Condition = (player) => !_excludedIds.Contains("fireball_burst"),
            Excludes = new[] { "fireball_multishot" },
            Apply = (player, times) =>
            {
                //Primera vez da 2 proyectiles, las siguientes +1
                _stats.FireballBurstCount += (times == 1) ? 2 : 1;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "fireball_piercing",
            ElementType = Element.Fire,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => !_excludedIds.Contains("fireball_piercing"),
            Excludes = new[] { "fireball_explosive" },
            Apply = (player, times) =>
            {
                _stats.FireballPiercing = true;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "fireball_multishot",
            ElementType = Element.Fire,
            GetDescriptionArguments = (times) => new object[] { _stats.FireballCount },
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => _stats.FireballCount <= 10 && !_excludedIds.Contains("fireball_multishot"),
            Excludes = new[] { "fireball_burst" },
            Apply = (player, times) =>
            {
                _stats.FireballCount += 2;
            },
        });

        //Bola de fuego explosiva
        _allUpgrades.Add(new Upgrade
        {
            Id = "fireball_explosive",
            ElementType = Element.Fire,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => !_excludedIds.Contains("fireball_explosive"),
            Excludes = new[] { "fireball_piercing" },
            Apply = (player, times) =>
            {
                _stats.FireballExplosive = true;
            }
        });

        //Upgrade stackeable de arte (escala con veces aplicado)
        _allUpgrades.Add(new Upgrade
        {
            Id = "fireball_damage",
            ElementType = Element.Fire,
            GetDescriptionArguments = (times) => new object[] { 2f * times },
            Type = UpgradeType.Art,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                //Primer vez +5, segunda +10, tercera +15...
                _stats.BonusFireballDamage += 2f * times;
            }
        });

        //-------- WATER BOLT --------
        _allUpgrades.Add(new Upgrade
        {
            Id = "unlock_water_bolt",
            ElementType = Element.Water,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => !_stats.HasWaterBolt,
            Apply = (player, times) =>
            {
                _stats.HasWaterBolt = true;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "water_bolt_damage",
            ElementType = Element.Water,
            GetDescriptionArguments = (times) => new object[] { _stats.BonusWaterBoltDamage },
            Type = UpgradeType.Art,
            IsInfinite = true,
            Condition = (player) => _stats.HasWaterBolt,
            Apply = (player, times) =>
            {
                _stats.BonusWaterBoltDamage += 2f;
                _stats.HasWaterBolt = true;
            }
        });

        //-------- FROST RAY --------
        //Upgrade único que desbloquea un rayo de hielo
        _allUpgrades.Add(new Upgrade
        {
            Id = "unlock_frost_ray",
            ElementType = Element.Ice,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Apply = (player, times) =>
            {
                _stats.HasFrostRay = true;
            }
        });

        // Permafrost desactivado temporalmente junto con FrozenEffect.
        // _allUpgrades.Add(new Upgrade
        // {
        //     Id = "frost_ray_permafrost",
        //     Name = "Permafrost",
        //     GetDescription = (times) => "Frost Ray now COMPLETELY freezes enemies hit. Freeze duration is halved. The bosses can't be completely frozen.",
        //     Type = UpgradeType.Art,
        //     IsInfinite = false,
        //     Condition = (player) => _stats.HasFrostRay,
        //     Apply = (player, times) =>
        //     {
        //         _stats.FrostRaySlowFactor = 0f;
        //         _stats.FrostRaySlowDuration /= 2f;
        //     }
        // });

        _allUpgrades.Add(new Upgrade
        {
            Id = "frost_ray_wide",
            ElementType = Element.Ice,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => _stats.HasFrostRay && !_excludedIds.Contains("frost_ray_wide"),
            Excludes = new[] { "frost_ray_chain" },
            Apply = (player, times) =>
            {
                _stats.FrostRayWidth = 40f;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "frost_ray_chain",
            ElementType = Element.Ice,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => _stats.HasFrostRay && !_excludedIds.Contains("frost_ray_chain"),
            Excludes = new[] { "frost_ray_wide" },
            Apply = (player, times) =>
            {
                _stats.FrostRayChain = true;
            }
        });

        // Lingering Chill desactivado temporalmente junto con FrozenEffect.
        // _allUpgrades.Add(new Upgrade
        // {
        //     Id = "frost_ray_slow_duration",
        //     Name = "Lingering Chill",
        //     GetDescription = (times) => (_stats.FrostRaySlowFactor != 0) ? $"+1s Frost Ray slow duration." : $"+0.5s Frost Ray freeze duration.",
        //     GetDescriptionKey = (times) => _stats.FrostRaySlowFactor != 0
        //         ? "upgrade.frost_ray_slow_duration.description"
        //         : "upgrade.frost_ray_slow_duration.freeze_description",
        //     Type = UpgradeType.Art,
        //     IsInfinite = true,
        //     Condition = (player) => _stats.HasFrostRay,
        //     Apply = (player, times) =>
        //     {
        //         _stats.FrostRaySlowDuration += (_stats.FrostRaySlowFactor != 0) ? 1f : 0.5f;
        //     }
        // });

        //-------- REPULSION BURST --------
        //Desbloquea el RepulsionBurst
        _allUpgrades.Add(new Upgrade
        {
            Id = "unlock_repulsion_burst",
            ElementType = Element.Earth,
            GetDescriptionArguments = (times) => new object[] { _stats.RepulsionBurstFireRate },
            Type = UpgradeType.Art,
            IsInfinite = false,
            Apply = (player, times) =>
            {
                _stats.HasRepulsionBurst = true;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "repulsion_burst_damage",
            ElementType = Element.Earth,
            GetDescriptionArguments = (times) => new object[] { _stats.BonusRepulsionBurstDamage },
            Type = UpgradeType.Art,
            IsInfinite = true,
            Condition = (player) => _stats.HasRepulsionBurst,
            Apply = (player, times) =>
            {
                _stats.BonusRepulsionBurstDamage += 3f;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "repulsion_burst_shockwave",
            ElementType = Element.Earth,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => _stats.HasRepulsionBurst && !_excludedIds.Contains("repulsion_burst_shockwave"),
            Excludes = new[] { "repulsion_burst_extended" },
            Apply = (player, times) =>
            {
                _stats.RepulsionBurstShockwave = true;
            }
        });

        _allUpgrades.Add(new Upgrade
        {
            Id = "repulsion_burst_extended",
            ElementType = Element.Earth,
            Type = UpgradeType.Art,
            IsInfinite = false,
            Condition = (player) => _stats.HasRepulsionBurst && !_excludedIds.Contains("repulsion_burst_extended"),
            Excludes = new[] { "repulsion_burst_shockwave" },
            Apply = (player, times) =>
            {
                _stats.RepulsionBurstRange += 60f;
                _stats.RepulsionBurstForce += 100f;
            }
        });

        //-------- NOVA DE FUEGO --------
        //La Nova lanza 3 bolas de fuego al explotar
        _allUpgrades.Add(new Upgrade
        {
            Id = "nova_chain",
            ElementType = Element.Fire,
            Type = UpgradeType.Ability,
            IsInfinite = false,
            Apply = (player, times) =>
            {
                _stats.FireNovaChain = true;
            }
        });

        //-0.5 segundos al cooldown de la Nova
        _allUpgrades.Add(new Upgrade
        {
            Id = "nova_cooldown",
            ElementType = Element.Fire,
            GetDescriptionArguments = (times) => new object[] { _stats.FireNovaCooldown },
            Type = UpgradeType.Ability,
            IsInfinite = true,
            Apply = (player, times) =>
            {
                _stats.FireNovaCooldown = Mathf.Max(1f, _stats.FireNovaCooldown - 0.5f);
            },
            Condition = (player) => _stats.FireNovaCooldown > 1
        });
    }
}
