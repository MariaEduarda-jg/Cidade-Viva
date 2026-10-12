using System.Collections.Generic;
using UnityEngine;

// Base do veículo em cima da bancada: uma estrutura neutra com encaixes pra todos os tipos de peça.
// A frente do chassi é +Z. Depois de montado, é este objeto que vai pra pista de teste.
public class ChassiDaOficina : MonoBehaviour
{
    // Nome usado na VeiculoReceita.chassi (ex.: "DuasRodas").
    public string id;
    public EncaixeDaOficina[] encaixes;
    public float raioDaRoda = 0.38f;

    // O motor elétrico vai no mesmo lugar do motor.
    public static TipoDePeca TipoDoEncaixe(TipoDePeca tipo)
    {
        return tipo == TipoDePeca.MotorEletrico ? TipoDePeca.Motor : tipo;
    }

    // Peças que o modelo do veículo não tem desenhadas: continuam à mostra depois de encaixadas.
    public static bool FicaAMostra(TipoDePeca tipo)
    {
        return tipo == TipoDePeca.Trilho || tipo == TipoDePeca.Engate || tipo == TipoDePeca.Cobertura;
    }

    public IEnumerable<EncaixeDaOficina> EncaixesDe(TipoDePeca tipo)
    {
        tipo = TipoDoEncaixe(tipo);
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.tipo == tipo)
            {
                yield return e;
            }
        }
    }

    public int Ocupados(TipoDePeca tipo)
    {
        int n = 0;
        foreach (EncaixeDaOficina e in EncaixesDe(tipo))
        {
            if (!e.Livre)
            {
                n++;
            }
        }
        return n;
    }

    public int Vagas(TipoDePeca tipo)
    {
        int n = 0;
        foreach (EncaixeDaOficina e in EncaixesDe(tipo))
        {
            n++;
        }
        return n;
    }

    // Encaixe livre do tipo. Os "preferidos" primeiros encaixes da lista (ex.: as 4 rodas dos cantos)
    // são preenchidos antes dos que sobram (as rodas extras do meio).
    // Com "perto" informado, escolhe o mais próximo desse ponto MEDIDO AO LONGO DO VEÍCULO (frente/trás):
    // de onde a criança olha, as rodas do lado de lá ficam escondidas atrás das de cá, então a distância
    // de verdade poria todas do mesmo lado. Assim, soltar perto da frente enche a frente (um lado, depois o outro).
    public EncaixeDaOficina EncaixeLivre(TipoDePeca tipo, Vector3? perto = null, int preferidos = 0)
    {
        for (int passada = 0; passada < 2; passada++)
        {
            EncaixeDaOficina melhor = null;
            float menor = float.MaxValue;
            int indice = -1;
            foreach (EncaixeDaOficina e in EncaixesDe(tipo))
            {
                indice++;
                bool preferido = preferidos <= 0 || indice < preferidos;
                if (!e.Livre || (passada == 0 && !preferido))
                {
                    continue;
                }
                if (perto == null)
                {
                    return e;
                }
                float d = Mathf.Abs(Vector3.Dot(e.transform.position - perto.Value, transform.forward));
                if (d < menor - 0.01f)
                {
                    menor = d;
                    melhor = e;
                }
            }
            if (melhor != null)
            {
                return melhor;
            }
        }
        return null;
    }

    public GameObject ModeloPronto { get; private set; }

    // Modelo do veículo de verdade em cima da bancada, todo cinza ("molde") até a montagem terminar.
    public GameObject Molde { get; private set; }
    private readonly List<Renderer> pecasDoMolde = new List<Renderer>();
    private readonly List<Material[]> coresDoMolde = new List<Material[]>();

    // Põe o modelo do veículo no lugar da estrutura genérica e leva os encaixes pros lugares certos
    // dele (rodas nas rodas, assento em cima, luzes na frente...), medindo o modelo. Assim a criança
    // monta em cima do veículo que vai sair, e não numa barra que não parece com nada.
    public void VestirMolde(GameObject prefab, float escalaExtra, float giroY, Material barro, float comprimento)
    {
        if (prefab == null || Molde != null)
        {
            return;
        }
        LimparPintura();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<EncaixeDaOficina>() == null)
            {
                r.enabled = false;
            }
        }

        // Mede o modelo solto no mundo, já virado pra frente certa.
        Molde = Instantiate(prefab);
        Molde.name = "Molde";
        Molde.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, giroY, 0f));
        Molde.transform.localScale = Vector3.one;
        Renderer[] partes = Molde.GetComponentsInChildren<Renderer>();
        if (partes.Length == 0)
        {
            return;
        }
        Bounds b = partes[0].bounds;
        foreach (Renderer r in partes)
        {
            b.Encapsulate(r.bounds);
        }

        float s = comprimento / Mathf.Max(0.01f, b.size.z) * Mathf.Max(0.1f, escalaExtra);
        Molde.transform.SetParent(transform, false);
        Molde.transform.localPosition = Vector3.zero;
        Molde.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        Molde.transform.localScale = Vector3.one * s;

        // Cada material do modelo ganha uma cópia no shader do molde, com a mesma textura e cor:
        // cinza por fora, e a cor de verdade só onde uma peça já foi encaixada.
        foreach (Renderer r in partes)
        {
            Material[] originais = r.sharedMaterials;
            pecasDoMolde.Add(r);
            coresDoMolde.Add(originais);
            var cinza = new Material[originais.Length];
            for (int i = 0; i < cinza.Length; i++)
            {
                cinza[i] = new Material(barro);
                Material o = originais[i];
                if (o != null && o.HasProperty("_BaseMap") && o.GetTexture("_BaseMap") != null)
                {
                    cinza[i].SetTexture("_BaseMap", o.GetTexture("_BaseMap"));
                }
                if (o != null && o.HasProperty("_BaseColor"))
                {
                    cinza[i].SetColor("_BaseColor", o.GetColor("_BaseColor"));
                }
                materiaisDoMolde.Add(cinza[i]);
            }
            r.sharedMaterials = cinza;
        }

        float L = b.size.z * s, W = b.size.x * s, H = b.size.y * s, meio = b.center.z * s;
        bool duas = id == "DuasRodas";
        int roda = 0;
        foreach (EncaixeDaOficina e in encaixes)
        {
            Transform t = e.transform;
            switch (e.tipo)
            {
                case TipoDePeca.Roda:
                    if (id == "UmEixo")
                    {
                        // Um eixo só: uma roda de cada lado.
                        float r = Mathf.Clamp(H * 0.42f, 0.15f, 0.5f);
                        t.localPosition = new Vector3((roda == 0 ? 1f : -1f) * (W * 0.5f - r * 0.3f), r, meio - L * 0.08f);
                        t.localScale = Vector3.one * (r / raioDaRoda);
                    }
                    else if (duas)
                    {
                        float r = Mathf.Clamp(H * 0.36f, 0.15f, 0.6f);
                        t.localPosition = new Vector3(0f, r, meio + (roda == 0 ? 1f : -1f) * (L * 0.5f - r));
                        t.localScale = Vector3.one * (r / raioDaRoda);
                    }
                    else
                    {
                        // Ordem dos encaixes: frente (cá, lá), trás (cá, lá), meio (cá, lá).
                        float r = Mathf.Clamp(H * 0.26f, 0.12f, 0.5f);
                        float z = roda < 2 ? L * 0.3f : (roda < 4 ? -L * 0.3f : 0f);
                        t.localPosition = new Vector3((roda % 2 == 0 ? 1f : -1f) * (W * 0.5f - r * 0.3f), r, meio + z);
                        t.localScale = Vector3.one * (r / raioDaRoda);
                    }
                    roda++;
                    break;
                case TipoDePeca.Pedal:
                    t.localPosition = new Vector3(0f, H * 0.3f, meio - L * 0.03f);
                    break;
                case TipoDePeca.Motor:
                    t.localPosition = duas ? new Vector3(0f, H * 0.45f, meio + L * 0.02f) : new Vector3(0f, H * 0.68f, meio + L * 0.33f);
                    break;
                case TipoDePeca.Assento:
                    t.localPosition = duas ? new Vector3(0f, H * 0.9f, meio - L * 0.2f) : new Vector3(0f, H * 0.82f, meio - L * 0.05f);
                    break;
                case TipoDePeca.Luzes:
                    t.localPosition = duas ? new Vector3(0f, H * 0.72f, meio + L * 0.42f) : new Vector3(0f, H * 0.55f, meio + L * 0.47f);
                    break;
                case TipoDePeca.Sirene:
                    t.localPosition = new Vector3(0f, H * 1.04f, meio);
                    break;
                case TipoDePeca.Carroceria:
                    t.localPosition = duas ? new Vector3(0f, H * 0.8f, meio - L * 0.4f) : new Vector3(0f, H * 0.8f, meio - L * 0.34f);
                    break;
                case TipoDePeca.Engate:
                    // No reboque o engate é na frente (a ponta que prende em quem puxa); nos outros, atrás.
                    t.localPosition = id == "UmEixo" ? new Vector3(0f, H * 0.4f, meio + L * 0.5f) : new Vector3(0f, H * 0.3f, meio - L * 0.5f - 0.05f);
                    break;
                case TipoDePeca.Cobertura:
                    t.localPosition = new Vector3(0f, H * 1.1f, meio - L * 0.05f);
                    t.localScale = Vector3.one * Mathf.Max(0.8f, W / 0.95f);
                    break;
                case TipoDePeca.Trilho:
                    t.localPosition = new Vector3(0f, 0.03f, meio);
                    t.localScale = Vector3.one * (L / 0.9f);
                    break;
            }
        }
    }

    private readonly List<Material> materiaisDoMolde = new List<Material>();
    private static readonly Vector4[] esferas = new Vector4[12];
    private static readonly int IdDasEsferas = Shader.PropertyToID("_MoldeEsferas");
    private static readonly int IdDaQuantidade = Shader.PropertyToID("_MoldeQuantidade");

    // Tamanho da parte do veículo que cada peça "pinta" ao encaixar (raio em metros, antes da escala da bancada).
    private float RaioDaPintura(EncaixeDaOficina e)
    {
        switch (e.tipo)
        {
            case TipoDePeca.Roda: return raioDaRoda * e.transform.localScale.x * 1.3f;
            case TipoDePeca.Motor:
            case TipoDePeca.MotorEletrico: return 0.5f;
            case TipoDePeca.Carroceria: return 0.55f;
            case TipoDePeca.Assento: return 0.4f;
            case TipoDePeca.Trilho:
            case TipoDePeca.Engate:
            case TipoDePeca.Cobertura: return 0.01f;
            default: return 0.34f;
        }
    }

    // A peça encaixada some e, no lugar dela, aquela parte do veículo aparece com a cor de verdade.
    public void Fundir(PecaDaOficina peca)
    {
        if (Molde == null || peca == null)
        {
            return;
        }
        if (!FicaAMostra(peca.tipo))
        {
            foreach (Renderer r in peca.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }
        AtualizarPintura();
    }

    // Saindo da bancada pra pista: os trilhos ficam (a pista tem os dela).
    public void SairDaBancada()
    {
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.Ocupante != null && e.Ocupante.tipo == TipoDePeca.Trilho)
            {
                e.Ocupante.gameObject.SetActive(false);
            }
        }
    }

    // Um veículo inteiro, solto, com a frente em +Z e o comprimento pedido (ex.: o caminhão trator que puxa o reboque).
    public static Transform CriarModeloSolto(GameObject prefab, float giroY, float comprimento)
    {
        var raiz = new GameObject("Veiculo_Que_Puxa").transform;
        GameObject modelo = Instantiate(prefab);
        modelo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, giroY, 0f));
        modelo.transform.localScale = Vector3.one;
        Renderer[] partes = modelo.GetComponentsInChildren<Renderer>();
        float tamanho = 1f;
        if (partes.Length > 0)
        {
            Bounds b = partes[0].bounds;
            foreach (Renderer r in partes)
            {
                b.Encapsulate(r.bounds);
            }
            tamanho = Mathf.Max(0.01f, b.size.z);
        }
        modelo.transform.SetParent(raiz, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        modelo.transform.localScale = Vector3.one * (comprimento / tamanho);
        return raiz;
    }

    private void AtualizarPintura()
    {
        int n = 0;
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.Livre || e.Ocupante.EmMovimento || n >= esferas.Length)
            {
                continue;
            }
            Vector3 p = e.transform.position;
            esferas[n++] = new Vector4(p.x, p.y, p.z, RaioDaPintura(e) * transform.lossyScale.x);
        }
        Shader.SetGlobalVectorArray(IdDasEsferas, esferas);
        Shader.SetGlobalFloat(IdDaQuantidade, n);
    }

    public static void LimparPintura()
    {
        Shader.SetGlobalVectorArray(IdDasEsferas, esferas);
        Shader.SetGlobalFloat(IdDaQuantidade, 0f);
    }

    private void OnDestroy()
    {
        foreach (Material m in materiaisDoMolde)
        {
            Destroy(m);
        }
    }

    // Montagem terminada: o molde ganha as cores do veículo e as peças soltas somem dentro dele.
    public void RevelarMolde()
    {
        if (Molde == null)
        {
            return;
        }
        LimparPintura();
        for (int i = 0; i < pecasDoMolde.Count; i++)
        {
            pecasDoMolde[i].sharedMaterials = coresDoMolde[i];
        }
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.Ocupante != null && !FicaAMostra(e.Ocupante.tipo))
            {
                foreach (Renderer r in e.Ocupante.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }
        }
    }

    // O veículo montado "ganha vida": a estrutura da bancada some e entra o modelo de verdade do veículo.
    // "giroY" acerta modelos cuja frente não aponta pra +Z (senão o veículo anda de ré).
    public void VirarModeloPronto(GameObject prefab, float escala, float giroY)
    {
        if (prefab == null || ModeloPronto != null)
        {
            return;
        }
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
        ModeloPronto = Instantiate(prefab, transform);
        ModeloPronto.name = "Modelo_Pronto";
        ModeloPronto.transform.localPosition = Vector3.zero;
        ModeloPronto.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        ModeloPronto.transform.localScale = Vector3.one * escala;
    }

    public void EsconderMarcadores()
    {
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.marcador != null)
            {
                e.marcador.SetActive(false);
            }
        }
    }

    // Gira rodas e pedal conforme o veículo anda "metros" pra frente.
    public void Girar(float metros)
    {
        float graus = metros / Mathf.Max(0.05f, raioDaRoda) * Mathf.Rad2Deg;
        foreach (EncaixeDaOficina e in encaixes)
        {
            if (e.Ocupante == null || e.Ocupante.visual == null)
            {
                continue;
            }
            if (e.Ocupante.tipo == TipoDePeca.Roda)
            {
                e.Ocupante.visual.Rotate(graus, 0f, 0f, Space.Self);
            }
            else if (e.Ocupante.tipo == TipoDePeca.Pedal)
            {
                e.Ocupante.visual.Rotate(graus * 0.5f, 0f, 0f, Space.Self);
            }
        }
    }
}
