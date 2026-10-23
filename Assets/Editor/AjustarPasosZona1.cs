#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class AjustarPasosZona1
{
    [MenuItem("Not Home/Zona 1/Ajustar pasos estrechos")]
    public static void Ajustar()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (escena.name != "Zona 1")
        {
            Debug.LogWarning("Abri la escena Zona 1 antes de ajustar los pasos.");
            return;
        }

        GameObject objeto = GameObject.Find("Zona1_Modulo_2x2_Tilemap");
        Tilemap tilemap = objeto != null ? objeto.GetComponent<Tilemap>() : null;
        if (tilemap == null)
        {
            Debug.LogError("No se encontro el Tilemap principal de Zona 1.");
            return;
        }

        // Libera una fila de techo en los dos corredores que tenían menos
        // altura que el CapsuleCollider2D del jugador.
        Vector3Int[] celdas =
        {
            new Vector3Int(-8, -10, 0),
            new Vector3Int(-7, -10, 0),
            new Vector3Int(-17, -22, 0),
            new Vector3Int(-15, -22, 0)
        };

        Undo.RegisterCompleteObjectUndo(tilemap, "Ajustar pasos estrechos Zona 1");
        int eliminadas = 0;
        foreach (Vector3Int celda in celdas)
        {
            if (!tilemap.HasTile(celda)) continue;
            tilemap.SetTile(celda, null);
            eliminadas++;
        }

        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log($"Zona 1: se liberaron {eliminadas} bloques de techo en pasos estrechos.");
    }
}
#endif
