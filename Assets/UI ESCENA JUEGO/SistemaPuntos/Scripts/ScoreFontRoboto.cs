using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

[RequireComponent(typeof(TMP_Text))]
public class ScoreFontRoboto : MonoBehaviour
{
    [SerializeField] private Font roboto;
    private static TMP_FontAsset fuenteRuntime;

    private void Awake()
    {
        if (roboto == null) return;

        if (fuenteRuntime == null)
        {
            fuenteRuntime = TMP_FontAsset.CreateFontAsset(
                roboto, 90, 9, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic, true);

            if (fuenteRuntime != null) fuenteRuntime.name = "Roboto-Regular SDF Runtime";
        }

        TMP_Text texto = GetComponent<TMP_Text>();
        if (texto != null && fuenteRuntime != null)
        {
            texto.font = fuenteRuntime;
            texto.ForceMeshUpdate(true, true);
        }
    }
}
