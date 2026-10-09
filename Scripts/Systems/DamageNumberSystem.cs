using System.Collections.Generic;
using System.Xml;
using Godot;

public static class DamageNumberSystem
{
    private static PackedScene _scene;

    //Tiempo mínimo entre números para la misma entidad
    private const float CooldownPerEntity = 0.3f;

    //Diccionario que trackea cuándo fue el último número por entidad
    private static Dictionary<ulong, float> _lastSpawnTime = new();

    public static void Initialize(PackedScene scene)
    {
        _scene = scene;
    }

    public static void Spawn(SceneTree tree, Vector2 position, float damage, bool isCrit = false, ulong entityId = 0)
    {
        if (_scene == null) return;

        //Si hay un entityId, verificamos el coodown
        if (entityId != 0)
        {
            float currentTime = Time.GetTicksMsec() / 1000f;

            if (_lastSpawnTime.TryGetValue(entityId, out float lastTime))
            {
                if (currentTime - lastTime < CooldownPerEntity)
                    return; //Todavía está en cooldown, por lo que no mostramos el número
            }

            _lastSpawnTime[entityId] = currentTime;
        }

        //Solo se muestra el número si el daño es mayor a 1. Evita mostrar decimales insignificantes
        if (damage < 1f) return;

        DamageNumber number = _scene.Instantiate<DamageNumber>();

        //Pequeño offset aleatorio para que no se superpongan
        float offsetX = (float)GD.RandRange(-15, 15);
        number.Position = position + new Vector2(offsetX, 0);

        number.Initialize(damage, isCrit);

        //Se lo agrega al contenedor de proyectiles para que esté en el mundo
        tree.Root.FindChild("Projectiles", true, false)?.AddChild(number);
    }

    public static void SpawnReaction(SceneTree tree, Vector2 position, string reactionName, float reactionDamage)
    {
        if (_scene == null) return;

        DamageNumber number = _scene.Instantiate<DamageNumber>();
        float offsetX = (float)GD.RandRange(-15, 15);
        number.Position = position + new Vector2(offsetX, 0);
        number.InitializeReaction(reactionName, reactionDamage);

        tree.Root.FindChild("Projectiles", true, false)?.AddChild(number);
    }
}