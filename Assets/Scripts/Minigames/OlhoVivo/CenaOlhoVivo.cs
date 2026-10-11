using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Liga um ponto de erro da fase (pelo id) aos objetos da cena: o que aparece enquanto o erro
// está lá e o que aparece depois de corrigido. Os dois são opcionais.
[Serializable]
public class VisualDeErro
{
    public string id;
    public GameObject visualErrado;
    public GameObject visualCorrigido;
}

// "Olho Vivo!": a criança clica nos erros escondidos na cena, em meio a coisas certas, antes do
// tempo acabar. Cada ponto de erro da fase vira um hotspot invisível; clicar nele corrige o erro
// (troca o visual errado pelo corrigido, selo de acerto, som e explicação curta). Clicar numa
// coisa certa só mostra um recado; clicar fora ou num erro já corrigido não faz nada.
// Termina ao achar todos os erros ou quando o tempo acaba.
public class CenaOlhoVivo : MinigameBase
{
    public OlhoVivoFaseConfig fase;

    [Header("Cena")]
    // Os hotspots são criados dentro desta área, a partir do modelo inativo.
    public RectTransform areaDaCena;
    public HotspotOlhoVivo modeloDeHotspot;
    public List<VisualDeErro> visuais = new List<VisualDeErro>();

    [Header("Interface")]
    public Text textoTempo;
    public Text textoEncontrados;
    public Text textoExplicacao;

    [Header("Som")]
    public AudioSource audioSource;

    private readonly List<HotspotOlhoVivo> hotspots = new List<HotspotOlhoVivo>();
    private bool[] encontrado;
    private AudioClip somDeAcerto;
    private float tempoRestante;
    private float tempoDaExplicacaoRestante;
    private int encontrados;

    public bool Jogando { get; private set; }
    public int Encontrados => encontrados;
    public int Pontuacao => encontrados * fase.pontosPorErro
        + Mathf.RoundToInt(fase.bonusMaximoDeTempo * Mathf.Clamp01(tempoRestante / fase.tempoTotal));

    private void Start()
    {
        MostrarTexto(fase.instrucao, fase.corDoRecadoDeCerto, 0f);
        somDeAcerto = fase.somDeAcerto != null ? fase.somDeAcerto : CriarSomDeAcerto();

        // Os certos primeiro, pra os hotspots de erro ficarem por cima quando se sobrepõem.
        for (int i = 0; i < fase.elementosCertos.Count; i++)
        {
            ElementoCerto elemento = fase.elementosCertos[i];
            HotspotOlhoVivo hotspot = CriarHotspot("Certo_" + i, elemento.posicao, elemento.tamanho);
            int indice = i;
            hotspot.botao.onClick.AddListener(() => ClicarNoCerto(indice));
        }

        encontrado = new bool[fase.pontosDeErro.Count];
        for (int i = 0; i < fase.pontosDeErro.Count; i++)
        {
            PontoDeErro ponto = fase.pontosDeErro[i];
            MostrarVisual(ponto, false);

            HotspotOlhoVivo hotspot = CriarHotspot("Erro_" + i, ponto.posicao, ponto.tamanho);
            int indice = i;
            hotspot.botao.onClick.AddListener(() => Clicar(indice));
            hotspots.Add(hotspot);
        }

        tempoRestante = fase.tempoTotal;
        Jogando = true;
        AtualizarTextos();
    }

    private void Update()
    {
        if (tempoDaExplicacaoRestante > 0f)
        {
            tempoDaExplicacaoRestante -= Time.deltaTime;
            if (tempoDaExplicacaoRestante <= 0f && Jogando)
            {
                MostrarTexto(fase.instrucao, fase.corDoRecadoDeCerto, 0f);
            }
        }

        if (!Jogando)
        {
            return;
        }

        tempoRestante = Mathf.Max(0f, tempoRestante - Time.deltaTime);
        AtualizarTextos();
        if (tempoRestante <= 0f)
        {
            Jogando = false;
            MostrarFim();
        }
    }

    public bool FoiEncontrado(int indice)
    {
        return encontrado[indice];
    }

    // Chamado pelo hotspot de cada erro.
    public void Clicar(int indice)
    {
        if (!Jogando || encontrado[indice])
        {
            return;
        }

        PontoDeErro ponto = fase.pontosDeErro[indice];
        encontrado[indice] = true;
        encontrados++;
        MostrarVisual(ponto, true);
        hotspots[indice].MarcarCorrigido();

        if (audioSource != null)
        {
            audioSource.PlayOneShot(somDeAcerto);
        }

        MostrarTexto(ponto.explicacao, fase.corDaExplicacaoDeErro, fase.tempoDaExplicacao);
        AtualizarTextos();

        if (encontrados >= fase.pontosDeErro.Count)
        {
            // Para o cronômetro e deixa a última explicação aparecer antes da tela final.
            Jogando = false;
            Invoke(nameof(MostrarFim), fase.esperaAntesDoFim);
        }
    }

    // Chamado pelo hotspot de cada coisa certa: só um recado, sem penalidade.
    public void ClicarNoCerto(int indice)
    {
        if (!Jogando)
        {
            return;
        }

        MostrarTexto(fase.elementosCertos[indice].mensagem, fase.corDoRecadoDeCerto, fase.tempoDaExplicacao);
    }

    private HotspotOlhoVivo CriarHotspot(string nome, Vector2 posicao, Vector2 tamanho)
    {
        HotspotOlhoVivo hotspot = Instantiate(modeloDeHotspot, areaDaCena);
        hotspot.name = nome;
        hotspot.Configurar(posicao, tamanho);
        hotspot.gameObject.SetActive(true);
        return hotspot;
    }

    private void MostrarVisual(PontoDeErro ponto, bool corrigido)
    {
        VisualDeErro visual = visuais.Find(v => v.id == ponto.id);
        if (visual == null)
        {
            return;
        }

        if (visual.visualErrado != null)
        {
            visual.visualErrado.SetActive(!corrigido);
        }

        if (visual.visualCorrigido != null)
        {
            visual.visualCorrigido.SetActive(corrigido);
        }
    }

    private void MostrarTexto(string mensagem, Color cor, float duracao)
    {
        textoExplicacao.text = mensagem;
        textoExplicacao.color = cor;
        tempoDaExplicacaoRestante = duracao;
    }

    private void MostrarFim()
    {
        string titulo = encontrados >= fase.pontosDeErro.Count
            ? fase.tituloAoAcharTodos
            : string.Format(fase.tituloAoAcabarOTempo, encontrados, fase.pontosDeErro.Count);
        FinalizarJogo(Pontuacao, titulo);
    }

    private void AtualizarTextos()
    {
        textoTempo.text = string.Format(fase.formatoTempo, Mathf.CeilToInt(tempoRestante));
        textoEncontrados.text = string.Format(fase.formatoEncontrados, encontrados, fase.pontosDeErro.Count);
    }

    // Duas notas subindo, curtinhas, pra não depender de arquivo de áudio.
    private static AudioClip CriarSomDeAcerto()
    {
        const int frequenciaDeAmostragem = 44100;
        const float duracaoDaNota = 0.12f;
        float[] notas = { 880f, 1318.5f };

        int amostrasPorNota = Mathf.RoundToInt(frequenciaDeAmostragem * duracaoDaNota);
        float[] amostras = new float[amostrasPorNota * notas.Length];
        for (int n = 0; n < notas.Length; n++)
        {
            for (int i = 0; i < amostrasPorNota; i++)
            {
                float t = (float)i / frequenciaDeAmostragem;
                float volume = 1f - (float)i / amostrasPorNota;
                amostras[n * amostrasPorNota + i] = Mathf.Sin(2f * Mathf.PI * notas[n] * t) * volume * 0.4f;
            }
        }

        AudioClip clip = AudioClip.Create("Acerto", amostras.Length, 1, frequenciaDeAmostragem, false);
        clip.SetData(amostras, 0);
        return clip;
    }
}
