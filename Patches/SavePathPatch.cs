using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;

namespace MoreSaves.Patches;

[HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.SaveRun))]
public class SavePatch
{
    static void Prefix(ref AbstractRoom? preFinishedRoom, ref RunSaveManager __instance, ref bool ____forceSynchronous, ref ISaveStore ____saveStore, ref IProfileIdProvider ____profileIdProvider)
    {
        if (!RunManager.Instance.ShouldSave || (RunManager.Instance.NetService.Type != NetGameType.Singleplayer && RunManager.Instance.NetService.Type != NetGameType.Host))
        {
            return;
        }
        SerializableRun value = RunManager.Instance.ToSave(preFinishedRoom);

        string saveName;
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            saveName = DateTime.Now.ToString("MMM dd HH-mm") + " " + value.Players[0].CharacterId?.ToString().Substring("CHARACTER.".Length) + ".save";
        }
        else
        {
            saveName = DateTime.Now.ToString("dd MMM HH-mm");
            foreach (SerializablePlayer player in value.Players)
            {
                saveName += " " + PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId) + " " + player.CharacterId?.ToString().Substring("CHARACTER.".Length);
            }
            saveName += ".save";
        }
        string savePath = RunSaveManager.GetRunSavePath(____profileIdProvider.CurrentProfileId, "MoreSaves");

        if (!____saveStore.DirectoryExists(savePath))
            ____saveStore.CreateDirectory(savePath);

        savePath = Path.Combine(savePath, saveName);

        using MemoryStream stream = new MemoryStream();
        {
            JsonSerializer.Serialize(stream, value, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
            stream.Seek(0L, SeekOrigin.Begin);
            ____saveStore.WriteFile(savePath, stream.ToArray());
        }
    }
}
