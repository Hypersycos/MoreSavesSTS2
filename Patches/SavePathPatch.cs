using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Migrations;
using MegaCrit.Sts2.Core.Saves.Runs;
using MoreSaves.MainMenu;
using System.Reflection;
using System.Text.Json;

namespace MoreSaves.Patches;

#pragma warning disable IDE0005

public class Store
{
    public static string currentSPSave = "Mar 15 16-19 IRONCLAD";
    public static string currentMPSave = "Mar 15 16-19 IRONCLAD";
    public static int saveCount = 0;
    public static int multiSaveCount = 0;
    public static NMainMenu? mainMenu = null;
    public static NMultiplayerSubmenu? submenu = null;

    public static ISaveStore? lastSaveStore = null;
    public static IEnumerable<string> spSaves = [];
    public static IEnumerable<string> mpSaves = [];

    public static string SaveDir => RunSaveManager.GetRunSavePath(SaveManager.Instance.CurrentProfileId, "MoreSaves");
    public static ReadSaveResult<SerializableRun> GetSPRun(string name)
    {
        currentSPSave = name;
        return SaveManager.Instance.LoadRunSave();
    }

    public static ReadSaveResult<SerializableRun> GetMPRun(string name)
    {
        currentMPSave = name;
        PlatformType platformType = ((SteamInitializer.Initialized && !CommandLineHelper.HasArg("fastmp")) ? PlatformType.Steam : PlatformType.None);
        return SaveManager.Instance.LoadAndCanonicalizeMultiplayerRunSave(PlatformUtil.GetLocalPlayerId(platformType));
    }
}

[HarmonyPatch]
public class SubmenuPatch
{
    static NewContinueScreen? spContinueScreen;
    static NewAbandonScreen? spAbandonScreen;
    static NewMPContinueScreen? mpContinueScreen;
    static NewMPAbandonScreen? mpAbandonScreen;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NSubmenuStack), nameof(NSubmenuStack.InitializeForMainMenu))]
    static void ClearSubmenus(NMainMenu mainMenu)
    {
        spContinueScreen = null;
        spAbandonScreen = null;
        mpContinueScreen = null;
        mpAbandonScreen = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NMainMenuSubmenuStack), nameof(NMainMenuSubmenuStack.GetSubmenuType), new Type[] {typeof(Type)})]
    static bool GetAddedSubmenus(Type type, NMainMenuSubmenuStack __instance, ref NSubmenu __result)
    {
        if (type == typeof(NewContinueScreen))
        {
            if (spContinueScreen == null)
            {
                spContinueScreen = NewContinueScreen.Create()!;
                spContinueScreen.Visible = false;
                __instance.AddChildSafely(spContinueScreen);
            }
            __result = spContinueScreen;
            return false;
        }

        if (type == typeof(NewAbandonScreen))
        {
            if (spAbandonScreen == null)
            {
                spAbandonScreen = NewAbandonScreen.Create()!;
                spAbandonScreen.Visible = false;
                __instance.AddChildSafely(spAbandonScreen);
            }
            __result = spAbandonScreen;
            return false;
        }

        if (type == typeof(NewMPContinueScreen))
        {
            if (mpContinueScreen == null)
            {
                mpContinueScreen = NewMPContinueScreen.Create()!;
                mpContinueScreen.Visible = false;
                __instance.AddChildSafely(mpContinueScreen);
            }
            __result = mpContinueScreen;
            return false;
        }

        if (type == typeof(NewMPAbandonScreen))
        {
            if (mpAbandonScreen == null)
            {
                mpAbandonScreen = NewMPAbandonScreen.Create()!;
                mpAbandonScreen.Visible = false;
                __instance.AddChildSafely(mpAbandonScreen);
            }
            __result = mpAbandonScreen;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(NMultiplayerSubmenu))]
public class MultiplayerMenuPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("UpdateButtons")]
    static void ReEnableButton(NSubmenuButton ____hostButton)
    {
        if (SaveManager.Instance.HasMultiplayerRunSave)
        {
            ____hostButton.Visible = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("StartLoad")]
    static bool ModifyContinue(NMultiplayerSubmenu __instance, NSubmenuStack ____stack)
    {
        if (Store.multiSaveCount == 1)
        {
            return true;
        }

        Store.submenu = __instance;
        ____stack.PushSubmenuType<NewMPContinueScreen>();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("AbandonRun")]
    static bool ModifyAbandon(NMultiplayerSubmenu __instance, NSubmenuStack ____stack)
    {
        if (Store.multiSaveCount == 1)
        {
            return true;
        }

        Store.submenu = __instance;
        ____stack.PushSubmenuType<NewMPAbandonScreen>();
        return false;
    }

}


[HarmonyPatch(typeof(NMainMenu))]
public class MenuPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu.Create))]
    static void GrabMenu(bool openTimeline, ref NMainMenu __result)
    {
        Store.mainMenu = __result;
        Log.Info("Grabbed mainmenu!");
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu.RefreshButtons))]
    static void ReEnableButton(NMainMenuTextButton ____singleplayerButton)
    {
        if (SaveManager.Instance.HasRunSave)
        {
            ____singleplayerButton.Visible = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnContinueButtonPressed")]
    static bool ModifyContinue(NMainMenu __instance, NMainMenuTextButton ____continueButton, ref NMainMenuTextButton? ____lastHitButton)
    {
        if (Store.saveCount == 1)
            return true;

        ____lastHitButton = ____continueButton;
        __instance.SubmenuStack.PushSubmenuType<NewContinueScreen>();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnAbandonRunButtonPressed")]
    static bool ModifyAbandon(NMainMenu __instance, NMainMenuTextButton ____abandonRunButton, ref NMainMenuTextButton? ____lastHitButton)
    {
        if (Store.saveCount == 1)
            return true;

        ____lastHitButton = ____abandonRunButton;
        __instance.SubmenuStack.PushSubmenuType<NewAbandonScreen>();
        return false;
    }
}

[HarmonyPatch(typeof(NMainMenuContinueButton))]
public class ContinueButtonPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnFocus")]
    static bool AllowContinuePopup()
    {
        Log.Info("Sp saves: "+Store.saveCount.ToString());
        return Store.saveCount == 1;
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public class CharacterSelectPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(NCharacterSelectScreen.BeginRun))]
    public static void ChangeSaveName(StartRunLobby ____lobby)
    {
        if (____lobby.NetService.Type == NetGameType.Singleplayer)
        {
            Store.currentSPSave = DateTime.Now.ToString("MMM dd HH-mm") + " " + ____lobby.Players[0].character.Title.GetFormattedText();
        }
        else
        {
            Store.currentMPSave = DateTime.Now.ToString("MMM dd HH-mm");
            foreach (LobbyPlayer player in ____lobby.Players)
            {
                Store.currentMPSave += " " + PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.id) + " " + player.character.Title.GetFormattedText();
            }
        }
    }
}

[HarmonyPatch(typeof(RunSaveManager))]
public class RunSaveManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("CurrentRunSavePath", MethodType.Getter)]
    static bool SingleplayerPath(ref string __result, IProfileIdProvider ____profileIdProvider)
    {
        __result = Path.Combine(Store.SaveDir, Store.currentSPSave + ".spsave");
        Log.Info(__result);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("CurrentMultiplayerRunSavePath", MethodType.Getter)]
    static bool MultiplayerPath(ref string __result, IProfileIdProvider ____profileIdProvider)
    {
        __result = Path.Combine(Store.SaveDir, Store.currentMPSave + ".mpsave");
        Log.Info(__result);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveManager.HasRunSave), MethodType.Getter)]
    static bool HasSingleplayerRun(ref bool __result, ISaveStore ____saveStore, IProfileIdProvider ____profileIdProvider)
    {
        Store.lastSaveStore = ____saveStore;
        IEnumerable<string> files = ____saveStore.GetFilesInDirectory(Store.SaveDir).Where((name) => name.Length > 6 && name.Substring(name.Length - 6) == "spsave");
        Store.spSaves = files;
        Store.saveCount = files.Count();
        __result = Store.saveCount > 0;

        if (__result)
        {
            Store.currentSPSave = files.Last();
            Store.currentSPSave = Store.currentSPSave.Substring(0, Store.currentSPSave.Length - 7);
        }
        else
        {
            Store.currentSPSave = "";
        }

        Log.Info("Has Singleplayer run: " + __result.ToString());
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SaveManager.HasMultiplayerRunSave), MethodType.Getter)]
    static bool HasMultiplayerRun(ref bool __result, ISaveStore ____saveStore, IProfileIdProvider ____profileIdProvider)
    {
        Store.lastSaveStore = ____saveStore;
        IEnumerable<string> files = ____saveStore.GetFilesInDirectory(Store.SaveDir).Where((name) => name.Length > 6 && name.Substring(name.Length - 6) == "mpsave");
        Store.mpSaves = files;
        Store.multiSaveCount = files.Count();
        __result = Store.multiSaveCount > 0;

        if (__result)
        {
            Store.currentMPSave = files.Last();
            Store.currentMPSave = Store.currentMPSave.Substring(0, Store.currentMPSave.Length - 7);
        }
        else
        {
            Store.currentMPSave = "";
        }

        Log.Info("Has Multiplayer run: " + __result.ToString());
        return false;
    }
}