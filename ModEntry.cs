using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

[ModInitializer("Initialize")]
public class ModEntry
{ 
    public static void Initialize()
    {
        var harmony = new Harmony("MoreSaves.patch");
        harmony.PatchAll();
    }
}