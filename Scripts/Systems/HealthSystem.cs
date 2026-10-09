using System;
using Godot;

public class HealthSystem
{
    //Vida máxima y vida actual
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }

    //Propiedad calculada: devuelve la vida como un valor entre 0 y 1, lo cual es útil para barras de vida.
    public float HealthPercent => CurrentHealth / MaxHealth;

    //Devuelve true si la vida llegó a 0
    public bool IsDead => CurrentHealth <= 0;

#if DEBUG
    public bool IsImmortal { get; set; } = false;
#endif

    //Una "señal" que se dispara cuando el personaje muere. Cualquier otro sistema puede "suscribirse" a dicha señal
    //para ejecutar o hacer algo cuando esta se dispare.
    public event Action OnDeath;

    //Una señal que se dispara cuando recibe daño (útil para iframes)
    public event Action OnDamageTaken;

    //Una señal que se dispara cuando la vida cambia, la UI lo utilizará para la barra de vida.
    public event Action<float, float> OnHealthChanged;

    public bool IsInvulnerable { get; set; } = false;

    public HealthSystem(float maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector2 position, SceneTree tree, ulong entityId = 0, bool showDamageNumber = true)
    {
        if (IsDead) return;

#if DEBUG
        if (IsImmortal) return;
#endif

        if (IsInvulnerable) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        //Avisa que la vida cambió
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnDamageTaken?.Invoke();

        if (showDamageNumber)
            DamageNumberSystem.Spawn(tree, position, amount, false, entityId);

        if (IsDead)
            OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void LoadHealth(float health)
    {
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        CurrentHealth += amount; //Subir la vida máxima también cura esa cantidad
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void SetMaxHealth(float newMax)
    {
        MaxHealth = newMax;
        CurrentHealth = newMax; //los enemigos nuevos empiezan con vida completa
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
    
}