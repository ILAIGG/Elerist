using Godot;

public partial class Boss : CharacterBody2D, IEnemy
{
    private static readonly Color FrozenTint = new(0.72f, 0.9f, 1f);

    [Export] public Element ElementType { get; set; } = Element.Neutral;
    [Export] public float Speed = 160f;
    [Export] public float MaxHealth = 5500f;
    [Export] public float ContactDamage = 80f;
    private float _damageCooldown = 0f;

    private const float DamageInterval = 0.5f;
    private const float DashCooldown = 10f;
    private const float DashChargeDuration = 2.5f;
    private const float DashDirectionLockDuration = 0.5f;
    private const float DashLength = 600f;
    private const float DashWidth = 58f;
    private const float DashSpeed = 840f;
    private const float DashDuration = DashLength / DashSpeed;
    private const int DashDirectionChangeCount = 2;

    private Sprite2D _sprite;
    private Sprite2D _reactionSprite;
    private Node2D _dashIndicator;
    private Polygon2D _dashFill;
    private Vector2 _spriteOriginalPosition;
    private Vector2 _dashDirection;
    private Vector2 _dashOrigin;
    private float _dashCooldown = DashCooldown;
    private float _dashChargeTimer;
    private float _dashTimer;
    private int _dashDirectionChangeCount;
    private bool _isChargingDash;
    private bool _isDashing;

    public HealthSystem Health { get; private set; }
    public ElementalAccumulator ElementalEffects { get; } = new();

    private Player _player;

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

        _sprite = GetNode<Sprite2D>("Sprite2D");
        _spriteOriginalPosition = _sprite.Position;
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

        AddToGroup("boss");
        CreateDashIndicator();
        _player = GetTree().GetFirstNodeInGroup("player") as Player;
    }

    public override void _PhysicsProcess(double delta)
    {
        _statusEffects.Update(delta);
        ElementalEffects.Update(delta);
        UpdateStatusEffectVisuals();

        if (_player == null) return;

        float frameDelta = (float)delta;
        _dashCooldown = Mathf.Max(0f, _dashCooldown - frameDelta);

        if (_isChargingDash)
        {
            UpdateDashCharge(frameDelta);
            return;
        }

        if (_isDashing)
        {
            UpdateDash(frameDelta);
            return;
        }

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

        if (_dashCooldown <= 0f)
        {
            BeginDashCharge(direction);
            return;
        }

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
        //El jefe resiste el slow, nunca puede quedar completamente inmóvil
        float resistedFactor = Mathf.Max(effect.MovementFactor, MinSlowFactor);

        _statusEffects.Apply(new FrozenEffect(resistedFactor, effect.RemainingDuration));
        UpdateStatusEffectVisuals();
    }

    public void TakeElementalDamage(float amount, Element attackElement, Vector2 position, SceneTree tree, ulong entityId = 0)
    {
        float multiplier = ElementalChart.GetDamageMultiplier(attackElement, ElementType);
        float finalDamage = amount * multiplier;
        Health.TakeDamage(finalDamage, position, tree, entityId, true, attackElement);
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
        DamageNumberSystem.SpawnReaction(
            GetTree(), GlobalPosition, reactionName, reactionDamage,
            ElementColorPalette.GetReactionColor(reaction));
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

    private void CreateDashIndicator()
    {
        _dashIndicator = new Node2D
        {
            Name = "DashIndicator",
            Visible = false,
            ZIndex = 1
        };
        AddChild(_dashIndicator);

        var baseArea = new Polygon2D
        {
            Polygon = new Vector2[]
            {
                Vector2.Zero,
                new(DashLength, 0),
                new(DashLength, DashWidth),
                new(0, DashWidth)
            },
            Position = new Vector2(0, -DashWidth / 2f),
            Color = new Color(0.95f, 0.08f, 0.08f, 0.25f)
        };
        _dashIndicator.AddChild(baseArea);

        _dashFill = new Polygon2D
        {
            Position = new Vector2(0, -DashWidth / 2f),
            Color = new Color(1f, 0.04f, 0.04f, 0.65f)
        };
        _dashIndicator.AddChild(_dashFill);
    }

    private void BeginDashCharge(Vector2 direction)
    {
        _dashDirection = direction;
        _dashOrigin = GlobalPosition;
        _dashChargeTimer = 0f;
        _dashDirectionChangeCount = 0;
        _dashCooldown = DashCooldown;
        _isChargingDash = true;
        _dashIndicator.Rotation = direction.Angle();
        _dashIndicator.Visible = true;
        UpdateDashFill(0f);
        Velocity = Vector2.Zero;
    }

    private void UpdateDashCharge(float delta)
    {
        _dashChargeTimer += delta;
        Velocity = Vector2.Zero;
        _sprite.Position = _spriteOriginalPosition + new Vector2(
            Mathf.Sin(_dashChargeTimer * 52f) * 2f,
            Mathf.Cos(_dashChargeTimer * 47f) * 1.5f);

        float nextDirectionChange = (DashChargeDuration - DashDirectionLockDuration)
            * (_dashDirectionChangeCount + 1f) / DashDirectionChangeCount;
        while (_dashDirectionChangeCount < DashDirectionChangeCount
            && _dashChargeTimer >= nextDirectionChange)
        {
            UpdateDashDirection();
            _dashDirectionChangeCount++;
            nextDirectionChange = (DashChargeDuration - DashDirectionLockDuration)
                * (_dashDirectionChangeCount + 1f) / DashDirectionChangeCount;
        }

        UpdateDashFill(_dashChargeTimer / DashChargeDuration);

        if (_dashChargeTimer < DashChargeDuration)
            return;

        _sprite.Position = _spriteOriginalPosition;
        _isChargingDash = false;
        _isDashing = true;
        _dashTimer = 0f;
        _dashIndicator.Visible = false;
    }

    private void UpdateDashDirection()
    {
        _dashDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
        _dashIndicator.Rotation = _dashDirection.Angle();
        if (_dashDirection.X != 0)
            _sprite.FlipH = _dashDirection.X < 0;
    }

    private void UpdateDash(float delta)
    {
        Velocity = _dashDirection * DashSpeed;
        MoveAndSlide();
        _dashTimer += delta;

        if (_dashTimer < DashDuration)
            return;

        _isDashing = false;
        Velocity = Vector2.Zero;
        DamageDashArea();
    }

    private void UpdateDashFill(float progress)
    {
        float filledLength = DashLength * Mathf.Clamp(progress, 0f, 1f);
        _dashFill.Polygon = new Vector2[]
        {
            Vector2.Zero,
            new(filledLength, 0),
            new(filledLength, DashWidth),
            new(0, DashWidth)
        };
    }

    private void DamageDashArea()
    {
        var shape = new RectangleShape2D
        {
            Size = new Vector2(DashLength, DashWidth)
        };
        Vector2 areaCenter = _dashOrigin + _dashDirection * (DashLength / 2f);
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(_dashDirection.Angle(), areaCenter),
            CollisionMask = 1,
            CollideWithAreas = false,
            CollideWithBodies = true
        };

        foreach (Godot.Collections.Dictionary result in GetWorld2D().DirectSpaceState.IntersectShape(query))
        {
            if (result["collider"].AsGodotObject() is Player player)
                player.Health.TakeDamage(ContactDamage, player.GlobalPosition, GetTree(), GetInstanceId());
        }
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
