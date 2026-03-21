using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MoreSaves.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreSaves.MainMenu
{
    public abstract partial class NewBaseScreen : NSubmenu
    {
        protected Control buttonContainer;
        protected Control loadingOverlay;
        protected override Control? InitialFocusedControl => buttonContainer.GetChild<Control>(0);

        protected abstract string localisationBase { get; }

        public override void _Ready()
        {
            ConnectSignals();
            buttonContainer = GetNode<Control>("Panel/MarginContainer/ButtonContainer");
            loadingOverlay = GetNode<Control>("Panel/LoadingIndicator");
            //GetNode<MegaLabel>("TitleLabel").SetTextAutoSize("PLACEHOLDER");//new LocString("main_menu_ui", localisationBase+".title").GetFormattedText());
        }

        protected abstract void InnerBuildOptions();

        protected async Task BuildOptions()
        {
            foreach (Node child in buttonContainer.GetChildren())
            {
                child.QueueFreeSafely();
            }

            InnerBuildOptions();

            ActiveScreenContext.Instance.Update();
        }

        public override void OnSubmenuOpened()
        {
            loadingOverlay.Visible = false;
            TaskHelper.RunSafely(BuildOptions());
        }

        protected static T? Create<T>() where T : NewBaseScreen, new()
        {
            string _scenePath = SceneHelper.GetScenePath("screens/join_friend_submenu");

            Node scene = PreloadManager.Cache.GetScene(_scenePath).Instantiate(PackedScene.GenEditState.Disabled);

            scene.GetNode<Label>("Panel/NoFriendsText").Visible = false;

            var myScript = new T();

            myScript.GlobalPosition = (scene as Control)!.GlobalPosition;
            myScript.Size = new Vector2(1920, 1080);

            foreach (Node child in scene.GetChildren())
            {
                if (child is Node2D n2d)
                {
                    Vector2 oldPos = n2d.GlobalPosition;
                    scene.RemoveChild(child);
                    myScript.AddChild(child);
                    n2d.GlobalPosition = oldPos;
                }
                else if (child is Control c)
                {
                    Vector2 oldPos = c.GlobalPosition;

                    scene.RemoveChild(child);
                    myScript.AddChild(child);

                    c.GlobalPosition = oldPos;
                }
            }

            return myScript as T;
        }
    }
}
