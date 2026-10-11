using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Monta o asset Assets/Resources/VerbeteDatabase.asset a partir de Assets/Data/verbetes.json
    // e das ilustrações de Assets/Art/Verbetes. Rodar de novo sempre que o JSON ou as imagens mudarem.
    // O JSON vem do glossário Glotrânsito: as definições não são editadas aqui nem em lugar nenhum.
    public static class VerbetesConstrutor
    {
        public const string CaminhoDoJson = "Assets/Data/verbetes.json";
        public const string PastaDasIlustracoes = "Assets/Art/Verbetes";
        public const string CaminhoDoAsset = "Assets/Resources/" + VerbeteDatabase.NomeNoResources + ".asset";

        [MenuItem("Cidade Viva/Verbetes/Atualizar banco de verbetes")]
        public static void Atualizar()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(CaminhoDoJson);
            if (json == null) { Debug.LogError("[Verbetes] Não achei " + CaminhoDoJson); return; }

            // Ilustrações viram sprites, reduzidas pra 1024 px (as originais passam de 2 MB cada).
            var sprites = new List<Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PastaDasIlustracoes }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single
                    || imp.maxTextureSize != 1024 || imp.mipmapEnabled)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;
                    imp.maxTextureSize = 1024;
                    imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
                if (sprite != null) sprites.Add(sprite);
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            ArtePaleta.GarantirPasta(Path.GetDirectoryName(CaminhoDoAsset).Replace('\\', '/'));
            var banco = AssetDatabase.LoadAssetAtPath<VerbeteDatabase>(CaminhoDoAsset);
            if (banco == null)
            {
                banco = ScriptableObject.CreateInstance<VerbeteDatabase>();
                AssetDatabase.CreateAsset(banco, CaminhoDoAsset);
            }
            banco.arquivoJson = json;
            banco.ilustracoes = sprites.ToArray();
            EditorUtility.SetDirty(banco);
            AssetDatabase.SaveAssets();
            banco.Recarregar();

            var semIlustracao = new List<string>();
            foreach (var v in banco.Todos)
            {
                if (v.ilustracao == null) semIlustracao.Add(v.palavra);
                if (v.ilustracao == null != v.semIlustracao)
                    Debug.LogWarning($"[Verbetes] '{v.palavra}': o JSON diz semIlustracao={v.semIlustracao}, mas a imagem {(v.ilustracao == null ? "não foi" : "foi")} encontrada.");
            }
            Debug.Log($"[Verbetes] {banco.Quantidade} verbetes, {sprites.Count} ilustrações. Sem ilustração: "
                      + (semIlustracao.Count == 0 ? "nenhum" : string.Join(", ", semIlustracao)) + ".");
        }
    }
}
