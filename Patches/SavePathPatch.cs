using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace MoreSaves.Patches;

public class Store
{
    public static string currentSave = "Mar 15 16-19 IRONCLAD";
    public static NMainMenu? mainMenu = null;
}

[HarmonyPatch]
public class MenuPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu.Create))]
    static void GrabMenu(ref NMainMenu __result)
    {
        Store.mainMenu = __result;
        Log.Info("Grabbed mainmenu!");
    }
}

[HarmonyPatch(typeof(RunSaveManager))]
public class RunSaveManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("CurrentRunSavePath", MethodType.Getter)]
    static bool SingleplayerPath(ref string __result, IProfileIdProvider ____profileIdProvider)
    {
        __result = Path.Combine(RunSaveManager.GetRunSavePath(____profileIdProvider.CurrentProfileId, "MoreSaves"), Store.currentSave+".spsave");
        Log.Info(__result);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("CurrentMultiplayerRunSavePath", MethodType.Getter)]
    static bool MultiplayerPath(ref string __result, IProfileIdProvider ____profileIdProvider)
    {
        __result = Path.Combine(RunSaveManager.GetRunSavePath(____profileIdProvider.CurrentProfileId, "MoreSaves"), Store.currentSave+".mpsave");
        Log.Info(__result);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveManager.HasRunSave), MethodType.Getter)]
    static bool HasSingleplayerRun(ref bool __result, ISaveStore ____saveStore, IProfileIdProvider ____profileIdProvider)
    {
        string dir = Path.Combine(RunSaveManager.GetRunSavePath(____profileIdProvider.CurrentProfileId, "MoreSaves"));
        __result = ____saveStore.GetFilesInDirectory(dir).Where((name) => name.Length > 6 && name.Substring(name.Length - 6) == "spsave").Count() > 0;
        Log.Info("Has Singleplayer run: " + __result.ToString());
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveManager.HasMultiplayerRunSave), MethodType.Getter)]
    static bool HasMultiplayerRun(ref bool __result, ISaveStore ____saveStore, IProfileIdProvider ____profileIdProvider)
    {
        string dir = Path.Combine(RunSaveManager.GetRunSavePath(____profileIdProvider.CurrentProfileId, "MoreSaves"));
        __result = ____saveStore.GetFilesInDirectory(dir).Where((name) => name.Length > 6 && name.Substring(name.Length - 6) == "mpsave").Count() > 0;
        Log.Info("Has Multiplayer run: " + __result.ToString());
        return false;
    }
}

/*    [HarmonyPrefix]
    [HarmonyPatch(typeof(NPauseMenu), "CloseToMenu")]
    static void QuitPrefix()
    {
        Log.Info("Prefix starting");

        Log.Info("lastSave: " + lastSave?.ToString());
        Log.Info("saveStore: " + saveStore?.ToString());
        Log.Info("profileIdProvider: " + profileIdProvider?.ToString());

        if (lastSave == null || saveStore == null || profileIdProvider == null)
            return;

        string saveName;
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            saveName = DateTime.Now.ToString("MMM dd HH-mm") + " " + lastSave!.Players[0].CharacterId?.ToString().Substring("CHARACTER.".Length) + ".save";
        }
        else
        {
            saveName = DateTime.Now.ToString("MMM dd HH-mm");
            foreach (SerializablePlayer player in lastSave!.Players)
            {
                saveName += " " + PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId) + " " + player.CharacterId?.ToString().Substring("CHARACTER.".Length);
            }
            saveName += ".save";
        }
        string saveDir = RunSaveManager.GetRunSavePath(profileIdProvider!.CurrentProfileId, "MoreSaves");

        if (!saveStore!.DirectoryExists(saveDir))
            saveStore!.CreateDirectory(saveDir);

        string savePath = Path.Combine(saveDir, saveName);

        using MemoryStream stream = new MemoryStream();
        {
            JsonSerializer.Serialize(stream, lastSave!, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
            stream.Seek(0L, SeekOrigin.Begin);
            saveStore.WriteFile(savePath, stream.ToArray());
        }

        saveStore.DeleteFile((RunManager.Instance.NetService.Type.IsMultiplayer() ? RunSaveManager..CurrentMultiplayerRunSavePath : CurrentRunSavePath);, saveName);

        lastSave = null;
        saveStore = null;
        profileIdProvider = null;
    }*/