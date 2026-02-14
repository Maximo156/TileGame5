using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSave
{
    static string persistentDataPath;
    static WorldSave _activeSave;
    static WorldSave ActiveSave { 
        get
        {
            if(_activeSave == null)
            {
                _activeSave = GetFirstSave();
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

    static string GetWorldDirectoryName(string worldName)
    {
        return Path.Join(BaseSavesPath, worldName);
    }

    static string GetWorldMetadataFileName(string worldName)
    {
        return Path.Join(GetWorldDirectoryName(worldName), META_DATA_FILE_NAME);
    }

    public static WorldSave CreateNewSave(string name, string seed)
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
            writer.Write(JsonUtility.ToJson(new Metadata() { seed = seed }));
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
        ActiveSave = save;
        SceneManager.LoadScene(1);
    }

    public static WorldSave GetFirstSave()
    {
        var saves = LoadSaves();
        if(saves.Count == 0)
        {
            return CreateNewSave("DEFAULT_SAVE", null);
        }
        else
        {
            return saves[0];
        }
    }

    public readonly uint seed;
    public readonly string worldName;

    public string DirectoryPath => GetWorldDirectoryName(worldName);

    WorldSave(string worldName)
    {
        var metadata = JsonUtility.FromJson<Metadata>(File.ReadAllText(GetWorldMetadataFileName(worldName)));
        seed = (uint)metadata.seed.GetHashCode();
        this.worldName = worldName;
    }

    public void Delete()
    {
        Directory.Delete(GetWorldDirectoryName(worldName), true);
    }

    struct Metadata
    {
        public string seed;
    }
}

public class WorldAlreadyExistsException : Exception
{

}
