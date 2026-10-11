using System;
using System.Collections.Generic;
using UnityEngine;

// Um erro escondido na cena. O id liga este ponto aos visuais da cena (CenaOlhoVivo.visuais).
[Serializable]
public class PontoDeErro
{
    public string id;
    // Só pra identificar o erro no Inspector.
    public string descricao;
    // Posição do hotspot em relação ao centro da área da cena.
    public Vector2 posicao;
    public Vector2 tamanho = new Vector2(200f, 200f);
    [TextArea]
    public string explicacao;
}

// Algo que está certo na cena. Clicar nele só mostra um recado, sem penalidade.
[Serializable]
public class ElementoCerto
{
    public string descricao;
    public Vector2 posicao;
    public Vector2 tamanho = new Vector2(200f, 200f);
    [TextArea]
    public string mensagem;
}

// Uma fase do "Olho Vivo!": onde estão os erros e as coisas certas, os textos e as regras.
// Pra criar outra fase, duplique o asset, troque as listas e monte os visuais na cena.
[CreateAssetMenu(fileName = "OlhoVivo_Fase", menuName = "Cidade Viva/Olho Vivo - Fase")]
public class OlhoVivoFaseConfig : ScriptableObject
{
    [Header("Fase")]
    public List<PontoDeErro> pontosDeErro = new List<PontoDeErro>();
    public List<ElementoCerto> elementosCertos = new List<ElementoCerto>();
    public float tempoTotal = 30f;

    [Header("Pontuação")]
    public int pontosPorErro = 15;
    // Bônus proporcional ao tempo que sobrou (só sobra tempo se achar todos os erros).
    public int bonusMaximoDeTempo = 40;

    [Header("Apresentação")]
    [TextArea]
    public string instrucao = "Ache os erros escondidos na rua!";
    public Color corDaExplicacaoDeErro = new Color(0.6f, 1f, 0.6f);
    public Color corDoRecadoDeCerto = Color.white;
    public float tempoDaExplicacao = 3f;
    public float esperaAntesDoFim = 2f;
    public string formatoTempo = "Tempo: {0}";
    public string formatoEncontrados = "Erros: {0} / {1}";
    public string tituloAoAcharTodos = "Olho vivo! Você achou todos os erros!";
    [TextArea]
    public string tituloAoAcabarOTempo = "O tempo acabou!\nVocê achou {0} de {1} erros";
    // Opcional: sem clipe, toca um "plim" gerado por código.
    public AudioClip somDeAcerto;
}
