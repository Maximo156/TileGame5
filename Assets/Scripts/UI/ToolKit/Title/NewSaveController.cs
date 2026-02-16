using System;
using System.Text.RegularExpressions;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;

public class NewSaveController
{
    Button back;
    Button create;

    public NewSaveSettings settings;

    public NewSaveController(VisualElement mainScreen, Action viewMain, Action viewLoad) 
    {
        back = mainScreen.Q<Button>("Back");
        back.clicked += viewMain;

        create = mainScreen.Q<Button>("Create");
        create.clicked += OnCreate;

        settings = new NewSaveSettings();
        mainScreen.dataSource = settings;
    }

    void OnCreate()
    {
        try
        {
            var newSave = WorldSave.CreateNewSave(settings.Name, settings.seed);
            WorldSave.PlaySave(newSave);
        }
        catch (WorldAlreadyExistsException)
        {
            settings.error = true;
        }
    }

    public class NewSaveSettings
    {
        public static Regex NameRegex = new Regex(@"^[a-zA-Z0-9\-_ ]*$", RegexOptions.Compiled);
        [DontCreateProperty]
        string m_name;
        public string seed;
        public bool error = false;

        [CreateProperty]
        bool isValid => !string.IsNullOrWhiteSpace(Name);

        [CreateProperty]
        public string Name
        {
            get => m_name;
            set
            {
                if (NameRegex.IsMatch(value))
                {
                    m_name = value;
                    error = false;
                }
            }
        }
    }
}
