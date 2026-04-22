using System;
using System.Text.Json.Serialization;

public class PlayerData
{
    public string PlayerId { get; set; }
    public string Nickname { get; set; }
    public float LastX { get; set; }
    public float LastY { get; set; }
    public float LastZ { get; set; }
    public DateTime LastLogin { get; set; }

    [JsonIgnore]
    public bool Dirty { get; set; } = false;

    public PlayerData() { }

    public PlayerData(string id, string name, float x, float y, float z)
    {
        PlayerId = id;
        Nickname = name;
        LastX = x;
        LastY = y;
        LastZ = z;
        LastLogin = DateTime.Now;
        Dirty = true;
    }
}