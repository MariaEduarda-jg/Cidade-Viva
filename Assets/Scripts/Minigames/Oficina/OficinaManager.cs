using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Oficina de Veículos: a criança lê (e ouve) a definição de um verbete de veículo e monta o veículo
// arrastando as peças da prateleira pra bancada. A receita de cada veículo (VeiculoReceita) sai só
// da definição do glossário. Montou: dá uma volta de teste na pista e ganha o cartão do verbete.
//
// Não há derrota: peça errada volta pra prateleira com um balão explicando, e só custa uma estrela.
//
// Campos do nível (Niveis_Oficina) usados aqui:
//   quantidadeDeItens   - quantas peças erradas entram na prateleira, além das certas. Com pelo menos uma,
//                         entram também os "extras aceitos" da receita (assento etc.), que encaixam sem erro
//   tempoEmSegundos     - tempo por veículo (0 = sem relógio); começa na primeira peça tocada.
//                         Estourar não encerra a partida: só perde a estrela "No tempo"
//   regra dica_visivel  - a dica fica sempre ligada e não custa estrela (nível que ensina)
//   regra sem_dica      - não existe botão de dica: só a definição
//   regra pecas_parecidas - entram peças parecidas com as certas (roda sobrando, os dois motores)
//   extra fases_de_revisao - nível sem veículo próprio: quantos dos veículos anteriores ele revisa
// Os veículos de cada nível são as VeiculoReceita de Resources/Veiculos com aquele "nivel".
// Estrelas: 3 = sem erro, no tempo e sem usar dica; 2 = dois desses; 1 = concluiu.
public class OficinaManager : MinigameBase
{
    public const string RegraDicaVisivel = "dica_visivel";
    public const string RegraSemDica = "sem_dica";
    public const string RegraPecasParecidas = "pecas_parecidas";
    public const string ExtraFasesDeRevisao = "fases_de_revisao";

    public enum Etapa { Parada, Montando, Testando, Apresentando, Terminada }
    public enum MotivoDaRecusa { Nenhum, ForaDaDefinicao, JaCompleto, Proibida }

    [Header("Oficina")]
    public OficinaConfig config;
    public OficinaHUD hud;
    public OficinaPista pista;
    public AudioSource som;
    public Camera cameraDoJogo;
    public Transform poseDaCameraNaOficina;
    public Transform poseDaCameraNaPista;

    [Header("Bancada e prateleira")]
    // Onde o chassi fica (em cima da bancada).
    public Transform bancada;
    [Tooltip("Metade da largura e altura da área, em volta da bancada, onde soltar uma peça conta como tentativa.")]
    public Vector2 meiaZonaDaBancada = new Vector2(2.1f, 1.7f);
    public Transform[] vagasDaPrateleira;
    // Modelos inativos da cena, copiados a cada veículo.
    public ChassiDaOficina[] modelosDeChassi;
    public PecaDaOficina[] modelosDePeca;

    [Tooltip("Cor do veículo enquanto está sendo montado (o molde).")]
    public Material materialDoMolde;

    [Header("Efeitos")]
    public ParticleSystem estrelinhas;
    public ParticleSystem confete;
    public ParticleSystem poeira;

    public Etapa EtapaAtual { get; private set; }
    // Veículo da vez (começa em 0) e quantos veículos este nível tem.
    public int Fase { get; private set; }
    public int TotalDeFases { get { return fases.Count; } }
    public VeiculoReceita Receita { get; private set; }
    public Verbete VerbeteAtual { get; private set; }
    public ChassiDaOficina Chassi { get; private set; }
    public IReadOnlyList<PecaDaOficina> Pecas { get { return pecas; } }
    public int Erros { get; private set; }
    public int ErrosDaFase { get; private set; }
    public bool UsouDica { get; private set; }
    public bool EstourouOTempo { get; private set; }
    public bool DicaLigada { get; private set; }
    public float TempoRestante { get; private set; }
    public int Pontuacao { get; private set; }
    public MotivoDaRecusa UltimaRecusa { get; private set; }
    public string UltimoBalao { get; private set; }

    private readonly List<VeiculoReceita> fases = new List<VeiculoReceita>();
    private readonly List<PecaDaOficina> pecas = new List<PecaDaOficina>();
    private PecaDaOficina arrastando;
    private Plane planoDeArrasto;
    private bool relogioLigado;
    private float tempoGasto;
    private int selecionada = -1;

    private void Start()
    {
        if (cameraDoJogo == null)
        {
            cameraDoJogo = Camera.main;
        }

        if (EscolhendoNivel)
        {
            hud.gameObject.SetActive(false);
            return;
        }

        fases.AddRange(EscolherFases(NivelAtual, Mathf.RoundToInt(Nivel.Extra(ExtraFasesDeRevisao, 2f))));
        if (fases.Count == 0)
        {
            Debug.LogError("Oficina: nenhuma VeiculoReceita em Resources/" + VeiculoReceita.PastaNoResources + ".");
            return;
        }

        pista.PegouMoeda += AoPegarMoeda;
        pista.Engatou += AoEngatar;
        ComecarFase(0);
    }

    private void OnDestroy()
    {
        if (pista != null)
        {
            pista.PegouMoeda -= AoPegarMoeda;
            pista.Engatou -= AoEngatar;
        }
    }

    // Todas as receitas, por nível e ordem. Criar um asset novo em Resources/Veiculos já o inclui aqui.
    public static List<VeiculoReceita> CarregarReceitas()
    {
        var todas = new List<VeiculoReceita>(Resources.LoadAll<VeiculoReceita>(VeiculoReceita.PastaNoResources));
        todas.Sort((a, b) => a.nivel != b.nivel ? a.nivel.CompareTo(b.nivel)
            : (a.ordem != b.ordem ? a.ordem.CompareTo(b.ordem) : string.CompareOrdinal(a.name, b.name)));
        return todas;
    }

    // Veículos de um nível: os que têm aquele "nivel". Nível sem veículo próprio revisa os últimos dos níveis anteriores.
    public static List<VeiculoReceita> EscolherFases(int nivel, int fasesDeRevisao)
    {
        List<VeiculoReceita> todas = CarregarReceitas();
        List<VeiculoReceita> doNivel = todas.FindAll(r => r.nivel == nivel);
        if (doNivel.Count > 0)
        {
            return doNivel;
        }

        List<VeiculoReceita> anteriores = todas.FindAll(r => r.nivel < nivel);
        if (anteriores.Count == 0)
        {
            return todas;
        }
        int quantos = Mathf.Clamp(fasesDeRevisao, 1, anteriores.Count);
        return anteriores.GetRange(anteriores.Count - quantos, quantos);
    }

    // ------------------------------------------------------------------ começo de cada veículo

    private void ComecarFase(int indice)
    {
        foreach (PecaDaOficina p in pecas)
        {
            if (p != null)
            {
                Destroy(p.gameObject);
            }
        }
        pecas.Clear();
        if (Chassi != null)
        {
            Destroy(Chassi.gameObject);
        }
        if (puxador != null)
        {
            Destroy(puxador.gameObject);
        }

        Fase = indice;
        Receita = fases[indice];
        VerbeteAtual = VerbeteDatabase.Instancia.Buscar(Receita.idDoVerbete);
        if (VerbeteAtual == null)
        {
            Debug.LogError("Oficina: a receita " + Receita.name + " aponta pra um verbete que não existe: " + Receita.idDoVerbete);
            return;
        }

        ChassiDaOficina modelo = System.Array.Find(modelosDeChassi, c => c.id == Receita.chassi);
        if (modelo == null)
        {
            Debug.LogError("Oficina: não existe chassi \"" + Receita.chassi + "\" (receita " + Receita.name + ").");
            return;
        }
        Chassi = Instantiate(modelo, bancada);
        Chassi.name = "Veiculo_" + Receita.idDoVerbete;
        Chassi.transform.localPosition = Vector3.zero;
        Chassi.transform.localRotation = Quaternion.identity;
        Chassi.gameObject.SetActive(true);
        // Em cima da bancada fica o próprio veículo, cinza, com os encaixes nos lugares dele.
        if (Receita.modeloPronto != null)
        {
            Chassi.VestirMolde(Receita.modeloPronto, Receita.escalaDoModeloPronto, Receita.giroDoModeloPronto, materialDoMolde, config.comprimentoDoVeiculoNaBancada);
        }
        Chassi.EsconderMarcadores();
        foreach (ItemDaReceita item in Receita.itens)
        {
            if (Chassi.Vagas(item.peca) < item.quantidade)
            {
                Debug.LogError("Oficina: o chassi " + Receita.chassi + " não tem encaixes suficientes de " + item.peca + " pra receita " + Receita.name + ".");
            }
        }

        MontarPrateleira();

        ErrosDaFase = 0;
        ExtrasColocados = 0;
        relogioLigado = false;
        TempoRestante = Nivel.tempoEmSegundos;
        UltimaRecusa = MotivoDaRecusa.Nenhum;
        arrastando = null;
        selecionada = -1;

        Posicionar(poseDaCameraNaOficina);
        bool dicaVisivel = Nivel.TemRegra(RegraDicaVisivel);
        DicaLigada = dicaVisivel;
        hud.gameObject.SetActive(true);
        hud.MostrarVerbete(VerbeteAtual.palavra, VerbeteAtual.definicao, string.Format(config.formatoDoNivel, NivelAtual, indice + 1, fases.Count), VerbeteAtual.ilustracao);
        hud.EsconderEtiqueta();
        hud.ModoMontagem(!dicaVisivel && !Nivel.TemRegra(RegraSemDica), Narracao.Voz.Disponivel);
        hud.Recado(config.recadoInicial);
        hud.Tempo(Nivel.TemLimiteDeTempo, 1f);
        hud.EsconderBalao();
        AtualizarDica();
        AtualizarCriterios();

        EtapaAtual = Etapa.Montando;
        narrouADefinicao = false;
    }

    private bool narrouADefinicao;

    // Peças certas + as erradas do nível, embaralhadas sempre do mesmo jeito pra cada nível e veículo.
    private void MontarPrateleira()
    {
        var tipos = new List<TipoDePeca>();
        foreach (ItemDaReceita item in Receita.itens)
        {
            // O que a definição diz que "pode ter ou não" só entra junto com as peças erradas, mais abaixo.
            for (int i = 0; i < item.quantidade && !item.opcional; i++)
            {
                tipos.Add(item.peca);
            }
        }

        if (Nivel.TemRegra(RegraPecasParecidas))
        {
            foreach (ItemDaReceita item in Receita.itens)
            {
                // Uma roda a mais do que a definição diz.
                if (item.peca == TipoDePeca.Roda && !item.ouMais)
                {
                    tipos.Add(TipoDePeca.Roda);
                }
                // A outra peça que também serviria: só uma das duas cabe.
                tipos.AddRange(item.tambemAceita);
            }
            if (Receita.ItemQueAceita(TipoDePeca.Motor) == null && Receita.ItemQueAceita(TipoDePeca.MotorEletrico) == null)
            {
                tipos.Add(TipoDePeca.Motor);
                tipos.Add(TipoDePeca.MotorEletrico);
            }
        }

        int erradas = Nivel.quantidadeDeItens;
        if (erradas > 0)
        {
            // Extras: o veículo tem, a definição não cita. Só entram se o chassi tiver onde pôr.
            foreach (TipoDePeca extra in Receita.extrasAceitos)
            {
                if (Receita.ItemQueAceita(extra) == null && !tipos.Contains(extra) && Chassi.Vagas(extra) > 0)
                {
                    tipos.Add(extra);
                }
            }
            foreach (ItemDaReceita item in Receita.itens)
            {
                if (item.opcional && !tipos.Contains(item.peca))
                {
                    tipos.Add(item.peca);
                }
            }
            // Peças que a definição exclui (reboque "sem motor"): entram pra criança ter a chance de ler e recusar.
            foreach (ItemDaReceita proibida in Receita.proibidas)
            {
                if (!tipos.Contains(proibida.peca))
                {
                    tipos.Add(proibida.peca);
                }
            }
        }

        // Erradas de verdade: peças que este veículo não tem.
        foreach (TipoDePeca t in config.ordemDasPecasErradas)
        {
            if (erradas <= 0)
            {
                break;
            }
            if (Receita.ItemQueAceita(t) != null || Receita.Proibida(t) != null || Receita.extrasAceitos.Contains(t) || tipos.Contains(t))
            {
                continue;
            }
            tipos.Add(t);
            erradas--;
        }

        if (tipos.Count > vagasDaPrateleira.Length)
        {
            tipos.RemoveRange(vagasDaPrateleira.Length, tipos.Count - vagasDaPrateleira.Length);
        }

        var sorteio = new System.Random(NivelAtual * 100 + Fase);
        for (int i = tipos.Count - 1; i > 0; i--)
        {
            int j = sorteio.Next(i + 1);
            TipoDePeca troca = tipos[i];
            tipos[i] = tipos[j];
            tipos[j] = troca;
        }

        for (int i = 0; i < tipos.Count; i++)
        {
            PecaDaOficina modelo = System.Array.Find(modelosDePeca, m => m.tipo == tipos[i]);
            if (modelo == null)
            {
                Debug.LogError("Oficina: falta o modelo da peça " + tipos[i] + ".");
                continue;
            }
            PecaDaOficina p = Instantiate(modelo);
            p.name = "Peca_" + tipos[i] + "_" + (i + 1);
            p.gameObject.SetActive(true);
            p.UsarVisual(Receita.chassi == "DuasRodas", Receita.variante);
            p.MorarEm(vagasDaPrateleira[i]);
            pecas.Add(p);
        }
    }

    // ------------------------------------------------------------------ ações do jogador

    // Primeira peça daquele tipo que ainda está na prateleira (ou null).
    public PecaDaOficina PecaNaPrateleira(TipoDePeca tipo)
    {
        return pecas.Find(p => p != null && p.tipo == tipo && p.NaPrateleira && !p.EmMovimento);
    }

    // A criança tocou numa peça da prateleira.
    public void PegarPeca(PecaDaOficina peca)
    {
        if (EtapaAtual != Etapa.Montando || peca == null || !peca.NaPrateleira)
        {
            return;
        }
        LigarRelogio();
        hud.EsconderBalao();
        Tocar(config.somDePegar);
    }

    // Leva uma peça da prateleira pra bancada. Devolve true se encaixou. Se a definição não pede a peça
    // (ou já tem todas as que pede), ela volta pra prateleira e um balão explica o motivo.
    // "perto": onde a peça foi solta, pra escolher o encaixe mais próximo (ex.: qual das rodas).
    public bool ColocarPeca(PecaDaOficina peca, Vector3? perto = null)
    {
        if (EtapaAtual != Etapa.Montando || peca == null || !peca.NaPrateleira)
        {
            return false;
        }
        LigarRelogio();

        ItemDaReceita item = Receita.ItemQueAceita(peca.tipo);
        if (item == null)
        {
            ItemDaReceita proibida = Receita.Proibida(peca.tipo);
            if (proibida != null)
            {
                return Recusar(peca, MotivoDaRecusa.Proibida, string.Format(config.balaoDeProibida, proibida.trechoDaDefinicao));
            }
            if (Receita.extrasAceitos.Contains(peca.tipo))
            {
                return ColocarExtra(peca);
            }
            string palavra = VerbeteAtual.palavra;
            palavra = palavra.Length > 0 ? char.ToUpper(palavra[0]) + palavra.Substring(1) : palavra;
            return Recusar(peca, MotivoDaRecusa.ForaDaDefinicao, string.Format(config.balaoForaDaDefinicao, config.NomeDaPeca(peca.tipo), palavra));
        }

        EncaixeDaOficina encaixe = Chassi.EncaixeLivre(item.peca, perto, item.quantidade);
        bool completo = !item.ouMais && Chassi.Ocupados(item.peca) >= item.quantidade;
        if (encaixe == null || completo)
        {
            Verbete outro = string.IsNullOrEmpty(item.verbeteDoTrecho) ? null : VerbeteDatabase.Instancia.Buscar(item.verbeteDoTrecho);
            string texto = item.basePadrao ? config.balaoDeBaseCompleta
                : (outro != null
                    ? string.Format(config.balaoJaCompletoDeOutroVerbete, outro.palavra, item.trechoDaDefinicao)
                    : string.Format(config.balaoJaCompleto, item.trechoDaDefinicao));
            return Recusar(peca, MotivoDaRecusa.JaCompleto, texto);
        }

        encaixe.Ocupante = peca;
        peca.EncaixarEm(encaixe, config.tempoDeVooDaPeca);
        StartCoroutine(Fundir(peca));
        UltimaRecusa = MotivoDaRecusa.Nenhum;
        Tocar(config.somDeEncaixe);
        Soltar(estrelinhas, encaixe.transform.position);
        hud.Recado(config.recadoDeAcerto);
        hud.EsconderBalao();
        AtualizarDica();

        if (VeiculoCompleto())
        {
            StartCoroutine(Concluir());
        }
        return true;
    }

    // Peça que o veículo tem de verdade, mas a definição não cita: encaixa, não conta erro e não é exigida.
    // O recado avisa que ela não está na definição, pra criança não achar que estava.
    private bool ColocarExtra(PecaDaOficina peca)
    {
        EncaixeDaOficina encaixe = Chassi.EncaixeLivre(peca.tipo);
        UltimaRecusa = MotivoDaRecusa.Nenhum;
        if (encaixe == null)
        {
            peca.VoltarPraPrateleira(config.tempoDeVooDaPeca);
            UltimoBalao = config.balaoDeExtraRepetido;
            hud.Balao(cameraDoJogo.WorldToScreenPoint(peca.Vaga.position), UltimoBalao, config.tempoDoBalao);
            return false;
        }

        encaixe.Ocupante = peca;
        peca.EncaixarEm(encaixe, config.tempoDeVooDaPeca);
        StartCoroutine(Fundir(peca));
        ExtrasColocados++;
        Tocar(config.somDeEncaixe);
        Soltar(estrelinhas, encaixe.transform.position);
        hud.Recado(string.Format(config.recadoDeExtra, config.NomeDaPeca(peca.tipo)));
        hud.EsconderBalao();
        return true;
    }

    // Quando a peça chega no lugar, ela some e aquela parte do veículo ganha a cor de verdade:
    // a criança vai completando o desenho do veículo, peça por peça.
    private IEnumerator Fundir(PecaDaOficina peca)
    {
        ChassiDaOficina chassi = Chassi;
        while (peca != null && peca.EmMovimento)
        {
            yield return null;
        }
        if (chassi != null && peca != null)
        {
            chassi.Fundir(peca);
        }
    }

    // Quantos extras já foram encaixados neste veículo.
    public int ExtrasColocados { get; private set; }

    // Botão "Dica": mostra as peças que a receita pede e onde elas vão. Custa a estrela "Sem dica".
    public void PedirDica()
    {
        if (EtapaAtual != Etapa.Montando || DicaLigada || Nivel.TemRegra(RegraSemDica))
        {
            return;
        }
        DicaLigada = true;
        UsouDica = true;
        Tocar(config.somDeDica);
        AtualizarDica();
        AtualizarCriterios();
    }

    // Botão "Ouvir": narra a definição do glossário, sem mudar nada.
    public void OuvirDefinicao()
    {
        if (VerbeteAtual != null && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(VerbeteAtual.definicao);
        }
    }

    public bool VeiculoCompleto()
    {
        foreach (ItemDaReceita item in Receita.itens)
        {
            if (!item.opcional && Chassi.Ocupados(item.peca) < item.quantidade)
            {
                return false;
            }
        }
        return true;
    }

    private bool Recusar(PecaDaOficina peca, MotivoDaRecusa motivo, string texto)
    {
        Erros++;
        ErrosDaFase++;
        UltimaRecusa = motivo;
        UltimoBalao = texto;
        peca.VoltarPraPrateleira(config.tempoDeVooDaPeca * 1.4f);
        Tocar(config.somDeErro);
        Soltar(poeira, peca.Vaga.position);
        hud.Balao(cameraDoJogo.WorldToScreenPoint(peca.Vaga.position), texto, config.tempoDoBalao);
        AtualizarCriterios();
        return false;
    }

    // ------------------------------------------------------------------ teclado, mouse e relógio

    private void Update()
    {
        if (EtapaAtual != Etapa.Montando)
        {
            if (EtapaAtual == Etapa.Parada)
            {
                hud.EsconderEtiqueta();
            }
            if (EtapaAtual == Etapa.Testando && pista.EsperandoEngate && (global::Teclado.Apertou(KeyCode.Return) || global::Teclado.Apertou(KeyCode.Space)))
            {
                Engatar();
            }
            return;
        }

        // A voz pode ficar pronta um instante depois de a tela abrir: aí aparece o botão "Ouvir".
        // A definição só é lida quando a criança aperta o botão (numa sala cheia, ler sozinho vira barulho).
        if (!narrouADefinicao && Narracao.Voz.Disponivel)
        {
            narrouADefinicao = true;
            hud.botaoOuvir.SetActive(true);
        }

        if (relogioLigado)
        {
            tempoGasto += Time.deltaTime;
            if (Nivel.TemLimiteDeTempo && TempoRestante > 0f)
            {
                TempoRestante = Mathf.Max(0f, TempoRestante - Time.deltaTime);
                hud.Tempo(true, TempoRestante / Nivel.tempoEmSegundos);
                if (TempoRestante <= 0f)
                {
                    EstourouOTempo = true;
                    hud.Recado(config.recadoDeTempoEsgotado);
                    AtualizarCriterios();
                }
            }
        }

        Arrastar();
        Teclado();
        MostrarNomeDaPeca();
    }

    // Peça debaixo do mouse ou do dedo (ou escolhida pelo teclado): aparece o nome dela.
    public PecaDaOficina PecaApontada { get; private set; }

    public string NomeDaPeca(PecaDaOficina peca)
    {
        string nome = config.NomeDaPeca(peca.tipo);
        return nome.Length > 0 ? char.ToUpper(nome[0]) + nome.Substring(1) : nome;
    }

    private void MostrarNomeDaPeca()
    {
        PecaDaOficina alvo = arrastando;
        if (alvo == null && !Toque.SobreAInterface())
        {
            RaycastHit toque;
            if (Physics.Raycast(cameraDoJogo.ScreenPointToRay(Toque.Posicao), out toque, 100f))
            {
                alvo = toque.collider.GetComponentInParent<PecaDaOficina>();
            }
        }
        if (alvo == null && selecionada >= 0 && selecionada < pecas.Count && pecas[selecionada].NaPrateleira)
        {
            alvo = pecas[selecionada];
        }

        PecaApontada = alvo;
        if (alvo != null)
        {
            hud.Etiqueta(cameraDoJogo.WorldToScreenPoint(alvo.transform.position), NomeDaPeca(alvo));
        }
        else
        {
            hud.EsconderEtiqueta();
        }
    }

    private void Arrastar()
    {
        if (arrastando == null)
        {
            if (!Toque.Apertou || (Toque.SobreAInterface()))
            {
                return;
            }
            RaycastHit toque;
            if (!Physics.Raycast(cameraDoJogo.ScreenPointToRay(Toque.Posicao), out toque, 100f))
            {
                return;
            }
            PecaDaOficina peca = toque.collider.GetComponentInParent<PecaDaOficina>();
            if (peca == null || !peca.NaPrateleira || peca.EmMovimento)
            {
                return;
            }
            arrastando = peca;
            planoDeArrasto = new Plane(-cameraDoJogo.transform.forward, bancada.position + Vector3.up);
            PegarPeca(peca);
            return;
        }

        Ray raio = cameraDoJogo.ScreenPointToRay(Toque.Posicao);
        float distancia;
        Vector3 ponto = planoDeArrasto.Raycast(raio, out distancia) ? raio.GetPoint(distancia) : arrastando.transform.position;
        if (Toque.Segurando)
        {
            arrastando.transform.position = ponto;
            arrastando.transform.localScale = Vector3.one * 1.15f;
            return;
        }

        PecaDaOficina solta = arrastando;
        arrastando = null;
        Vector3 d = ponto - (bancada.position + Vector3.up);
        if (Mathf.Abs(d.x) <= meiaZonaDaBancada.x && Mathf.Abs(d.y) <= meiaZonaDaBancada.y)
        {
            ColocarPeca(solta, ponto);
        }
        else
        {
            // Soltar fora da bancada não é erro: a peça só volta pro lugar.
            solta.VoltarPraPrateleira(config.tempoDeVooDaPeca);
        }
    }

    // Setas escolhem a peça da prateleira; Enter ou Espaço leva pra bancada; D pede dica; O ouve a definição.
    private void Teclado()
    {
        int passo = (global::Teclado.Apertou(KeyCode.RightArrow) ? 1 : 0) - (global::Teclado.Apertou(KeyCode.LeftArrow) ? 1 : 0);
        if (passo != 0 && pecas.Count > 0)
        {
            DestacarSelecionada(false);
            for (int i = 0; i < pecas.Count; i++)
            {
                selecionada = ((selecionada < 0 ? (passo > 0 ? -1 : 0) : selecionada) + passo + pecas.Count) % pecas.Count;
                if (pecas[selecionada].NaPrateleira)
                {
                    break;
                }
            }
            DestacarSelecionada(true);
        }

        if ((global::Teclado.Apertou(KeyCode.Return) || global::Teclado.Apertou(KeyCode.Space)) && selecionada >= 0 && selecionada < pecas.Count)
        {
            PecaDaOficina peca = pecas[selecionada];
            DestacarSelecionada(false);
            PegarPeca(peca);
            ColocarPeca(peca);
        }
        if (global::Teclado.Apertou(KeyCode.D))
        {
            PedirDica();
        }
        if (global::Teclado.Apertou(KeyCode.O))
        {
            OuvirDefinicao();
        }
    }

    private void DestacarSelecionada(bool destacar)
    {
        if (selecionada >= 0 && selecionada < pecas.Count && pecas[selecionada].NaPrateleira && !pecas[selecionada].EmMovimento)
        {
            pecas[selecionada].transform.localScale = Vector3.one * (destacar ? 1.25f : 1f);
        }
    }

    private void LigarRelogio()
    {
        relogioLigado = true;
    }

    // ------------------------------------------------------------------ dica e estrelas

    private void AtualizarDica()
    {
        Chassi.EsconderMarcadores();
        if (!DicaLigada)
        {
            hud.Dica(null);
            return;
        }

        var partes = new List<string>();
        foreach (ItemDaReceita item in Receita.itens)
        {
            if (item.opcional)
            {
                continue;
            }
            partes.Add(string.Format(config.formatoDeItemDaDica, item.quantidade, config.NomeDaPeca(item.peca)) + (item.ouMais ? config.sufixoOuMais : ""));
            int faltam = item.quantidade - Chassi.Ocupados(item.peca);
            foreach (EncaixeDaOficina e in Chassi.EncaixesDe(item.peca))
            {
                if (faltam > 0 && e.Livre && e.marcador != null)
                {
                    e.marcador.SetActive(true);
                    faltam--;
                }
            }
        }
        hud.Dica(string.Format(config.formatoDaDica, string.Join(", ", partes)));
    }

    private void AtualizarCriterios()
    {
        hud.Criterios(Erros == 0, !EstourouOTempo, !UsouDica, config.rotulosDosCriterios);
    }

    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        int cumpridos = (Erros == 0 ? 1 : 0) + (!EstourouOTempo ? 1 : 0) + (!UsouDica ? 1 : 0);
        return cumpridos >= 3 ? 3 : (cumpridos == 2 ? 2 : 1);
    }

    // ------------------------------------------------------------------ veículo pronto, teste e cartão

    private IEnumerator Concluir()
    {
        EtapaAtual = Etapa.Parada;
        Pontuacao += Mathf.Max(config.minimoPorVeiculo, config.pontosPorVeiculo - config.descontoPorErro * ErrosDaFase);
        hud.Recado(config.recadoDeVeiculoPronto);
        hud.Dica(null);
        Chassi.EsconderMarcadores();
        yield return new WaitForSeconds(config.tempoDeVooDaPeca + 0.1f);
        Tocar(config.somDeAcerto);
        // O molde cinza ganha as cores: é o veículo pronto.
        Chassi.RevelarMolde();
        Soltar(estrelinhas, Chassi.transform.position + Vector3.up * 0.6f);
        Soltar(confete, Chassi.transform.position + Vector3.up);
        yield return new WaitForSeconds(1.6f);
        IniciarTeste();
    }

    // O veículo "ganha vida": vai pra pista e a criança dirige.
    private void IniciarTeste()
    {
        foreach (PecaDaOficina p in pecas)
        {
            if (p != null && p.NaPrateleira)
            {
                p.gameObject.SetActive(false);
            }
        }

        EtapaAtual = Etapa.Testando;
        hud.ModoTeste();
        Posicionar(poseDaCameraNaPista);
        Chassi.SairDaBancada();

        // Veículo sem motor (reboque): quem anda é o veículo que puxa, depois de engatado.
        float comprimento = config.comprimentoDoVeiculoNaBancada * config.escalaDoVeiculoNaPista;
        float comprimentoDoPuxador = 0f;
        VeiculoReceita quemPuxa = string.IsNullOrEmpty(Receita.puxadoPor) ? null : CarregarReceitas().Find(r => r.idDoVerbete == Receita.puxadoPor);
        if (quemPuxa != null && quemPuxa.modeloPronto != null)
        {
            comprimentoDoPuxador = comprimento * quemPuxa.escalaDoModeloPronto;
            puxador = ChassiDaOficina.CriarModeloSolto(quemPuxa.modeloPronto, quemPuxa.giroDoModeloPronto, comprimentoDoPuxador);
        }

        pista.Comecar(Chassi, config, Receita.andaEmTrilhos, Receita.velocidadeNaPista, puxador,
            comprimento * Receita.escalaDoModeloPronto, comprimentoDoPuxador, AoTerminarOTeste);
        hud.ControlesDaPista(pista.EsperandoEngate, !pista.EmTrilhos);
        if (pista.EsperandoEngate)
        {
            hud.Recado(config.recadoDeEngatar);
            return;
        }

        bool temMotor = Receita.ItemQueAceita(TipoDePeca.Motor) != null || Receita.ItemQueAceita(TipoDePeca.MotorEletrico) != null;
        Tocar(temMotor ? config.somDeMotorLigando : config.somDeCampainha);
        hud.Recado(pista.EmTrilhos ? config.recadoDoTesteNosTrilhos : config.recadoDoTeste);
    }

    private Transform puxador;

    // Botão "Engatar" (ou Enter/Espaço): o veículo que puxa dá ré e prende no reboque.
    public void Engatar()
    {
        if (EtapaAtual == Etapa.Testando && pista.EsperandoEngate)
        {
            pista.Engatar();
        }
    }

    private void AoEngatar()
    {
        Tocar(config.somDeEngate);
        Tocar(config.somDeMotorLigando);
        Soltar(estrelinhas, Chassi.transform.position + Vector3.up);
        hud.ControlesDaPista(false, true);
        hud.Recado(config.recadoDeEngatado);
    }

    private void AoPegarMoeda(Vector3 onde)
    {
        Tocar(config.somDeMoeda);
        Soltar(estrelinhas, onde + Vector3.up * 0.6f);
    }

    private void AoTerminarOTeste()
    {
        EtapaAtual = Etapa.Apresentando;
        hud.Recado(config.recadoDoFimDoTeste);
        Soltar(confete, Chassi.transform.position + Vector3.up);
        // O cartão desbloqueia o verbete no álbum do perfil ativo e mostra a definição do glossário.
        CartaoVerbete.Mostrar(Receita.idDoVerbete, AoFecharOCartao);
    }

    private void AoFecharOCartao()
    {
        if (Fase + 1 < fases.Count)
        {
            ComecarFase(Fase + 1);
            return;
        }

        EtapaAtual = Etapa.Terminada;
        FinalizarNivel(new ResultadoDoNivel { concluiu = true, pontuacao = Pontuacao, erros = Erros, tempoGasto = tempoGasto, tempoRestante = TempoRestante });
    }

    // ------------------------------------------------------------------ utilidades

    private void Posicionar(Transform pose)
    {
        cameraDoJogo.transform.SetPositionAndRotation(pose.position, pose.rotation);
    }

    private void Tocar(AudioClip clipe)
    {
        if (clipe != null && som != null)
        {
            som.PlayOneShot(clipe);
        }
    }

    private static void Soltar(ParticleSystem efeito, Vector3 onde)
    {
        if (efeito != null)
        {
            efeito.transform.position = onde;
            efeito.Play();
        }
    }
}
