using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

[CreateAssetMenu(fileName = "NewProcessingBlock", menuName = "Block/ProcessingBlock", order = 1)]
public class ProcessingBlock : Wall, IInterfaceBlock, IStatefulBlock
{
    [Header("Processing Info")]
    public int inputs = 1;
    public float fuelEfficiency = 1;
    public List<ItemRecipe> Recipes;
    public List<Item> Fuels;

    public BlockState GetState()
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

public class ProcessingBlockState : BlockState, ITickableState
{
    [JsonProperty]
    readonly ProcessingBlock block;
    public Inventory inputs;
    public Inventory outputs = new Inventory(1);
    public LimitedInventory fuels;

    [JsonConstructor]
    ProcessingBlockState(ProcessingBlock block, Inventory inputs, Inventory outputs, LimitedInventory fuels, float? timeLeft, float? curFuel, float? lastUsedFuel, int _curRecipeIndex) : this(block)
    {
        this.inputs = inputs;
        this.outputs = outputs;
        this.fuels = new LimitedInventory((i, _, _) => block.Fuels.Contains(i), 1);
        fuels.CopyToInventory(this.fuels);
        this.timeLeft = timeLeft;
        this.curFuel = curFuel;
        this.lastUsedFuel = lastUsedFuel;
        this._curRecipeIndex = _curRecipeIndex;
        lastTick = Utilities.CurMilli();

        inputs.OnItemChanged += InventoriesUpdate;
        fuels.OnItemChanged += InventoriesUpdate;
        outputs.OnItemChanged += InventoriesUpdate;
    }

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

    [JsonProperty]
    int _curRecipeIndex = -1;

    [JsonIgnore]
    public ItemRecipe curRecipe 
    { 
        get
        {
            return _curRecipeIndex == -1 ? null : block.Recipes[_curRecipeIndex];
        }
    }
    public void Tick()
    {
        var deltaTime = Utilities.CurMilli() - lastTick;
        if(timeLeft == null || timeLeft < 0)
        {
            if(curRecipe != null)
            {
                outputs.AddItem(new ItemStack(curRecipe.Result));
                curRecipe.UseRecipe(inputs);
                _curRecipeIndex = -1;
                timeLeft = null;
                TrigerSafeStateChange();
            }
            var potentialIndex = block.Recipes.FindIndex(r => r.CanProduce(inputs.GetAllItems()));
            if (potentialIndex != -1)
            {
                var potentialRecipe = block.Recipes[potentialIndex];
                if (CanProduce(potentialRecipe))
                {
                    _curRecipeIndex = potentialIndex;
                    timeLeft = potentialRecipe.craftingTime * 1000;
                    TrigerSafeStateChange();
                }
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
                _curRecipeIndex = -1;
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
                _curRecipeIndex = -1;
            }
        }
        TrigerSafeStateChange();
    }

    void TrigerSafeStateChange()
    {
        CallbackManager.AddCallback(() => TriggerStateChange());
    }
}
