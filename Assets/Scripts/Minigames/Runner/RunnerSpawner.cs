using UnityEngine;

// Cria obstáculos e coletáveis no topo da pista, em faixas aleatórias, copiando modelos
// inativos da cena. O espaçamento é por distância percorrida, então os objetos continuam
// igualmente espaçados quando a velocidade sobe.
public class RunnerSpawner : MonoBehaviour
{
    public RunnerManager manager;
    public RunnerFaixas faixas;
    public RectTransform pista;
    public RunnerObjeto[] modelosDeObstaculo;
    public RunnerObjeto[] modelosDeColetavel;
    public RunnerObjeto[] modelosDePowerUp = new RunnerObjeto[0];
    [Range(0f, 1f)] public float chanceDeColetavel = 0.3f;
    [Range(0f, 1f)] public float chanceDePowerUp = 0.08f;
    public float distanciaEntreObjetos = 400f;

    private float distanciaDesdeUltimo;

    private void Update()
    {
        if (!manager.Jogando)
        {
            return;
        }

        distanciaDesdeUltimo += manager.Velocidade * Time.deltaTime;
        if (distanciaDesdeUltimo >= distanciaEntreObjetos)
        {
            distanciaDesdeUltimo -= distanciaEntreObjetos;
            Criar();
        }
    }

    private void Criar()
    {
        // Sem power-ups na lista, a chance deles não conta (a de coletável fica igual).
        float limiteDePowerUp = modelosDePowerUp.Length > 0 ? chanceDePowerUp : 0f;
        float sorteio = Random.value;
        RunnerObjeto[] modelos = modelosDeObstaculo;
        if (sorteio < limiteDePowerUp)
        {
            modelos = modelosDePowerUp;
        }
        else if (modelosDeColetavel.Length > 0 && sorteio < limiteDePowerUp + chanceDeColetavel)
        {
            modelos = modelosDeColetavel;
        }

        if (modelos.Length == 0)
        {
            return;
        }

        RunnerObjeto modelo = modelos[Random.Range(0, modelos.Length)];
        RunnerObjeto objeto = Instantiate(modelo, pista);
        objeto.manager = manager;
        objeto.gameObject.SetActive(true);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchoredPosition = new Vector2(
            faixas.PosicaoX(faixas.FaixaAleatoria()),
            pista.rect.height / 2f + rt.rect.height / 2f);
    }
}
