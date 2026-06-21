using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

public class FileController
{
    VisualElement fileExplorer;
    string selectedDir;
    TextField fileName;
    Action<string> onFileSelected;
    public FileController(VisualElement parent, VisualTreeAsset doc, Action<string> onFileSelected, string rootPath) 
    {
        selectedDir = rootPath;
        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }
        this.onFileSelected = onFileSelected;
        fileExplorer = doc.Instantiate();
        parent.Add(fileExplorer);

        var tree = fileExplorer.Q<TreeView>("TreeView");

        tree.makeItem = () =>
        {
            var l = new Label();
            l.style.height = tree.fixedItemHeight;
            return l;
        };
        tree.bindItem = (e, i) =>
        {
            var item = tree.GetItemDataForIndex<FileObject>(i);
            ((Label)e).text = $"{item.displayName}";
        };
        tree.selectionType = SelectionType.Single;
        tree.selectedIndicesChanged += (selectedIndices) => OnSelect(tree, selectedIndices);

        int i = 1;
        tree.SetRootItems(new List<TreeViewItemData<FileObject>>() { new TreeViewItemData<FileObject>(0, new FileObject(rootPath, FileObjectType.Directory), GetFileStructure(rootPath, ref i)) });
        tree.Rebuild();

        var select = fileExplorer.Q<Button>("Select");
        select.RegisterCallback<ClickEvent>(OnSave);
        fileExplorer.Q<Button>("Close").RegisterCallback<ClickEvent>(OnClose);

        fileName = fileExplorer.Q<TextField>("FileName");
        fileName.RegisterValueChangedCallback<string>((s) =>
        {
            select.SetEnabled(!string.IsNullOrWhiteSpace(s.newValue));
        });
    }

    List<TreeViewItemData<FileObject>> GetFileStructure(string rootPath, ref int startIndex)
    {
        var res = new List<TreeViewItemData<FileObject>>();
        var dirs = Directory.GetDirectories(rootPath);
        foreach ( var dir in dirs )
        {
            res.Add(new TreeViewItemData<FileObject>(startIndex++, new FileObject(dir, FileObjectType.Directory), GetFileStructure(dir, ref startIndex)));
        }

        var files = Directory.GetFiles(rootPath);
        foreach( var file in files.Where(f => f.EndsWith(".json")))
        {
            res.Add(new TreeViewItemData<FileObject>(startIndex++, new FileObject(file, FileObjectType.File)));
        }
        return res;
    }

    void OnSelect(TreeView tree, IEnumerable<int> indecies)
    {
        if (!indecies.Any())
        {
            return;
        }
        var item = tree.GetItemDataForIndex<FileObject>(indecies.First());
        if(item.type == FileObjectType.Directory)
        {
            fileName.value = "";
            selectedDir = item.name;
        }
        else
        {
            fileName.value = item.displayName;
            selectedDir = Path.GetDirectoryName(item.name);
        }
        fileName.Focus();
    }

    void OnSave(ClickEvent _)
    {
        var selectedName = Path.Combine(selectedDir, Path.GetFileNameWithoutExtension(fileName.value) + ".json");
        if(!File.Exists(selectedName))
        {
            if(!Directory.Exists(Path.GetDirectoryName(selectedName)))
            { 
                Directory.CreateDirectory(Path.GetDirectoryName(selectedName));
            }
            File.Create(selectedName);
        }
        onFileSelected(selectedName);
        OnClose(null);
    }

    void OnClose(ClickEvent _)
    {
        fileExplorer.RemoveFromHierarchy();
    }

    enum FileObjectType
    {
        File,
        Directory
    }

    struct FileObject
    {
        public string name;
        public string displayName => Path.GetFileNameWithoutExtension(name);
        public FileObjectType type;

        public FileObject(string name, FileObjectType type)
        {
            this.name = name;
            this.type = type;
        }
    }
}
