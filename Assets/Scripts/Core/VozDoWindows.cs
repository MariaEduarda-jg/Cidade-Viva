#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;

// Narração no Windows (Editor e build de Desktop): usa a voz de leitura do próprio Windows
// (System.Speech), em português. O Windows em pt-BR já vem com a voz "Microsoft Maria".
// A fala roda num PowerShell escondido, pra não travar o jogo; "Parar" encerra esse processo.
// Sem voz em português instalada, Disponivel fica falso e o botão "Ouvir" some, sem erro.
public class VozDoWindows : IVozNarradora
{
    private const string EscolherVoz =
        "Add-Type -AssemblyName System.Speech\n" +
        "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer\n" +
        "$v = $s.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -like 'pt*' } | " +
        "Sort-Object { if ($_.VoiceInfo.Culture.Name -eq 'pt-BR') { 0 } else { 1 } } | Select-Object -First 1\n" +
        "if (-not $v) { exit 1 }\n";

    // null = ainda conferindo se existe voz em português.
    private static bool? temVoz;
    private static bool conferindo;
    private Process falando;

    public VozDoWindows()
    {
        Application.quitting += Parar;
        Conferir();
    }

    public bool Disponivel
    {
        get { return temVoz == true; }
    }

    public void Falar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return;
        }
        if (Application.isEditor)
        {
            UnityEngine.Debug.Log("narração: " + texto);
        }
        if (temVoz != true)
        {
            return;
        }

        Parar();
        // O texto vai em base64 dentro do comando: acentos e aspas chegam inteiros.
        string emBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(texto));
        falando = Rodar(EscolherVoz +
            "$s.SelectVoice($v.VoiceInfo.Name)\n$s.Rate = -1\n" +
            "$s.Speak([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + emBase64 + "')))\n", null);
    }

    public void Parar()
    {
        try
        {
            if (falando != null && !falando.HasExited)
            {
                falando.Kill();
            }
        }
        catch (Exception)
        {
            // O processo pode ter acabado entre a conferência e o Kill: não importa.
        }
        falando = null;
    }

    // Pergunta ao Windows, uma vez, se há voz em português. A resposta chega em cerca de 1 segundo.
    private static void Conferir()
    {
        if (temVoz != null || conferindo)
        {
            return;
        }
        conferindo = true;
        Process p = Rodar(EscolherVoz + "exit 0\n", codigo => temVoz = codigo == 0);
        if (p == null)
        {
            temVoz = false;
        }
    }

    private static Process Rodar(string comandos, Action<int> aoTerminar)
    {
        try
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(comandos)),
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            if (aoTerminar != null)
            {
                p.EnableRaisingEvents = true;
                p.Exited += (remetente, argumentos) => aoTerminar(p.ExitCode);
            }
            p.Start();
            return p;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Narração: não foi possível usar a voz do Windows (" + e.Message + ").");
            return null;
        }
    }
}
#endif
