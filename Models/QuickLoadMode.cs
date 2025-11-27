using System.ComponentModel;

namespace ContinuousLoadAmmo.Models;

public enum QuickLoadMode
{
    [Description("Highest Penetration Available")]
    HighestPenetrationAvailable,
    [Description("Last Bullet in Magazine")]
    LastBulletMagazine,
    [Description("Last Used Magazine Preset")]
    LastMagazinePreset
}
