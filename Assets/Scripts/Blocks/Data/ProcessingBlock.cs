using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewProcessingBlock", menuName = "Block/ProcessingBlock", order = 1)]
public class ProcessingBlock : Wall, ITickableBlock, IInterfaceBlock
{
    [Header("Processing Info")]
    public int inputs = 1;
    public float fuelEfficiency = 1;
    public List<ItemRecipe> Recipes;
    public List<Item> Fuels;

    public bool Tick(Vector2Int worlPosition, BlockSlice slice, System.Random rand)
    {
        (slice.State as ProcessingBlockState).Process();
        return false;
    }

    public override BlockState GetState()
    {
        return new ProcessingBlockState(this);
    }

    public void OnValidate()
    {
        if (Fuels.Any(f => f.BurnTime == 0))
        {
            Debug.LogWarning("Added fuel has 0 burn time, removing");
            Fuels = Fuels.Where(f => f.BurnTime > 0).ToList();
        }
    }
}

public class ProcessingBlockState : BlockState
{
    public delegate void StateChange(ProcessingBlockState state);
    public event StateChange OnStateChange;

    readonly ProcessingBlock block;
    public Inventory inputs;
    public Inventory outputs = new Inventory(1);
    public LimitedInventory fuels;

    public ProcessingBlockState(ProcessingBlock block)
    {
        this.block = block;
        fuels = new LimitedInventory((i, _, _) => block.Fuels.Contains(i), 1);
        inputs = new Inventory(block.inputs);
        inputs.OnItemChanged += InventoriesUpdate;
        fuels.OnItemChanged += InventoriesUpdate;
        outputs.OnItemChanged += InventoriesUpdate;
    }

    public override void CleanUp(Vector2Int worldPos)
    {
        inputs.OnItemChanged -= InventoriesUpdate;
        fuels.OnItemChanged -= InventoriesUpdate;
        outputs.OnItemChanged -= InventoriesUpdate;

        Utilities.DropItems(worldPos, inputs.GetAllItems(false));
        Utilities.DropItems(worldPos, fuels.GetAllItems(false));
        Utilities.DropItems(worldPos, outputs.GetAllItems(false));
    }

    long lastTick;
    public float? timeLeft { get; private set; } = null;
    public float? curFuel { get; private set; } = null;
    public float? lastUsedFuel { get; private set; } = null;
    public ItemRecipe curRecipe { get; private set; }
    public void Process()
    {
        var deltaTime = Utilities.CurMilli() - lastTick;
        if(timeLeft == null || timeLeft < 0)
        {
            if(curRecipe != null)
            {
                outputs.AddItem(new ItemStack(curRecipe.Result));
                curRecipe.UseRecipe(inputs);
                curRecipe = null;
                timeLeft = null;
                TrigerSafeStateChange();
            }
            var potentialRecipe = block.Recipes.FirstOrDefault(r => r.CanProduce(inputs.GetAllItems()));
            if (CanProduce(potentialRecipe))
            {
                curRecipe = potentialRecipe;
                timeLeft = potentialRecipe.craftingTime * 1000;
                TrigerSafeStateChange();
            }
        }
        if((curFuel ?? 0) <= 0 && curRecipe is not null)
        {
            var fuelStack = fuels.CheckSlot(0);
            if (fuelStack != null)
            {
                var fuel = new ItemStack(fuelStack.Item, 1);
                fuels.RemoveItem(fuel);
                curFuel = fuel.Item.BurnTime * 1000 * block.fuelEfficiency;
                lastUsedFuel = curFuel;
                TrigerSafeStateChange();
            }
            else
            {
                timeLeft = null;
                curRecipe = null;
                TrigerSafeStateChange();
            }
        }
        
        if(curFuel >= 0)
        {
            curFuel -= deltaTime;
            if(timeLeft != null)
            {
                timeLeft -= deltaTime;
            }
            TrigerSafeStateChange();
        }

        lastTick = Utilities.CurMilli();
    }

    bool CanProduce(ItemRecipe recipe)
    {
        return recipe != null && recipe.CanProduce(inputs.GetAllItems()) && outputs.CanAddSlot(recipe.Result, 0) && (curFuel > 0 || fuels.GetAllItems(false).Any());
    }

    void InventoriesUpdate(Inventory _)
    {
        if(curRecipe != null)
        {
            if (!CanProduce(curRecipe))
            {
                timeLeft = null;
                curRecipe = null;
                TrigerSafeStateChange();
            }
        }
    }

    void TrigerSafeStateChange()
    {
        CallbackManager.AddCallback(() => OnStateChange?.Invoke(this));
    }
}
