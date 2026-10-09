using System;
using Godot;

public partial class Player : CharacterBody2D
{
    //[Export] permite modificar el valor de una variable desde el inspector directamente sin tener que tocar el código

    //Sprites
    private Sprite2D _sprite;

    //Dash
    [Export] public float DashSpeed = 600f;
    [Export] public float DashDuration = 0.15f; //Segundos que dura el dash
    [Export] public float BaseDashCooldown = 2f; //Segundos entre dashes

    //I-Frames
    [Export] public float IFrameDuration = 0.5f;
    private float _iFrameTimer = 0f;

    //Regeneración
    [Export] public float RegenCooldown = 3f;
    private float _regenTimer = 0f;

    private bool _isDashing = false;
    private float _dashTimer = 0f; //Tiempo restante del dash activo
    private float _dashCooldownTimer = 0f; //tiempo restante del cooldown
    private Vector2 _dashDirection; //Dirección del dash
    private bool _needsCollisionRestoration = false; //Espera a que el jugador deje de solaparse con enemigos para restaurar la colisión
    public Vector2 LastMovementDirection { get; private set; } = Vector2.Down;

    //Componentes
    public PlayerStats Stats { get; private set; }
    public ArtCaster ArtCaster { get; private set; }
    public AbilityManager AbilityManager { get; private set; }
    public HealthSystem Health { get; private set; }
    public ExperienceSystem Experience { get; private set; }
    public UpgradeSystem Upgrades { get; private set; }

    public override void _Ready()
    {
        //Se inicializan los stats
        Stats = new PlayerStats();
        Stats.DashCooldown = BaseDashCooldown;

        //Se obtienen los componentes nodo desde la escena
        ArtCaster = GetNode<ArtCaster>("ArtCaster");
        AbilityManager = GetNode<AbilityManager>("AbilityManager");

        //Se inicializan los componentes con referencias al jugador y sus stats
        ArtCaster.Initialize(this, Stats);
        AbilityManager.Initialize(this, Stats);

        _sprite = GetNode<Sprite2D>("Sprite2D");

        //Sistemas "Puros"
        Health = new HealthSystem(Stats.MaxHealth);
        //Se suscribe al evento de muerte
        Health.OnDeath += OnPlayerDeath;
        //Se suscribe al recibir daño para los iframes
        Health.OnDamageTaken += OnPlayerDamageTaken;

        Experience = new ExperienceSystem();
        Experience.OnLevelUp += OnLevelUp;

        Upgrades = new UpgradeSystem(this, Stats);

        if (GameManager.Instance != null && GameManager.Instance.ActiveSave != null)
        {
            var save = GameManager.Instance.ActiveSave;
            
            Experience.SetLevelAndXP(save.PlayerLevel, save.PlayerXP);

            foreach (var kvp in save.AcquiredUpgrades)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    Upgrades.ApplyUpgradeById(kvp.Key);
                }
            }

            if (save.PlayerCurrentHealth > 0)
            {
                Health.LoadHealth(save.PlayerCurrentHealth);
            }

            if (save.PlayerDashCooldown > 0)
            {
                Stats.DashCooldown = save.PlayerDashCooldown;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleDash((float)delta);
        HandleMovement();

        ArtCaster.Process((float)delta);
        AbilityManager.Process((float)delta);

        if (_needsCollisionRestoration)
            CheckCollisionRestoration();

        if (_iFrameTimer > 0f)
        {
            _iFrameTimer -= (float)delta;
            if (_iFrameTimer <= 0f)
            {
                Health.IsInvulnerable = false;
                _sprite.Modulate = new Color(1, 1, 1, 1);
            }
            else
            {
                //Efecto de parpadeo visual durante los iframes
                float alpha = Mathf.Sin(_iFrameTimer * 30f) > 0 ? 1f : 0.4f;
                _sprite.Modulate = new Color(1, 1, 1, alpha);
            }
        }

        //Regeneración de vida
        if (Stats.HealthRegen > 0f && !Health.IsDead)
        {
            _regenTimer += (float)delta;
            if (_regenTimer >= RegenCooldown)
            {
                _regenTimer -= RegenCooldown;
                Health.Heal(Stats.HealthRegen);
            }
        }
    }

    private void HandleMovement()
    {
        //Si el jugador está dasheando, el movimiento normal no se aplica
        if (_isDashing) return;

        //Lee el input del teclado y lo convierte en una dirección. GetAxis devuelve -1, 0 o 1 según que teclas están siendo presionadas.
        Vector2 direction = new(
            Input.GetAxis("move_left", "move_right"),
            Input.GetAxis("move_up", "move_down")
        );

        //Si hay alguna dirección, se normaliza el vector, evitando que el jugador se mueva más rápido en diagonal.
        if (direction != Vector2.Zero)
        {
            direction = direction.Normalized();
            LastMovementDirection = direction; //guarda la última dirección

            //Solo se actualiza FlipH si hay movimiento horizontal
            if (direction.X != 0)
                _sprite.FlipH = direction.X < 0;
        }

        //Aplica la velocidad al CharacterBody2D
        Velocity = direction * Stats.Speed;

        //MoveAndSlide mueve al personaje y maneja las colisiones automáticamente (se "desliza" contra las paredes)
        MoveAndSlide();
    }

    private void HandleDash(float delta)
    {
        //Se reduce el cooldown con el tiempo
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= delta;

        //Si el jugador está en medio de un dash
        if (_isDashing)
        {
            _dashTimer -= delta;

            //El mago se mueve en la dirección del dash
            Velocity = _dashDirection * DashSpeed;
            MoveAndSlide();

            //Si el dash terminó, el jugador vuelve al estado normal
            if (_dashTimer <= 0f)
            {
                _isDashing = false;

                //Al terminar el dash, marca que necesita restaurar la colisión
                if (Stats.DashIsInvincible)
                {
                    _needsCollisionRestoration = true;
                }
            }

            return;
        }

        //Detecta si el jugador quiere dashear
        if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0f)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        //La dirección del dash es la dirección del movimiento actual
        Vector2 direction = new(
            Input.GetAxis("move_left", "move_right"),
            Input.GetAxis("move_up", "move_down")
        );

        //Si el jugador no se está moviendo, se hace un dash hacia el último punto hacia donde miraba por defecto
        if (direction == Vector2.Zero)
            direction = LastMovementDirection;

        _dashDirection = direction.Normalized();
        _isDashing = true;
        AudioManager.Instance.PlaySfx("sfx.dash");
        _dashTimer = DashDuration;
        _dashCooldownTimer = Stats.DashCooldown;

        //Si tiene el upgrade, se desactiva la capa de colisión del jugador para que los enemigos no puedan hacerle daño durante el dash.
        if (Stats.DashIsInvincible)
        {
            SetCollisionLayerValue(1, false);
            SetCollisionMaskValue(2, false); //Permite atravesar enemigos
        }
    }

    //Método para cooldown
    public float GetDashCooldownRemaining() => Mathf.Max(0f, _dashCooldownTimer);

    private void CheckCollisionRestoration()
    {
        //Verifica si el jugador sigue superpuesto con algún enemigo (radio aprox 85px)
        bool isOverlapping = false;
        var enemies = GetTree().GetNodesInGroup("enemies");
        foreach (var node in enemies)
        {
            if (node is Node2D enemy)
            {
                if (GlobalPosition.DistanceTo(enemy.GlobalPosition) < 85f)
                {
                    isOverlapping = true;
                    break;
                }
            }
        }

        //Si ya no está tocando a ningún enemigo, restaura la colisión
        if (!isOverlapping)
        {
            SetCollisionLayerValue(1, true);
            SetCollisionMaskValue(2, true);
            _needsCollisionRestoration = false;
        }
    }

    private void OnLevelUp(int newLevel)
    {
        //Se busca la pantalla de upgrades y se muestra
        UpgradeScreen upgradeScreen = GetTree().Root.FindChild("UpgradeScreen", true, false) as UpgradeScreen;
        upgradeScreen?.ShowCard(Upgrades.GetUpgradeChoices());
    }

    private void OnPlayerDamageTaken()
    {
        _iFrameTimer = IFrameDuration;
        Health.IsInvulnerable = true;
        AudioManager.Instance.PlaySfx("sfx.playerhit");
    }

    private void OnPlayerDeath()
    {
        //Se busca la pantalla de Game Over y se la muestra
        Defeat defeat = GetTree().Root.FindChild("Defeat", true, false) as Defeat;
        defeat?.ShowDefeat();
    }
}
