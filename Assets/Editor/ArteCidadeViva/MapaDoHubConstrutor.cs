using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Mapa do Hub (2026-11-26): o HubConstrutor, ao montar o chão e os prédios, anota aqui o que vai pro desenho
    // (Ret, RetGirado, Circ, Anel, Linha, Rotulo). No fim, Desenhar() pinta uma imagem do Plano Piloto visto de
    // cima (Assets/Art/UI/Mapa/mapa_do_hub.png) na mesma escala do mundo, e Interface() monta o botão "Mapa"
    // e a tela do mapa (MapaDoHub). As cores são as do mapa, não as da paleta 3D.
    public static class MapaDoHubConstrutor
    {
        const string CaminhoDaImagem = "Assets/Art/UI/Mapa/mapa_do_hub.png";
        // O pedaço do mundo que o mapa mostra (x e z). 4:3, como a imagem.
        public static readonly Rect Mundo = Rect.MinMaxRect(-90f, -100f, 270f, 170f);
        const int Largura = 1440, Altura = 1080;

        public static readonly Color Fundo = Hex("CFE2A0"), Grama = Hex("B5D682"), Agua = Hex("86C3E8"), Via = Hex("A3A6AB"),
            Ciclovia = Hex("D98C6A"), Calcada = Hex("ECE5D3"), Predio = Hex("FFFFFF"), Bloco = Hex("F4EEDB"), Escola = Hex("F2C14E"),
            Comercio = Hex("EE9F5C"), Monumento = Hex("FBFBF7"), Borda = Hex("6E7B85");

        struct Peca
        {
            public int tipo; // 0 ret girado, 1 círculo, 2 anel, 3 linha
            public Vector2 c, tam;
            public float giro, raio, esp;
            public Vector2[] pts;
            public Color cor;
        }

        struct Rotulo { public string texto; public Vector2 pos; public int tamanho; public bool claro; }

        static readonly List<Peca> pecas = new List<Peca>();
        static readonly List<Rotulo> rotulos = new List<Rotulo>();

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }

        public static void Comecar() { pecas.Clear(); rotulos.Clear(); }

        public static void Ret(float x0, float z0, float x1, float z1, Color cor) =>
            pecas.Add(new Peca { tipo = 0, c = new Vector2((x0 + x1) / 2f, (z0 + z1) / 2f), tam = new Vector2(Mathf.Abs(x1 - x0), Mathf.Abs(z1 - z0)), cor = cor });

        // Retângulo de largura tam.x (em x local) e fundo tam.y (em z local), girado giroY graus como um objeto da cena.
        public static void RetGirado(Vector3 centro, Vector2 tam, float giroY, Color cor) =>
            pecas.Add(new Peca { tipo = 0, c = new Vector2(centro.x, centro.z), tam = tam, giro = giroY, cor = cor });

        public static void Circ(float x, float z, float raio, Color cor) =>
            pecas.Add(new Peca { tipo = 1, c = new Vector2(x, z), raio = raio, cor = cor });

        public static void Anel(float x, float z, float raio, float esp, Color cor) =>
            pecas.Add(new Peca { tipo = 2, c = new Vector2(x, z), raio = raio, esp = esp, cor = cor });

        public static void Linha(Color cor, float esp, params Vector2[] pts) =>
            pecas.Add(new Peca { tipo = 3, pts = pts, esp = esp, cor = cor });

        public static void Escrever(string texto, float x, float z, int tamanho = 24, bool claro = false) =>
            rotulos.Add(new Rotulo { texto = texto, pos = new Vector2(x, z), tamanho = tamanho, claro = claro });

        // ------------------------------------------------------------------ a imagem

        public static Sprite Desenhar()
        {
            var px = new Color[Largura * Altura];
            for (int i = 0; i < px.Length; i++) px[i] = Fundo;
            float escala = Largura / Mundo.width; // pixels por metro
            foreach (var p in pecas)
            {
                // Caixa da peça em pixels.
                Vector2 min, max;
                switch (p.tipo)
                {
                    case 0:
                        float m = (p.tam.magnitude) / 2f;
                        min = p.c - Vector2.one * m; max = p.c + Vector2.one * m; break;
                    case 1:
                    case 2:
                        min = p.c - Vector2.one * (p.raio + p.esp); max = p.c + Vector2.one * (p.raio + p.esp); break;
                    default:
                        min = p.pts[0]; max = p.pts[0];
                        foreach (var q in p.pts) { min = Vector2.Min(min, q); max = Vector2.Max(max, q); }
                        min -= Vector2.one * p.esp; max += Vector2.one * p.esp; break;
                }
                int x0 = Mathf.Max(0, Mathf.FloorToInt((min.x - Mundo.xMin) * escala)), x1 = Mathf.Min(Largura - 1, Mathf.CeilToInt((max.x - Mundo.xMin) * escala));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((min.y - Mundo.yMin) * escala)), y1 = Mathf.Min(Altura - 1, Mathf.CeilToInt((max.y - Mundo.yMin) * escala));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        // 4 amostras por pixel (bordas suaves).
                        int dentro = 0;
                        for (int s = 0; s < 4; s++)
                        {
                            var w = new Vector2(Mundo.xMin + (x + .25f + .5f * (s & 1)) / escala, Mundo.yMin + (y + .25f + .5f * (s >> 1)) / escala);
                            if (Dentro(p, w)) dentro++;
                        }
                        if (dentro == 0) continue;
                        int i = y * Largura + x;
                        px[i] = Color.Lerp(px[i], p.cor, dentro / 4f * p.cor.a);
                        px[i].a = 1f;
                    }
            }
            var tex = new Texture2D(Largura, Altura, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            ArtePaleta.GarantirPasta(Path.GetDirectoryName(CaminhoDaImagem).Replace('\\', '/'));
            File.WriteAllBytes(CaminhoDaImagem, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(CaminhoDaImagem, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(CaminhoDaImagem);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoDaImagem);
        }

        static bool Dentro(Peca p, Vector2 w)
        {
            switch (p.tipo)
            {
                case 0:
                    Vector2 d = w - p.c;
                    // Giro de objeto (graus em volta de Y, horário visto de cima): leva o ponto pro espaço local.
                    float a = p.giro * Mathf.Deg2Rad;
                    float lx = d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a);
                    float lz = d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a);
                    return Mathf.Abs(lx) <= p.tam.x / 2f && Mathf.Abs(lz) <= p.tam.y / 2f;
                case 1:
                    return (w - p.c).sqrMagnitude <= p.raio * p.raio;
                case 2:
                    float r = (w - p.c).magnitude;
                    return Mathf.Abs(r - p.raio) <= p.esp / 2f;
                default:
                    for (int i = 1; i < p.pts.Length; i++)
                        if (DistanciaAoTrecho(w, p.pts[i - 1], p.pts[i]) <= p.esp / 2f) return true;
                    return false;
            }
        }

        static float DistanciaAoTrecho(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }

        // ------------------------------------------------------------------ a tela

        public struct LugarDoMapa
        {
            public string nome, objeto, cor;
        }

        // Botão "Mapa" no canto de baixo à esquerda, a tela do mapa (escondida) e o escurecer das viagens.
        public static void Interface(Sprite imagem, LugarDoMapa[] lugares)
        {
            var canvas = GameObject.Find("UI_Hub").transform;
            var jogador = GameObject.Find("Player");
            var mapa = canvas.gameObject.AddComponent<MapaDoHub>();
            mapa.jogador = jogador != null ? jogador.transform : null;
            mapa.mundo = Mundo;

            mapa.botaoMapa = Botao("Botao_Mapa", canvas, "Mapa (M)", "Petroleo_Claro", new Vector2(200, 120), new Vector2(330, 110), 46);
            TravessiaConstrutor.Ancorar(mapa.botaoMapa, Vector2.zero);

            var painel = Imagem("Tela_Do_Mapa", canvas, arredondado, C("Petroleo_Escuro", .94f), Vector2.zero, new Vector2(1920, 1080), true);
            mapa.painel = painel.gameObject;
            Texto("Titulo", painel.transform, "Mapa do Plano Piloto", 56, C("Branco"), new Vector2(0, 492), new Vector2(1100, 80));
            Texto("Dica", painel.transform, "Toque num lugar pra ir até lá.", 30, C("Mostarda_Clara"), new Vector2(0, 438), new Vector2(1100, 44), FontStyle.Normal);
            mapa.botaoFechar = Botao("Botao_Fechar_Mapa", painel.transform, "", "Petroleo_Claro", new Vector2(-850, 460), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);

            // O desenho à esquerda e, à direita, a lista "Ir para:" (na superquadra as áreas ficam perto demais pra
            // ter o nome em cima de cada bolinha).
            var desenho = Imagem("Desenho", painel.transform, imagem, Color.white, new Vector2(-190, -62), new Vector2(1240, 930));
            desenho.preserveAspect = false;
            mapa.desenho = (RectTransform)desenho.transform;
            float k = 1240f / Mundo.width;
            Vector2 NaImagem(Vector2 w) => new Vector2((w.x - Mundo.center.x) * k, (w.y - Mundo.center.y) * k);

            foreach (var r in rotulos)
            {
                var t = Texto("Rotulo", desenho.transform, r.texto, r.tamanho, r.claro ? C("Branco") : C("Petroleo_Escuro"), NaImagem(r.pos), new Vector2(360, r.tamanho * 2.4f));
                var contorno = t.gameObject.AddComponent<Outline>();
                contorno.effectColor = r.claro ? new Color(0, 0, 0, .45f) : new Color(1, 1, 1, .7f);
                contorno.effectDistance = new Vector2(2, -2);
            }

            var lista = new List<MapaDoHub.LugarNoMapa>();
            Texto("Ir_Para", painel.transform, "Ir para:", 40, C("Branco"), new Vector2(690, 345), new Vector2(380, 60));
            for (int i = 0; i < lugares.Length; i++)
            {
                var l = lugares[i];
                var area = GameObject.Find(l.objeto);
                if (area == null) { Debug.LogError("[Mapa] Área não encontrada: " + l.objeto); continue; }
                var pos = NaImagem(new Vector2(area.transform.position.x, area.transform.position.z));
                var b = Botao("Lugar_" + l.objeto, desenho.transform, "", l.cor, pos, new Vector2(50, 50), 0, null, circulo);
                var item = Botao("Ir_Para_" + l.objeto, painel.transform, l.nome, l.cor, new Vector2(700, 255 - i * 118), new Vector2(380, 100), 34);
                var ti = item.transform.Find("Frente/Texto").GetComponent<Text>();
                ti.resizeTextForBestFit = true; ti.resizeTextMinSize = 26; ti.resizeTextMaxSize = 34;
                ((RectTransform)ti.transform).anchoredPosition = new Vector2(30, 0);
                // A mesma bolinha do desenho, no começo do botão, pra criança ligar um ao outro.
                Imagem("Bolinha", item.transform.Find("Frente"), circulo, C("Branco"), new Vector2(-150, 0), new Vector2(46, 46));
                Imagem("Cor", item.transform.Find("Frente"), circulo, C(l.cor), new Vector2(-150, 0), new Vector2(34, 34));
                lista.Add(new MapaDoHub.LugarNoMapa { nome = l.nome, area = area.transform, botao = b, botaoDaLista = item });
            }
            mapa.lugares = lista.ToArray();

            // Você está aqui: bolinha vermelha com uma seta que gira pra onde o jogador olha.
            var voce = Imagem("Voce_Esta_Aqui", desenho.transform, null, Color.clear, Vector2.zero, new Vector2(10, 10));
            mapa.voceEstaAqui = (RectTransform)voce.transform;
            Imagem("Borda", voce.transform, circulo, C("Branco"), Vector2.zero, new Vector2(58, 58));
            var bola = Imagem("Bola", voce.transform, circulo, C("Vermelho"), Vector2.zero, new Vector2(46, 46));
            var seta = Imagem("Seta", bola.transform, S("ui_seta_voltar"), C("Branco"), Vector2.zero, new Vector2(34, 34));
            var giroDaSeta = new GameObject("Giro", typeof(RectTransform)).GetComponent<RectTransform>();
            giroDaSeta.SetParent(bola.transform, false);
            seta.transform.SetParent(giroDaSeta, false);
            seta.rectTransform.localEulerAngles = new Vector3(0, 0, -90); // a seta do ícone aponta pra esquerda; aqui, pra cima (norte)
            mapa.setaDoJogador = giroDaSeta;
            var etiqueta = Imagem("Etiqueta", voce.transform, arredondado, C("Vermelho"), new Vector2(0, 58), new Vector2(250, 48));
            Texto("Texto", etiqueta.transform, "Você está aqui", 28, C("Branco"), Vector2.zero, new Vector2(240, 44));

            var escurecer = Imagem("Escurecer", canvas, null, new Color(0, 0, 0, 0), Vector2.zero, new Vector2(1920, 1080), true);
            mapa.escurecer = escurecer;
            painel.gameObject.SetActive(false);
            escurecer.gameObject.SetActive(false);
            EditorUtility.SetDirty(mapa);
        }
    }
}
