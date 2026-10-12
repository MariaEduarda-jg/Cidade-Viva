using System;
using System.Collections.Generic;
using UnityEngine;

// Peças que existem na prateleira da Oficina de Veículos.
public enum TipoDePeca
{
    Roda,
    Pedal,
    Motor,
    MotorEletrico,
    Trilho,
    Sirene,
    Luzes,
    Carroceria,
    Engate,
    Assento,
    // Peças de coisas que não são veículo de rua: nenhum verbete pede, servem só de peça errada.
    Asa,
    Helice,
    Ancora,
    VelaDeBarco,
    Remo,
    // "Parte de cima para cobrir" do quadriciclo.
    Cobertura,
}

// Uma linha da receita: que peça, quantas, e a frase da definição que manda pôr essa peça.
[Serializable]
public class ItemDaReceita
{
    [Tooltip("Peça pedida. O motor elétrico usa o mesmo encaixe do motor.")]
    public TipoDePeca peca;

    [Tooltip("Outras peças que também servem no mesmo encaixe (ex.: a definição diz só \"motor\", então o motor elétrico também vale).")]
    public List<TipoDePeca> tambemAceita = new List<TipoDePeca>();

    [Min(1)]
    public int quantidade = 1;

    [Tooltip("A definição diz \"ou mais\" (ex.: \"quatro ou mais rodas\"): além da quantidade, aceita extras nos encaixes que sobrarem.")]
    public bool ouMais;

    [Tooltip("Trecho copiado da definição que justifica esta peça. Tem que existir, letra por letra, na definição. Fica vazio só em peça de base padrão.")]
    public string trechoDaDefinicao;

    [Tooltip("Vazio = o trecho é da definição do próprio veículo. Preencha com o id de outro verbete quando a definição cita esse verbete (ex.: \"Bicicleta com motor\" -> bicicleta).")]
    public string verbeteDoTrecho = "";

    [Tooltip("Como a definição chama o outro verbete, quando não usa a palavra exata dele (ex.: a motoneta diz \"moto\" pra motocicleta).")]
    public string comoEhCitado = "";

    [Tooltip("BASE PADRÃO: a definição não fala desta peça (ex.: as rodas da ambulância). Ela entra porque o chassi precisa dela pra existir. Não vem do glossário.")]
    public bool basePadrao;

    [Tooltip("A definição diz que pode ter ou não (ex.: a cobertura do quadriciclo): encaixa sem erro, mas não é exigida.")]
    public bool opcional;

    public bool Aceita(TipoDePeca tipo)
    {
        return tipo == peca || tambemAceita.Contains(tipo);
    }
}

// Receita de montagem de um veículo na Oficina. Um asset por veículo, em Assets/Resources/Veiculos:
// pra acrescentar um veículo ao jogo, basta criar outro asset lá (menu Create > Cidade Viva).
//
// REGRA: a receita sai SOMENTE da definição do verbete no glossário Glotrânsito (Assets/Data/verbetes.json).
// Cada item guarda o trecho que o justifica. O que a definição não diz e o veículo precisa pra existir
// (rodas, motor) entra marcado como "base padrão". Nada de completar com o que "todo mundo sabe".
[CreateAssetMenu(fileName = "Receita_Veiculo", menuName = "Cidade Viva/Oficina: receita de veículo (VeiculoReceita)")]
public class VeiculoReceita : ScriptableObject
{
    public const string PastaNoResources = "Veiculos";

    [Tooltip("Id do verbete em verbetes.json (ex.: \"bicicleta_eletrica\").")]
    public string idDoVerbete;

    [Range(1, 5)]
    [Tooltip("Nível da Oficina em que este veículo é montado.")]
    public int nivel = 1;

    [Tooltip("Ordem dentro do nível (menor primeiro).")]
    public int ordem;

    [Tooltip("Chassi usado na bancada: nome de um Modelo_Chassi_<nome> da cena (DuasRodas, QuatroRodas, UmEixo).")]
    public string chassi = "DuasRodas";

    public List<ItemDaReceita> itens = new List<ItemDaReceita>();

    [Tooltip("Peças que a definição exclui (ex.: reboque \"sem motor\"; bonde \"com motor elétrico\" exclui o motor comum). Em cada uma: a peça e o trecho que a exclui. Entram na prateleira como erradas e o balão cita o trecho.")]
    public List<ItemDaReceita> proibidas = new List<ItemDaReceita>();

    [Tooltip("Peças que o veículo tem na vida real, mas a definição não cita (ex.: assento). Encaixam sem contar erro e não são exigidas pra completar. Isto NÃO vem da definição: é escolha da equipe.")]
    public List<TipoDePeca> extrasAceitos = new List<TipoDePeca>();

    [Header("Teste na pista")]
    [Tooltip("A definição diz que anda por trilhos (bonde): na pista ele segue os trilhos, sem volante.")]
    public bool andaEmTrilhos;
    [Tooltip("Id do verbete do veículo que puxa este (reboque -> caminhao_trator). Com isto, antes do teste há o passo de engatar, e quem a criança dirige é o veículo que puxa.")]
    public string puxadoPor = "";
    [Tooltip("Velocidade na pista em relação ao normal (a motoneta é \"mais devagar\" que a moto).")]
    public float velocidadeNaPista = 1f;

    [Header("Aparência (não muda a receita)")]
    [Tooltip("Formato especial das peças deste veículo (hoje: \"Motoneta\", que usa o assento de pernas juntas). Vazio = o formato do chassi.")]
    public string variante = "";
    [Tooltip("Prefab do veículo inteiro: é o molde cinza da bancada e o veículo do teste na pista. Vazio = estrutura genérica.")]
    public GameObject modeloPronto;
    [Tooltip("Ajuste fino do tamanho do modelo (1 = o tamanho padrão da bancada).")]
    public float escalaDoModeloPronto = 1f;
    [Tooltip("Giro em Y pra frente do modelo apontar pra onde o veículo anda (+Z). Use 180 se ele andar de ré.")]
    public float giroDoModeloPronto;

    [TextArea]
    [Tooltip("Anotação pra equipe (não aparece no jogo): dúvidas e escolhas de interpretação da definição.")]
    public string observacao = "";

    public ItemDaReceita ItemQueAceita(TipoDePeca tipo)
    {
        foreach (ItemDaReceita item in itens)
        {
            if (item.Aceita(tipo))
            {
                return item;
            }
        }
        return null;
    }

    public ItemDaReceita Proibida(TipoDePeca tipo)
    {
        foreach (ItemDaReceita item in proibidas)
        {
            if (item.Aceita(tipo))
            {
                return item;
            }
        }
        return null;
    }

    public bool Tem(TipoDePeca tipo)
    {
        foreach (ItemDaReceita item in itens)
        {
            if (item.peca == tipo)
            {
                return true;
            }
        }
        return false;
    }
}
