using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScreenController
{
    Button newSave;
    Button loadSave;

    public MainScreenController(VisualElement mainScreen, Action viewNew, Action viewLoad)
    {
        newSave = mainScreen.Q<Button>("NewSave");
        loadSave = mainScreen.Q<Button>("LoadSave");

        newSave.clicked += viewNew;
        loadSave.clicked += viewLoad;
    }
}
