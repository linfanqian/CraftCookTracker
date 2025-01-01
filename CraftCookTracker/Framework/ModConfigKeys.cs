using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace CraftCookTracker.Framework
{
    /// <summary>A set of parsed key bindings.</summary>
    internal class ModConfigKeys
    {
        public KeybindList OpenUnmadeList { get; set; } = KeybindList.ForSingle(SButton.R);
    }
}
