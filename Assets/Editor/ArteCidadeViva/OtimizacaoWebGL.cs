using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CidadeViva.EditorArte
{
    // Alívios pro WebGL (computadores de escola), usados pelos construtores:
    //  - Combinar: junta as malhas de um objeto montado com formas (ex.: a Catedral, que tinha 293 pedaços)
    //    numa malha por material, salva em Assets/Art/Malhas/Combinadas;
    //  - TirarSombraDosPequenos: objetos pequenos (menos de 1,2 m) deixam de projetar sombra
    //    (quase não se nota, e cada um custava uma chamada de desenho a mais na sombra).
    public static class OtimizacaoWebGL
    {
        const string PastaDasMalhas = "Assets/Art/Malhas/Combinadas";
        public const float TamanhoPequeno = 1.2f;

        public static void Combinar(Transform raiz, string nome)
        {
            ArtePaleta.GarantirPasta(PastaDasMalhas);
            var grupos = new Dictionary<Material, List<CombineInstance>>();
            var paraApagar = new List<GameObject>();
            Matrix4x4 base_ = raiz.worldToLocalMatrix;
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    Material mat = mr.sharedMaterials[Mathf.Min(s, mr.sharedMaterials.Length - 1)];
                    if (!grupos.TryGetValue(mat, out var lista)) grupos[mat] = lista = new List<CombineInstance>();
                    lista.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = base_ * mf.transform.localToWorldMatrix });
                }
                paraApagar.Add(mf.gameObject);
            }
            foreach (var go in paraApagar)
            {
                if (go == raiz.gameObject) { Object.DestroyImmediate(go.GetComponent<MeshRenderer>()); Object.DestroyImmediate(go.GetComponent<MeshFilter>()); }
                else if (go != null) Object.DestroyImmediate(go);
            }
            // Grupos vazios que sobraram.
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>().Reverse())
                if (t != raiz && t.childCount == 0 && t.GetComponents<Component>().Length == 1) Object.DestroyImmediate(t.gameObject);

            foreach (var g in grupos)
            {
                var malha = new Mesh { name = $"{nome}_{g.Key.name}", indexFormat = IndexFormat.UInt32 };
                malha.CombineMeshes(g.Value.ToArray(), true, true);
                malha.RecalculateBounds();
                string caminho = $"{PastaDasMalhas}/{malha.name}.asset";
                AssetDatabase.DeleteAsset(caminho);
                AssetDatabase.CreateAsset(malha, caminho);
                var go = new GameObject("Combinado_" + g.Key.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(raiz, false);
                go.GetComponent<MeshFilter>().sharedMesh = malha;
                go.GetComponent<MeshRenderer>().sharedMaterial = g.Key;
            }
        }

        // Na cena aberta.
        public static int TirarSombraDosPequenos()
        {
            int n = 0;
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r is ParticleSystemRenderer || r.shadowCastingMode == ShadowCastingMode.Off) continue;
                if (r.bounds.size.magnitude >= TamanhoPequeno) continue;
                r.shadowCastingMode = ShadowCastingMode.Off;
                EditorUtility.SetDirty(r);
                n++;
            }
            return n;
        }
    }
}
