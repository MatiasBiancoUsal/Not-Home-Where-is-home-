using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RestoreZona3TilePalette
{
    private const string Folder = "Assets/-------DISENO NIVELES/Zona 3/Tilemap Piedra/";
    private const string PalettePath = Folder + "New Tile Palette.prefab";

    [MenuItem("Tools/Restaurar paleta de Zona 3")]
    public static void Restore()
    {
        var fitLeft = GetOrCreateTile(
            Folder + "plataforma_encastre_borde_izquierdo.asset",
            Folder + "plataforma_encastre_borde_izquierdo.png");
        var fitCenter = GetOrCreateTile(
            Folder + "plataforma_encastre_centro.asset",
            Folder + "plataforma_encastre_centro.png");
        var fitRight = GetOrCreateTile(
            Folder + "plataforma_encastre_borde_derecho.asset",
            Folder + "plataforma_encastre_borde_derecho.png");

        if (fitLeft == null || fitCenter == null || fitRight == null)
        {
            Debug.LogError("No se pudo restaurar la paleta de Zona 3 porque falta una de sus cuatro piezas.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(PalettePath);
        try
        {
            var tilemap = root.GetComponentInChildren<Tilemap>(true);
            if (tilemap == null)
            {
                Debug.LogError("La paleta de Zona 3 no contiene un Tilemap.");
                return;
            }

            tilemap.ClearAllTiles();
            tilemap.SetTile(new Vector3Int(0, 0, 0), fitLeft);
            tilemap.SetTile(new Vector3Int(3, 0, 0), fitCenter);
            tilemap.SetTile(new Vector3Int(6, 0, 0), fitRight);

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Paleta de Zona 3 restaurada, incluyendo la plataforma de encastre dividida en tres piezas.");
    }

    private static Tile GetOrCreateTile(string tilePath, string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null &&
            (importer.textureType != TextureImporterType.Sprite ||
             importer.spriteImportMode != SpriteImportMode.Single ||
             importer.spritePixelsPerUnit != 256))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Sprite sprite = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
        {
            if (asset is Sprite found)
            {
                sprite = found;
                break;
            }
        }

        if (sprite == null)
            return null;

        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = System.IO.Path.GetFileNameWithoutExtension(tilePath);
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
