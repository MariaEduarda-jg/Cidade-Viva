using UnityEngine;
using UnityEngine.EventSystems;

// Câmera do Hub: segue o jogador por trás e gira em volta dele com o mouse.
// O mouse escolhe pra onde o personagem olha (Giro) e o quanto a câmera olha de cima (Inclinacao);
// as teclas andam em relação a esse olhar (ver PlayerMovement).
// Dois jeitos de olhar:
//   - clicando no cenário, o mouse fica "preso" no jogo: mexer pros lados gira sem parar (dá a volta
//     inteira) e mexer pra cima e pra baixo inclina. Esc solta o mouse, pra usar os botões da tela;
//   - com o mouse solto, mexer pros lados ainda gira, e encostar o cursor na borda esquerda ou direita
//     continua girando.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("Onde a câmera fica em relação ao jogador quando o jogo começa (olhando pra +Z).")]
    public Vector3 offset = new Vector3(0f, 4f, -6f);
    public float lookHeightOffset = 1.5f;
    public float followSpeed = 10f;

    [Header("Olhar com o mouse")]
    public bool olharComOMouse = true;
    [Tooltip("Graus por unidade do eixo \"Mouse X\".")]
    public float sensibilidadeDoGiro = 2f;
    [Tooltip("Graus por unidade do eixo \"Mouse Y\" (só com o mouse preso).")]
    public float sensibilidadeDaInclinacao = 1.4f;
    [Tooltip("Menor e maior inclinação, em graus (0 = na altura do jogador, 90 = de cima).")]
    public Vector2 limitesDaInclinacao = new Vector2(-8f, 65f);
    [Tooltip("Fração da largura da tela, de cada lado, em que o cursor solto e parado continua girando a câmera.")]
    public float bordaDaTela = 0.07f;
    [Tooltip("Graus por segundo com o cursor na borda.")]
    public float giroNaBorda = 70f;

    [Header("Não ficar atrás de prédio")]
    [Tooltip("Se um prédio (ou outra coisa com colisor) fica entre o jogador e a câmera, ela sobe e, se precisar, chega mais perto.")]
    public bool desviarDasCoisas = true;
    public float raioDaColisao = 0.3f;
    public float distanciaMinima = 1.6f;

    // Pra onde o jogador está olhando, em graus em volta de Y (0 = +Z, como era a câmera fixa).
    public float Giro { get; set; }
    // Quanto a câmera olha de cima, em graus.
    public float Inclinacao { get; set; }
    public static bool MousePreso { get { return Cursor.lockState == CursorLockMode.Locked; } }

    private float distancia;

    private void Awake()
    {
        // A distância e a inclinação iniciais saem do "offset", medido a partir do ponto que a câmera olha.
        Vector3 braco = offset - Vector3.up * lookHeightOffset;
        distancia = braco.magnitude;
        Inclinacao = Mathf.Asin(Mathf.Clamp(braco.y / Mathf.Max(distancia, 0.01f), -1f, 1f)) * Mathf.Rad2Deg;
    }

    private void OnDisable()
    {
        // Ao sair do Hub o cursor volta, senão o minigame começa sem mouse.
        SoltarOMouse();
    }

    public static void SoltarOMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        bool parado = !olharComOMouse || Time.timeScale <= 0f || CartaoVerbete.EstaAberto;
        if (parado || Teclado.Apertou(KeyCode.Escape))
        {
            if (MousePreso)
            {
                SoltarOMouse();
            }
            if (parado)
            {
                return;
            }
        }

        bool sobreAInterface = Toque.SobreAInterface();
        if (MousePreso)
        {
            Girar(Toque.EixoDoMouse("Mouse X") * sensibilidadeDoGiro, -Toque.EixoDoMouse("Mouse Y") * sensibilidadeDaInclinacao);
            return;
        }

        // Com o cursor fora da janela ou em cima de um botão, a câmera fica quieta.
        Vector3 cursor = Toque.Posicao;
        bool naTela = cursor.x >= 0f && cursor.x <= Screen.width && cursor.y >= 0f && cursor.y <= Screen.height;
        if (!naTela || sobreAInterface)
        {
            return;
        }
        if (Toque.Apertou)
        {
            // Clique no cenário: prende o mouse no jogo.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        float giro = Toque.EixoDoMouse("Mouse X") * sensibilidadeDoGiro;
        float borda = Screen.width * bordaDaTela;
        if (cursor.x < borda)
        {
            giro -= giroNaBorda * Time.deltaTime;
        }
        else if (cursor.x > Screen.width - borda)
        {
            giro += giroNaBorda * Time.deltaTime;
        }
        Girar(giro, 0f);
    }

    private void Girar(float giro, float inclinacao)
    {
        Giro = Mathf.Repeat(Giro + giro, 360f);
        Inclinacao = Mathf.Clamp(Inclinacao + inclinacao, limitesDaInclinacao.x, limitesDaInclinacao.y);
    }

    // Onde a câmera fica pra um jogador nessa posição, com o giro e a inclinação atuais.
    public Vector3 PosicaoPara(Vector3 posicaoDoJogador)
    {
        Vector3 foco = posicaoDoJogador + Vector3.up * lookHeightOffset;
        Vector3 posicao = foco - Quaternion.Euler(Inclinacao, Giro, 0f) * Vector3.forward * distancia;
        // Nunca abaixo do chão.
        posicao.y = Mathf.Max(posicao.y, 0.4f);
        return posicao;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 foco = target.position + Vector3.up * lookHeightOffset;
        Vector3 desejada = PosicaoPara(target.position);
        Vector3 livre = desviarDasCoisas ? Desviar(foco, desejada) : desejada;
        // Chegando mais perto (prédio no caminho) vai rápido, pra não mostrar o prédio por dentro; voltando, devagar.
        bool aproximando = (livre - foco).sqrMagnitude < (transform.position - foco).sqrMagnitude - 0.01f;
        float rapidez = aproximando && inclinacaoExtra > 0.5f ? followSpeed * 2f : followSpeed;
        transform.position = Vector3.Lerp(transform.position, livre, Mathf.Min(1f, rapidez * Time.deltaTime));
        transform.LookAt(foco);
    }

    // Prédio no caminho: primeiro a câmera sobe (olha mais de cima, por cima dele); só se nem assim der,
    // ela chega mais perto. A subida é suave e some quando o caminho fica livre.
    // Paredes invisíveis ficam na camada "Ignore Raycast" e não contam.
    private float inclinacaoExtra;

    private Vector3 Desviar(Vector3 foco, Vector3 desejada)
    {
        float precisa = 0f;
        for (float extra = 0f; extra <= 60f; extra += 10f)
        {
            float inclinacao = Mathf.Min(Inclinacao + extra, 80f);
            if (Distancia(foco, Posicao(foco, inclinacao), out _) < 0f) { precisa = inclinacao - Inclinacao; break; }
            precisa = inclinacao - Inclinacao;
        }
        inclinacaoExtra = Mathf.MoveTowards(inclinacaoExtra, precisa, 90f * Time.deltaTime);
        Vector3 posicao = Posicao(foco, Inclinacao + inclinacaoExtra);
        float bate = Distancia(foco, posicao, out Vector3 d);
        if (bate < 0f) return posicao;
        return foco + d * Mathf.Max(distanciaMinima, bate - 0.1f);
    }

    private Vector3 Posicao(Vector3 foco, float inclinacao)
    {
        Vector3 p = foco - Quaternion.Euler(Mathf.Min(inclinacao, 80f), Giro, 0f) * Vector3.forward * distancia;
        p.y = Mathf.Max(p.y, 0.4f);
        return p;
    }

    // Distância do foco até a primeira coisa no caminho da câmera (-1 se o caminho está livre).
    private float Distancia(Vector3 foco, Vector3 posicao, out Vector3 d)
    {
        d = posicao - foco;
        float dist = d.magnitude;
        if (dist < 0.01f) return -1f;
        d /= dist;
        float menor = -1f;
        foreach (RaycastHit h in Physics.SphereCastAll(foco, raioDaColisao, d, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.distance <= 0f || h.collider is CharacterController || h.collider.transform.IsChildOf(target)) continue;
            if (menor < 0f || h.distance < menor) menor = h.distance;
        }
        return menor;
    }
}
