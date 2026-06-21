using ComposableBlocks;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.UIElements;

public class StructureBlockDisplay : InteractiveDislay
{
#if UNITY_EDITOR
    readonly string rootDir = Application.dataPath;
#else
    readonly string rootDir = Application.persistentDataPath;
#endif

    public VisualTreeAsset FileController;
    public override bool OpenInv => false;

    IntegerField xInput;
    IntegerField yInput;
    Button showButton;
    Button fileButton;
    Button saveButton;
    Button loadButton;

    private StructureBehaviourState _attachedState;
    private VisualElement panel;
    private VisualElement root;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q<VisualElement>("Panel");
        panel.style.visibility = Visibility.Hidden;

        xInput = panel.Q<IntegerField>("xInput");
        xInput.RegisterValueChangedCallback(UpdateX);

        yInput = panel.Q<IntegerField>("yInput");
        yInput.RegisterValueChangedCallback(UpdateY);

        showButton = panel.Q<Button>("Toggle");
        showButton.RegisterCallback<ClickEvent>(ToggleVisual);

        fileButton = panel.Q<Button>("File");
        fileButton.RegisterCallback<ClickEvent>(RenderFileSelector);

        saveButton = panel.Q<Button>("Save");
        saveButton.RegisterCallback<ClickEvent>(Save);

        loadButton = panel.Q<Button>("Load");
        loadButton.RegisterCallback<ClickEvent>(Load);

        panel.Q<Button>("Clear").RegisterCallback<ClickEvent>(Clear);
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
        return typeof(StructureBlockBehaviour);
    }

    public override void InitDisplay(Vector2Int worldPos, Block _, BlockState state, byte simpleState, IInventoryContainer otherInventory)
    {
        _attachedState = state.GetState<StructureBehaviourState>();
        _attachedState.SetPos(worldPos);
        _attachedState.ToggleVisuals(true);

        UpdateBounds();
        showButton.text = _attachedState.VisualShown() ? "Hide" : "Show";
        bool hasLoc = !string.IsNullOrWhiteSpace(_attachedState.FileSaveLocation);
        fileButton.text = !hasLoc ? "Select Save..." : CalcFileLocationText(_attachedState.FileSaveLocation);
        saveButton.SetEnabled(hasLoc);
        loadButton.SetEnabled(hasLoc);
    }

    void UpdateBounds()
    {
        xInput.value = _attachedState.Size.x;
        yInput.value = _attachedState.Size.y;
    }

    public override void Detach()
    {
    }

    public void Save(ClickEvent _) 
    {
        var component = new StructureComponent(_attachedState.Size);
        int i = 0;
        foreach(var pos in _attachedState.GetBounds().allPositionsWithin)
        {
            var b = ChunkManager.GetBlock(pos.ToVector2Int(), false);
            component.SetSlice(b, i++);
        }
        var json = JsonConvert.SerializeObject(component, DefaultJsonSettings.settings);
        File.WriteAllText(_attachedState.FileSaveLocation, json);
    }

    public void Load(ClickEvent _)
    {
        var json = File.ReadAllText(_attachedState.FileSaveLocation);
        var component = JsonConvert.DeserializeObject<StructureComponent>(json);
        _attachedState.Size = component.size;
        UpdateBounds();
        Clear(null);
        int i = 0;
        foreach (var pos in _attachedState.GetBounds().allPositionsWithin)
        {
            var slice = component.GetSlice(i++);
            var p = pos.ToVector2Int();
            ChunkManager.SetSlice(p, slice);
        }
    }

    public void UpdateX(ChangeEvent<int> changeEvent)
    {
        _attachedState.Size = new Vector2Int(changeEvent.newValue, _attachedState.Size.y);
    }

    public void UpdateY(ChangeEvent<int> changeEvent)
    {
        _attachedState.Size = new Vector2Int(_attachedState.Size.x, changeEvent.newValue);
    }

    public void ToggleVisual(ClickEvent _)
    {
        var curShown = _attachedState.VisualShown();
        _attachedState.ToggleVisuals(!curShown);
        showButton.text = !curShown ? "Hide" : "Show";
    }

    public void RenderFileSelector(ClickEvent _)
    {
        new FileController(root, FileController, SetFileLocation, Path.Combine(rootDir, "structures"));
    }

    void SetFileLocation(string loc)
    {
        _attachedState.FileSaveLocation = loc;
        fileButton.text = CalcFileLocationText(loc);
        saveButton.SetEnabled(true);
        loadButton.SetEnabled(true);
    }

    string  CalcFileLocationText(string dir)
    {
        return dir.Remove(0, Application.persistentDataPath.Length + 1);
    }

    void Clear(ClickEvent _)
    {
        foreach (var pos in _attachedState.GetBounds().allPositionsWithin)
        {
            var p = pos.ToVector2Int();
            ChunkManager.BreakBlock(p, true, false);
            ChunkManager.BreakBlock(p, false, false);
            ChunkManager.BreakBlock(p, false, false);
        }
    }
}