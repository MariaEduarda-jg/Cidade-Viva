using System.Globalization;
using System.Text;
using UnityEngine;

// Ponto único que todo minigame chama ao terminar.
// Salva localmente via PlayerPrefs, por minigame: se já foi completado e o melhor recorde.
// Chaves: "Completado_<Id>" e "Recorde_<Id>", onde <Id> é o nome da área sem espaços,
// pontuação nem acentos (ex.: "Onde Eu Vou?" -> "OndeEuVou").
public static class GameProgress
{
    public static void FinalizarMinigame(string nomeDaArea, int pontuacao)
    {
        string id = IdDaArea(nomeDaArea);
        int recordeAnterior = ObterRecorde(nomeDaArea);

        if (pontuacao > recordeAnterior)
            PlayerPrefs.SetInt("Recorde_" + id, pontuacao);
        PlayerPrefs.SetInt("Completado_" + id, 1);
        PlayerPrefs.Save();

        Debug.Log("Minigame " + nomeDaArea + " finalizado com pontuação " + pontuacao
            + " (recorde: " + ObterRecorde(nomeDaArea) + ")");
    }

    public static int ObterRecorde(string nomeDaArea)
    {
        return PlayerPrefs.GetInt("Recorde_" + IdDaArea(nomeDaArea), 0);
    }

    public static bool FoiCompletado(string nomeDaArea)
    {
        return PlayerPrefs.GetInt("Completado_" + IdDaArea(nomeDaArea), 0) == 1;
    }

    // Mantém só letras e dígitos, sem acento: "Semáforo Mágico" -> "SemaforoMagico".
    static string IdDaArea(string nomeDaArea)
    {
        var id = new StringBuilder();
        foreach (char c in nomeDaArea.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                id.Append(c);
        }
        return id.ToString();
    }
}
