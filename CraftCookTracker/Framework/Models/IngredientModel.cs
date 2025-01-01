namespace CraftCookTracker.Framework
{
    public class IngredientModel
    {
        /// <summary>The (un)qualified item ID.</summary>
        public string Id { get; set; }

        /// <summary>Item name.</summary>
        public string DisplayName { get; set; }

        /// <summary>Required quntity for 100% crafting/cooking.</summary>
        public int RequiredQuantity { get; set; }

        /// <summary>The quantity in chests/fridges.</summary>
        public int InventoryQuantity { get; set; }

        public IngredientModel(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
            RequiredQuantity = 0;
            InventoryQuantity = 0;
        }
    }
}
