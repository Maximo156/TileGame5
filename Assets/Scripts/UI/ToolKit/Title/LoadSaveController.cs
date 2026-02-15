using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Animation;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class LoadSaveController
{
    Button back;
    Button newSave;

    List<WorldSave> saves;
    ListView worldList;

    public LoadSaveController(VisualElement mainScreen, Action viewMain, Action viewNew, VisualTreeAsset listElementTemplate)
    {
        back = mainScreen.Q<Button>("Back");
        back.clicked += viewMain;

        newSave = mainScreen.Q<Button>("NewSave");
        newSave.clicked += viewNew;

        worldList = mainScreen.Q<ListView>("ListView");

        UpdateList();
        FillList(listElementTemplate);
    }

    void FillList(VisualTreeAsset listElementTemplate)
    {
        worldList.itemsSource = saves;

        worldList.makeItem = () =>
        {
            var newEntry = listElementTemplate.Instantiate();
            var newListEntryLogic = new ListEntryController();
            newEntry.userData = newListEntryLogic;
            newListEntryLogic.SetVisualElement(newEntry, UpdateList);
            return newEntry;
        };

        worldList.bindItem = (item, index) =>
        {
            (item.userData as ListEntryController)?.SetSave(saves[index]);
        };

        worldList.makeNoneElement = () =>
        {
            var label = new Label();
            label.text = "No saves found";
            return label;
        };
    }

    public void UpdateList()
    {
        saves = WorldSave.LoadSaves();
        worldList.itemsSource = saves;
    }

    class ListEntryController
    {
        WorldSave save;
        VisualElement entry;
        Action updateList;

        public void SetVisualElement(VisualElement visualElement, Action updateList)
        {
            entry = visualElement;
            var playSave = visualElement.Q<Button>("PlaySave");
            playSave.clicked += Play;
            this.updateList = updateList;
            var delete = visualElement.Q<Button>("Delete");
            delete.clicked += Delete;
        }

        public void SetSave(WorldSave save)
        {
            this.save = save;
            entry.dataSource = save;
        }

        void Play()
        {
            WorldSave.PlaySave(save);
        }

        void Delete()
        {
            save.Delete();
            updateList.Invoke();
        }
    }
}