using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A campanha no Hub: NPCs, conversas (balões com Ouvir), objetivo fixo no alto, trilha de setas no chão,
// tutorial do Dia 1 com a mãe, áreas bloqueadas (o NPC explica) e a volta de uma missão (o NPC agradece,
// o lugar se conserta com uma animação curta e as moedas aparecem). Gerado pelo CampanhaConstrutor.
public class CampanhaNoHub : MonoBehaviour
{
    public static CampanhaNoHub Instancia { get; private set; }

    [Header("Cena")]
    public NpcDaCampanha[] npcs = new NpcDaCampanha[0];
    public Transform jogador;
    [Tooltip("Um grupo Conserto_<id> por missão, com os filhos Antes e Depois.")]
    public Transform consertos;
    public ParticleSystem confete;
    public ParticleSystem poeira;
    public AudioSource som;
    public AudioClip somDoConserto, somDasMoedas, somDaConversa;

    [Header("Trilha de setas")]
    public GameObject modeloDaSeta;
    [Tooltip("Grade do mapa pra achar o caminho (assada pelo construtor): 1 = dá pra andar.")]
    public byte[] livre = new byte[0];
    public int largura, altura;
    public Vector2 origemDaGrade;
    public float passoDaGrade = 0.6f;
    public float espacoEntreSetas = 2.4f;
    public int maximoDeSetas = 45;

    [Header("Interface")]
    public GameObject barraDoObjetivo;
    public Text textoDoObjetivo;
    public GameObject painelDaConversa;
    public Text nomeDeQuemFala;
    public Text textoDaFala;
    public Button botaoOuvir;
    public Button botaoSeguir;
    public Text textoDoSeguir;
    public Button botaoConversar;
    public Button botaoPular;
    [Tooltip("Botão \"Ir\" na barra do objetivo: leva o jogador pra perto de quem ele precisa encontrar.")]
    public Button botaoIr;
    public RectTransform setaDoAlbum;
    public GameObject avisoDasMoedas;
    public Text textoDasMoedas;
    public GameObject dicaDoOlhar;

    public bool Conversando => painelDaConversa != null && painelDaConversa.activeSelf;

    private readonly List<Transform> setas = new List<Transform>();
    private string[] falas = new string[0];
    private int falaAtual;
    private Action aoTerminar;
    private NpcDaCampanha falando;
    private PlayerMovement movimento;
    private float proximaTrilha;
    private Vector3 alvoDaTrilha;
    private bool temAlvo;
    private bool ocupado; // sequência de volta da missão rodando
    private List<Vector3> caminhoGuardado;
    private Vector3 alvoGuardado, inicioGuardado;
    private NpcDaCampanha alvoDoIr;

    private CampanhaDados D => Campanha.Dados;
    private ProgressoDaCampanha P => Campanha.P;

    private void Awake()
    {
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        if (!Campanha.Existe)
        {
            gameObject.SetActive(false);
            return;
        }
        Campanha.Preparar();
        movimento = jogador != null ? jogador.GetComponent<PlayerMovement>() : null;
        painelDaConversa.SetActive(false);
        botaoConversar.gameObject.SetActive(false);
        avisoDasMoedas.SetActive(false);
        setaDoAlbum.gameObject.SetActive(false);
        modeloDaSeta.SetActive(false);
        botaoOuvir.onClick.AddListener(Ouvir);
        botaoSeguir.onClick.AddListener(Seguir);
        botaoConversar.onClick.AddListener(ConversarComQuemEstaPerto);
        botaoPular.onClick.AddListener(PularTutorial);
        if (botaoIr != null) botaoIr.onClick.AddListener(IrAteONpc);
        botaoOuvir.GetComponentInChildren<Text>().text = D.textoOuvir;
        botaoConversar.GetComponentInChildren<Text>().text = D.textoConversar;
        botaoPular.GetComponentInChildren<Text>().text = D.textoPular;

        // Consertos das missões já feitas: já aparecem consertados.
        foreach (MissaoDaCampanha m in Campanha.Todas())
        {
            if (Campanha.Feita(m)) AplicarConserto(m, false);
        }
        StartCoroutine(Comecar());
    }

    private IEnumerator Comecar()
    {
        yield return null; // deixa o HubSpawn pôr o jogador no lugar
        Atualizar();
        MissaoDaCampanha voltou = Campanha.Buscar(Campanha.VoltandoDe);
        Campanha.VoltandoDe = null;
        if (voltou != null)
        {
            yield return Voltar(voltou);
        }
        Atualizar();
    }

    // ------------------------------------------------------------------ a cada quadro

    private void Update()
    {
        if (ocupado) return;
        if (Conversando)
        {
            if (Teclado.Apertou(KeyCode.Return) || Teclado.Apertou(KeyCode.Space) || Teclado.Apertou(KeyCode.E)) Seguir();
            else if (Teclado.Apertou(KeyCode.O)) Ouvir();
            return;
        }

        NpcDaCampanha perto = QuemEstaPerto();
        botaoConversar.gameObject.SetActive(perto != null && !AvisoDoHub.Visivel);
        if (perto != null && (Teclado.Apertou(KeyCode.E) || TocouNo(perto)))
        {
            Conversar(perto);
            return;
        }

        if (!P.tutorialFeito && P.passoDoTutorial == 0 && Npc(QuemFala.Mae) != null && Npc(QuemFala.Mae).Distancia < 4f)
        {
            P.passoDoTutorial = 1;
            Atualizar();
        }

        if (Time.time >= proximaTrilha)
        {
            proximaTrilha = Time.time + 0.25f;
            DesenharTrilha();
        }
        AnimarSetas();
    }

    private NpcDaCampanha QuemEstaPerto()
    {
        NpcDaCampanha melhor = null;
        foreach (NpcDaCampanha n in npcs)
        {
            if (n != null && n.Perto && (melhor == null || n.Distancia < melhor.Distancia)) melhor = n;
        }
        return melhor;
    }

    private bool TocouNo(NpcDaCampanha npc)
    {
        if (!Toque.Apertou || Toque.SobreAInterface() || Camera.main == null) return false;
        Ray raio = Camera.main.ScreenPointToRay(Toque.Posicao);
        return Physics.Raycast(raio, out RaycastHit h, 60f) && h.collider.GetComponentInParent<NpcDaCampanha>() == npc;
    }

    public NpcDaCampanha Npc(QuemFala q)
    {
        foreach (NpcDaCampanha n in npcs) if (n != null && n.quem == q) return n;
        return null;
    }

    private NpcDaCampanha NpcDaArea(string area)
    {
        foreach (NpcDaCampanha n in npcs) if (n != null && n.areaDoHub == area) return n;
        return null;
    }

    // ------------------------------------------------------------------ objetivo, "!" e trilha

    public void Atualizar()
    {
        string objetivo = "";
        temAlvo = false;
        alvoDoIr = null;
        NpcDaCampanha mae = Npc(QuemFala.Mae);
        MissaoDaCampanha ativa = Campanha.Ativa;

        if (!P.tutorialFeito && P.passoDoTutorial <= 1)
        {
            objetivo = P.passoDoTutorial == 0 ? D.objetivoAndarAteAMae : D.objetivoFalarComAMae;
            Alvo(mae);
        }
        else if (!P.tutorialFeito && P.passoDoTutorial == 4)
        {
            objetivo = D.objetivoAbrirOAlbum;
        }
        else if (Campanha.MaeTemConversa)
        {
            objetivo = D.objetivoVoltarAMae;
            Alvo(mae);
            alvoDoIr = mae;
        }
        else if (ativa != null)
        {
            objetivo = ativa.objetivo;
            GameObject area = GameObject.Find(ativa.areaDoHub);
            if (area != null) { alvoDaTrilha = area.transform.position; temAlvo = true; }
            alvoDoIr = Npc(ativa.npc);
        }
        else if (Campanha.Sugerida != null)
        {
            MissaoDaCampanha s = Campanha.Sugerida;
            objetivo = string.Format(D.objetivoFalarCom, D.NomeDe(s.npc), D.LugarDe(s.npc));
            Alvo(Npc(s.npc));
            alvoDoIr = Npc(s.npc);
        }
        else
        {
            objetivo = D.objetivoLivre;
        }

        textoDoObjetivo.text = objetivo;
        barraDoObjetivo.SetActive(!string.IsNullOrEmpty(objetivo));
        botaoPular.gameObject.SetActive(!P.tutorialFeito);
        // No começo do tutorial (andar até a mãe e falar com ela) não tem "Ir": é ali que a criança aprende a andar.
        if (botaoIr != null) botaoIr.gameObject.SetActive(alvoDoIr != null && (P.tutorialFeito || P.passoDoTutorial >= 2));
        setaDoAlbum.gameObject.SetActive(!P.tutorialFeito && P.passoDoTutorial == 4);
        if (dicaDoOlhar != null) dicaDoOlhar.SetActive(P.tutorialFeito);

        // "!" em quem tem conversa nova; seta grande na mãe no começo do tutorial.
        foreach (NpcDaCampanha n in npcs)
        {
            if (n == null) continue;
            bool exclama;
            if (n.quem == QuemFala.Mae)
                exclama = (!P.tutorialFeito && P.passoDoTutorial <= 1) || Campanha.MaeTemConversa;
            else
                exclama = P.passoDoTutorial >= 2 || P.tutorialFeito ? Campanha.DisponivelDe(n.quem) != null && (ativa == null || ativa.npc != n.quem) : false;
            n.Marcar(exclama, n.quem == QuemFala.Mae && !P.tutorialFeito && P.passoDoTutorial <= 1);
        }
        proximaTrilha = 0f;
    }

    private void Alvo(NpcDaCampanha npc)
    {
        if (npc == null) return;
        alvoDaTrilha = npc.transform.position;
        temAlvo = true;
    }

    private void DesenharTrilha()
    {
        int usadas = 0;
        if (temAlvo && jogador != null && !Conversando)
        {
            if (caminhoGuardado == null || Vector3.Distance(alvoGuardado, alvoDaTrilha) > 0.5f || Vector3.Distance(inicioGuardado, jogador.position) > 1.5f)
            {
                caminhoGuardado = Caminho(jogador.position, alvoDaTrilha);
                alvoGuardado = alvoDaTrilha;
                inicioGuardado = jogador.position;
            }
            var caminho = new List<Vector3>(caminhoGuardado);
            caminho[0] = jogador.position;
            float andado = 1.6f;
            float total = 0f;
            for (int i = 1; i < caminho.Count; i++) total += Vector3.Distance(caminho[i - 1], caminho[i]);
            if (total > 3.5f)
            {
                for (int i = 1; i < caminho.Count && usadas < maximoDeSetas; i++)
                {
                    Vector3 a = caminho[i - 1], b = caminho[i];
                    float trecho = Vector3.Distance(a, b);
                    Vector3 dir = trecho > 0.01f ? (b - a) / trecho : Vector3.forward;
                    float d = andado;
                    for (; d < trecho && usadas < maximoDeSetas; d += espacoEntreSetas)
                    {
                        Vector3 p = a + dir * d;
                        if (Vector3.Distance(p, alvoDaTrilha) < 2f) continue;
                        p.y = 0.08f;
                        if (Physics.CheckSphere(p + Vector3.up * 0.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        Transform s = Seta(usadas++);
                        s.SetPositionAndRotation(p, Quaternion.LookRotation(dir));
                    }
                    andado = d - trecho;
                }
            }
        }
        for (int i = usadas; i < setas.Count; i++) setas[i].gameObject.SetActive(false);
    }

    private Transform Seta(int i)
    {
        while (setas.Count <= i)
        {
            GameObject s = Instantiate(modeloDaSeta, transform);
            s.name = "Seta_Da_Trilha";
            setas.Add(s.transform);
        }
        setas[i].gameObject.SetActive(true);
        return setas[i];
    }

    private void AnimarSetas()
    {
        float t = Time.time * 2.2f;
        for (int i = 0; i < setas.Count; i++)
        {
            if (!setas[i].gameObject.activeSelf) continue;
            float k = 0.85f + 0.25f * Mathf.Max(0f, Mathf.Sin(t - i * 0.45f));
            setas[i].localScale = new Vector3(k, 1f, k);
        }
    }

    // Caminho de verdade pelo Hub: busca A* numa grade do mapa (1 = dá pra andar), assada pelo construtor
    // com os colisores do cenário (o mesmo teste de cápsula do jogador). Depois o caminho é "alisado":
    // só ficam os pontos onde ele precisa virar. Público pro piloto de testes seguir a mesma trilha.
    public List<Vector3> Caminho(Vector3 de, Vector3 ate)
    {
        var pontos = new List<Vector3> { de };
        if (livre == null || livre.Length != largura * altura || largura == 0)
        {
            pontos.Add(ate);
            return pontos;
        }
        int a = CelulaLivreMaisPerto(Celula(de)), b = CelulaLivreMaisPerto(Celula(ate));
        if (a < 0 || b < 0) { pontos.Add(ate); return pontos; }

        var custo = new float[livre.Length];
        var veio = new int[livre.Length];
        for (int i = 0; i < custo.Length; i++) { custo[i] = float.MaxValue; veio[i] = -1; }
        var fila = new FilaDePrioridade();
        custo[a] = 0f;
        fila.Por(a, Heuristica(a, b));
        int passos = 0;
        while (fila.Quantos > 0 && passos++ < 400000)
        {
            int u = fila.Tirar();
            if (u == b) break;
            int ux = u % largura, uz = u / largura;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int vx = ux + dx, vz = uz + dz;
                    if (vx < 0 || vz < 0 || vx >= largura || vz >= altura) continue;
                    int v = vz * largura + vx;
                    if (livre[v] == 0) continue;
                    // Diagonal só se as duas vizinhas retas também estão livres (não corta quina).
                    if (dx != 0 && dz != 0 && (livre[uz * largura + vx] == 0 || livre[vz * largura + ux] == 0)) continue;
                    float nc = custo[u] + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                    if (nc < custo[v])
                    {
                        custo[v] = nc;
                        veio[v] = u;
                        fila.Por(v, nc + Heuristica(v, b));
                    }
                }
        }
        if (veio[b] < 0 && a != b) { pontos.Add(ate); return pontos; }

        var celulas = new List<int>();
        for (int c = b; c >= 0; c = veio[c]) { celulas.Add(c); if (c == a) break; }
        celulas.Reverse();
        // Alisar: de cada ponto guardado, pula pro mais longe que ainda se vê em linha reta pela grade.
        int k = 0;
        while (k < celulas.Count - 1)
        {
            int j = celulas.Count - 1;
            while (j > k + 1 && !LinhaLivre(celulas[k], celulas[j])) j--;
            if (j != celulas.Count - 1) pontos.Add(Centro(celulas[j]));
            k = j;
        }
        pontos.Add(ate);
        return pontos;
    }

    private int Celula(Vector3 p)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((p.x - origemDaGrade.x) / passoDaGrade), 0, largura - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt((p.z - origemDaGrade.y) / passoDaGrade), 0, altura - 1);
        return z * largura + x;
    }

    private Vector3 Centro(int c)
    {
        return new Vector3(origemDaGrade.x + (c % largura) * passoDaGrade, 0f, origemDaGrade.y + (c / largura) * passoDaGrade);
    }

    private float Heuristica(int a, int b)
    {
        int dx = Mathf.Abs(a % largura - b % largura), dz = Mathf.Abs(a / largura - b / largura);
        return Mathf.Max(dx, dz) + 0.4142f * Mathf.Min(dx, dz);
    }

    private int CelulaLivreMaisPerto(int c)
    {
        if (livre[c] != 0) return c;
        int cx = c % largura, cz = c / largura;
        for (int r = 1; r <= 8; r++)
        {
            int melhor = -1;
            float d = float.MaxValue;
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, z = cz + dz;
                    if (x < 0 || z < 0 || x >= largura || z >= altura || livre[z * largura + x] == 0) continue;
                    float dd = dx * dx + dz * dz;
                    if (dd < d) { d = dd; melhor = z * largura + x; }
                }
            if (melhor >= 0) return melhor;
        }
        return -1;
    }

    private bool LinhaLivre(int a, int b)
    {
        int x0 = a % largura, z0 = a / largura, x1 = b % largura, z1 = b / largura;
        int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(z1 - z0));
        for (int i = 0; i <= n; i++)
        {
            float t = n == 0 ? 0f : i / (float)n;
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), z = Mathf.RoundToInt(Mathf.Lerp(z0, z1, t));
            // Folga de uma célula pros lados (o caminho alisado não raspa nas coisas).
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx != 0 && dz != 0) continue;
                    int xx = Mathf.Clamp(x + dx, 0, largura - 1), zz = Mathf.Clamp(z + dz, 0, altura - 1);
                    if (livre[zz * largura + xx] == 0) return false;
                }
        }
        return true;
    }

    // Fila de prioridade mínima (heap binário) pro A*.
    private class FilaDePrioridade
    {
        private readonly List<int> itens = new List<int>();
        private readonly List<float> prioridades = new List<float>();
        public int Quantos => itens.Count;

        public void Por(int item, float prioridade)
        {
            itens.Add(item);
            prioridades.Add(prioridade);
            int i = itens.Count - 1;
            while (i > 0)
            {
                int pai = (i - 1) / 2;
                if (prioridades[pai] <= prioridades[i]) break;
                Trocar(i, pai);
                i = pai;
            }
        }

        public int Tirar()
        {
            int topo = itens[0];
            int ultimo = itens.Count - 1;
            Trocar(0, ultimo);
            itens.RemoveAt(ultimo);
            prioridades.RemoveAt(ultimo);
            int i = 0;
            while (true)
            {
                int e = 2 * i + 1, d = e + 1, m = i;
                if (e < itens.Count && prioridades[e] < prioridades[m]) m = e;
                if (d < itens.Count && prioridades[d] < prioridades[m]) m = d;
                if (m == i) break;
                Trocar(i, m);
                i = m;
            }
            return topo;
        }

        private void Trocar(int a, int b)
        {
            (itens[a], itens[b]) = (itens[b], itens[a]);
            (prioridades[a], prioridades[b]) = (prioridades[b], prioridades[a]);
        }
    }

    // ------------------------------------------------------------------ conversas

    private void ConversarComQuemEstaPerto()
    {
        NpcDaCampanha n = QuemEstaPerto();
        if (n != null) Conversar(n);
    }

    public void Conversar(NpcDaCampanha npc)
    {
        if (npc.quem == QuemFala.Mae) ConversarComAMae(npc);
        else ConversarComAjudante(npc);
    }

    private void ConversarComAMae(NpcDaCampanha mae)
    {
        var lista = new List<string>();
        int dia = Campanha.DiaAtual;
        bool abre = false;
        if (P.diaFechado < dia - 1 && Campanha.Dia(dia - 1) != null) lista.AddRange(Campanha.Dia(dia - 1).falasDoFim);
        if (!Campanha.Acabou && P.diaAberto < dia && Campanha.Dia(dia) != null)
        {
            lista.AddRange(Campanha.Dia(dia).falasDaMae);
            abre = true;
        }
        if (Campanha.Acabou && P.diaFechado < dia - 1) lista.AddRange(D.falasDoFimDaCampanha);
        bool conversaNova = lista.Count > 0;
        if (!conversaNova)
        {
            MissaoDaCampanha s = Campanha.Sugerida;
            if (s != null)
            {
                foreach (string f in D.falasDaMaeComMissao) lista.Add(string.Format(f, D.NomeDe(s.npc), D.LugarDe(s.npc)));
            }
            else lista.AddRange(D.falasDoFimDaCampanha);
        }
        Abrir(mae, lista.ToArray(), D.textoSeguir, () =>
        {
            if (conversaNova || abre) Campanha.AbrirDia();
            if (!P.tutorialFeito && P.passoDoTutorial <= 1) { P.passoDoTutorial = 2; Campanha.Salvar(); }
            Atualizar();
        });
    }

    private void ConversarComAjudante(NpcDaCampanha npc)
    {
        MissaoDaCampanha missao = Campanha.DisponivelDe(npc.quem);
        string motivo;
        if (!Campanha.AreaLiberada(npc.areaDoHub, out motivo) && missao == null)
        {
            Abrir(npc, new[] { motivo }, D.textoSeguir, Atualizar);
            return;
        }
        if (missao != null && (P.tutorialFeito || P.passoDoTutorial >= 2))
        {
            Abrir(npc, missao.falas, D.textoVamos, () =>
            {
                Campanha.Aceitar(missao);
                if (!P.tutorialFeito && P.passoDoTutorial == 2) { P.passoDoTutorial = 3; Campanha.Salvar(); }
                Atualizar();
            });
            return;
        }
        MissaoDaCampanha ativa = Campanha.Ativa;
        string fala = ativa != null && ativa.npc == npc.quem ? D.falaMissaoAceita
            : Campanha.AjudouAlgumaVez(npc.quem) ? D.falaJaAjudou
            : (!P.tutorialFeito && P.passoDoTutorial < 2 ? D.falaAntesDaMae : D.falaSemMissaoHoje);
        Abrir(npc, new[] { fala }, D.textoSeguir, Atualizar);
    }

    private void Abrir(NpcDaCampanha npc, string[] lista, string textoDoUltimo, Action fim)
    {
        if (lista == null || lista.Length == 0) { fim?.Invoke(); return; }
        falando = npc;
        falas = lista;
        falaAtual = 0;
        aoTerminar = fim;
        ultimoTextoDoSeguir = textoDoUltimo;
        painelDaConversa.SetActive(true);
        painelDaConversa.transform.SetAsLastSibling();
        botaoConversar.gameObject.SetActive(false);
        botaoPular.gameObject.SetActive(false);
        if (botaoIr != null) botaoIr.gameObject.SetActive(false);
        if (dicaDoOlhar != null) dicaDoOlhar.SetActive(false);
        if (movimento != null) movimento.enabled = false;
        npc.Acenar();
        Tocar(somDaConversa, 0.6f);
        MostrarFala();
        DesenharTrilhaVazia();
    }

    private string ultimoTextoDoSeguir;

    private void MostrarFala()
    {
        nomeDeQuemFala.text = D.NomeDe(falando.quem);
        textoDaFala.text = falas[falaAtual];
        textoDoSeguir.text = falaAtual == falas.Length - 1 ? ultimoTextoDoSeguir : D.textoSeguir;
        botaoOuvir.gameObject.SetActive(Narracao.Voz.Disponivel);
    }

    private void Ouvir()
    {
        if (Conversando) Narracao.Voz.Falar(textoDaFala.text);
    }

    private void Seguir()
    {
        if (!Conversando) return;
        Narracao.Voz.Parar();
        falaAtual++;
        if (falaAtual < falas.Length)
        {
            MostrarFala();
            return;
        }
        painelDaConversa.SetActive(false);
        if (movimento != null) movimento.enabled = true;
        Action fim = aoTerminar;
        aoTerminar = null;
        fim?.Invoke();
        if (!Conversando) Atualizar();
    }

    private void DesenharTrilhaVazia()
    {
        foreach (Transform s in setas) s.gameObject.SetActive(false);
    }

    // "Ir": com um escurecer rápido, põe o jogador na frente de quem ele precisa encontrar (num lugar livre da grade).
    public void IrAteONpc()
    {
        if (Conversando || ocupado || alvoDoIr == null || jogador == null) return;
        Vector3 frente = alvoDoIr.transform.position + alvoDoIr.olharPadrao.normalized * 2.2f;
        if (livre != null && livre.Length == largura * altura && largura > 0)
        {
            int c = CelulaLivreMaisPerto(Celula(frente));
            if (c >= 0) frente = Centro(c);
        }
        Vector3 olhar = alvoDoIr.transform.position - frente;
        olhar.y = 0f;
        Quaternion giro = olhar.sqrMagnitude > 0.01f ? Quaternion.LookRotation(olhar) : jogador.rotation;
        if (MapaDoHub.Instancia != null) MapaDoHub.Instancia.Levar(frente, giro);
        else { MapaDoHub.Teleportar(jogador, frente, giro); Atualizar(); }
    }

    private void PularTutorial()
    {
        Campanha.PularTutorial();
        Atualizar();
    }

    // ------------------------------------------------------------------ áreas

    // Chamado pela AreaTrigger. Devolve true se a entrada normal (tela de seleção) pode seguir.
    public bool AoEntrarNaArea(AreaTrigger area, Transform quem)
    {
        NpcDaCampanha npc = NpcDaArea(area.name);
        if (npc == null || ocupado) return npc == null;
        MissaoDaCampanha ativa = Campanha.Ativa;
        if (ativa != null && ativa.areaDoHub == area.name)
        {
            Campanha.Jogar(ativa);
            return false;
        }
        if (Campanha.AreaLiberada(area.name, out string motivo)) return true;

        // Não liberada: volta um pouco e o ajudante explica.
        CharacterController cc = quem.GetComponent<CharacterController>();
        Vector3 fora = quem.position + (quem.position - area.transform.position).normalized * 2.5f;
        fora.y = quem.position.y;
        if (cc != null) cc.enabled = false;
        quem.position = fora;
        if (cc != null) cc.enabled = true;
        Abrir(npc, new[] { motivo }, D.textoSeguir, Atualizar);
        return false;
    }

    // ------------------------------------------------------------------ volta de uma missão

    private IEnumerator Voltar(MissaoDaCampanha m)
    {
        NpcDaCampanha npc = Npc(m.npc);
        if (npc != null && jogador != null)
        {
            // O jogador aparece na frente de quem deu a missão.
            Vector3 frente = npc.transform.position + npc.olharPadrao.normalized * 2.4f;
            CharacterController cc = jogador.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            jogador.SetPositionAndRotation(new Vector3(frente.x, jogador.position.y, frente.z), Quaternion.LookRotation(-npc.olharPadrao));
            if (cc != null) cc.enabled = true;
            foreach (CameraFollow c in FindObjectsByType<CameraFollow>())
            {
                if (c.target != jogador) continue;
                c.Giro = Quaternion.LookRotation(-npc.olharPadrao).eulerAngles.y;
                c.transform.position = c.PosicaoPara(jogador.position);
                c.transform.LookAt(jogador.position + Vector3.up * c.lookHeightOffset);
            }
        }
        if (!Campanha.Feita(m)) yield break; // não terminou: a missão continua aceita

        ocupado = true;
        Campanha.Concluir(m);
        yield return new WaitForSeconds(0.6f);
        AplicarConserto(m, true);
        yield return new WaitForSeconds(1.4f);
        int ganhou = Loja.Moedas - Campanha.MoedasAntesDaMissao;
        if (ganhou > 0) StartCoroutine(MostrarMoedas(ganhou));
        ocupado = false;

        bool primeiraDoTutorial = !P.tutorialFeito && P.passoDoTutorial == 3;
        bool fim = false;
        Abrir(npc != null ? npc : Npc(QuemFala.Mae), new[] { m.agradecimento }, D.textoSeguir, () => fim = true);
        while (!fim) yield return null;
        if (primeiraDoTutorial)
        {
            fim = false;
            Abrir(Npc(QuemFala.Mae), D.falasDoAlbum, D.textoSeguir, () => fim = true);
            while (!fim) yield return null;
            P.passoDoTutorial = 4;
            Campanha.Salvar();
        }
    }

    // Antes some (encolhe com poeira) e Depois aparece (cresce com confete). Sem animação: só troca.
    private void AplicarConserto(MissaoDaCampanha m, bool animar)
    {
        Transform g = consertos != null ? consertos.Find("Conserto_" + m.id) : null;
        if (g == null) return;
        Transform antes = g.Find("Antes"), depois = g.Find("Depois");
        if (!animar)
        {
            if (antes != null) antes.gameObject.SetActive(false);
            if (depois != null) depois.gameObject.SetActive(true);
            return;
        }
        StartCoroutine(AnimarConserto(antes, depois));
    }

    private IEnumerator AnimarConserto(Transform antes, Transform depois)
    {
        Tocar(somDoConserto, 1f);
        if (antes != null)
        {
            Soltar(poeira, antes.position);
            Vector3 e = antes.localScale;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                antes.localScale = e * (1f - t / 0.45f);
                yield return null;
            }
            antes.gameObject.SetActive(false);
            antes.localScale = e;
        }
        if (depois != null)
        {
            Vector3 e = depois.localScale;
            depois.localScale = Vector3.zero;
            depois.gameObject.SetActive(true);
            Soltar(confete, depois.position + Vector3.up * 1.5f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                float pulo = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;
                depois.localScale = e * Mathf.Min(1f, k * 1.3f) * pulo;
                yield return null;
            }
            depois.localScale = e;
        }
    }

    private IEnumerator MostrarMoedas(int quantas)
    {
        textoDasMoedas.text = string.Format(D.textoMoedas, quantas);
        avisoDasMoedas.SetActive(true);
        avisoDasMoedas.transform.SetAsLastSibling();
        Tocar(somDasMoedas, 1f);
        Transform t = avisoDasMoedas.transform;
        for (float s = 0f; s < 0.3f; s += Time.deltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.1f, s / 0.3f);
            yield return null;
        }
        t.localScale = Vector3.one;
        yield return new WaitForSeconds(2.6f);
        avisoDasMoedas.SetActive(false);
    }

    private void Soltar(ParticleSystem ps, Vector3 onde)
    {
        if (ps == null) return;
        ps.transform.position = onde;
        ps.Play();
    }

    private void Tocar(AudioClip clip, float volume)
    {
        if (som != null && clip != null) som.PlayOneShot(clip, volume);
    }

    private void LateUpdate()
    {
        // Seta do Álbum pulsando (tutorial).
        if (setaDoAlbum != null && setaDoAlbum.gameObject.activeSelf)
        {
            setaDoAlbum.localScale = Vector3.one * (1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
        }
    }
}
