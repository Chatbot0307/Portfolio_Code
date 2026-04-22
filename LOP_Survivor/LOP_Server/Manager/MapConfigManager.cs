using System;
using System.IO;
using System.Text.Json;

public class MapConfigManager
{
    private static MapConfigManager _instance;
    public static MapConfigManager Instance => _instance ??= new MapConfigManager();

    public MapSettings Settings { get; private set; }
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapConfig.json");

    public void LoadConfig()
    {
        if (File.Exists(_configPath))
        {
            string jsonString = File.ReadAllText(_configPath);
            Settings = JsonSerializer.Deserialize<MapSettings>(jsonString);
            Console.WriteLine("[Config] Map configuration loaded from JSON.");
        }
        else
        {
            // 파일이 없으면 기본값으로 생성
            Settings = new MapSettings();
            SaveConfig();
            Console.WriteLine("[Config] No config found. Default created.");
        }
    }

    public void SaveConfig()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(Settings, options);
        File.WriteAllText(_configPath, jsonString);
    }
}