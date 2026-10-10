namespace MagicCarpetRemastered.Scripts.World;

public static class PlanetSizes
{
    public static readonly int[] Radii = { 500, 800, 1000, 1200 };
    public static readonly string[] Names = { "Small", "Medium", "Large", "XL" };
    public static int SelectedIndex { get; set; } = 1;
    public static int SelectedRadius => Radii[SelectedIndex];
}
