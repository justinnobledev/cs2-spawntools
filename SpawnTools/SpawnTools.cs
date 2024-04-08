using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SpawnTools;

[MinimumApiVersion(78)]
public class SpawnTools : BasePlugin
{
    public override string ModuleName { get; } = "Spawn Tools";
    public override string ModuleVersion { get; } = "1.2";
    public override string ModuleAuthor { get; } = "Retro - https://insanitygaming.net";
    public override string ModuleDescription { get; } = "Allows you to dynamically create spawn points per map";

    private string _configPath = "";

    private bool _wasHotReload = false;

    private class CustomSpawnPoint
    {
        [JsonPropertyName("team")]
        public CsTeam Team { get; set; }
        [JsonPropertyName("origin")]
        public string Origin { get; set; }
        [JsonPropertyName("angel")]
        public string Angel { get; set; }
    }

    private class Options
    {
        public bool DeleteDefaultSpawns { get; set; } = false;
    }

    private class Config
    {
        public List<CustomSpawnPoint> SpawnPoints { get; set; }
        public Options Options { get; set; }
    }
    private Config? _config;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            if (_config is {Options.DeleteDefaultSpawns: false}) return;
            if (entity is not SpawnPoint) return;
            
            Server.NextFrame(entity.Remove);
        });
        _wasHotReload = hotReload;
        if(hotReload)
            OnMapStart(Server.MapName);
    }

    private static string VectorToString(Vector3 vec)
    {
        return $"{vec.X}|{vec.Y}|{vec.Z}";
    }

    private static Vector3 StringToVector(string str)
    {
        var explode = str.Split("|");
        return new Vector3(x: float.Parse(explode[0]), y: float.Parse(explode[1]), z: float.Parse(explode[2]));
    }

    private static Vector NormalVectorToValve(Vector3 v)
    {
        return new Vector(v.X, v.Y, v.Z);
    }

    private async void OnMapStart(string mapName)
    {
        _config = null;
        _configPath =
            Path.Combine(ModuleDirectory, $"../../configs/plugins/spawntools/{Server.MapName.ToLower()}.json");
        
        if (!File.Exists(_configPath))
            return;
        
        try
        {
            var jsonString = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<Config>(jsonString);
            _config = config;
        }
        catch(Exception e)
        {
            try
            {
                var jsonString = await File.ReadAllTextAsync(_configPath);
                var points = JsonSerializer.Deserialize<List<CustomSpawnPoint>>(jsonString);
                _config = new Config
                {
                    Options = new Options(),
                    SpawnPoints = points ?? new List<CustomSpawnPoint>()
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine(e);
        }

        if (_wasHotReload)
        {
            OnRoundStart(null, null);
            _wasHotReload = false;
        }
    }

    [ConsoleCommand("css_addspawn", "Adds a new spawn point")]
    [RequiresPermissions("@spawntools/add")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY, minArgs:1, usage:"[ct/t/both]")]
    public void CommandAddSpawnPoint(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PlayerPawn.IsValid) return;
        var arg = command.GetArg(1);
        if (!arg.Equals("ct") && !arg.Equals("t") && !arg.Equals("both"))
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} {ChatColors.LightRed}{arg}{ChatColors.Default} is not a valid team.");
            return;
        }
        var origin = player.PlayerPawn.Value.AbsOrigin;
        if (origin == null)
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} You do not have an origin");
            return;
        }

        var angel = player.PlayerPawn.Value.AbsRotation;
        if (angel == null)
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} You do not have a rotation");
            return;
        }
        if(arg.Equals("ct") || arg.Equals("both"))
        {
            var point = new CustomSpawnPoint
            {
                Team = CsTeam.CounterTerrorist,
                Origin = VectorToString(new Vector3(origin.X, origin.Y, origin.Z)),
                Angel = VectorToString(new Vector3(angel.X, angel.Y, angel.Z))
            };
            _config?.SpawnPoints.Add(point);
        }

        if (arg.Equals("t") || arg.Equals("both"))
        {
            var point = new CustomSpawnPoint
            {
                Team = CsTeam.Terrorist,
                Origin = VectorToString(new Vector3(origin.X, origin.Y, origin.Z)),
                Angel = VectorToString(new Vector3(angel.X, angel.Y, angel.Z))
            };
            _config?.SpawnPoints.Add(point);
        }
        var jsonString = JsonSerializer.Serialize(_config);
        File.WriteAllText(_configPath, jsonString);
        player.PrintToChat($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} Added {(arg.Equals("ct") ? ChatColors.Blue : ChatColors.LightRed)}{arg}{ChatColors.Default} spawn point");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Console.WriteLine($"Round started {_config?.SpawnPoints.Count ?? 0}");
        var noVel = new Vector(0f, 0f, 0f);
        var spawn = 0;
        if (_config?.SpawnPoints == null) return HookResult.Continue;
        foreach (var spawnPoint in _config?.SpawnPoints!)
        {
            SpawnPoint? entity;
            if(spawnPoint.Team == CsTeam.Terrorist)
                entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");
            else
                entity = Utilities.CreateEntityByName<CInfoPlayerCounterterrorist>("info_player_counterterrorist");
            if (entity == null) continue;
            var angel = StringToVector(spawnPoint.Angel);
            entity.Teleport(
                NormalVectorToValve(StringToVector(spawnPoint.Origin)),
                new QAngle(angel.X, angel.Y, angel.Z),
                noVel);
            entity.DispatchSpawn();
            spawn++;
        }

        Logger.LogInformation(
            $"Created a total of {spawn} out of {_config.SpawnPoints.Count}");
        return HookResult.Continue;
    }
}