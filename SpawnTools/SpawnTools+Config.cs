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
public partial class SpawnTools {
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
                    SpawnPoints = points ?? [],
                    DeletedSpawns = []
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

    private class CustomSpawnPoint
    {
        [JsonPropertyName("team")]
        public CsTeam Team { get; set; }
        [JsonPropertyName("origin")]
        public string Origin { get; set; }
        
        [JsonConverter(typeof(AngleConverter))]
        [JsonPropertyName("angle")]
        public string Angle { get; set; }   
    }

    public class AngleConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the property name
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                
                // Advance to the value
                reader.Read();
                
                if (propertyName == "angle" || propertyName == "angel")
                {
                    return reader.GetString();
                }
            }

            throw new JsonException("Invalid JSON format for CustomSpawnPoint.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteString("angle", value);
        }
    }

    private class Config
    {
        public List<CustomSpawnPoint> SpawnPoints { get; set; } = [];
        public List<string> DeletedSpawns { get; set; } = [];
    }
}