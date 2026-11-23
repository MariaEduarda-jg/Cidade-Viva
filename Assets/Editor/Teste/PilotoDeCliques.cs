using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Teste "como uma pessoa jogando": percorre o jogo só com cliques na tela (posição do botão -> raycast do
// EventSystem -> clique), toques e arrastos simulados (Toque) e teclas simuladas (Teclado), sem chamar
// métodos dos managers. Pra decidir onde tocar ele "olha a tela" (lê posições e estados, como a criança veria).
// Anota botão que não responde, coberto ou fora da área 16:9, texto cortado ou pequeno, material rosa,
// erros e avisos do Console, e tira fotos (1920x1080) em Temp/fotos_finais.
// Uso: entrar em Play no Menu_Inicial e chamar PilotoDeCliques.Comecar() (ou o menu Cidade Viva > Teste).
public static class PilotoDeCliques
{
    const string Pasta = "Temp/piloto_final";
    const string PastaDasFotos = "Temp/fotos_finais";
    static Stack<IEnumerator> pilha;
    static float espera;
    static int fotos;
    static readonly HashSet<string> console = new HashSet<string>();
    static readonly HashSet<string> cartoesFotografados = new HashSet<string>();
    static readonly HashSet<string> notasDadas = new HashSet<string>();
    static string[] partes;

    [MenuItem("Cidade Viva/Teste/Piloto de cliques (fluxo inteiro, em Play no menu)")]
    public static void PeloMenu() => Comecar();

    public static void Comecar(params string[] somente)
    {
        if (!EditorApplication.isPlaying) { Debug.LogError("[Piloto] Entre em Play no Menu_Inicial antes."); return; }
        partes = somente;
        Directory.CreateDirectory(Pasta);
        Directory.CreateDirectory(PastaDasFotos);
        foreach (string f in new[] { "log.txt", "notas.txt", "console.txt", "desempenho.txt", "textos.txt" })
            File.WriteAllText(Path.Combine(Pasta, f), "");
        console.Clear(); cartoesFotografados.Clear(); notasDadas.Clear(); fotos = 0;
        Application.logMessageReceived -= AoLogar;
        Application.logMessageReceived += AoLogar;
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Piloto 1920x1080");
        pilha = new Stack<IEnumerator>();
        pilha.Push(Roteiro());
        EditorApplication.update -= Passo;
        EditorApplication.update += Passo;
    }

    static bool Faz(string parte) => partes == null || partes.Length == 0 || partes.Contains(parte);

    // ------------------------------------------------------------------ motor

    static void Passo()
    {
        if (!EditorApplication.isPlaying) { L("TIMEOUT saiu do play"); Parar(); return; }
        if (Time.unscaledTime < espera) return;
        try
        {
            while (pilha.Count > 0)
            {
                var e = pilha.Peek();
                if (!e.MoveNext()) { pilha.Pop(); continue; }
                if (e.Current is IEnumerator sub) { pilha.Push(sub); continue; }
                espera = Time.unscaledTime + (e.Current is float f ? f : 0f);
                return;
            }
            Parar();
        }
        catch (Exception ex) { L("TIMEOUT erro do piloto: " + ex); Parar(); }
    }

    static void Parar()
    {
        EditorApplication.update -= Passo;
        Application.logMessageReceived -= AoLogar;
        Toque.PararDeSimular();
    }

    static void AoLogar(string msg, string pilhaDeChamadas, LogType tipo)
    {
        if (tipo == LogType.Log) return;
        string linha = msg.Split('\n')[0];
        string origem = pilhaDeChamadas.Split('\n').FirstOrDefault(l => l.Contains("Assets/")) ?? "";
        string chave = tipo + "|" + linha;
        if (console.Add(chave))
            File.AppendAllText(Path.Combine(Pasta, "console.txt"), $"[{tipo}] ({SceneManager.GetActiveScene().name}) {linha}  {origem.Trim()}\n");
    }

    static void L(string s) => File.AppendAllText(Path.Combine(Pasta, "log.txt"), $"{Time.unscaledTime:0.0} [{SceneManager.GetActiveScene().name}] {s}\n");

    static void Nota(string s)
    {
        if (!notasDadas.Add(s)) return;
        L("NOTA: " + s);
        File.AppendAllText(Path.Combine(Pasta, "notas.txt"), $"[{SceneManager.GetActiveScene().name}] {s}\n");
    }

    static string Cena => SceneManager.GetActiveScene().name;

    // Espera, cuidando do cartão do verbete (foto da primeira vez e "Continuar").
    static IEnumerator Esperar(float seg) { yield return EsperarAte(() => false, seg); }

    static IEnumerator EsperarAte(Func<bool> pronto, float max, string oque = null)
    {
        float t0 = Time.unscaledTime;
        while (!pronto())
        {
            if (CartaoVerbete.EstaAberto) yield return FecharCartao();
            if (Time.unscaledTime - t0 > max)
            {
                if (oque != null) Nota("demorou demais: " + oque);
                yield break;
            }
            yield return 0.05f;
        }
    }

    static IEnumerator FecharCartao()
    {
        var c = UnityEngine.Object.FindAnyObjectByType<CartaoVerbete>();
        if (c == null) yield break;
        float t0 = Time.unscaledTime;
        while (!c.botaoContinuar.interactable && Time.unscaledTime - t0 < 4f) yield return 0.1f;
        string palavra = c.textoDaPalavra.text;
        if (cartoesFotografados.Add(palavra)) yield return Foto("cartao_" + palavra.Replace(' ', '_'));
        Clicar(c.botaoContinuar, "Continuar (cartão " + palavra + ")");
        yield return 0.6f;
    }

    // ------------------------------------------------------------------ ver a tela

    static Rect AreaEmPixels
    {
        get
        {
            Rect a = TelaFixa.Area;
            return new Rect(a.x * Screen.width, a.y * Screen.height, a.width * Screen.width, a.height * Screen.height);
        }
    }

    static Vector2 NaTela(RectTransform rt)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
    }

    static Vector2 NaTela(Vector3 mundo)
    {
        Vector3 p = Camera.main.WorldToScreenPoint(mundo);
        return new Vector2(p.x, p.y);
    }

    static bool Visivel(Vector3 mundo)
    {
        Vector3 p = Camera.main.WorldToScreenPoint(mundo);
        return p.z > 0f && AreaEmPixels.Contains(new Vector2(p.x, p.y));
    }

    static Selectable Botao(string nome)
    {
        var candidatos = UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsInactive.Exclude)
            .Where(s => s.gameObject.activeInHierarchy)
            .Where(s => s.name == nome || (s.GetComponentInChildren<Text>() != null && s.GetComponentInChildren<Text>().text == nome)).ToList();
        // Se há mais de um com o mesmo nome, vale o que está por cima (o que a criança vê e toca).
        foreach (var s in candidatos)
        {
            var es = EventSystem.current;
            if (es == null) break;
            var dados = new PointerEventData(es) { position = NaTela((RectTransform)s.transform) };
            var hits = new List<RaycastResult>();
            es.RaycastAll(dados, hits);
            if (hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(s.transform)) return s;
        }
        return candidatos.FirstOrDefault();
    }

    static string Caminho(Transform t)
    {
        var partes = new List<string>();
        for (int i = 0; t != null && i < 4; i++, t = t.parent) partes.Insert(0, t.name);
        return string.Join("/", partes);
    }

    // Clica como um dedo: acha o botão na tela, faz o raycast do EventSystem e clica no que estiver por cima.
    static bool Clicar(Selectable alvo, string rotulo)
    {
        if (alvo == null) { Nota($"botão \"{rotulo}\" não existe ou não está à mostra"); return false; }
        if (!alvo.interactable) Nota($"botão \"{rotulo}\" está desligado (interactable = false) na hora de tocar");
        Vector2 p = NaTela((RectTransform)alvo.transform);
        if (!AreaEmPixels.Contains(p)) Nota($"botão \"{rotulo}\" está FORA da área 16:9 ({p.x:0},{p.y:0})");
        var es = EventSystem.current;
        if (es == null) { Nota("cena sem EventSystem: " + rotulo); return false; }
        var dados = new PointerEventData(es) { position = p, button = PointerEventData.InputButton.Left, clickCount = 1 };
        var hits = new List<RaycastResult>();
        es.RaycastAll(dados, hits);
        if (hits.Count == 0) { Nota($"botão \"{rotulo}\": o toque não acerta nada (sem raycast)"); return false; }
        GameObject topo = hits[0].gameObject;
        bool certo = topo.transform.IsChildOf(alvo.transform);
        if (!certo) Nota($"botão \"{rotulo}\" está COBERTO por {Caminho(topo.transform)}");
        dados.pointerCurrentRaycast = dados.pointerPressRaycast = hits[0];
        GameObject press = ExecuteEvents.ExecuteHierarchy(topo, dados, ExecuteEvents.pointerDownHandler);
        GameObject click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(topo);
        dados.pointerPress = press ?? click;
        dados.rawPointerPress = topo;
        if (dados.pointerPress != null) ExecuteEvents.Execute(dados.pointerPress, dados, ExecuteEvents.pointerUpHandler);
        if (click != null) ExecuteEvents.Execute(click, dados, ExecuteEvents.pointerClickHandler);
        L($"clique: {rotulo} {(certo ? "ok" : "(acertou outra coisa)")}");
        return certo;
    }

    static bool Clicar(string nome) => Clicar(Botao(nome), nome);

    // Segurar e soltar (botões de acelerar/virar, que respondem a apertar e soltar).
    static readonly Dictionary<Selectable, PointerEventData> segurados = new Dictionary<Selectable, PointerEventData>();

    static void Segurar(Selectable alvo, bool segurar, string rotulo)
    {
        if (alvo == null) { Nota($"botão de segurar \"{rotulo}\" não existe"); return; }
        if (segurar == segurados.ContainsKey(alvo)) return;
        if (segurar)
        {
            Vector2 p = NaTela((RectTransform)alvo.transform);
            if (!AreaEmPixels.Contains(p)) Nota($"botão \"{rotulo}\" está FORA da área 16:9");
            var dados = new PointerEventData(EventSystem.current) { position = p, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(dados, hits);
            if (hits.Count == 0) { Nota($"botão de segurar \"{rotulo}\": o toque não acerta nada"); return; }
            if (!hits[0].gameObject.transform.IsChildOf(alvo.transform)) Nota($"botão \"{rotulo}\" está COBERTO por {Caminho(hits[0].gameObject.transform)}");
            dados.pointerCurrentRaycast = dados.pointerPressRaycast = hits[0];
            dados.pointerPress = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, dados, ExecuteEvents.pointerDownHandler);
            segurados[alvo] = dados;
        }
        else
        {
            var dados = segurados[alvo];
            segurados.Remove(alvo);
            if (dados.pointerPress != null) ExecuteEvents.Execute(dados.pointerPress, dados, ExecuteEvents.pointerUpHandler);
        }
    }

    static void SoltarTudo()
    {
        foreach (var s in segurados.Keys.ToList()) Segurar(s, false, s.name);
    }

    // Toque com o dedo num objeto 3D (ou num ponto da tela).
    static IEnumerator Tocar(Vector2 p, string rotulo)
    {
        if (!AreaEmPixels.Contains(p)) Nota($"toque em \"{rotulo}\" fora da área 16:9");
        Toque.Simular(p, true);
        yield return 0.08f;
        Toque.Simular(p, false);
        yield return 0.08f;
        L("toque: " + rotulo);
    }

    static IEnumerator Arrastar(Vector2 de, Vector2 ate, float duracao, string rotulo)
    {
        Toque.Simular(de, true);
        yield return 0.1f;
        int passos = Mathf.Max(6, Mathf.RoundToInt(duracao / 0.04f));
        for (int i = 1; i <= passos; i++)
        {
            Toque.Simular(Vector2.Lerp(de, ate, i / (float)passos), true);
            yield return 0.04f;
        }
        yield return 0.1f;
        Toque.Simular(ate, false);
        yield return 0.15f;
        L($"arrasto: {rotulo}");
    }

    static IEnumerator Tecla(KeyCode k, float duracao = 0.1f)
    {
        Teclado.Simular(k, true);
        yield return duracao;
        Teclado.Simular(k, false);
        yield return 0.05f;
    }

    // ------------------------------------------------------------------ fotos e inspeção

    static IEnumerator Foto(string nome)
    {
        yield return 0.15f;
        fotos++;
        string arquivo = $"{PastaDasFotos}/{fotos:00}_{Cena}_{nome}.png";
        ScreenCapture.CaptureScreenshot(arquivo);
        yield return 0.25f;
        Inspecionar(nome);
    }

    // Texto cortado ou pequeno, material rosa.
    static void Inspecionar(string momento)
    {
        float escalaPra1080 = 1080f / Mathf.Max(1f, AreaEmPixels.height);
        foreach (Text t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude))
        {
            if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.1f) continue;
            Canvas c = t.canvas;
            if (c == null || c.renderMode == RenderMode.WorldSpace) continue;
            var gen = t.cachedTextGenerator;
            int tamanho = t.resizeTextForBestFit && gen.fontSizeUsedForBestFit > 0 ? gen.fontSizeUsedForBestFit : t.fontSize;
            float px = tamanho * c.scaleFactor * escalaPra1080;
            string texto = t.text.Replace("\n", " ");
            if (texto.Length > 40) texto = texto.Substring(0, 40) + "...";
            if (px < 26f) Nota($"texto pequeno ({px:0} px em 1080): \"{texto}\" ({Caminho(t.transform)})");
            if (gen.characterCountVisible > 0 && gen.characterCountVisible < t.text.Replace("\n", "").Replace(" ", "").Length * 0.95f
                && t.verticalOverflow == VerticalWrapMode.Truncate)
                Nota($"texto CORTADO: \"{texto}\" ({Caminho(t.transform)})");
        }
        foreach (Renderer r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            foreach (Material m in r.sharedMaterials)
                if (m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                    Nota($"MATERIAL faltando/rosa em {Caminho(r.transform)}");
        }
    }

    static IEnumerator Medir(string nome)
    {
        int quadros = 0;
        float t0 = Time.unscaledTime;
        int tris = 0, batches = 0, setPass = 0;
        while (Time.unscaledTime - t0 < 2f)
        {
            quadros++;
            tris = Mathf.Max(tris, UnityStats.triangles);
            batches = Mathf.Max(batches, UnityStats.drawCalls);
            setPass = Mathf.Max(setPass, UnityStats.setPassCalls);
            yield return 0f;
        }
        float fps = quadros / (Time.unscaledTime - t0);
        int luzes = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Count(l => l.enabled);
        int sombras = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Count(r => r.enabled && r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off);
        File.AppendAllText(Path.Combine(Pasta, "desempenho.txt"), $"{Cena}\t{nome}\tfps={fps:0}\ttriangulos={tris}\tdrawcalls={batches}\tsetpass={setPass}\tluzes={luzes}\tcom_sombra={sombras}\n");
    }

    // ------------------------------------------------------------------ roteiro

    static IEnumerator Roteiro()
    {
        L("== começo");
        yield return 2f;
        if (Faz("menu")) yield return Menu();
        if (Faz("entrar")) yield return Entrar();
        if (Faz("abertura")) yield return Abertura();
        yield return EsperarAte(() => Cena == "Hub", 10, "chegar no Hub");
        if (Faz("hub")) { yield return Foto("hub"); yield return Medir("andando"); }
        if (Faz("oficina")) yield return Area("Area_Oficina", "Minigame_Oficina", null, Oficina());
        if (Faz("obras")) yield return Area("Area_ObrasDaCidade", "Minigame_Obras", "Pontes e Passagens", Obras());
        if (Faz("ruas")) yield return Area("Area_ObrasDaCidade", "Minigame_Obras", "Planejar a rua", Obras());
        if (Faz("motorista")) yield return Area("Area_CruzamentoCentral", "Minigame_Sinais", "Motorista Mirim", Motorista());
        if (Faz("sinaleiro")) yield return Area("Area_CruzamentoCentral", "Minigame_Sinais", "Sinaleiro", Sinaleiro());
        if (Faz("carro")) yield return Area("Area_RuaViva", "Minigame_RuaViva_Carro", "Passeio de Carro", Carro());
        if (Faz("pedalada")) yield return Area("Area_RuaViva", "Minigame_RuaViva_Carro", "Pedalada", Pedalada());
        if (Faz("travessia")) yield return Area("Area_TravessiaSegura", "Minigame_TravessiaSegura", null, Travessia());
        if (Faz("corre")) yield return Area("Area_CorreCorrendo", "Minigame_CorreCorrendo", null, Corre());
        if (Faz("album")) yield return Album();
        if (Faz("loja")) yield return LojaDoHub();
        if (Faz("sair")) yield return Sair();
        L("FIM");
    }

    static IEnumerator Menu()
    {
        yield return Foto("menu");
        yield return Medir("menu");
        Clicar("Créditos");
        yield return 0.5f;
        yield return Foto("creditos");
        Clicar(GameObject.Find("Tela_Creditos").transform.Find("Cartao/Botao_Voltar").GetComponent<Button>(), "Voltar (créditos)");
        yield return 0.4f;
        Clicar("Criar meu perfil");
        yield return 0.5f;
        foreach (char c in "PILOTO") { Clicar("Tecla_" + c); yield return 0.12f; }
        yield return Foto("criar_nome");
        Clicar("Botao_Pronto");
        yield return 0.5f;
        var avatar = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b => b.name.StartsWith("Avatar_")).OrderBy(b => b.name).FirstOrDefault();
        Clicar(avatar, "avatar");
        yield return 0.3f;
        yield return Foto("criar_avatar");
        Clicar("Botao_Pronto");
        yield return 0.5f;
        var figuras = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b => b.name.StartsWith("Figura_")).OrderBy(b => b.name).ToList();
        for (int i = 0; i < 3 && i < figuras.Count; i++) { Clicar(figuras[i], "figura " + figuras[i].name); yield return 0.25f; }
        yield return Foto("criar_codigo");
        Clicar("Botao_Pronto");
        yield return EsperarAte(() => Cena != "Menu_Inicial", 8, "sair do menu depois de criar o perfil");
        L("perfil ativo: " + Perfis.Ativo.nome);
    }

    static IEnumerator Entrar()
    {
        yield return EsperarAte(() => Cena == "Menu_Inicial", 5);
        Clicar("Entrar");
        yield return 0.8f;
        var item = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.name.StartsWith("Perfil_Piloto"));
        Clicar(item, "perfil Piloto");
        yield return 0.6f;
        var figuras = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b => b.name.StartsWith("Figura_")).OrderBy(b => b.name).ToList();
        for (int i = 0; i < 3 && i < figuras.Count; i++) { Clicar(figuras[i], "figura " + figuras[i].name); yield return 0.25f; }
        yield return Foto("entrar_codigo");
        Clicar("Botao_Pronto");
        yield return EsperarAte(() => Cena == "Hub", 8, "entrar no Hub com o perfil");
    }

    static IEnumerator Abertura()
    {
        yield return EsperarAte(() => Cena == "Abertura", 6, "abrir a abertura");
        if (Cena != "Abertura") yield break;
        var h = UnityEngine.Object.FindAnyObjectByType<CenaDaHistoria>();
        int plano = -1;
        float t0 = Time.unscaledTime;
        bool mediu = false;
        while (Cena == "Abertura" && Time.unscaledTime - t0 < 60f)
        {
            if (h != null && h.PlanoAtual != plano && h.PlanoAtual >= 0)
            {
                plano = h.PlanoAtual;
                yield return 2.2f;
                if (Cena != "Abertura") break;
                yield return Foto("abertura_plano_" + (plano + 1));
                if (plano == 2 && !mediu) { mediu = true; yield return Medir("chuva_e_enchente"); }
                if (plano == 0) Clicar(h.botaoOuvir.GetComponent<Button>(), "Ouvir (legenda)");
            }
            yield return 0.1f;
        }
        L($"abertura terminou em {Time.unscaledTime - t0:0.0} s");
    }

    // Anda até a área (setas simuladas, pelas ruas), escolhe a aba e o nível 1, joga, e volta ao Hub.
    static readonly Dictionary<string, Vector3[]> Caminhos = new Dictionary<string, Vector3[]>
    {
        // Pelo Eixo, na pista da direita (x = 6): o calçadão do meio (x = 0) tem a área do Cruzamento em z = 50.
        ["Area_Oficina"] = new[] { V(10, -10), V(10, 30), V(6, 36), V(6, 64), V(-76, 64), V(-76, 70) },
        ["Area_ObrasDaCidade"] = new[] { V(10, -10), V(10, 30), V(6, 36), V(6, 64), V(46, 64), V(46, 69) },
        ["Area_CruzamentoCentral"] = new[] { V(10, -10), V(10, 30), V(6, 36), V(6, 46), V(0, 50) },
        ["Area_RuaViva"] = new[] { V(10, -10), V(10, 30), V(6, 36), V(6, 64), V(-45, 64), V(-45, 48) },
        ["Area_TravessiaSegura"] = new[] { V(10, -10), V(0, -10), V(0, -20) },
        ["Area_CorreCorrendo"] = new[] { V(10, -10), V(-10, -10), V(-10, 0), V(-20, 0) },
    };

    static Vector3 V(float x, float z) => new Vector3(x, 0, z);

    static IEnumerator Area(string area, string cena, string aba, IEnumerator jogo)
    {
        L($"== {area} ({aba ?? cena})");
        if (Cena != "Hub")
        {
            Nota("estava em " + Cena + " em vez do Hub: voltando pela seta");
            Clicar("Botao_VoltarAoHub");
        }
        yield return EsperarAte(() => Cena == "Hub", 15, "voltar ao Hub");
        if (Cena != "Hub") yield break;
        yield return 1f;
        var player = GameObject.Find("Player");
        var caminho = new List<Vector3>();
        if (Vector3.Distance(Achatar(player.transform.position), V(10, -10)) > 3f)
            caminho.Add(new Vector3(player.transform.position.x > 30 ? 10 : player.transform.position.x, 0, -35));
        caminho.AddRange(Caminhos[area]);
        foreach (Vector3 alvo in caminho)
        {
            yield return AndarAte(player, alvo, cena);
            if (Cena != "Hub") break;
        }
        yield return EsperarAte(() => Cena == cena, 6, "entrar em " + cena);
        if (Cena != cena) { Nota($"não entrou em {cena} andando até a área {area}"); yield break; }
        yield return 1.5f;
        yield return Foto("selecao" + (aba != null ? "_" + aba.Replace(' ', '_') : ""));
        if (aba != null)
        {
            var botaoDaAba = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.GetComponentInChildren<Text>() != null && b.GetComponentInChildren<Text>().text == aba);
            if (botaoDaAba != null && !TituloDaSelecaoE(aba))
            {
                Clicar(botaoDaAba, "aba " + aba);
                yield return EsperarAte(() => TituloDaSelecaoE(aba), 8, "abrir a aba " + aba);
                yield return 1f;
                yield return Foto("selecao_" + aba.Replace(' ', '_'));
            }
        }
        Clicar("Nivel_1");
        yield return 2f;
        yield return jogo;
        SoltarTudo();
        Toque.PararDeSimular();
        yield return EsperarAte(() => UnityEngine.Object.FindAnyObjectByType<TelaFimDeJogo>() != null, 5, "tela de fim");
        yield return 1.5f;
        var fim = UnityEngine.Object.FindAnyObjectByType<TelaFimDeJogo>();
        if (fim != null)
        {
            yield return Foto("fim_de_jogo");
            L("fim: " + string.Join(" | ", fim.GetComponentsInChildren<Text>().Select(t => t.text.Replace("\n", " ")).Where(t => t.Length > 0)));
            Clicar("Button_VoltarAoHub");
        }
        else
        {
            Nota("sem tela de fim depois do nível 1 de " + (aba ?? cena) + ": voltando pelo botão da seta");
            Clicar("Botao_VoltarAoHub");
        }
        yield return EsperarAte(() => Cena == "Hub", 10, "voltar ao Hub");
    }

    static bool TituloDaSelecaoE(string aba)
    {
        var tela = UnityEngine.Object.FindAnyObjectByType<TelaSelecaoDeNivel>();
        return tela != null && tela.titulo != null && tela.titulo.text == aba;
    }

    static Vector3 Achatar(Vector3 v) => new Vector3(v.x, 0, v.z);

    static IEnumerator AndarAte(GameObject player, Vector3 alvo, string cenaDoFim)
    {
        float t0 = Time.unscaledTime, tUltimo = Time.unscaledTime;
        Vector3 ultimo = player.transform.position;
        KeyCode[] teclas = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
        while (Cena == "Hub" && player != null)
        {
            Vector3 d = Achatar(alvo - player.transform.position);
            if (d.magnitude < 1f) break;
            // As setas andam em relação ao olhar da câmera.
            Vector3 frente = Achatar(Camera.main.transform.forward).normalized;
            Vector3 direita = Vector3.Cross(Vector3.up, frente);
            float f = Vector3.Dot(d.normalized, frente), r = Vector3.Dot(d.normalized, direita);
            Teclado.Simular(KeyCode.W, f > 0.3f);
            Teclado.Simular(KeyCode.S, f < -0.3f);
            Teclado.Simular(KeyCode.D, r > 0.3f);
            Teclado.Simular(KeyCode.A, r < -0.3f);
            if (Time.unscaledTime - tUltimo > 2.5f)
            {
                if (Vector3.Distance(player.transform.position, ultimo) < 0.6f)
                {
                    Nota($"o personagem ficou PRESO perto de {player.transform.position:F0} indo pra {alvo:F0}");
                    var cc = player.GetComponent<CharacterController>();
                    if (cc) cc.enabled = false;
                    player.transform.position = alvo + Vector3.up;
                    if (cc) cc.enabled = true;
                }
                ultimo = player.transform.position;
                tUltimo = Time.unscaledTime;
            }
            if (Time.unscaledTime - t0 > 40f) { Nota("andar demorou demais até " + alvo); break; }
            yield return 0.05f;
        }
        foreach (var k in teclas) Teclado.Simular(k, false);
    }

    // ------------------------------------------------------------------ jogos

    static IEnumerator Oficina()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<OficinaManager>();
        bool fotoMontando = false, fotoPista = false;
        float t0 = Time.unscaledTime;
        var tentativas = new Dictionary<PecaDaOficina, int>();
        while (!m.Finalizado && Time.unscaledTime - t0 < 240f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            if (m.EtapaAtual == OficinaManager.Etapa.Montando && m.Receita != null)
            {
                if (!fotoMontando) { fotoMontando = true; yield return Foto("montando"); yield return Medir("montando"); }
                var peca = m.Pecas.FirstOrDefault(p => p != null && p.NaPrateleira && !p.EmMovimento && m.Receita.ItemQueAceita(p.tipo) != null
                    && m.Receita.Proibida(p.tipo) == null && (!tentativas.ContainsKey(p) || tentativas[p] < 2));
                if (peca == null) { yield return 0.3f; continue; }
                tentativas[peca] = tentativas.TryGetValue(peca, out int n) ? n + 1 : 1;
                yield return Arrastar(NaTela(peca.transform.position), NaTela(m.bancada.position + Vector3.up), 0.5f, "peça " + peca.tipo);
                yield return 0.6f;
                continue;
            }
            if (m.EtapaAtual == OficinaManager.Etapa.Testando && m.pista.Ativa)
            {
                if (!fotoPista) { fotoPista = true; yield return 0.8f; yield return Foto("pista"); }
                if (m.pista.EsperandoEngate) { Clicar("Botao_Engatar"); yield return 1.5f; continue; }
                Transform carro = m.pista.Guiado;
                var moeda = m.pista.moedas.Where(x => x.activeSelf).OrderBy(x => Vector3.Distance(x.transform.position, carro.position)).FirstOrDefault();
                Segurar(Botao("Botao_Acelerar"), true, "Acelerar");
                float angulo = moeda != null ? Vector3.SignedAngle(carro.forward, Achatar(moeda.transform.position - carro.position), Vector3.up) : 0f;
                Segurar(Botao("Botao_Direita"), angulo > 10f, "Direita");
                Segurar(Botao("Botao_Esquerda"), angulo < -10f, "Esquerda");
                yield return 0.05f;
                continue;
            }
            SoltarTudo();
            yield return 0.2f;
        }
        L($"oficina: finalizado={m.Finalizado} erros={m.Erros}");
    }

    static IEnumerator Obras()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<ObrasManager>();
        float t0 = Time.unscaledTime;
        bool foto = false;
        int tentativas = 0;
        while (!m.Finalizado && Time.unscaledTime - t0 < 200f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            var p = m.ProblemaAtual;
            var lote = m.IndiceDoProblema >= 0 ? m.Lotes[m.IndiceDoProblema] : null;
            if (p == null || lote == null || lote.resolvido) { yield return 0.3f; continue; }
            if (!foto) { foto = true; yield return Foto("problema"); yield return Medir("obra"); }
            var palavra = VerbeteDatabase.Instancia.Buscar(p.correta).palavra;
            var botao = UnityEngine.Object.FindObjectsByType<BotaoDeEstrutura>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.nome != null && b.nome.text.ToLower() == palavra.ToLower());
            if (botao == null) { Nota("não achei o botão da estrutura " + palavra); yield return 0.5f; continue; }
            if (m.EstruturaEscolhida != p.correta) { Clicar(botao.GetComponent<Selectable>() ?? botao.GetComponentInChildren<Selectable>(), "estrutura " + palavra); yield return 0.6f; }
            if (++tentativas == 1) yield return Foto("escolheu_estrutura");
            yield return Tocar(NaTela(lote.local.transform.position), "lugar da obra");
            float esperaObra = Time.unscaledTime;
            while (!lote.resolvido && Time.unscaledTime - esperaObra < 18f)
            {
                if (CartaoVerbete.EstaAberto) yield return FecharCartao();
                yield return 0.2f;
            }
            if (!lote.resolvido) Nota("a obra não ficou pronta depois de tocar no lugar (" + palavra + ")");
            if (tentativas == 1) yield return Foto("obra_pronta");
            yield return 0.5f;
        }
        L($"obras: finalizado={m.Finalizado} erros={m.Erros} resolvidos={m.Resolvidos}");
    }

    static IEnumerator Motorista()
    {
        var g = UnityEngine.Object.FindAnyObjectByType<SinaisManager>();
        var mo = g.motorista;
        var acelerar = Botao("Botao_Acelerar");
        var frear = Botao("Botao_Frear");
        float t0 = Time.unscaledTime;
        var corFotografada = new HashSet<CorDoSinal>();
        while (!g.Finalizado && Time.unscaledTime - t0 < 200f)
        {
            if (CartaoVerbete.EstaAberto) { SoltarTudo(); yield return FecharCartao(); continue; }
            var c = mo.ProximoCruzamento;
            bool acel = true, freio = false;
            if (c != null)
            {
                float dist = c.sDaLinha - mo.S;
                var cor = c.sinal.Cor;
                if (dist < 26f && dist > 0f && corFotografada.Add(cor)) { SoltarTudo(); yield return Foto("sinal_" + cor); continue; }
                if (cor == CorDoSinal.Amarelo && dist < 30f && dist > -1f) { acel = mo.Velocidade < 2.5f; freio = mo.Velocidade > 4f; }
                else if (cor == CorDoSinal.Vermelho && dist < 35f && dist > -0.5f)
                {
                    float parada = 2.5f + mo.Velocidade * mo.Velocidade / 10f + mo.Velocidade * 0.25f;
                    acel = dist > parada + 6f && mo.Velocidade < 6f;
                    freio = dist <= parada + 1f;
                }
            }
            Segurar(acelerar, acel && !freio, "Acelerar");
            Segurar(frear, freio, "Frear");
            yield return 0.05f;
        }
        SoltarTudo();
        L($"motorista: finalizado={g.Finalizado} erros={g.Erros}");
    }

    static IEnumerator Sinaleiro()
    {
        var g = UnityEngine.Object.FindAnyObjectByType<SinaisManager>();
        var s = g.sinaleiro;
        yield return 1f;
        yield return Foto("cruzamento");
        var verdeA = UnityEngine.Object.FindObjectsByType<BotaoDeSinal>(FindObjectsInactive.Exclude).FirstOrDefault(b => b.grupo == 0 && b.cor == CorDoSinal.Verde);
        if (verdeA == null) { Nota("não achei o botão verde da via azul"); yield break; }
        Clicar(verdeA.GetComponent<Selectable>(), "verde da via azul");
        float t0 = Time.unscaledTime;
        bool foto = false;
        while (!g.Finalizado && Time.unscaledTime - t0 < 120f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            if (!foto && s.CarrosQuePassaram > 0) { foto = true; yield return Foto("carros_passando"); }
            yield return 0.3f;
        }
        L($"sinaleiro: finalizado={g.Finalizado} erros={g.Erros} carros={s.CarrosQuePassaram}/{s.TotalDeCarros}");
    }

    static IEnumerator Carro()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<RuaVivaCarroManager>();
        yield return EsperarAte(() => m.EtapaAtual == RuaVivaCarroManager.Etapa.Missao, 5, "cartão da missão");
        yield return Foto("missao");
        Clicar("Botao_Vamos");
        float t0 = Time.unscaledTime;
        bool fotoCinto = false, fotoChave = false, fotoPasseio = false;
        var pedidosTocados = new HashSet<ObjetoDoSafari>();
        while (!m.Finalizado && Time.unscaledTime - t0 < 200f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            switch (m.EtapaAtual)
            {
                case RuaVivaCarroManager.Etapa.Cintos:
                    if (m.EsperandoCinto && m.CintoDaVez != null)
                    {
                        if (!fotoCinto) { fotoCinto = true; yield return Foto("cinto_antes"); yield return Medir("dentro_do_carro"); }
                        var c = m.CintoDaVez;
                        yield return Arrastar(NaTela(c.fivela.position), NaTela(c.fecho.position), 0.7f, "fivela do assento " + m.AssentoDaVez);
                        yield return 0.8f;
                        if (!c.Preso) Nota($"arrastar a fivela até o fecho não prendeu o cinto do assento {m.AssentoDaVez}");
                    }
                    break;
                case RuaVivaCarroManager.Etapa.Chave:
                    if (!fotoChave) { fotoChave = true; yield return Foto("todos_de_cinto"); }
                    yield return Tocar(NaTela(m.carro.chave.bounds.center), "chave");
                    yield return 1.5f;
                    break;
                case RuaVivaCarroManager.Etapa.Passeio:
                case RuaVivaCarroManager.Etapa.Parada:
                    if (!fotoPasseio && m.TempoGasto > 3f) { fotoPasseio = true; yield return Foto("passeio"); yield return Medir("passeio"); }
                    var obj = m.ObjetoPedido;
                    if (obj != null && Visivel(obj.transform.position + Vector3.up) && !m.Pausado)
                    {
                        if (pedidosTocados.Add(obj)) yield return Foto("pedido_" + m.VerbetePedido);
                        int antes = m.Acertos;
                        yield return Tocar(NaTela(obj.transform.position + Vector3.up), "safari " + m.VerbetePedido);
                        yield return 0.4f;
                        if (m.Acertos == antes && m.ObjetoPedido == obj) L("toque no safari não acertou ainda: " + m.VerbetePedido);
                    }
                    break;
            }
            yield return 0.15f;
        }
        L($"carro: finalizado={m.Finalizado} acertos={m.Acertos}/{m.Pedidos} toquesErrados={m.ToquesErrados} cintos={m.CintosPresos}");
    }

    static IEnumerator Pedalada()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<PedaladaManager>();
        yield return EsperarAte(() => m.EtapaAtual == PedaladaManager.Etapa.Roteiro, 5, "cartão do roteiro");
        yield return Foto("roteiro");
        Clicar("Botao_Vamos");
        yield return 0.6f;
        yield return Foto("capacete");
        var capacete = m.coisasDaCabeca.First(c => c.id == "capacete");
        yield return Tocar(NaTela(capacete.transform.position + Vector3.up * 0.1f), "capacete");
        yield return EsperarAte(() => m.EtapaAtual == PedaladaManager.Etapa.Pedalando, 10, "começar a pedalar");
        var tr = m.Roteiro.trechos;
        float t0 = Time.unscaledTime;
        bool foto = false;
        while (!m.Finalizado && Time.unscaledTime - t0 < 120f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            if (m.EtapaAtual == PedaladaManager.Etapa.Pedalando)
            {
                if (!foto && m.S > 20f) { foto = true; yield return Foto("pedalando"); yield return Medir("pedalando"); }
                int k = m.TrechoAtual;
                float inicioProx = 0f; for (int i = 0; i <= k; i++) inicioProx += tr[i].comprimento;
                int alvo = (k + 1 < tr.Length && inicioProx - m.S <= 11f) ? tr[k + 1].FaixaCerta : tr[k].FaixaCerta;
                // Teclado: setas (a criança com teclado).
                if (m.Faixa < alvo) yield return Tecla(KeyCode.RightArrow);
                else if (m.Faixa > alvo) yield return Tecla(KeyCode.LeftArrow);
            }
            yield return 0.1f;
        }
        L($"pedalada: finalizado={m.Finalizado} lugarErrado={m.VezesNoLugarErrado} batidas={m.Batidas}");
    }

    static IEnumerator Travessia()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<TravessiaManager>();
        yield return 1f;
        yield return Foto("calcada");
        float t0 = Time.unscaledTime;
        bool foto = false;
        while (!m.Finalizado && Time.unscaledTime - t0 < 90f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            if (!m.Olhou) { Clicar("Botao_Olhar"); yield return 1.5f; continue; }
            int pista = Mathf.Clamp(m.Posicao, 0, m.trafego.pistas.Count - 1);
            if (m.Posicao <= m.trafego.pistas.Count && m.trafego.TempoAteAFaixa(pista) > 2.2f)
            {
                yield return Tecla(KeyCode.UpArrow);
                if (!foto) { foto = true; yield return Foto("atravessando"); }
                yield return 0.8f;
            }
            yield return 0.1f;
        }
        L($"travessia: finalizado={m.Finalizado} vidas={m.Vidas} tropeços={m.Tropecos}");
    }

    static IEnumerator Corre()
    {
        var m = UnityEngine.Object.FindAnyObjectByType<CorreManager>();
        yield return 1f;
        float t0 = Time.unscaledTime;
        bool foto = false;
        while (!m.Finalizado && Time.unscaledTime - t0 < 120f)
        {
            if (CartaoVerbete.EstaAberto) { yield return FecharCartao(); continue; }
            if (!foto && m.pista.Distancia > 20f) { foto = true; yield return Foto("correndo"); yield return Medir("correndo"); }
            float larg = 2.4f;
            int faixa = m.jogador.Faixa;
            bool Bloqueada(int f) => m.pista.objetos.Any(o => o != null && !o.resolvido && o.categoria == CorreObjeto.Categoria.Obstaculo
                && Mathf.RoundToInt(o.x / larg) == f && o.z > 0.5f && o.z < 10f);
            if (Bloqueada(faixa))
            {
                int[] opcoes = { faixa - 1, faixa + 1 };
                int livre = opcoes.Where(f => f >= -1 && f <= 1).FirstOrDefault(f => !Bloqueada(f) && !Bloqueada((f + faixa) / 2));
                if (livre != 0 || (faixa != 0 && !Bloqueada(0)))
                    Clicar(livre > faixa ? "Botao_Direita" : "Botao_Esquerda");
                else
                    Clicar("Botao_Pular");
                yield return 0.35f;
            }
            yield return 0.05f;
        }
        L($"corre: finalizado={m.Finalizado} escudos={m.Escudos} batidas={m.Batidas} moedas={m.Moedas}");
    }

    static IEnumerator Album()
    {
        yield return EsperarAte(() => Cena == "Hub", 10);
        yield return 1f;
        Clicar("Botao_Album");
        yield return EsperarAte(() => Cena == "Album_Glotransito", 6, "abrir o álbum");
        yield return 1f;
        yield return Foto("album");
        var fig = UnityEngine.Object.FindObjectsByType<FigurinhaDoAlbum>(FindObjectsInactive.Exclude).FirstOrDefault(f => f.bloqueado != null && !f.bloqueado.activeSelf);
        if (fig != null) { Clicar(fig.botao, "figurinha " + fig.nome.text); yield return 1.2f; yield return Foto("album_cartao"); yield return FecharCartao(); }
        else Nota("nenhuma figurinha desbloqueada no álbum depois de jogar");
        var abas = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b => b.name.StartsWith("Aba_")).OrderBy(b => b.name).ToList();
        if (abas.Count > 1) { Clicar(abas[1], abas[1].name); yield return 0.5f; yield return Foto("album_aba_2"); }
        Clicar("Botao_Voltar");
        yield return EsperarAte(() => Cena == "Hub", 6, "voltar do álbum");
    }

    static IEnumerator LojaDoHub()
    {
        yield return 1f;
        L("moedas antes da loja: " + Loja.Moedas);
        Clicar("Botao_Loja");
        yield return 0.6f;
        yield return Foto("loja");
        var item = Loja.Catalogo.itens.Where(i => i.preco <= Loja.Moedas).OrderByDescending(i => i.tipo == TipoDeItemDaLoja.Acessorio).FirstOrDefault();
        if (item == null) { Nota($"com {Loja.Moedas} moedas não dá pra comprar nada depois de jogar o nível 1 de tudo"); item = Loja.Catalogo.itens[0]; }
        var botao = GameObject.Find("Item_" + item.id)?.transform.Find("Botao")?.GetComponent<Button>();
        Clicar(botao, "Comprar " + item.nome);
        yield return 0.4f;
        Clicar(botao, "Vestir " + item.nome);
        yield return 0.4f;
        yield return Foto("loja_vestido");
        Clicar(GameObject.Find("Painel_Da_Loja")?.transform.Find("Cartao/Botao_Voltar")?.GetComponent<Button>(), "fechar a loja");
        yield return 1f;
        yield return Foto("hub_vestido");
        L($"loja: tem {item.id}={Loja.Tem(item.id)} vestido={Loja.Vestido(item.id)} moedas={Loja.Moedas}");
    }

    static IEnumerator Sair()
    {
        Clicar("Botao_Trocar_De_Perfil");
        yield return EsperarAte(() => Cena == "Menu_Inicial", 6, "voltar ao menu");
        yield return Foto("menu_de_novo");
        Clicar("Entrar");
        yield return 0.6f;
        yield return Foto("lista_de_perfis");
    }
}
