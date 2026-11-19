using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Loja mínima do Hub (versão final): acessórios feitos com formas da paleta (Prefabs/Art/Loja),
    // figura de cada item (o personagem do Hub vestido, fotografado), o catálogo em
    // Assets/Resources/CatalogoDaLoja.asset e o painel da loja (chamado pelo MenuConstrutor.PainelDoHub).
    public static class LojaConstrutor
    {
        const string CaminhoDoCatalogo = "Assets/Resources/CatalogoDaLoja.asset";
        const string PastaDosIcones = "Assets/Art/UI/Loja";
        const string Personagem = "Assets/Prefabs/Art/Personagens/Familia_FilhoMaisVelho_Animado.prefab";
        public const int IndiceDaCamiseta = 0;

        struct Def
        {
            public string id, nome, cor; public TipoDeItemDaLoja tipo; public int preco; public Vector3 naCabeca;
        }

        static readonly Def[] Itens =
        {
            new Def { id = "camiseta_vermelha", nome = "Camiseta vermelha", tipo = TipoDeItemDaLoja.Roupa, preco = 20, cor = "Vermelho" },
            new Def { id = "camiseta_amarela", nome = "Camiseta amarela", tipo = TipoDeItemDaLoja.Roupa, preco = 20, cor = "Mostarda" },
            new Def { id = "camiseta_azul", nome = "Camiseta azul", tipo = TipoDeItemDaLoja.Roupa, preco = 30, cor = "Petroleo_Claro" },
            new Def { id = "camiseta_rosa", nome = "Camiseta rosa", tipo = TipoDeItemDaLoja.Roupa, preco = 30, cor = "Rosa" },
            new Def { id = "bone", nome = "Boné", tipo = TipoDeItemDaLoja.Acessorio, preco = 40, naCabeca = new Vector3(0, .37f, -.02f) },
            new Def { id = "oculos", nome = "Óculos escuros", tipo = TipoDeItemDaLoja.Acessorio, preco = 50, naCabeca = new Vector3(0, .19f, .255f) },
            new Def { id = "chapeu_de_festa", nome = "Chapéu de festa", tipo = TipoDeItemDaLoja.Acessorio, preco = 60, naCabeca = new Vector3(0, .53f, -.02f) },
        };

        [MenuItem("Cidade Viva/Hub/Reconstruir os itens da loja")]
        public static CatalogoDaLoja Gerar()
        {
            ArtePaleta.GarantirPasta(PastaDosIcones);
            ArtePaleta.GarantirPasta("Assets/Resources");
            var acessorios = new Dictionary<string, GameObject>
            {
                ["bone"] = Acessorio("Bone", r =>
                {
                    F.M(r, "Copa", "Semiesfera", "Vermelho", F.V(0, 0, 0), F.V(.56f, .46f, .58f));
                    F.Cubo(r, "Aba", "Vermelho", F.V(0, .02f, .34f), F.V(.42f, .03f, .22f));
                    F.Esfera(r, "Botao", "Branco", F.V(0, .23f, 0), F.V(.05f, .035f, .05f));
                }),
                ["oculos"] = Acessorio("Oculos", r =>
                {
                    foreach (float x in new[] { -.1f, .1f })
                    {
                        F.Cilindro(r, "Aro", "Grafite", F.V(x, 0, 0), F.V(.16f, .015f, .16f), F.V(90, 0, 0));
                        F.Cilindro(r, "Lente", "Petroleo_Escuro", F.V(x, 0, .008f), F.V(.135f, .015f, .135f), F.V(90, 0, 0));
                    }
                    F.Cubo(r, "Ponte", "Grafite", F.V(0, .02f, 0), F.V(.06f, .02f, .02f));
                    foreach (float x in new[] { -.235f, .235f })
                        F.Cubo(r, "Haste", "Grafite", F.V(x, .02f, -.13f), F.V(.015f, .02f, .26f));
                    foreach (float x in new[] { -.205f, .205f })
                        F.Cubo(r, "Canto", "Grafite", F.V(x, .02f, 0), F.V(.07f, .02f, .02f));
                }),
                ["chapeu_de_festa"] = Acessorio("Chapeu_De_Festa", r =>
                {
                    F.M(r, "Cone", "Cone", "Rosa", F.V(0, 0, 0), F.V(.46f, .7f, .46f));
                    F.M(r, "Listra", "Cone", "Mostarda", F.V(0, .1f, 0), F.V(.36f, .2f, .36f));
                    F.Esfera(r, "Pompom", "Mostarda", F.V(0, .36f, 0), F.V(.1f, .1f, .1f));
                }),
            };

            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDaLoja>(CaminhoDoCatalogo);
            if (catalogo == null)
            {
                catalogo = ScriptableObject.CreateInstance<CatalogoDaLoja>();
                AssetDatabase.CreateAsset(catalogo, CaminhoDoCatalogo);
            }
            catalogo.moedasPorEstrela = 10;
            catalogo.itens = Itens.Select(d => new ItemDaLoja
            {
                id = d.id, nome = d.nome, tipo = d.tipo, preco = d.preco, posicaoNaCabeca = d.naCabeca,
                corDaRoupa = d.tipo == TipoDeItemDaLoja.Roupa ? ArtePaleta.Mat(d.cor) : null,
                acessorio = acessorios.TryGetValue(d.id, out var go) ? go : null,
            }).ToList();
            foreach (var item in catalogo.itens) item.icone = Icone(item);
            EditorUtility.SetDirty(catalogo);
            AssetDatabase.SaveAssets();
            return catalogo;
        }

        static GameObject Acessorio(string nome, System.Action<Transform> montar)
        {
            var raiz = F.Grupo("Acessorio_" + nome);
            montar(raiz.transform);
            foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            F.Salvar(raiz, "Loja");
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/Loja/Acessorio_{nome}.prefab");
        }

        // Fotografa o personagem do Hub (parado) vestindo o item: é a figura do cartão na loja.
        static Sprite Icone(ItemDaLoja item)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Personagem));
            go.transform.position = new Vector3(0, -500, 0);
            var animator = go.GetComponentInChildren<Animator>();
            var parado = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.ToLower().Contains("idle")) : null;
            if (parado != null) parado.SampleAnimation(animator.gameObject, 0f);
            var corpo = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (item.corDaRoupa != null)
            {
                var mats = corpo.sharedMaterials;
                mats[IndiceDaCamiseta] = item.corDaRoupa;
                corpo.sharedMaterials = mats;
            }
            var cabeca = go.GetComponentsInChildren<Transform>().First(t => t.name == "Head");
            if (item.acessorio != null)
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(item.acessorio);
                a.transform.SetPositionAndRotation(cabeca.position + go.transform.rotation * item.posicaoNaCabeca, go.transform.rotation);
                a.transform.SetParent(cabeca, true);
            }

            var cam = new GameObject("Camera_Do_Icone").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ArtePaleta.Cor("Mostarda_Clara");
            cam.fieldOfView = 28f;
            bool roupa = item.tipo == TipoDeItemDaLoja.Roupa;
            Vector3 alvo = go.transform.position + new Vector3(0, roupa ? .95f : 1.32f, .05f);
            cam.transform.position = alvo + new Vector3(.55f, .2f, roupa ? 2.6f : 1.9f);
            cam.transform.LookAt(alvo);
            var luz = new GameObject("Luz_Do_Icone").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(30, 200, 0);
            luz.intensity = 1.1f;

            const int tam = 256;
            var rt = new RenderTexture(tam, tam, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(tam, tam, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, tam, tam), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(luz.gameObject);
            Object.DestroyImmediate(go);
            rt.Release();

            string caminho = $"{PastaDosIcones}/icone_{item.id}.png";
            File.WriteAllBytes(caminho, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(caminho);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        }

        // ------------------------------------------------------------------ jogador e painel

        // Chamado pelo HubConstrutor.Jogador: o personagem do Hub veste o que o perfil escolheu.
        public static void ConfigurarJogador(GameObject jogador, GameObject visual)
        {
            var roupa = jogador.GetComponent<RoupaDoJogador>();
            if (roupa == null) roupa = jogador.AddComponent<RoupaDoJogador>();
            roupa.visual = visual.transform;
            roupa.corpo = visual.GetComponentInChildren<SkinnedMeshRenderer>();
            roupa.indiceDaCamiseta = IndiceDaCamiseta;
            roupa.cabeca = visual.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Head");
        }

        // Chamado pelo MenuConstrutor.PainelDoHub: contador de moedas, botão "Loja" e o painel.
        public static void Painel(Canvas canvas)
        {
            if (Loja.Catalogo == null || AssetDatabase.LoadAssetAtPath<CatalogoDaLoja>(CaminhoDoCatalogo) == null) Gerar();
            var loja = canvas.gameObject.AddComponent<LojaDoHub>();

            // Contador (embaixo do Álbum) e botão "Loja".
            var contador = Imagem("Contador_De_Moedas", canvas.transform, arredondado, C("Branco", .94f), new Vector2(-210, -232), new Vector2(300, 74));
            TravessiaConstrutor.Ancorar(contador, new Vector2(1, 1));
            Imagem("Moeda", contador.transform, S("ui_moeda"), Color.white, new Vector2(-100, 0), new Vector2(58, 58));
            loja.contadorDoHub = Texto("Texto", contador.transform, "0", 44, C("Petroleo_Escuro"), new Vector2(35, 0), new Vector2(190, 64));
            var botao = Botao("Botao_Loja", canvas.transform, "Loja", "Rosa", new Vector2(-210, -345), new Vector2(300, 120), 56);
            TravessiaConstrutor.Ancorar(botao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(botao.onClick, loja.Abrir);

            // Painel: fundo escuro, cartão com título, moedas, grade de itens e "Voltar".
            var painel = Imagem("Painel_Da_Loja", canvas.transform, null, new Color(0, 0, 0, .55f), Vector2.zero, Vector2.zero, true);
            painel.rectTransform.anchorMin = Vector2.zero; painel.rectTransform.anchorMax = Vector2.one;
            painel.rectTransform.offsetMin = painel.rectTransform.offsetMax = Vector2.zero;
            loja.painel = painel.gameObject;
            var cartao = Imagem("Cartao", painel.transform, arredondado, C("Branco", .97f), new Vector2(0, -10), new Vector2(1600, 960), true);
            Texto("Titulo", cartao.transform, "Loja", 80, C("Petroleo"), new Vector2(0, 410), new Vector2(600, 110));
            var voltar = Botao("Botao_Voltar", cartao.transform, "", "Petroleo_Claro", new Vector2(-710, 405), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            UnityEventTools.AddPersistentListener(voltar.onClick, loja.Fechar);
            var moedas = Imagem("Moedas", cartao.transform, arredondado, C("Mostarda_Clara"), new Vector2(600, 410), new Vector2(300, 90));
            Imagem("Moeda", moedas.transform, S("ui_moeda"), Color.white, new Vector2(-95, 0), new Vector2(66, 66));
            loja.moedasDoPainel = Texto("Texto", moedas.transform, "0", 52, C("Petroleo_Escuro"), new Vector2(35, 0), new Vector2(190, 80));

            var grade = Ret("Itens", cartao.transform, new Vector2(0, -20), new Vector2(1520, 720));
            var layout = grade.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(350, 345);
            layout.spacing = new Vector2(24, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            loja.grade = grade;

            var modelo = Imagem("Modelo_Item", grade, arredondado, C("Areia"), Vector2.zero, new Vector2(350, 345));
            Imagem("Icone", modelo.transform, null, Color.white, new Vector2(0, 62), new Vector2(190, 190));
            var nome = Texto("Nome", modelo.transform, "", 32, C("Petroleo_Escuro"), new Vector2(0, -52), new Vector2(330, 44));
            nome.resizeTextForBestFit = true; nome.resizeTextMinSize = 20; nome.resizeTextMaxSize = 32;
            var preco = Imagem("Preco", modelo.transform, arredondado, C("Branco", .9f), new Vector2(110, 148), new Vector2(120, 50));
            Imagem("Moeda", preco.transform, S("ui_moeda"), Color.white, new Vector2(-34, 0), new Vector2(38, 38));
            Texto("Texto", preco.transform, "0", 32, C("Petroleo_Escuro"), new Vector2(18, 0), new Vector2(70, 44));
            var vestido = Imagem("Vestido", modelo.transform, arredondado, C("Verde"), new Vector2(-100, 148), new Vector2(130, 46));
            Texto("Texto", vestido.transform, "vestido", 26, C("Branco"), Vector2.zero, new Vector2(124, 40));
            Botao("Botao", modelo.transform, "Comprar", "Verde", new Vector2(0, -122), new Vector2(260, 78), 36);
            modelo.gameObject.SetActive(false);
            loja.modeloDeItem = modelo.gameObject;

            loja.mensagem = Texto("Mensagem", cartao.transform, "", 36, C("Cinza_Escuro"), new Vector2(0, -425), new Vector2(1400, 60), FontStyle.Normal);
            painel.gameObject.SetActive(false);
        }
    }
}
