using System.Collections.Generic;
using UnityEngine;

// Cria obstáculos, coletáveis e power-ups no topo da pista, em faixas aleatórias, copiando
// modelos inativos da cena. O tipo, a cor e o rótulo de cada modelo vêm da RunnerConfig (pelo id).
// O espaçamento é por distância percorrida, então os objetos continuam igualmente espaçados
// quando a velocidade sobe.
public class RunnerSpawner : MonoBehaviour
{
    public RunnerManager manager;
    public RunnerFaixas faixas;
    public RectTransform pista;
    public RunnerObjeto[] modelos;

    private readonly List<RunnerObjeto> obstaculos = new List<RunnerObjeto>();
    private readonly List<RunnerObjeto> coletaveis = new List<RunnerObjeto>();
    private readonly List<RunnerObjeto> powerUps = new List<RunnerObjeto>();
    private float distanciaDesdeUltimo;

    private void Start()
    {
        foreach (RunnerObjeto modelo in modelos)
        {
            modelo.Aplicar(manager.config.Buscar(modelo.id));
            switch (modelo.Tipo)
            {
                case TipoRunnerObjeto.Coletavel:
                    coletaveis.Add(modelo);
                    break;
                case TipoRunnerObjeto.Protecao:
                    powerUps.Add(modelo);
                    break;
                default:
                    obstaculos.Add(modelo);
                    break;
            }
        }
    }

    private void Update()
    {
        if (!manager.Jogando)
        {
            return;
        }

        distanciaDesdeUltimo += manager.Velocidade * Time.deltaTime;
        if (distanciaDesdeUltimo >= manager.config.distanciaEntreObjetos)
        {
            distanciaDesdeUltimo -= manager.config.distanciaEntreObjetos;
            Criar();
        }
    }

    private void Criar()
    {
        // Sem power-ups na lista, a chance deles não conta (a de coletável fica igual).
        float limiteDePowerUp = powerUps.Count > 0 ? manager.config.chanceDePowerUp : 0f;
        float sorteio = Random.value;
        List<RunnerObjeto> opcoes = obstaculos;
        if (sorteio < limiteDePowerUp)
        {
            opcoes = powerUps;
        }
        else if (coletaveis.Count > 0 && sorteio < limiteDePowerUp + manager.config.chanceDeColetavel)
        {
            opcoes = coletaveis;
        }

        if (opcoes.Count == 0)
        {
            return;
        }

        RunnerObjeto modelo = opcoes[Random.Range(0, opcoes.Count)];
        RunnerObjeto objeto = Instantiate(modelo, pista);
        objeto.manager = manager;
        objeto.Aplicar(manager.config.Buscar(modelo.id));
        objeto.gameObject.SetActive(true);

        RectTransform rt = (RectTransform)objeto.transform;
        rt.anchoredPosition = new Vector2(
            faixas.PosicaoX(faixas.FaixaAleatoria()),
            pista.rect.height / 2f + rt.rect.height / 2f);
    }
}
