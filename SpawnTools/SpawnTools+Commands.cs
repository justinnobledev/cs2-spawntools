
using System.Linq;
using System.Numerics;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SpawnTools;

public partial class SpawnTools
{
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

        var angle = player.PlayerPawn.Value.AbsRotation;
        if (angle == null)
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
                Angle = VectorToString(new Vector3(angle.X, angle.Y, angle.Z))
            };
            _config?.SpawnPoints.Add(point);
        }

        if (arg.Equals("t") || arg.Equals("both"))
        {
            var point = new CustomSpawnPoint
            {
                Team = CsTeam.Terrorist,
                Origin = VectorToString(new Vector3(origin.X, origin.Y, origin.Z)),
                Angle = VectorToString(new Vector3(angle.X, angle.Y, angle.Z))
            };
            _config?.SpawnPoints.Add(point);
        }
        var jsonString = JsonSerializer.Serialize(_config);
        File.WriteAllText(_configPath, jsonString);
        player.PrintToChat($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} Added {(arg.Equals("ct") ? ChatColors.Blue : ChatColors.LightRed)}{arg}{ChatColors.Default} spawn point");
    }

    [ConsoleCommand("css_delspawn", "Deletes the closest spawn point")]
    [RequiresPermissions("@spawntools/add")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY)]
    public void CommandDelSpawnPoint(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PlayerPawn.IsValid) return;
        if(player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
        var spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").ToList();
        spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist"));

        SpawnPoint? closest = null;
        float distance = -1f;
        foreach(var spawn in spawnPoints) 
        {
            var howFar = (spawn.AbsOrigin! - player!.PlayerPawn.Value?.AbsOrigin!).Length2DSqr();
            if(howFar > 20f || (distance != -1f && distance < howFar)) return;
            closest = spawn;
            distance = howFar;
        }
        if(closest is null) return;

        if(closest.UniqueHammerID == "42069")
        {
            var elem = _config!.SpawnPoints.First((p) => p.Angle == VectorToString(closest.AbsRotation!));
            if(elem is null) return;
            _config!.SpawnPoints.Remove(elem);
            closest.Remove();
        }
        else {
            if(_config!.DeletedSpawns.Contains(closest.UniqueHammerID)) return;
            _config.DeletedSpawns.Add(closest.UniqueHammerID);
        }

        var jsonString = JsonSerializer.Serialize(_config);
        File.WriteAllText(_configPath, jsonString);
    }
}