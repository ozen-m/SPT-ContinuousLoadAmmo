using EFT.InventoryLogic;

namespace ContinuousLoadAmmo.Models;

public class MagPresetCancelled : InventoryError
{
    private const string Error = "Magazine preset loading is cancelled";

    public override string ToString()
    {
        return Error;
    }

    public override string GetLocalizedDescription()
    {
        return Error;
    }
}
