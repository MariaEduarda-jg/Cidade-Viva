using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CidadeViva.EditorArte
{
    // Look do jogo: ajustes do URP (sombras suaves, SSAO), o prefab Ambiente_Padrao
    // (sol de fim de tarde + volume de pós-processamento + céu em degradê) e a aplicação em todas as cenas.
    public static class AmbienteConstrutor
    {
        public const string CaminhoPrefab = "Assets/Prefabs/Ambiente_Padrao.prefab";
        const string CaminhoPerfil = "Assets/Settings/Ambiente_Padrao_Volume.asset";
        const string CaminhoCeu = "Assets/Materials/Ceu_Degrade.mat";
        const string CaminhoRenderer = "Assets/Settings/URP_Renderer.asset";

        [MenuItem("Cidade Viva/Ambiente/Recriar Ambiente_Padrao e aplicar em todas as cenas")]
        public static void RecriarEAplicar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigurarURP();
            CriarPrefab();
            AplicarEmTodasAsCenas();
        }

        public static void ConfigurarURP()
        {
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                var so = new SerializedObject(urp);
                Definir(so, "m_SoftShadowsSupported", true);
                Definir(so, "m_ShadowDistance", 80f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(urp);
            }

            var dados = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(CaminhoRenderer);
            if (dados == null) { Debug.LogError("[Ambiente] Renderer do URP não encontrado em " + CaminhoRenderer); return; }
            if (dados.postProcessData == null)
                dados.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");

            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (var f in dados.rendererFeatures)
                if (f is ScreenSpaceAmbientOcclusion existente) ssao = existente;
            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "SSAO";
                AssetDatabase.AddObjectToAsset(ssao, dados);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long idLocal);
                var soDados = new SerializedObject(dados);
                var lista = soDados.FindProperty("m_RendererFeatures");
                lista.arraySize++;
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = ssao;
                var mapa = soDados.FindProperty("m_RendererFeatureMap");
                mapa.arraySize++;
                mapa.GetArrayElementAtIndex(mapa.arraySize - 1).longValue = idLocal;
                soDados.ApplyModifiedPropertiesWithoutUndo();
            }

            // Oclusão discreta: só um escurecido leve nos cantos e embaixo dos objetos.
            var soSsao = new SerializedObject(ssao);
            Definir(soSsao, "m_Settings.Intensity", 0.7f);
            Definir(soSsao, "m_Settings.Radius", 0.35f);
            Definir(soSsao, "m_Settings.DirectLightingStrength", 0.25f);
            Definir(soSsao, "m_Settings.Falloff", 80f);
            soSsao.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ssao);
            EditorUtility.SetDirty(dados);
            AssetDatabase.SaveAssets();
        }

        public static void CriarPrefab()
        {
            // Céu
            var ceu = AssetDatabase.LoadAssetAtPath<Material>(CaminhoCeu);
            var shader = Shader.Find("CidadeViva/CeuDegrade");
            if (ceu == null)
            {
                ceu = new Material(shader);
                AssetDatabase.CreateAsset(ceu, CaminhoCeu);
            }
            ceu.shader = shader;
            ceu.SetColor("_CorTopo", new Color(0.50f, 0.71f, 0.86f));
            ceu.SetColor("_CorHorizonte", new Color(1.00f, 0.84f, 0.70f));
            ceu.SetColor("_CorChao", new Color(0.80f, 0.72f, 0.62f));
            ceu.SetFloat("_Expoente", 0.75f);
            EditorUtility.SetDirty(ceu);

            // Perfil de pós-processamento
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(CaminhoPerfil);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, CaminhoPerfil);
            }
            foreach (var antigo in new List<VolumeComponent>(perfil.components))
                if (antigo != null) Object.DestroyImmediate(antigo, true);
            perfil.components.Clear();

            var tom = Adicionar<Tonemapping>(perfil);
            tom.mode.Override(TonemappingMode.Neutral);

            var cor = Adicionar<ColorAdjustments>(perfil);
            cor.postExposure.Override(0.05f);
            cor.contrast.Override(8f);
            cor.saturation.Override(8f);

            var branco = Adicionar<WhiteBalance>(perfil);
            branco.temperature.Override(6f);

            var bloom = Adicionar<Bloom>(perfil);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.2f);
            bloom.scatter.Override(0.6f);

            var vinheta = Adicionar<Vignette>(perfil);
            vinheta.intensity.Override(0.18f);
            vinheta.smoothness.Override(0.45f);
            EditorUtility.SetDirty(perfil);

            // Prefab
            var raiz = new GameObject("Ambiente_Padrao");
            var ambiente = raiz.AddComponent<AmbientePadrao>();

            var solGo = new GameObject("Sol");
            solGo.transform.SetParent(raiz.transform, false);
            solGo.transform.localEulerAngles = new Vector3(38f, 35f, 0f);
            var sol = solGo.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = new Color(1f, 0.87f, 0.70f);
            sol.intensity = 1.1f;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.6f;

            var volGo = new GameObject("Volume_Global");
            volGo.transform.SetParent(raiz.transform, false);
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = perfil;

            ambiente.ceu = ceu;
            ambiente.sol = sol;

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Object.DestroyImmediate(raiz);
            AssetDatabase.SaveAssets();
        }

        public static void AplicarEmTodasAsCenas()
        {
            string atual = SceneManager.GetActiveScene().path;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
                // A galeria é comprida demais pra neblina: esconderia as últimas fileiras.
                AplicarNaCenaAberta(!caminho.EndsWith("Galeria_Assets.unity"));
                EditorSceneManager.SaveScene(cena);
            }
            if (!string.IsNullOrEmpty(atual)) EditorSceneManager.OpenScene(atual, OpenSceneMode.Single);
        }

        // Põe o Ambiente_Padrao na cena aberta (uma vez só), tira as luzes direcionais soltas
        // e liga pós-processamento e céu em todas as câmeras.
        public static AmbientePadrao AplicarNaCenaAberta(bool neblina)
        {
            var cena = SceneManager.GetActiveScene();
            AmbientePadrao ambiente = null;
            foreach (var raiz in cena.GetRootGameObjects())
            {
                var existente = raiz.GetComponent<AmbientePadrao>();
                if (existente != null) { ambiente = existente; continue; }
                var luz = raiz.GetComponent<Light>();
                if (luz != null && luz.type == LightType.Directional) Object.DestroyImmediate(raiz);
            }
            if (ambiente == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
                go.transform.SetAsFirstSibling();
                ambiente = go.GetComponent<AmbientePadrao>();
            }
            ambiente.neblina = neblina;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ambiente);
            ambiente.Aplicar();

            foreach (var raiz in cena.GetRootGameObjects())
                foreach (var cam in raiz.GetComponentsInChildren<Camera>(true))
                {
                    cam.clearFlags = CameraClearFlags.Skybox;
                    var dadosCam = cam.GetUniversalAdditionalCameraData();
                    dadosCam.renderPostProcessing = true;
                    dadosCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    EditorUtility.SetDirty(cam);
                    EditorUtility.SetDirty(dadosCam);
                }
            EditorSceneManager.MarkSceneDirty(cena);
            return ambiente;
        }

        static T Adicionar<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            var c = perfil.Add<T>(true);
            c.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(c, perfil);
            return c;
        }

        static void Definir(SerializedObject so, string caminho, float valor)
        {
            var p = so.FindProperty(caminho);
            if (p != null) p.floatValue = valor;
            else Debug.LogWarning("[Ambiente] Propriedade não encontrada: " + caminho);
        }

        static void Definir(SerializedObject so, string caminho, bool valor)
        {
            var p = so.FindProperty(caminho);
            if (p != null) p.boolValue = valor;
            else Debug.LogWarning("[Ambiente] Propriedade não encontrada: " + caminho);
        }
    }
}
