using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace CraftCookTracker.Framework
{
    /// <summary>A menu to display unmade recipe names and required ingredients in a scrollable area.</summary>
    internal class UnmadeListMenu : IClickableMenu
    {
        private readonly UnmadeList UnmadeListObj;

        /// <summary>Scrolling.</summary>
        private readonly Rectangle ScrollArea;
        private int CurrentScrollY;
        private const int ScrollStep = 40;   // how many pixels we scroll per wheel “notch”
        private int ContentHeight = 0;

        /// <summary>Layout constants.</summary>
        private const int FixedWidth = 1000;
        private const int FixedHeight = 600;
        private const int Margin = 50;
        private const float TitleScale = 1.5f;
        private const float BottomPadding = 10f;

        /// <summary>Scrollbar.</summary>
        private const int ScrollbarWidth = 10;         // Width of the scrollbar
        private const int ScrollbarPadding = 15;       // Padding between scrollbar and ScrollArea
        private const int MinHandleHeight = 25;        // Minimum height of the scrollbar handle
        private Rectangle ScrollbarTrack;              // The track of the scrollbar
        private Rectangle ScrollbarHandle;             // The draggable handle of the scrollbar
        private bool IsDraggingScrollbar = false;      // Indicates if the scrollbar handle is being dragged
        private Vector2 DragOffset;                    // Offset between mouse click and handle position

        /// <summary>Symbols.</summary>
        private const string CheckMark = "O";
        private const string CrossMark = "X";
        private const int SymbolPadding = 5;        // Space between symbol and text

        /// <summary>Cut texts for scrolling.</summary>
        private static readonly RasterizerState ScissorRasterizer = new()
        {
            ScissorTestEnable = true,
            FillMode = FillMode.Solid,
            CullMode = CullMode.None
        };

        public UnmadeListMenu(UnmadeList unmadeList)
            : base(
                  x: 0,
                  y: 0,
                  width: FixedWidth,
                  height: FixedHeight,
                  showUpperRightCloseButton: true
                  )
        {
            UnmadeListObj = unmadeList;

            // this utility returns a Vector2 for top-left so that a menu is centered.
            Vector2 pos = Utility.getTopLeftPositionForCenteringOnScreen(width, height);

            // center the menu
            xPositionOnScreen = (int)pos.X;
            yPositionOnScreen = (int)pos.Y;

            // adjust the upperRightCloseButton position
            if (upperRightCloseButton != null)
            {
                upperRightCloseButton.bounds.X = xPositionOnScreen + width;
                upperRightCloseButton.bounds.Y = yPositionOnScreen - 36;
            }

            // scroll area is the rectangle inside the box
            ScrollArea = new Rectangle(
                xPositionOnScreen + Margin, 
                yPositionOnScreen + Margin,
                FixedWidth - Margin * 2, 
                FixedHeight - Margin * 2
            );

            // calculate total content height
            ContentHeight = GetTotalContentHeight();

            // define scrollbar
            ScrollbarTrack = new Rectangle(
                ScrollArea.Right + ScrollbarPadding,
                ScrollArea.Y,
                ScrollbarWidth,
                ScrollArea.Height
            );
            UpdateScrollbarHandle();

            Game1.playSound("bigSelect");
        }

        /// <summary>Draw the background and the scrollable content.</summary>
        public override void draw(SpriteBatch b)
        {
            // draw a semi-transparent background
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * 0.5f);

            // draw a box around the scroll area
            drawTextureBox(
                b,
                ScrollArea.X - Margin,
                ScrollArea.Y - Margin,
                ScrollArea.Width + Margin * 2,
                ScrollArea.Height + Margin * 2,
                Color.White
            );

            // start scissor
            b.End();
            b.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                ScissorRasterizer
            );

            Rectangle oldScissorRect = b.GraphicsDevice.ScissorRectangle;
            Rectangle newScissorRect = new(ScrollArea.X, ScrollArea.Y, ScrollArea.Width, ScrollArea.Height);
            newScissorRect = Rectangle.Intersect(newScissorRect, oldScissorRect);
            b.GraphicsDevice.ScissorRectangle = newScissorRect;

            // start text drawing
            Vector2 textPos = new(ScrollArea.X, ScrollArea.Y - CurrentScrollY);

            // draw the title of unmade recipes
            string unmadeTitle = UnmadeListObj.ShowingCooking
                ? $"{I18n.Crafting_Uncooked()}:"
                : $"{I18n.Crafting_Uncrafted()}:";
            b.DrawString(
                Game1.dialogueFont,  // font
                unmadeTitle,         // text
                textPos,             // position on screen
                Color.SaddleBrown,   // color
                0f,                  // rotation
                Vector2.Zero,        // origin
                TitleScale,          // scale factor (e.g. 1.5x bigger)
                SpriteEffects.None,
                1f                   // layer depth
            );

            // update textPos
            Vector2 unmadeTitleSize = Game1.dialogueFont.MeasureString(unmadeTitle);
            float unmadeTitleHeight = unmadeTitleSize.Y * TitleScale;
            textPos.Y += (unmadeTitleHeight + BottomPadding);

            // wrapped string of unmade recipes
            string unmadeRecipeStr = ConcatStrings(UnmadeListObj.UnmadeRecipes);

            // draw the list of unmade recipes
            b.DrawString(
                Game1.dialogueFont,  // font
                unmadeRecipeStr,     // text
                textPos,             // position on screen
                Color.SaddleBrown    // color
            );

            // update textPos
            Vector2 unmadeRecipeSize = Game1.dialogueFont.MeasureString(unmadeRecipeStr);
            float recipeHeight = unmadeRecipeSize.Y;
            textPos.Y += (recipeHeight + BottomPadding + 20f);

            // draw the title of required ingredients
            string ingredientTitle = $"{I18n.Crafting_RequiredIngredients()}:";
            b.DrawString(
                Game1.dialogueFont,  // font
                ingredientTitle,     // text
                textPos,             // position on screen
                Color.SaddleBrown,   // color
                0f,                  // rotation
                Vector2.Zero,        // origin
                TitleScale,          // scale factor (e.g. 1.5x bigger)
                SpriteEffects.None,
                1f                   // layer depth
            );

            // update textPos
            Vector2 ingredientTitleSize = Game1.dialogueFont.MeasureString(ingredientTitle);
            float ingredientTitleHeight = ingredientTitleSize.Y * TitleScale;
            textPos.Y += (ingredientTitleHeight + BottomPadding);

            // draw the list of required ingredients
            foreach (var ingredient in UnmadeListObj.RequiredIngredients.Values)
            {
                string ingredientName = ingredient.DisplayName;
                int requiredQuantity = ingredient.RequiredQuantity;
                int inventoryQuantity = ingredient.InventoryQuantity;

                // draw check or cross mark to show inventory sufficiency
                string symbol = (inventoryQuantity >= requiredQuantity) ? CheckMark : CrossMark;
                Color symbolColor = (inventoryQuantity >= requiredQuantity) ? Color.Green : Color.Red;
                b.DrawString(
                    Game1.dialogueFont,  // font
                    symbol,              // text
                    textPos,             // position on screen
                    symbolColor,         // color
                    0f,                  // rotation
                    Vector2.Zero,        // origin
                    1f,                  // scale
                    SpriteEffects.None,
                    0.9f                 // layer depth (ensure it's behind text if necessary)
                );

                // update textPos
                Vector2 symbolSize = Game1.dialogueFont.MeasureString(symbol);
                float symbolWidth = symbolSize.X + SymbolPadding;
                textPos.X += symbolWidth;

                string ingredientStr = $"{ingredientName}: " +
                    $"{I18n.Material_Require()} {requiredQuantity}, " +
                    $"{I18n.Material_Prepared()} {inventoryQuantity}";

                b.DrawString(
                    Game1.dialogueFont,  // font
                    ingredientStr,       // text
                    textPos,             // position on screen
                    Color.SaddleBrown    // color
                );

                // update textPos
                Vector2 ingredienrSize = Game1.dialogueFont.MeasureString(ingredientStr);
                float ingredientHeight = ingredienrSize.Y;
                textPos.Y += ingredientHeight;
                textPos.X -= symbolWidth;
            }

            // end scissor
            b.End();
            b.Begin(
                SpriteSortMode.Deferred, 
                BlendState.AlphaBlend, 
                SamplerState.PointClamp, 
                DepthStencilState.Default, 
                RasterizerState.CullNone
            );
            b.GraphicsDevice.ScissorRectangle = oldScissorRect;

            DrawScrollBar(b);
            base.draw(b);
            drawMouse(b);
        }

        /// <summary>Handle mouse wheel scrolling.</summary>
        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            // direction < 0 => scrolled down => content moves up => currentScrollY increases
            // direction > 0 => scrolled up => content moves down => currentScrollY decreases
            if (direction < 0)
                CurrentScrollY += ScrollStep;
            else if (direction > 0)
                CurrentScrollY -= ScrollStep;

            // Clamp so we can’t scroll past the top or bottom
            int maxScroll = Math.Max(0, ContentHeight - ScrollArea.Height);
            if (CurrentScrollY < 0)
                CurrentScrollY = 0;
            else if (CurrentScrollY > maxScroll)
                CurrentScrollY = maxScroll;

            // Update scrollbar handle position
            UpdateScrollbarHandle();
        }

        /// <summary>Key press actions.</summary>
        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);

            // if the pressed key is the same as the user's configured "menu" key
            // or the "cancel" key, we close the menu.
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key)
                || Game1.options.doesInputListContain(Game1.options.cancelButton, key))
            {
                CloseMenu();
            }
            else if (key == Keys.Left || key == Keys.Right)
            {
                UnmadeListObj.ToggleUnmadeList();

                // update the menu
                ContentHeight = GetTotalContentHeight();
                CurrentScrollY = 0;
                UpdateScrollbarHandle();
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>Left click actions.</summary>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // check if the click is within the scrollbar handle
            if (ScrollbarHandle.Contains(x, y))
            {
                IsDraggingScrollbar = true;
                // calculate the offset between mouse position and handle's top
                DragOffset = new Vector2(x - ScrollbarHandle.X, y - ScrollbarHandle.Y);
            }
            else
            {
                base.receiveLeftClick(x, y, playSound);

                // close button clicked
                if (upperRightCloseButton.containsPoint(x, y))
                {
                    CloseMenu();
                }
            }
        }

        /// <summary>Handle mouse release.</summary>
        public override void releaseLeftClick(int x, int y)
        {
            IsDraggingScrollbar = false;
            base.releaseLeftClick(x, y);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            // scrollbar
            if (IsDraggingScrollbar)
            {
                // get current mouse position
                int mouseY = Game1.getOldMouseY();

                // calculate the new Y position for the scrollbar handle
                int newHandleY = mouseY - (int)DragOffset.Y;

                // clamp the handle position within the scrollbar track
                newHandleY = Math.Clamp(newHandleY, ScrollbarTrack.Y, ScrollbarTrack.Y + ScrollbarTrack.Height - ScrollbarHandle.Height);

                // update scrollbar handle position
                ScrollbarHandle.Y = newHandleY;

                // calculate the scroll percentage based on handle position
                float scrollPercent = (float)(ScrollbarHandle.Y - ScrollbarTrack.Y) / (ScrollbarTrack.Height - ScrollbarHandle.Height);
                scrollPercent = MathHelper.Clamp(scrollPercent, 0f, 1f);

                // update CurrentScrollY based on scroll percentage
                int maxScrollY = ContentHeight - ScrollArea.Height;
                CurrentScrollY = (int)(scrollPercent * maxScrollY);

                // update scrollbar handle again to ensure consistency
                UpdateScrollbarHandle();
            }
        }

        /// <summary>Return to the last menu or exit the menu.</summary>
        private void CloseMenu()
        {
            Game1.playSound("bigDeSelect");

            if (UnmadeListObj.OldMenu != null)
                Game1.activeClickableMenu = UnmadeListObj.OldMenu;
            else
                exitThisMenu();

            UnmadeListObj.CloseUnmadeList();
        }

        /// <summary>Concatenate and wrap the list of strings.</summary>
        private string ConcatStrings(List<string> strings)
        {
            string result = "";
            foreach (string str in strings)
                result += $",{str}";
            result = WrapText(Game1.dialogueFont, result, ScrollArea.Width);
            return result;
        }

        /// <summary>
        ///     Splits a given text into lines so that no line is wider than the specified maxLineWidth.
        ///     Inserts '\n' where needed.
        /// </summary>
        private static string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            // if text is empty or single-line, just return as-is
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // break by ','
            string[] words = text.Split(',');

            string result = "";
            float currentLineWidth = 0f;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                // measure this word
                Vector2 size = font.MeasureString(word);

                // If this is the first word on the line, add it without a leading space
                if (currentLineWidth == 0f)
                {
                    result += word;
                    currentLineWidth = size.X;
                }
                else
                {
                    // measure the space before the next word
                    float spaceWidth = font.MeasureString(", ").X;
                    float nextWidth = currentLineWidth + spaceWidth + size.X;

                    if (nextWidth < maxLineWidth)
                    {
                        // we can append the word in this line
                        result += ", " + word;
                        currentLineWidth = nextWidth;
                    }
                    else
                    {
                        // start a new line
                        result += "\n" + word;
                        currentLineWidth = size.X;
                    }
                }
            }

            return result;
        }

        /// <summary>Return total content height for scrolling.</summary>
        private int GetTotalContentHeight()
        {
            SpriteFont font = Game1.dialogueFont;

            // titles
            string unmadeTitle = (I18n.Crafting_Uncooked().Length > I18n.Crafting_Uncrafted().Length) 
                ? I18n.Crafting_Uncooked()
                : I18n.Crafting_Uncrafted();
            string ingredientTitle = I18n.Crafting_RequiredIngredients();

            Vector2 unmadeTitleSize = font.MeasureString(unmadeTitle);
            Vector2 ingredientTitleSize = font.MeasureString(ingredientTitle);
            double titleHeight = TitleScale * (unmadeTitleSize.Y + ingredientTitleSize.Y);

            // paddings
            double paddingHeight = 10f * 3 + 20f;

            // recipes
            string recipeStr = ConcatStrings(UnmadeListObj.UnmadeRecipes);

            Vector2 recipeSize = font.MeasureString(recipeStr);
            double recipeHeight = recipeSize.Y;

            // ingredients
            Vector2 singleIngredientSize = font.MeasureString("a");
            double ingredientHeight = singleIngredientSize.Y * UnmadeListObj.RequiredIngredients.Count;

            // rounded total height
            int totalHeight = (int)Math.Ceiling(titleHeight + paddingHeight + recipeHeight + ingredientHeight);
            return totalHeight;
        }

        /// <summary>Updates the scrollbar handle size and position based on the current scroll state.</summary>
        private void UpdateScrollbarHandle()
        {
            if (ContentHeight <= ScrollArea.Height)
            {
                // content fits within the ScrollArea; no need for a scrollbar
                ScrollbarHandle = Rectangle.Empty;
                return;
            }

            // calculate the ratio of visible content
            float viewRatio = (float)ScrollArea.Height / ContentHeight;
            int handleHeight = (int)(ScrollbarTrack.Height * viewRatio);
            handleHeight = Math.Max(handleHeight, MinHandleHeight); // ensure minimum height

            // calculate the maximum scroll offset
            int maxScrollY = ContentHeight - ScrollArea.Height;

            // calculate the scroll percentage
            float scrollPercent = (float)CurrentScrollY / maxScrollY;
            scrollPercent = MathHelper.Clamp(scrollPercent, 0f, 1f);

            // calculate handle position
            int handleY = ScrollbarTrack.Y + (int)((ScrollbarTrack.Height - handleHeight) * scrollPercent);

            // set the scrollbar handle rectangle
            ScrollbarHandle = new Rectangle(
                ScrollbarTrack.X,
                handleY,
                ScrollbarTrack.Width,
                handleHeight
            );
        }

        /// <summary>Updates the current scroll position based on scrollbar handle movement.</summary>
        /// <param name="handleY">The new Y position of the scrollbar handle.</param>
        private void UpdateScrollFromHandle(int handleY)
        {
            if (ScrollbarHandle.Height == 0)
                return;

            // calculate the scroll percentage based on handle position
            float scrollPercent = (float)(handleY - ScrollbarTrack.Y) / (ScrollbarTrack.Height - ScrollbarHandle.Height);
            scrollPercent = MathHelper.Clamp(scrollPercent, 0f, 1f);

            // update CurrentScrollY based on scroll percentage
            int maxScrollY = ContentHeight - ScrollArea.Height;
            CurrentScrollY = (int)(scrollPercent * maxScrollY);

            // update scrollbar handle position
            UpdateScrollbarHandle();
        }

        /// <summary>Draws the vertical scrollbar track and handle.</summary>
        private void DrawScrollBar(SpriteBatch b)
        {
            // if content fits within the ScrollArea, no need to draw scrollbar
            if (ContentHeight <= ScrollArea.Height)
                return;

            // draw the scrollbar track
            b.Draw(
                Game1.fadeToBlackRect,  // Texture (a solid color)
                ScrollbarTrack,         // Position and size
                Color.Gray * 0.5f       // Color with transparency
            );

            // draw the scrollbar handle
            if (ScrollbarHandle != Rectangle.Empty)
            {
                b.Draw(
                    Game1.fadeToBlackRect,  // Texture (a solid color)
                    ScrollbarHandle,        // Position and size
                    Color.SaddleBrown       // Handle color
                );
            }
        }
    }
}
