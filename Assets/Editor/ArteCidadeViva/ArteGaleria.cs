using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Cena de conferência: todos os prefabs de Assets/Prefabs/Art lado a lado, por categoria.
    // Não entra no Build Settings.
    public static class ArteGaleria
    {
        public const string CaminhoDaCena = "Assets/Scenes/Galeria_Assets.unity";
        static readonly string[] Ordem = { "Lixo_Reciclagem", "Props", "Personagens", "Natureza", "Enchente", "Carros", "Ruas", "Predios" };
        const float LarguraMaxima = 90f;

        public static void GerarCena()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var raiz = new GameObject("Galeria").transform;
            float z = 0f, zMin = 0f;
            foreach (var categoria in Ordem)
            {
                string pasta = $"{ArteImportador.PastaPrefabs}/{categoria}";
                if (!AssetDatabase.IsValidFolder(pasta)) continue;
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { pasta })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
                if (prefabs.Count == 0) continue;

                var grupo = new GameObject(categoria).transform;
                grupo.SetParent(raiz, false);

                // Mede tudo antes, pra saber a profundidade de cada fileira.
                var itens = new List<(GameObject go, Bounds b)>();
                foreach (var caminho in prefabs)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(caminho));
                    go.transform.SetParent(grupo, false);

                    // Quem tem esqueleto aparece na pose de "parado" em vez da pose de modelagem (braços abertos).
                    var animator = go.GetComponentInChildren<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        var parado = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.ToLowerInvariant() == "idle");
                        if (parado != null) parado.SampleAnimation(animator.gameObject, 0f);
                    }
                    itens.Add((go, Limites(go)));
                }
                float maiorLargura = itens.Max(i => i.b.size.x);
                float folga = Mathf.Clamp(maiorLargura * .25f, .5f, 4f);
                float escalaTexto = Mathf.Clamp(.3f * Mathf.Sqrt(maiorLargura), .3f, 1.6f);

                Texto(grupo, fonte, categoria.Replace('_', ' ').ToUpperInvariant(), new Vector3(0, .02f, z - escalaTexto * 2.2f), escalaTexto * 2.2f);
                z -= escalaTexto * 5f;

                // Cada item ocupa a própria largura ou a da etiqueta, o que for maior.
                var vagas = itens.Select(i => Mathf.Max(i.b.size.x + folga, i.go.name.Length * escalaTexto * .5f + escalaTexto)).ToList();

                int inicio = 0;
                while (inicio < itens.Count)
                {
                    float largura = 0f; int fim = inicio;
                    while (fim < itens.Count && (fim == inicio || largura + vagas[fim] <= LarguraMaxima))
                    {
                        largura += vagas[fim];
                        fim++;
                    }
                    float profundidade = 0f;
                    for (int i = inicio; i < fim; i++) profundidade = Mathf.Max(profundidade, itens[i].b.size.z);

                    // Câmera fica em +Z olhando pra -Z, então a esquerda da tela é +X.
                    float x = largura / 2f;
                    float zCentro = z - profundidade / 2f;
                    for (int i = inicio; i < fim; i++)
                    {
                        var (go, b) = itens[i];
                        float cx = x - vagas[i] / 2f;
                        go.transform.position = new Vector3(cx - b.center.x, 0, zCentro - b.center.z);
                        Texto(grupo, fonte, go.name, new Vector3(cx, .02f, z + escalaTexto * 1.3f), escalaTexto);
                        x -= vagas[i];
                    }
                    z -= profundidade + escalaTexto * 4f + folga;
                    inicio = fim;
                }
                z -= folga * 2f;
                zMin = z;
            }

            var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
            chao.name = "Chao";
            Object.DestroyImmediate(chao.GetComponent<Collider>());
            chao.transform.position = new Vector3(0, -.02f, zMin / 2f);
            chao.transform.localScale = new Vector3(LarguraMaxima / 10f + 4f, 1f, -zMin / 10f + 6f);
            chao.GetComponent<MeshRenderer>().sharedMaterial = ArtePaleta.Mat("Areia");

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.69f, 0.84f, 0.9f);
            cam.fieldOfView = 40f;
            cam.farClipPlane = 800f;
            cam.transform.position = new Vector3(0, 55f, 60f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, -55f, zMin * .45f - 60f));

            // Mesmo look das cenas do jogo (sol, céu, pós-processamento), sem neblina.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(false);

            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoDaCena));
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
        }

        static Bounds Limites(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return b;
        }

        // Etiqueta deitada no chão, legível pela câmera que olha de +Z.
        static void Texto(Transform pai, Font fonte, string texto, Vector3 pos, float altura)
        {
            var go = new GameObject("Etiqueta_" + texto);
            go.transform.SetParent(pai, false);
            go.transform.position = pos;
            go.transform.eulerAngles = new Vector3(90, 180, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.font = fonte;
            tm.text = texto.Replace('_', ' ');
            tm.fontSize = 64;
            tm.characterSize = altura * .12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = ArtePaleta.Cor("Grafite");
            go.GetComponent<MeshRenderer>().sharedMaterial = fonte.material;
        }
    }
}
