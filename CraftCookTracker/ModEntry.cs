using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using CraftCookTracker.Framework;

namespace CraftCookTracker
{
    internal sealed class ModEntry : Mod
    {

        private IUnmadeList UnmadeListObj;

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            UnmadeListObj = new UnmadeList(Monitor);

            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
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
            if (e.Button == SButton.R)
            {
                // show unmade recipes and required materials
                if (!UnmadeListObj.IsOpened)
                    UnmadeListObj.OpenUnmadeList();
            }
        }

    }
}