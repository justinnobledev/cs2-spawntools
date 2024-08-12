using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;


namespace SpawnTools;

[MinimumApiVersion(78)]
public partial class SpawnTools : BasePlugin
{
    public override string ModuleName { get; } = "Spawn Tools";
    public override string ModuleVersion { get; } = "1.3";
    public override string ModuleAuthor { get; } = "Retro - https://insanitygaming.net";
    public override string ModuleDescription { get; } = "Allows you to dynamically create spawn points per map";

    private string _configPath = "";

    private bool _wasHotReload = false;

    private Config? _config;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            Server.NextFrame(()=>{
                if (_config?.DeletedSpawns.Count == 0) return;
                if(entity.Entity is null) return;
                if (entity is not SpawnPoint) return;
                var e2 = entity.Entity.As<CBaseEntity>();
                
                if(!_config!.DeletedSpawns.Contains(e2.UniqueHammerID)) return;
                
                Server.NextFrame(e2.Remove);
            });       
        });
        _wasHotReload = hotReload;
        if(hotReload)
            OnMapStart(Server.MapName);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Console.WriteLine($"Round started {_config?.SpawnPoints.Count ?? 0}");
        var noVel = new Vector(0f, 0f, 0f);
        var spawn = 0;
        if (_config?.SpawnPoints == null) return HookResult.Continue;
        Task.Run(async () => {
            foreach (var spawnPoint in _config?.SpawnPoints!)
            {
                SpawnPoint? entity;
                if(spawnPoint.Team == CsTeam.Terrorist)
                    entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");
                else
                    entity = Utilities.CreateEntityByName<CInfoPlayerCounterterrorist>("info_player_counterterrorist");
                if (entity == null) continue;
                var angle = StringToVector(spawnPoint.Angle);
                entity.Teleport(
                    NormalVectorToValve(StringToVector(spawnPoint.Origin)),
                    new QAngle(angle.X, angle.Y, angle.Z),
                    noVel);
                entity.UniqueHammerID = "42069";
                entity.DispatchSpawn();
                spawn++;
                await Task.Delay(5);
            }
            Logger.LogInformation(
                $"Created a total of {spawn} out of {_config.SpawnPoints.Count}");
        });
        
        return HookResult.Continue;
    }
}