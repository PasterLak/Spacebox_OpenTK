﻿using Spacebox.Common;
using Spacebox.Game.GUI;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class CrusherBlock : InteractiveBlock
    {
        private Recipe Recipe;

        public Storage InputStorage { get; private set; } = new Storage(1, 1);
        public Storage FuelStorage { get; private set; } = new Storage(1, 1);
        public Storage OutputStorage { get; private set; } = new Storage(1, 1);

        public bool IsRunning { get; private set; } = false;
        public CrusherBlock(BlockData blockData) : base(blockData)
        {
            OnUse += CrusherGUI.Toggle;

        }

        public override void Use()
        {
            CrusherGUI.Activate(this);
            base.Use();
        }

        public bool HasInput()
        {
            return InputStorage.GetSlot(0, 0).Count > 0;
        }

        public bool TryStartTask(out CrusherTask task)
        {
            task = null;

            if (!HasInput()) return false;
            if (IsRunning) return false;

            if (GameBlocks.TryGetRecipe("crusher", InputStorage.GetSlot(0, 0).Item.Id, out Recipe))
            {

            }
            else return false;


            if (Recipe == null) return false;

            IsRunning = true;
            task = new CrusherTask(Recipe.RequiredTicks, this);

            return true;
        }

        public void Craft()
        {

            if (Recipe == null)
            {
                IsRunning = false;
                return;
            }

            var inSlot = InputStorage.GetSlot(0, 0);
            var outSlot = OutputStorage.GetSlot(0, 0);

            if (inSlot.HasItem && inSlot.Item.Id == Recipe.Ingredient.Item.Id)
            {
                if (outSlot.HasItem)
                {
                    if (outSlot.Item.Id == Recipe.Product.Item.Id)
                    {
                        if (outSlot.HasFreeSpace)
                        {

                            inSlot.Count -= Recipe.Ingredient.Quantity;

                            if (outSlot.HasItem)
                            {
                                outSlot.Count += Recipe.Product.Quantity;
                            }

                        }
                    }

                }
                else
                {

                    var newInSlotCount = inSlot.Count - Recipe.Ingredient.Quantity;

                    if (newInSlotCount >= 0)
                    {
                        inSlot.Count = (byte)newInSlotCount;  
                        OutputStorage.TryAddItem(Recipe.Product.Item, Recipe.Product.Quantity);
                    }

                }


            }

            if (!HasInput()) IsRunning = false;
        }
    }
}
