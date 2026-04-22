using System;
using System.IO;
using System.Text.Json;

public class PlayerDataManager
{
    private static readonly string SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlayerData");

    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public PlayerDataManager()
    {
        if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
    }

    public void Save(PlayerData data)
    {
        try
        {
            string filePath = Path.Combine(SavePath, $"{data.PlayerId}.json");
            string jsonString = JsonSerializer.Serialize(data, _options);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to save player data: {ex.Message}");
        }
    }

    public PlayerData Load(string playerId)
    {
        try
        {
            string filePath = Path.Combine(SavePath, $"{playerId}.json");
            if (!File.Exists(filePath)) return null;

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PlayerData>(jsonString, _options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to load player data: {ex.Message}");
            return null;
        }
    }
}