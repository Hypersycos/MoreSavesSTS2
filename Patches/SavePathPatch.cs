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

namespace MoreSaves.Patches;

[HarmonyPatch]
public class SavePatch
{
    static SerializableRun? lastSave = null;
    static ISaveStore? saveStore = null;
    static IProfileIdProvider? profileIdProvider = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.SaveRun))]
    static void SavePostfix(ref AbstractRoom? preFinishedRoom, ref bool ____forceSynchronous, ref ISaveStore ____saveStore, ref IProfileIdProvider ____profileIdProvider)
    {
        Log.Info("Postfix starting");

        if (!RunManager.Instance.ShouldSave || (RunManager.Instance.NetService.Type != NetGameType.Singleplayer && RunManager.Instance.NetService.Type != NetGameType.Host))
        {
            return;
        }

        Log.Info("Postfix passed guard");

        lastSave = RunManager.Instance.ToSave(preFinishedRoom);
        saveStore = ____saveStore;
        profileIdProvider = ____profileIdProvider;

        Log.Info("lastSave: " + lastSave?.ToString());
        Log.Info("saveStore: " + saveStore?.ToString());
        Log.Info("profileIdProvider: " + profileIdProvider?.ToString());
    }

    [HarmonyPrefix]
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
    }
}
