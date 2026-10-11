using UnityEngine;

// Ícones usados pelos perfis: os bichinhos (avatar) e as figuras do código secreto.
// A ordem das listas faz parte dos dados salvos: o perfil guarda os índices. Pode trocar a arte
// de um ícone, mas não mude a ordem nem remova itens, senão os perfis existentes mudam de cara e de código.
[CreateAssetMenu(fileName = "Icones_De_Perfil", menuName = "Cidade Viva/Ícones de perfil")]
public class IconesDePerfil : ScriptableObject
{
    public Sprite[] avatares;
    public Sprite[] figuras;
    public Sprite avatarDoVisitante;

    public Sprite Avatar(int indice)
    {
        if (avatares != null && indice >= 0 && indice < avatares.Length)
        {
            return avatares[indice];
        }
        return avatarDoVisitante;
    }

    public Sprite Figura(int indice)
    {
        return figuras != null && indice >= 0 && indice < figuras.Length ? figuras[indice] : null;
    }
}
