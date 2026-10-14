namespace MagicCarpetRemastered.Scripts.World;

public static class BaseProjectCatalog
{
    public static void Register(BaseConstruction construction)
    {
        UpgradeCatalog.Register(construction);
        AutomaticDroneFleet.Register(construction);
    }
}
