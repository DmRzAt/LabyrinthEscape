using System.Collections.Generic;

public static class WeaponStats
{
    public struct Stat
    {
        public int damage;
        public float speed;
    }

    static readonly Dictionary<string, Stat> Table = new Dictionary<string, Stat>
    {
        { "Dagger",     new Stat { damage = 12, speed = 1.40f } },
        { "Cutlass",    new Stat { damage = 16, speed = 1.00f } },
        { "Sword",      new Stat { damage = 20, speed = 1.00f } },
        { "Rapier",     new Stat { damage = 24, speed = 1.40f } },
        { "Shortsword", new Stat { damage = 30, speed = 0.75f } },
        { "Katana",     new Stat { damage = 34, speed = 1.35f } },
    };

    public static readonly Stat Default = new Stat { damage = 20, speed = 1f };

    public static Stat Get(string weaponName)
    {
        if (!string.IsNullOrEmpty(weaponName) && Table.TryGetValue(weaponName, out var s)) return s;
        return Default;
    }

    public static bool Has(string weaponName) =>
        !string.IsNullOrEmpty(weaponName) && Table.ContainsKey(weaponName);

    public static string SpeedLabel(float speed) =>
        speed >= 1.25f ? "Fast" : (speed <= 0.85f ? "Slow" : "Medium");
}
