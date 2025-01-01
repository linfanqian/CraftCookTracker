using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace CraftCookTracker.Framework
{
    /// <summary>Display crafting counts.</summary>
    internal class RecipeDisplay
    {
        private static string OriginalDescription = string.Empty;

        public static void ShowRecipeCount(CraftingPage craftingPage)
        {
            if (craftingPage.hoverRecipe != null)
            {
                string productId = craftingPage.hoverRecipe.GetItemData().ItemId;
                string productName = craftingPage.hoverRecipe.name;

                int productCount = 0;

                // cooking recipe
                if (craftingPage.hoverRecipe.isCookingRecipe)
                {
                    Game1.player.recipesCooked.TryGetValue(productId, out productCount);
                }
                // crafting recipe
                else
                {
                    Game1.player.craftingRecipes.TryGetValue(productName, out productCount);
                }

                // display product count
                string productCountText = productCount == 0 ? I18n.Recipe_NotMade() : $"{I18n.Recipe_Made()}: {productCount}";
                if (OriginalDescription == string.Empty)
                {
                    OriginalDescription = craftingPage.hoverRecipe.description;
                }
                craftingPage.hoverRecipe.description = OriginalDescription + $"\n{productCountText}";
            }
        }
    }
}
