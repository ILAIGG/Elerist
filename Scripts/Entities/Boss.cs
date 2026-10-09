using System.Security.Cryptography.X509Certificates;
using Godot;

public partial class Boss : CharacterBody2D, IEnemy
{
    private static readonly Color FrozenTint = new(0.72f, 0.9f, 1f);

    [Export] public Element ElementType { get; set; } = Element.Neutral;
    [Export] public float Speed = 160f;
    [Export] public float MaxHealth = 7000f;
    [Export] public float ContactDamage = 80f;
    private float _damageCooldown = 0f;

    private const float DamageInterval = 0.5f;

    private Sprite2D _sprite;
    private Sprite2D _reactionSprite;

    public HealthSystem Health { get; private set; }
    public ElementalAccumulator ElementalEffects { get; } = new();

    private Player _player;
    private int _currentPhase = 1;

    private readonly StatusEffectSystem _statusEffects = new();
    //Factor mínimo de slow que se puede aplicar al jefe, 0.4 significa que nunca irá a menos del 40% de su velocidad
    private const float MinSlowFactor = 0.4f;

    //Variables para el knockback
    private Vector2 _knockbackVelocity = Vector2.Zero;
    private const float KnockbackDecay = 8f; //Que tan rápido se frena

    public override void _Ready()
    {
        ElementalEffects.ElementApplied += OnElementApplied;
        Health = new(MaxHealth);
        Health.OnDamageTaken += OnEnemyDamageTaken;
        Health.OnDeath += OnBossDeath;
        Health.OnHealthChanged += OnHealthChanged;

        _sprite = GetNode<Sprite2D>("Sprite2D");
        _reactionSprite = GetNodeOrNull<Sprite2D>("ReactionSprite");
        if (_reactionSprite == null)
        {
            _reactionSprite = new Sprite2D
            {
                Name = "ReactionSprite",
                ZIndex = _sprite.ZIndex + 1,
                Visible = false,
                Position = Vector2.Zero,
                Scale = _sprite.Scale,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest
            };
            AddChild(_reactionSprite);
        }

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    public override void _PhysicsProcess(double delta)
    {
        _statusEffects.Update(delta);
        ElementalEffects.Update(delta);
        UpdateStatusEffectVisuals();

        if (_player == null) return;

        if (_knockbackVelocity != Vector2.Zero)
        {
            Velocity = _knockbackVelocity;
            _knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackDecay * Speed * (float)delta);
            MoveAndSlide();
            return;
        }

        if (_damageCooldown > 0f)
            _damageCooldown -= (float)delta;

        Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();

        // Voltear sprite según dirección horizontal
        if (direction.X != 0)
            _sprite.FlipH = direction.X < 0;

        Velocity = direction * Speed * _statusEffects.MovementFactor;
        MoveAndSlide();

        bool touchingPlayer = false;
        //Daño por contacto
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            if (collision.GetCollider() is Player player)
            {
                touchingPlayer = true;
                if (_damageCooldown <= 0f)
                {
                    player.Health.TakeDamage(ContactDamage, player.GlobalPosition, GetTree(), GetInstanceId());
                    _damageCooldown = DamageInterval;
                }
            }
        }

        //Si se pierde el contacto, se resetea el cooldown para el próximo impacto
        if (!touchingPlayer)
        {
            _damageCooldown = 0f;
        }
    }

    public void ApplyStatusEffect(StatusEffect effect)
    {
        float actualFactor = effect.MovementFactor;

        //En fase 2, solo recibe el 20% del efecto del slow
        if (_currentPhase == 2)
        {
            float slowEffect = 1f - effect.MovementFactor;
            actualFactor = 1f - (slowEffect * 0.2f);
        }

        //El jefe resiste el slow, nunca puede quedar completamente inmóvil
        float resistedFactor = Mathf.Max(actualFactor, MinSlowFactor);

        _statusEffects.Apply(new FrozenEffect(resistedFactor, effect.RemainingDuration));
        UpdateStatusEffectVisuals();
    }

    public void TakeElementalDamage(float amount, Element attackElement, Vector2 position, SceneTree tree, ulong entityId = 0)
    {
        float multiplier = ElementalChart.GetDamageMultiplier(attackElement, ElementType);
        float finalDamage = amount * multiplier;
        Health.TakeDamage(finalDamage, position, tree, entityId);
        ElementalEffects.Apply(attackElement, finalDamage);
    }

    private void OnElementApplied(Element appliedElement, float amount)
    {
        if (!ElementalReactionResolver.TryResolve(ElementalEffects, appliedElement, out ElementalReaction reaction, out float reactionDamage))
            return;

        Element elementToConsume = appliedElement switch
        {
            Element.Fire when ElementalEffects.Has(Element.Water) => Element.Water,
            Element.Water when ElementalEffects.Has(Element.Fire) => Element.Fire,
            Element.Water when ElementalEffects.Has(Element.Ice) => Element.Ice,
            _ => Element.Water
        };

        ElementalEffects.Consume(appliedElement);
        ElementalEffects.Consume(elementToConsume);

        if (reaction == ElementalReaction.Vaporization)
        {
            Health.TakeDamage(reactionDamage, GlobalPosition, GetTree(), GetInstanceId(), false);
            ApplyStatusEffect(new VaporizedEffect(0.65f, 1.2f));
        }
        else if (reaction == ElementalReaction.Freezing)
        {
            ApplyStatusEffect(new FrozenEffect(0f, 1.5f));
        }

        string reactionName = LocalizationManager.Translate(reaction switch
        {
            ElementalReaction.Vaporization => "reaction.vaporization",
            ElementalReaction.Freezing => "reaction.freezing",
            _ => string.Empty
        });
        DamageNumberSystem.SpawnReaction(GetTree(), GlobalPosition, reactionName, reactionDamage);
    }

    private void UpdateStatusEffectVisuals()
    {
        if (_statusEffects.Has<FrozenEffect>())
        {
            _sprite.Modulate = FrozenTint;
            _reactionSprite.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Sprites/Reactions/frozen_reaction.png");
            _reactionSprite.Visible = true;
            return;
        }

        if (_statusEffects.Has<VaporizedEffect>())
        {
            _sprite.Modulate = Colors.White;
            _reactionSprite.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Sprites/Reactions/vaporization_reaction.png");
            _reactionSprite.Visible = true;
            return;
        }

        _sprite.Modulate = Colors.White;
        _reactionSprite.Visible = false;
    }

    public void ApplyKnockback(Vector2 force)
    {
        //El jefe resiste el knockback, recibe solo el 20% de la fuerza
        _knockbackVelocity = force * 0.2f;
    }

    private void OnHealthChanged(float current, float max)
    {
        float percent = current / max;

        //Fase 2: al llegar al 50% de vida
        if (percent <= 0.5f && _currentPhase == 1)
        {
            _currentPhase = 2;
            EnterPhase2();
        }
    }

    private void EnterPhase2()
    {
        //Se vuelve más rápido y cambia de color
        Speed *= 1.5f;
    }

    private void OnBossDeath()
    {
        GetTree().Root.FindChild("Level", true, false)
            ?.Call("OnBossDefeated");

        QueueFree();
    }

    private void OnEnemyDamageTaken()
    {
        AudioManager.Instance.PlaySfx("sfx.enemyhit");
    }
}
