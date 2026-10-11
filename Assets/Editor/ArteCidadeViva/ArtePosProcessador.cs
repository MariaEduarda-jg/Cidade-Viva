using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Modelos que mantêm as cores do autor (personagens, bichos, veículos do Quaternius etc.)
    // não passam pela paleta, mas ganham o mesmo acabamento fosco dela ao serem importados.
    public class ArtePosProcessador : AssetPostprocessor
    {
        // Pacotes cujo FBX traz a cor em espaço linear: sem a correção ficam escuros demais no Unity.
        static readonly string[] KitsComCorLinear = { };

        void OnPostprocessMaterial(Material m)
        {
            if (!assetPath.StartsWith("Assets/Art/")) return;
            Fosco(m);
            foreach (var kit in KitsComCorLinear)
                if (assetPath.Contains("/" + kit + "/") && m.HasProperty("_BaseColor"))
                {
                    var c = m.GetColor("_BaseColor");
                    m.SetColor("_BaseColor", new Color(c.gamma.r, c.gamma.g, c.gamma.b, c.a));
                }
        }

        public static void Fosco(Material m)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_SpecularHighlights"))
            {
                m.SetFloat("_SpecularHighlights", 0f);
                m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            if (m.HasProperty("_EnvironmentReflections"))
            {
                m.SetFloat("_EnvironmentReflections", 0f);
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
        }
    }
}
