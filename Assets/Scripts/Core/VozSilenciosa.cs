using UnityEngine;

// Voz de mentira, pra testar no Editor: não faz som, só escreve no Console o que seria falado.
// Fora do Editor (build de Desktop) não existe narração, então ela se declara indisponível e o botão "Ouvir" some.
public class VozSilenciosa : IVozNarradora
{
    public bool Disponivel
    {
        get { return Application.isEditor; }
    }

    public void Falar(string texto)
    {
        Debug.Log("narração: " + texto);
    }

    public void Parar()
    {
    }
}
