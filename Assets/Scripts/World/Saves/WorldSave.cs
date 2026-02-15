using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSave
{
    static string persistentDataPath;
    static WorldSave _activeSave;
    public static WorldSave ActiveSave { 
        get
        {
            if(_activeSave == null)
            {
                _activeSave = GetFirstSave();
                Debug.Log("Returning default");
            }
            return _activeSave;
        }
        set 
        {
            _activeSave = value;
        }
    }

    public static uint ActiveSeed => ActiveSave.seed;
    public static string ActiveSaveDirectoryPath => ActiveSave.DirectoryPath;

    const string META_DATA_FILE_NAME = "metadata";
    static string BaseSavesPath => Path.Join(persistentDataPath, "saves");

    static WorldSave()
    {
        persistentDataPath = Application.persistentDataPath;
    }

    static public void SaveSimple<T>(string path, T save)
    {
        var completePath = Path.Combine(ActiveSaveDirectoryPath, path);
        var dir = Path.GetDirectoryName(completePath);
        if(!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var json = JsonConvert.SerializeObject(save, DefaultJsonSettings.settings);
        File.WriteAllText(completePath, json);
    }

    static public T LoadSimple<T>(string path)
    {
        try
        {
            var text = File.ReadAllText(Path.Combine(ActiveSaveDirectoryPath, path));
            return JsonConvert.DeserializeObject<T>(text, DefaultJsonSettings.settings);
        } catch
        {
            return default(T);
        }
    }

    static string GetWorldDirectoryName(string worldName)
    {
        return Path.Join(BaseSavesPath, worldName);
    }

    static string GetWorldMetadataFileName(string worldName)
    {
        return Path.Join(GetWorldDirectoryName(worldName), META_DATA_FILE_NAME);
    }

    public static WorldSave CreateNewSave(string name, string seed, bool persistPlayer)
    {
        if(string.IsNullOrWhiteSpace(seed))
        {
            seed = RandomStringGenerator.GenerateRandomString(20);
        }
        var baseWorldDir = GetWorldDirectoryName(name);
        if(Directory.Exists(baseWorldDir))
        {
            throw new WorldAlreadyExistsException();
        }
        Directory.CreateDirectory(baseWorldDir);
        var metadataFileName = GetWorldMetadataFileName(name);
        
        using(var metaFile = File.Create(metadataFileName))
        using (StreamWriter writer = new StreamWriter(metaFile))
        {
            writer.Write(JsonUtility.ToJson(new Metadata() { seed = seed, persistPlayer = persistPlayer }));
        }

        return new WorldSave(name);
    }

    public static List<WorldSave> LoadSaves()
    {
        var saves = new List<WorldSave>();
        foreach(var d in Directory.GetDirectories(BaseSavesPath)) 
        {
            try
            {
                saves.Add(new WorldSave(Path.GetFileName(d)));
            } 
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
        return saves;
    }

    public static void PlaySave(WorldSave save)
    {
        if(save == null)
        {
            throw new ArgumentNullException("save");
        }

        ActiveSave = save;
        SceneManager.LoadScene(1);
    }

    public static async void ExitSave()
    {
        await SceneManager.LoadSceneAsync(0);
        ActiveSave = null;
    } 

    public static WorldSave GetFirstSave()
    {
        var name = WorldSaveSelect.SelectedSaveName;
        var selectedSave = LoadSaves().FirstOrDefault(s => s.worldName == name);
        if(selectedSave == null)
        {
            return CreateNewSave(string.IsNullOrWhiteSpace(name) ? "DEFAULT_SAVE" : name, null, false);
        }
        else
        {
            return selectedSave;
        }
    }

    public readonly uint seed;
    public readonly string worldName;
    public bool persistPlayer;

    public string DirectoryPath => GetWorldDirectoryName(worldName);

    WorldSave(string worldName)
    {
        var metadata = JsonUtility.FromJson<Metadata>(File.ReadAllText(GetWorldMetadataFileName(worldName)));
        seed = (uint)metadata.seed.GetHashCode();
        persistPlayer = metadata.persistPlayer;
        this.worldName = worldName;
    }

    public void Delete()
    {
        Directory.Delete(GetWorldDirectoryName(worldName), true);
    }

    struct Metadata
    {
        public string seed;
        public bool persistPlayer;
    }
}

public class WorldAlreadyExistsException : Exception
{

}
