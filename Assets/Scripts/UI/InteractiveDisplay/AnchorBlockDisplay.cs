using ComposableBlocks;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class AnchorBlockDisplay : InteractiveDislay
{
    public override bool OpenInv => false;

    Vector2Int worldPos;
    private VisualElement panel;
    private IntegerField codeInput;
    private Toggle keyInput;
    private AnchorDirection curDur;

    private void OnEnable()
    {
        panel = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("Panel");
        panel.style.visibility = Visibility.Hidden;

        codeInput = panel.Q<IntegerField>("Code");
        codeInput.RegisterValueChangedCallback(OnCodeUpdated);

        keyInput = panel.Q<Toggle>("Key");
        keyInput.RegisterValueChangedCallback(OnKeyUpdated);
        panel.Q<Button>("Rotate").RegisterCallback<ClickEvent>(OnRotate);
    }

    public override void Reposition(Vector3 pos)
    {
        if (!float.IsNaN(panel.resolvedStyle.height))
        {
            // Only show panel once height is resolved
            panel.style.visibility = Visibility.Visible;
        }
        panel.style.left = pos.x;
        panel.style.bottom = pos.y - panel.resolvedStyle.height;
    }

    public override Type TypeMatch()
    {
        return typeof(AnchorBlockBehaviour);
    }

    public override void InitDisplay(Vector2Int worldPos, Block _, BlockState state, byte simpleState, IInventoryContainer otherInventory)
    {
        int code;
        bool key;
        (code, key, curDur) = AnchorBlockBehaviour.DecodeState(simpleState);
        codeInput.value = code;
        keyInput.value = key;
        this.worldPos = worldPos;
    }

    public override void Detach()
    {
    }

    void OnKeyUpdated(ChangeEvent<bool> e)
    {
        ChunkManager.SetSimpleState(worldPos, AnchorBlockBehaviour.GetState(e.newValue, codeInput.value, curDur));
    }

    void OnCodeUpdated(ChangeEvent<int> e)
    {
        if (e.newValue >= 32 || e.newValue < 0)
        {
            codeInput.value = e.previousValue;
            return;
        }
        ChunkManager.SetSimpleState(worldPos, AnchorBlockBehaviour.GetState(keyInput.value, e.newValue, curDur));
    }

    void OnRotate(ClickEvent _)
    {
        curDur = (AnchorDirection)(((int)curDur + 1) % 4);
        ChunkManager.SetSimpleState(worldPos, AnchorBlockBehaviour.GetState(keyInput.value, codeInput.value, curDur));
    }
}