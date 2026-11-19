using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela inicial: entrar num perfil salvo, criar um perfil ou jogar como visitante.
// Pensada pra crianças de 6 a 9 anos: sem e-mail nem senha. O perfil é nome + bichinho + um
// "segredo" de 3 figuras. A cena (Menu_Inicial) é montada pelo MenuConstrutor; os botões chamam
// os métodos públicos daqui.
public class MenuInicial : MonoBehaviour
{
    public string cenaDoJogo = "Hub";
    public IconesDePerfil icones;

    [Header("Telas")]
    public GameObject telaInicio;
    public GameObject telaLista;
    public GameObject telaNome;
    public GameObject telaAvatar;
    public GameObject telaCodigo;
    public GameObject telaVisitante;
    public GameObject telaCreditos;

    [Header("Entrar: lista de perfis")]
    public Transform conteudoDaLista;
    // Item inativo que serve de molde: precisa de um Button, uma Image filha "Avatar" e um Text filho "Nome".
    public GameObject modeloDeItem;
    public GameObject avisoListaVazia;

    [Header("Criar: nome")]
    public Text textoDoNome;
    public Button botaoNomePronto;
    public string dicaDoNome = "Escreva seu nome";

    [Header("Criar: bichinho")]
    public RectTransform[] botoesDeAvatar;
    public Button botaoAvatarPronto;

    [Header("Segredo de 3 figuras (criar e entrar)")]
    public Text tituloDoCodigo;
    public Image[] espacosDoCodigo;
    public Image avatarNoCodigo;
    public Text nomeNoCodigo;
    public Text mensagemDoCodigo;
    public Button botaoCodigoPronto;
    public string tituloAoCriar = "Escolha 3 figuras para ser o seu segredo";
    public string tituloAoEntrar = "Qual é o seu segredo?";
    public string mensagemAoCriar = "Guarde bem! Você vai usar para entrar.";
    public string mensagemDeErro = "Ops! Não é esse. Tente de novo!";

    private enum Tela { Inicio, Lista, Nome, Avatar, Codigo, Visitante, Creditos }

    private Tela telaAtual;
    private bool criando;
    private string nome = "";
    private int avatar = -1;
    private Perfil perfilEscolhido;
    private readonly List<int> tentativa = new List<int>();
    private bool conferindo;

    private void Start()
    {
        Mostrar(Tela.Inicio);
    }

    private void Update()
    {
        if (telaAtual != Tela.Nome)
        {
            return;
        }

        // Quem tem teclado de verdade pode digitar; quem não tem usa as letras da tela.
        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                ApagarLetra();
            }
            else if (c == '\n' || c == '\r')
            {
                NomePronto();
            }
            else if (char.IsLetterOrDigit(c) || c == ' ')
            {
                Letra(c.ToString());
            }
        }
    }

    // ---------------------------------------------------------------- tela inicial

    public void AbrirEntrar()
    {
        criando = false;
        PreencherLista();
        Mostrar(Tela.Lista);
    }

    public void AbrirCriar()
    {
        criando = true;
        nome = "";
        avatar = -1;
        AtualizarNome();
        AtualizarAvatares();
        Mostrar(Tela.Nome);
    }

    public void AbrirVisitante()
    {
        Mostrar(Tela.Visitante);
    }

    // Botão "Ver abertura": mostra a abertura e volta pro menu.
    public void VerAbertura()
    {
        Historia.VerAbertura();
    }

    public void AbrirCreditos()
    {
        Mostrar(Tela.Creditos);
    }

    public void JogarComoVisitante()
    {
        Perfis.EntrarComoVisitante();
        Historia.EntrarNoJogo();
    }

    public void Voltar()
    {
        switch (telaAtual)
        {
            case Tela.Avatar: Mostrar(Tela.Nome); break;
            case Tela.Codigo: Mostrar(criando ? Tela.Avatar : Tela.Lista); break;
            default: Mostrar(Tela.Inicio); break;
        }
    }

    // ---------------------------------------------------------------- entrar

    private void PreencherLista()
    {
        for (int i = conteudoDaLista.childCount - 1; i >= 0; i--)
        {
            GameObject filho = conteudoDaLista.GetChild(i).gameObject;
            if (filho != modeloDeItem)
            {
                Destroy(filho);
            }
        }

        List<Perfil> perfis = Perfis.Listar();
        avisoListaVazia.SetActive(perfis.Count == 0);
        foreach (Perfil p in perfis)
        {
            GameObject item = Instantiate(modeloDeItem, conteudoDaLista);
            item.name = "Perfil_" + p.nome;
            item.transform.Find("Avatar").GetComponent<Image>().sprite = icones.Avatar(p.avatar);
            item.transform.Find("Nome").GetComponent<Text>().text = p.nome;
            Perfil escolhido = p;
            item.GetComponent<Button>().onClick.AddListener(() => EscolherPerfil(escolhido));
            item.SetActive(true);
        }
    }

    private void EscolherPerfil(Perfil perfil)
    {
        perfilEscolhido = perfil;
        AbrirCodigo();
    }

    // ---------------------------------------------------------------- criar: nome

    public void Letra(string letra)
    {
        if (nome.Length >= Perfis.TamanhoMaximoDoNome)
        {
            return;
        }

        // Primeira letra maiúscula, o resto minúsculo: "Ana", não "ANA" nem "ana".
        bool inicioDePalavra = nome.Length == 0 || nome[nome.Length - 1] == ' ';
        if (letra == " " && inicioDePalavra)
        {
            return;
        }
        nome += inicioDePalavra ? letra.ToUpper() : letra.ToLower();
        AtualizarNome();
    }

    public void ApagarLetra()
    {
        if (nome.Length > 0)
        {
            nome = nome.Substring(0, nome.Length - 1);
            AtualizarNome();
        }
    }

    public void NomePronto()
    {
        if (Perfis.LimparNome(nome).Length > 0)
        {
            Mostrar(Tela.Avatar);
        }
    }

    private void AtualizarNome()
    {
        bool vazio = nome.Length == 0;
        textoDoNome.text = vazio ? dicaDoNome : nome;
        textoDoNome.color = new Color(textoDoNome.color.r, textoDoNome.color.g, textoDoNome.color.b, vazio ? 0.45f : 1f);
        botaoNomePronto.interactable = Perfis.LimparNome(nome).Length > 0;
    }

    // ---------------------------------------------------------------- criar: bichinho

    public void EscolherAvatar(int indice)
    {
        avatar = indice;
        AtualizarAvatares();
    }

    public void AvatarPronto()
    {
        if (avatar >= 0)
        {
            AbrirCodigo();
        }
    }

    private void AtualizarAvatares()
    {
        for (int i = 0; i < botoesDeAvatar.Length; i++)
        {
            botoesDeAvatar[i].localScale = Vector3.one * (i == avatar ? 1.18f : 1f);
        }
        botaoAvatarPronto.interactable = avatar >= 0;
    }

    // ---------------------------------------------------------------- segredo de 3 figuras

    private void AbrirCodigo()
    {
        tentativa.Clear();
        conferindo = false;
        tituloDoCodigo.text = criando ? tituloAoCriar : tituloAoEntrar;
        avatarNoCodigo.sprite = icones.Avatar(criando ? avatar : perfilEscolhido.avatar);
        nomeNoCodigo.text = criando ? Perfis.LimparNome(nome) : perfilEscolhido.nome;
        AtualizarCodigo("");
        Mostrar(Tela.Codigo);
    }

    public void EscolherFigura(int indice)
    {
        if (conferindo || tentativa.Count >= Perfis.TamanhoDoCodigo)
        {
            return;
        }

        tentativa.Add(indice);
        bool completo = tentativa.Count == Perfis.TamanhoDoCodigo;
        AtualizarCodigo(criando && completo ? mensagemAoCriar : "");
        if (completo && !criando)
        {
            StartCoroutine(ConferirCodigo());
        }
    }

    public void ApagarFigura()
    {
        if (conferindo || tentativa.Count == 0)
        {
            return;
        }

        tentativa.RemoveAt(tentativa.Count - 1);
        AtualizarCodigo("");
    }

    // Só ao criar: a criança vê as 3 figuras e confirma.
    public void CodigoPronto()
    {
        if (!criando || tentativa.Count != Perfis.TamanhoDoCodigo)
        {
            return;
        }

        Perfis.Entrar(Perfis.Criar(nome, avatar, tentativa.ToArray()));
        Historia.EntrarNoJogo();
    }

    private IEnumerator ConferirCodigo()
    {
        conferindo = true;
        yield return new WaitForSeconds(0.35f);
        if (Perfis.CodigoConfere(perfilEscolhido, tentativa))
        {
            Perfis.Entrar(perfilEscolhido);
            Historia.EntrarNoJogo();
            yield break;
        }

        // Errou: sem bronca, só limpa e deixa tentar quantas vezes quiser.
        AtualizarCodigo(mensagemDeErro);
        yield return new WaitForSeconds(1.1f);
        tentativa.Clear();
        AtualizarCodigo(mensagemDeErro);
        conferindo = false;
    }

    private void AtualizarCodigo(string mensagem)
    {
        for (int i = 0; i < espacosDoCodigo.Length; i++)
        {
            bool preenchido = i < tentativa.Count;
            espacosDoCodigo[i].sprite = preenchido ? icones.Figura(tentativa[i]) : null;
            espacosDoCodigo[i].enabled = preenchido;
        }
        mensagemDoCodigo.text = mensagem;
        botaoCodigoPronto.gameObject.SetActive(criando);
        botaoCodigoPronto.interactable = tentativa.Count == Perfis.TamanhoDoCodigo;
    }

    // ---------------------------------------------------------------- telas

    private void Mostrar(Tela tela)
    {
        telaAtual = tela;
        telaInicio.SetActive(tela == Tela.Inicio);
        telaLista.SetActive(tela == Tela.Lista);
        telaNome.SetActive(tela == Tela.Nome);
        telaAvatar.SetActive(tela == Tela.Avatar);
        telaCodigo.SetActive(tela == Tela.Codigo);
        telaVisitante.SetActive(tela == Tela.Visitante);
        if (telaCreditos != null)
        {
            telaCreditos.SetActive(tela == Tela.Creditos);
        }
    }
}
