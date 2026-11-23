using System;
using System.Collections.Generic;
using UnityEngine;

// Um dia da campanha: a conversa da mãe e as missões.
[CreateAssetMenu(menuName = "Cidade Viva/Campanha: dia", fileName = "Dia")]
public class DiaDaCampanha : ScriptableObject
{
    public int numero = 1;
    public string titulo;
    [Tooltip("O que a mãe diz pra abrir o dia.")]
    [TextArea] public string[] falasDaMae = new string[0];
    [Tooltip("O que a mãe diz quando todas as missões do dia acabam.")]
    [TextArea] public string[] falasDoFim = new string[0];
    public MissaoDaCampanha[] missoes = new MissaoDaCampanha[0];
}
