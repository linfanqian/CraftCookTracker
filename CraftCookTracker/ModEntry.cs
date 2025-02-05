using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using CraftCookTracker.Framework;
using CraftCookTracker.Externel;

namespace CraftCookTracker
{
    internal sealed class ModEntry : Mod
    {
        private IUnmadeList UnmadeListObj;
        private ModConfig Config;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            UnmadeListObj = new UnmadeList(Monitor);
            Config = Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add some config options
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => I18n.Modconfig_ShowRecipeList(),
                tooltip: () => I18n.Modconfig_ShowRecipeListTip(),
                getValue: () => this.Config.OpenUnmadeList,
                setValue: value => this.Config.OpenUnmadeList = value
            );
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is CraftingPage craftingPage)
            {
                // add count display for crafting recipes
                RecipeDisplay.ShowRecipeCount(craftingPage);
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Config.OpenUnmadeList.JustPressed())
            {
                // show unmade recipes and required materials
                if (!UnmadeListObj.IsOpened)
                    UnmadeListObj.OpenUnmadeList();
            }
        }

    }
}