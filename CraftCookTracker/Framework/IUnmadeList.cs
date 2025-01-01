namespace CraftCookTracker.Framework
{
    /// <summary>Checklist for unmade recipes and their materials (interface).</summary>
    internal interface IUnmadeList
    {
        /// <summary>List of unmade recipe names.</summary>
        List<string> UnmadeRecipes { get; }
        /// <summary>List of <ingredient ID, ingredient info>.</summary>
        Dictionary<string, IngredientModel> RequiredIngredients { get; }

        /// <summary>Whether the UnmadeRecipes shown for cooking recipes.</summary>
        bool ShowingCooking { get; }
        /// <summary>Whether the list is opened.</summary>
        bool IsOpened { get; }

        public void OpenUnmadeList();
        public void ToggleUnmadeList();
        public void CloseUnmadeList();
    }
}
