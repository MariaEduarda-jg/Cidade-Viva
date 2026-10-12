using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Atalhos pra testar sem jogar. Só funcionam em Play Mode, porque mexem no perfil que está jogando.
    public static class TesteMenu
    {
        [MenuItem("Cidade Viva/Teste/Desbloquear verbete aleatório")]
        public static void DesbloquearVerbeteAleatorio()
        {
            if (!EmPlayMode()) return;
            var bloqueados = VerbeteDatabase.Instancia.Todos.Where(v => !GameProgress.VerbeteDesbloqueado(v.id)).ToList();
            if (bloqueados.Count == 0)
            {
                Debug.Log("[Teste] " + Perfis.Ativo.nome + " já tem todos os verbetes.");
                return;
            }

            // Passa pelo mesmo caminho do jogo: o cartão desbloqueia o verbete e mostra o selo "Novo!".
            var v = bloqueados[Random.Range(0, bloqueados.Count)];
            Debug.Log("[Teste] Desbloqueando \"" + v.palavra + "\" para " + Perfis.Ativo.nome + " (faltavam " + bloqueados.Count + ").");
            CartaoVerbete.Mostrar(v.id, null);
        }

        [MenuItem("Cidade Viva/Teste/Bloquear todos os verbetes de novo")]
        public static void BloquearTodos()
        {
            if (!EmPlayMode()) return;
            GameProgress.BloquearTodosOsVerbetes();
            Debug.Log("[Teste] Álbum de " + Perfis.Ativo.nome + " esvaziado.");
        }

        // ------------------------------------------------------------------ caos das áreas novas do Hub

        // Finge um progresso de verbetes numa área (não mexe no perfil) pra ver o caos mudando.
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Oficina/0%")] static void Oficina0() { SimularCaos("Oficina", 0f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Oficina/50%")] static void Oficina50() { SimularCaos("Oficina", .5f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Oficina/100%")] static void Oficina100() { SimularCaos("Oficina", 1f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Obras da Cidade/0%")] static void Obras0() { SimularCaos("Obras da Cidade", 0f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Obras da Cidade/50%")] static void Obras50() { SimularCaos("Obras da Cidade", .5f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Obras da Cidade/100%")] static void Obras100() { SimularCaos("Obras da Cidade", 1f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Cruzamento Central/0%")] static void Cruzamento0() { SimularCaos("Cruzamento Central", 0f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Cruzamento Central/50%")] static void Cruzamento50() { SimularCaos("Cruzamento Central", .5f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Cruzamento Central/100%")] static void Cruzamento100() { SimularCaos("Cruzamento Central", 1f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Rua Viva/0%")] static void RuaViva0() { SimularCaos("Rua Viva", 0f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Rua Viva/50%")] static void RuaViva50() { SimularCaos("Rua Viva", .5f); }
        [MenuItem("Cidade Viva/Teste/Caos do Hub/Rua Viva/100%")] static void RuaViva100() { SimularCaos("Rua Viva", 1f); }

        [MenuItem("Cidade Viva/Teste/Caos do Hub/Voltar ao progresso real")]
        public static void VoltarAoReal()
        {
            if (!EmPlayMode()) return;
            foreach (var controle in Controles()) controle.PararDeSimular();
            Debug.Log("[Teste] As áreas voltaram a mostrar o progresso real de " + Perfis.Ativo.nome + ".");
        }

        public static void SimularCaos(string area, float progresso)
        {
            if (!EmPlayMode()) return;
            var controle = Controles().FirstOrDefault(c => c.nomeDaArea == area);
            if (controle == null)
            {
                Debug.LogWarning("[Teste] A área \"" + area + "\" não está nesta cena. Abra o Hub.");
                return;
            }
            controle.Simular(progresso);
            Debug.Log("[Teste] " + area + " em " + Mathf.RoundToInt(progresso * 100f) + "% (faixa " + controle.Faixa + "): caos ligado = "
                + Ligados(controle.caosPorFaixa) + "; ordem ligada = " + Ligados(controle.ordemPorFaixa) + ".");
        }

        static ChaosController[] Controles()
        {
            return Resources.FindObjectsOfTypeAll<ChaosController>().Where(c => c.gameObject.scene.IsValid()).ToArray();
        }

        static string Ligados(GameObject[] grupos)
        {
            var nomes = grupos.Where(g => g != null && g.activeSelf).Select(g => g.name).ToArray();
            return nomes.Length == 0 ? "nenhum" : string.Join(", ", nomes);
        }

        static bool EmPlayMode()
        {
            if (Application.isPlaying) return true;
            Debug.LogWarning("[Teste] Aperte Play primeiro: os verbetes são desbloqueados no perfil que está jogando.");
            return false;
        }
    }
}
