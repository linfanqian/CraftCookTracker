using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace CraftCookTracker.Framework
{
    /// <summary>Checklist for unmade recipes and their materials.</summary>
    internal class UnmadeList : IUnmadeList
    {
        public IClickableMenu? OldMenu { get; set; }

        /// <summary>Encapsulates monitoring and logging.</summary>
        private static IMonitor Monitor = null!;

        private readonly static Dictionary<string, string> CookingRecipes = Game1.content.Load<Dictionary<string, string>>("Data/CookingRecipes");
        private readonly static Dictionary<string, string> CraftingRecipes = Game1.content.Load<Dictionary<string, string>>("Data/CraftingRecipes");

        /// <summary>List of unmade recipe names.</summary>
        public List<string> UnmadeRecipes { get; private set; }
        /// <summary>List of <ingredient ID, ingredient info>.</summary>
        public Dictionary<string, IngredientModel> RequiredIngredients { get; private set; }

        /// <summary>Whether the UnmadeRecipes shown for cooking recipes.</summary>
        public bool ShowingCooking { get; private set; }
        /// <summary>Whether the list is opened.</summary>
        public bool IsOpened { get; private set; }

        public UnmadeList(IMonitor monitor)
        {
            Monitor = monitor;
            IsOpened = false;
            OldMenu = null;
            UnmadeRecipes = new();
            RequiredIngredients = new();
        }

        /// <summary>Fill in and display UnmadeRecipes and RequiredIngredients.</summary>
        public void OpenUnmadeList()
        {
            UnmadeRecipes.Clear();
            RequiredIngredients.Clear();
            ShowingCooking = false;
            IsOpened = true;

            // If on a crafting page, will show specific recipes and the inventory
            if (Game1.activeClickableMenu is CraftingPage craftingPage)
            {
                // on cooking page
                if (craftingPage.cooking)
                {
                    HandleCookingProducts();
                    CheckCookingInventory();
                    ShowingCooking = true;
                }
                // on crafting page
                else
                {
                    HandleCraftingProducts();
                    CheckCraftingInventory();
                }
            }
            // Otherwise, show cooking recipes by default
            else
            {
                HandleCookingProducts();
                ShowingCooking = true;
            }

            // Open menu
            OldMenu = Game1.activeClickableMenu;
            Game1.activeClickableMenu = new UnmadeListMenu(this);
        }

        /// <summary>Toggle between cooking and crafting lists (only applies when not on CraftingPage).</summary>
        public void ToggleUnmadeList()
        {
            // do nothing if old menu is crafting page
            if (OldMenu == null || OldMenu is not CraftingPage)
            {
                UnmadeRecipes.Clear();
                RequiredIngredients.Clear();

                if (ShowingCooking)
                { 
                    HandleCraftingProducts();
                    ShowingCooking = false;
                }
                else
                {
                    HandleCookingProducts();
                    ShowingCooking = true;
                }
            }
        }

        /// <summary>Close the display of UnmadeRecipes and RequiredIngredients.</summary>
        public void CloseUnmadeList()
        {
            IsOpened = false;
            UnmadeRecipes.Clear();
            RequiredIngredients.Clear();
            OldMenu = null;
        }

        /// <summary>Parse cooking products and add them to list if not made.</summary>
        private void HandleCookingProducts()
        {
            foreach (var recipe in CookingRecipes)
            {
                string recipeName = recipe.Key;
                string recipeData = recipe.Value;

                // recipe data format:
                // "ingredient1 quantity ingredient2 quantity ... / effects / product ID / ..."

                // get the product ID
                string[] fields = recipeData.Split('/');
                string productId = fields[2];

                // skip cooked recipe
                if (Game1.player.recipesCooked.ContainsKey(productId))
                    continue;

                // report and skip error recipe
                var productData = ItemRegistry.GetData(productId);
                if (productData == null)
                {
                    Monitor.Log($"Could not find item with ID {productId}. " +
                        $"Skip the recipe {recipeName}.", LogLevel.Error);
                    continue;
                }

                // add uncooked recipe to list, give notice to locked recipes
                string productDisplayName = productData.DisplayName;
                if (!Game1.player.cookingRecipes.ContainsKey(recipeName))
                {
                    productDisplayName += $" ({I18n.Crafting_Ghosted()})";
                }
                UnmadeRecipes.Add(productDisplayName);

                // handle recipe ingredients
                HandleCookingIngredients(recipeName);
            }
        }

        /// <summary>Parse crafting products and add them to list if not made.</summary>
        private void HandleCraftingProducts()
        {
            foreach (var recipe in CraftingRecipes)
            {
                string recipeName = recipe.Key;
                string recipeData = recipe.Value;

                // recipe data format:
                // "ingredient1 quantity ingredient2 quantity ... / effects / product ID [product amount] / ..."

                // skip crafted recipe
                if (Game1.player.craftingRecipes.TryGetValue(recipeName, out int productCount) &&
                    productCount > 0)
                    continue;

                // skip wedding ring
                if (recipeName == "Wedding Ring")
                    continue;

                // get the qualified product ID
                string[] fields = recipeData.Split('/');
                string productUnqualifiedId = fields[2].Split(' ')[0];
                string isBigcraftable = fields[3];

                string productQualifiedId;
                if (isBigcraftable.ToLower() == "true")
                    productQualifiedId = $"(BC){productUnqualifiedId}";
                else
                    productQualifiedId = $"(O){productUnqualifiedId}";

                // use qualified item ID to get correct data
                var productData = ItemRegistry.GetData(productQualifiedId);

                // report and skip error recipe
                if (productData == null)
                {
                    Monitor.Log($"Could not find item with ID {productQualifiedId}. " +
                        $"Skip the recipe {recipeName}.", LogLevel.Error);
                    continue;
                }

                // add uncrafted recipe to list, give notice to locked recipes
                string productDisplayName = productData.DisplayName;
                if (!Game1.player.craftingRecipes.ContainsKey(recipeName))
                {
                    productDisplayName += $" ({I18n.Crafting_Ghosted()})";
                }
                UnmadeRecipes.Add(productDisplayName);

                // handle recipe ingredients
                HandleCraftingIngredients(recipeName);
            }
        }

        /// <summary>Parse ingredients and add them to list if required.</summary>
        private void HandleCookingIngredients(string recipeName)
        {
            // for recursion use
            string recipeData = CookingRecipes[recipeName];
            string[] recipeDataFields = recipeData.Split('/');

            // parse ingredients
            string[] ingredients = recipeDataFields[0].Split(' ');
            for (int i = 0; i < ingredients.Length; i += 2)
            {
                string ingredientId = ingredients[i];
                int quantity = int.Parse(ingredients[i + 1]);

                // recursion if this ingredient is also a recipe
                var ingredientData = ItemRegistry.GetData(ingredientId);
                if (ingredientData != null && CookingRecipes.ContainsKey(ingredientData.InternalName))
                {
                    HandleCookingIngredients(ingredientData.InternalName);
                    return; // don't move on
                }

                string? ingredientDisplayName = null;
                // negative ingredient ID means any item under the category
                if (ingredientId[0] == '-')
                {
                    // -5 and -6 are both shown as animal product
                    if (ingredientId == "-5")
                        ingredientDisplayName = I18n.Material_Egg();
                    else if (ingredientId == "-6")
                        ingredientDisplayName = I18n.Material_Milk();
                    else
                        ingredientDisplayName = SObject.GetCategoryDisplayName(int.Parse(ingredientId));

                    // add "any" suffix
                    ingredientDisplayName += $" ({I18n.Material_Any()})";

                }
                // otherwise get the exact item
                else
                {
                    if (ingredientData != null)
                        ingredientDisplayName = ingredientData.DisplayName;
                }

                // report and skip invalid ingredient
                if (ingredientDisplayName == null)
                {
                    Monitor.Log($"Could not find item with ID {ingredientId}. " +
                        $"Skip this ingredient for {recipeName}.", LogLevel.Error);
                    continue;
                }

                // create the ingredient instance if not exist
                if (!RequiredIngredients.ContainsKey(ingredientId))
                {
                    RequiredIngredients[ingredientId] = new IngredientModel(ingredientId, ingredientDisplayName);
                }

                // add required amount
                RequiredIngredients[ingredientId].RequiredQuantity += quantity;
            }
        }

        /// <summary>List of ingredient IDs and their amount.</summary>
        private void HandleCraftingIngredients(string recipeName)
        {
            // for recursion use
            string recipeData = CraftingRecipes[recipeName];
            string[] recipeDataFields = recipeData.Split('/');

            // parse ingredients
            string[] ingredients = recipeDataFields[0].Split(' ');
            for (int i = 0; i < ingredients.Length; i += 2)
            {
                string ingredientId = ingredients[i];
                int quantity = int.Parse(ingredients[i + 1]);

                // recursion if this ingredient is also a recipe
                var ingredientData = ItemRegistry.GetData(ingredientId);
                if (ingredientData != null && CraftingRecipes.ContainsKey(ingredientData.InternalName))
                {
                    HandleCraftingIngredients(ingredientData.InternalName);
                    return; // don't move on
                }

                string? ingredientDisplayName = null;
                // negative ingredient ID means any item under the category
                if (ingredientId[0] == '-')
                {
                    // -777 represents wild seeds, but can't be parsed
                    if (ingredientId == "-777")
                        ingredientDisplayName = I18n.Materail_WildSeeds();
                    else
                        ingredientDisplayName = SObject.GetCategoryDisplayName(int.Parse(ingredientId));

                    // add "any" suffix
                    ingredientDisplayName += $" ({I18n.Material_Any()})";

                }
                // otherwise get the exact item
                else
                {
                    if (ingredientData != null)
                        ingredientDisplayName = ingredientData.DisplayName;
                }

                // report and skip invalid ingredient
                if (ingredientDisplayName == null)
                {
                    Monitor.Log($"Could not find item with ID {ingredientId}. " +
                        $"Skip this ingredient for {recipeName}.", LogLevel.Error);
                    continue;
                }

                // create the ingredient instance if not exist
                if (!RequiredIngredients.ContainsKey(ingredientId))
                {
                    RequiredIngredients[ingredientId] = new IngredientModel(ingredientId, ingredientDisplayName);
                }

                // add required amount
                RequiredIngredients[ingredientId].RequiredQuantity += quantity;
            }
        }

        /// <summary>Check and add cooking ingredient inventories.</summary>
        private void CheckCookingInventory()
        {
            // get fridges
            List<Chest> fridges = new();
            if (Game1.currentLocation is FarmHouse farmHouse)
            {
                fridges = farmHouse.Objects.Values
                    .OfType<Chest>()
                    .Where(chest => chest.bigCraftable.Value && chest.ParentSheetIndex == 216)
                    .ToList();

                Chest? mainFridge = farmHouse.fridge?.Value;
                if (mainFridge != null)
                    fridges.Add(mainFridge);
            }
            else if (Game1.currentLocation is IslandFarmHouse islandHouse)
            {
                fridges = islandHouse.Objects.Values
                    .OfType<Chest>()
                    .Where(chest => chest.bigCraftable.Value && chest.ParentSheetIndex == 216)
                    .ToList();

                Chest? mainFridge = islandHouse.fridge?.Value;
                if (mainFridge != null)
                    fridges.Add(mainFridge);
            }

            // check items from fridges, add prepared ingredients for unmade recipes
            foreach (Chest fridge in fridges)
            {
                // skip empty fridges
                if (fridge.isEmpty())
                    continue;

                // add required items
                foreach (Item item in fridge.Items)
                {
                    if (item != null)
                    {
                        string itemUnqualifiedId = item.ItemId;
                        int quantity = item.Stack;
                        AddIngredientInventory(itemUnqualifiedId, quantity);
                    }

                }
            }
            foreach (Item item in Game1.player.Items)
            {
                if (item != null)
                {
                    string itemUnqualifiedId = item.ItemId;
                    int quantity = item.Stack;
                    AddIngredientInventory(itemUnqualifiedId, quantity);
                }
            }
        }

        /// <summary>Check and add crafting ingredient inventories.</summary>
        private void CheckCraftingInventory()
        {
            // These offsets include the 4 cardinal directions + 4 diagonals:
            var adjacencyOffsets = new Vector2[]
            {
                new Vector2( 1,  0),  // right
                new Vector2(-1,  0),  // left
                new Vector2( 0,  1),  // down
                new Vector2( 0, -1),  // up
                new Vector2( 1,  1),  // diagonal down-right
                new Vector2( 1, -1),  // diagonal up-right
                new Vector2(-1,  1),  // diagonal down-left
                new Vector2(-1, -1)   // diagonal up-left
            };

            
            Vector2? workbenchTile = GetNearestWorkbenchTile();
            // report workbench error and exit
            if (workbenchTile == null)
            {
                Monitor.Log("No workbench found.", LogLevel.Warn);
                return;
            }

            // search for workbench chests
            List<Chest> chests = new();
            foreach (Vector2 offset in adjacencyOffsets)
            {
                Vector2 adjacentTile = workbenchTile.Value + offset;
                if (Game1.currentLocation.objects.TryGetValue(adjacentTile, out SObject neighborObj) &&
                    neighborObj is Chest chest)
                {
                    chests.Add(chest);
                }
            }

            // check items from chests, add prepared ingredients for unmade recipes
            foreach (Chest chest in chests)
            {
                // skip empty chests
                if (chest.isEmpty())
                    continue;

                // add required items
                foreach (Item item in chest.Items)
                {
                    if (item != null)
                    {
                        string itemUnqualifiedId = item.ItemId;
                        int quantity = item.Stack;
                        AddIngredientInventory(itemUnqualifiedId, quantity);
                    }

                }
            }
            foreach (Item item in Game1.player.Items)
            {
                if (item != null)
                {
                    string itemQualifiedId = item.QualifiedItemId;
                    int quantity = item.Stack;
                    AddIngredientInventory(itemQualifiedId, quantity);
                }
            }
        }

        /// <summary>Add quantity into inventory if the ingredient is required.</summary>
        private void AddIngredientInventory(string ingredientId, int quantity)
        {
            if (RequiredIngredients.ContainsKey(ingredientId))
                RequiredIngredients[ingredientId].InventoryQuantity += quantity;
        }

        /// <summary>Add quantity into inventory if the ingredient is required.</summary>
        private Vector2? GetNearestWorkbenchTile()
        {
            Vector2? nearestWorkbenchTile = null;
            float nearestDistance = float.MaxValue;

            // get current location
            GameLocation location = Game1.currentLocation;
            if (location == null)
            {
                Monitor.Log("No current location found.", LogLevel.Warn);
                return null;
            }

            // get current tile position
            Vector2 playerTilePos = Game1.player.Tile;

            // Scan every object in this location.
            foreach (var pair in location.objects.Pairs)
            {
                SObject obj = pair.Value;

                // Check if it's a workbench (big craftable #208).
                if (obj != null && obj.bigCraftable.Value && obj.ParentSheetIndex == 208)
                {
                    float distance = Vector2.Distance(pair.Key, playerTilePos);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestWorkbenchTile = pair.Key;
                    }
                }
            }

            return nearestWorkbenchTile;
        }
    }
}
