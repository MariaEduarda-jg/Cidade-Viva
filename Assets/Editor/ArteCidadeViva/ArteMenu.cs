using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Menu "Cidade Viva > Arte". Tudo pode ser refeito a qualquer momento: os passos
    // atualizam os assets existentes em vez de duplicar.
    public static class ArteMenu
    {
        [MenuItem("Cidade Viva/Arte/Refazer tudo (materiais, prefabs e galeria)")]
        public static void RefazerTudo()
        {
            RefazerPrefabs();
            ArteGaleria.GerarCena();
            Debug.Log("[Arte] Pronto: paleta, prefabs e Galeria_Assets atualizados.");
        }

        [MenuItem("Cidade Viva/Arte/Refazer só materiais e prefabs")]
        public static void RefazerPrefabs()
        {
            ArtePaleta.GerarMateriais();
            ArteMalhas.GerarMalhas();
            ArteImportador.PrepararTexturas();
            ArteImportador.RemapearMateriais();
            ArteImportador.GerarPrefabs();
            ArteFormas.GerarPrefabs();
            ArteCidade.GerarPrefabs();
            ArteFamilia.GerarPrefabs();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Cidade Viva/Arte/Refazer só a cena Galeria_Assets")]
        public static void RefazerGaleria()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ArteGaleria.GerarCena();
        }
    }
}
