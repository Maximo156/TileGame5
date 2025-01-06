using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor.U2D.Sprites;

public class OreSpriteWizardTile : ScriptableWizard
{
    public string OreName = "name";
    public Color OreColor = Color.white;

    Color Target = new Color(1, 0, 1);

    [MenuItem("Assets/Create/OreSprite/Generate Ore Sprite Set...")]
    static void CreateWizard()
    {
        DisplayWizard("Generate Ore Sprite Set", typeof(OreSpriteWizardTile));
    }

    private void OnWizardCreate()
    {
        var baseResourcePath = "Sprites/Blocks/OreBlocks";
        var basePath = $"Assets/Resources/{baseResourcePath}.png";
        var baseSprite = Resources.Load<Sprite>(baseResourcePath);

        var changed = CopyTexture2D(baseSprite.texture);

        byte[] itemBGBytes = changed.EncodeToPNG();
        string path = $"Assets/Resources/Sprites/Blocks/{OreName}Blocks.png";
        File.WriteAllBytes(path, itemBGBytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        //AssetDatabase.ImportAsset(basePath, ImportAssetOptions.ForceUpdate);
        var baseImporter = (TextureImporter)AssetImporter.GetAtPath(basePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = baseImporter.textureType;
        importer.spriteImportMode = baseImporter.spriteImportMode;
        importer.spritePixelsPerUnit = baseImporter.spritePixelsPerUnit;
        importer.filterMode = baseImporter.filterMode;
        importer.textureCompression = baseImporter.textureCompression;
        var textSettings = new TextureImporterSettings();
        baseImporter.ReadTextureSettings(textSettings);
        importer.SetTextureSettings(textSettings);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        EditorUtility.SetDirty(importer);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var baseDataProvider = factory.GetSpriteEditorDataProviderFromObject(baseImporter);
        baseDataProvider.InitSpriteEditorDataProvider();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var origRects = baseDataProvider.GetSpriteRects();

        foreach(var rect in origRects)
        {
            rect.name = rect.name.Replace("Ore", OreName);
        }

        dataProvider.SetSpriteRects(origRects);

        dataProvider.Apply();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
    public Texture2D CopyTexture2D(Texture2D copiedTexture)
    {
        //Create a new Texture2D, which will be the copy.
        Texture2D texture = new Texture2D(copiedTexture.width, copiedTexture.height);
        //Choose your filtermode and wrapmode here.
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for(int x = 0; x < texture.width; x++)
        {
            for(int y = 0; y < texture.height; y++)
            {
                var curColor = copiedTexture.GetPixel(x, y);
                if (curColor == Target)
                {
                    //This line of code and if statement, turn Green pixels into Red pixels.
                    texture.SetPixel(x, y, OreColor);
                }
                else
                {
                    texture.SetPixel(x, y, curColor);
                }
            }
        }
        //Name the texture, if you want.
        texture.name = (OreName + "Block");

        //This finalizes it. If you want to edit it still, do it before you finish with .Apply(). Do NOT expect to edit the image after you have applied. It did NOT work for me to edit it after this function.
        texture.Apply();

        //Return the variable, so you have it to assign to a permanent variable and so you can use it.
        return texture;
    }
}
