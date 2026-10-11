using System.IO;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Paleta única do jogo. Todo material de arte sai daqui (URP/Lit fosco, sem brilho),
    // pra modelos de pacotes diferentes parecerem da mesma família.
    public static class ArtePaleta
    {
        public const string Pasta = "Assets/Materials/Paleta";
        public const string PastaTexturas = "Assets/Art/Texturas_Paleta";

        public static readonly (string nome, string hex)[] Cores =
        {
            ("Branco", "F4F1EA"), ("Branco_Concreto", "FBFAF6"), ("Areia", "E9D8B8"),
            ("Cinza_Claro", "C9D1D9"), ("Cinza", "8E9BAA"), ("Cinza_Escuro", "5B6878"),
            ("Asfalto", "66717F"), ("Asfalto_Escuro", "525C69"), ("Grafite", "2F3A45"),
            ("Verde_Claro", "A9D08F"), ("Verde", "7DB87A"), ("Verde_Escuro", "4F8F6B"), ("Verde_Musgo", "6C9A58"),
            ("Petroleo", "1F6F7A"), ("Petroleo_Claro", "4FA3A8"), ("Petroleo_Escuro", "15525C"), ("Vidro", "A9D6DD"),
            ("Mostarda", "E0A82E"), ("Mostarda_Clara", "F0CB6E"),
            ("Laranja", "E58A3A"), ("Vermelho", "D2553F"), ("Terracota", "C97355"), ("Rosa", "E6A3A8"),
            ("Marrom", "8A5A3C"), ("Marrom_Claro", "BC8E62"), ("Marrom_Escuro", "5E3D2A"),
            ("Pele_1", "F2CFAE"), ("Pele_2", "D9A377"), ("Pele_3", "A9714A"), ("Cabelo_Preto", "2B2523"),
        };

        public static Color Cor(string nome)
        {
            foreach (var (n, hex) in Cores)
                if (n == nome && ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
            throw new System.ArgumentException("Cor fora da paleta: " + nome);
        }

        public static Material Mat(string nome)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{Pasta}/Pal_{nome}.mat");
            if (m == null) throw new System.ArgumentException("Material da paleta não existe: " + nome);
            return m;
        }

        public static void GerarMateriais()
        {
            GarantirPasta(Pasta);
            foreach (var (nome, _) in Cores) CriarOuAtualizar(nome, Cor(nome));

            Emissivo("Luz_Vermelha", "E0483A");
            Emissivo("Luz_Amarela", "F2C230");
            Emissivo("Luz_Verde", "4CBB6A");
            CriarOuAtualizar("Luz_Apagada", Hex("3A444E"));

            var agua = CriarOuAtualizar("Agua", new Color(0.25f, 0.62f, 0.71f, 0.6f));
            agua.SetFloat("_Surface", 1f);
            agua.SetFloat("_Blend", 0f);
            agua.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            agua.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            agua.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            agua.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            agua.SetFloat("_ZWrite", 0f);
            agua.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            agua.SetOverrideTag("RenderType", "Transparent");
            agua.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            agua.SetShaderPassEnabled("ShadowCaster", false);
            agua.SetShaderPassEnabled("DepthOnly", false);
            EditorUtility.SetDirty(agua);
            AssetDatabase.SaveAssets();
        }

        // Material da paleta com textura (placas, rótulos, atlas de cor dos kits).
        public static Material MatTextura(string nome, string caminhoTextura, bool recorte, bool emissivo = false)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(caminhoTextura);
            if (tex == null) throw new FileNotFoundException("Textura não encontrada", caminhoTextura);
            var m = CriarOuAtualizar(nome, Color.white);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            if (recorte)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            if (emissivo)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white * 0.7f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void ConfigurarTextura(string caminho, bool alfa, bool mipmaps, int tamanhoMax, bool semCompressao)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            if (imp == null) throw new FileNotFoundException("Textura não encontrada", caminho);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaIsTransparency = alfa;
            imp.mipmapEnabled = mipmaps;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = tamanhoMax;
            imp.textureCompression = semCompressao ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }

        public static void GarantirPasta(string pasta)
        {
            if (AssetDatabase.IsValidFolder(pasta)) return;
            var pai = Path.GetDirectoryName(pasta).Replace('\\', '/');
            GarantirPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(pasta));
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static void Emissivo(string nome, string hex)
        {
            var cor = Hex(hex);
            var m = CriarOuAtualizar(nome, cor);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", cor * 0.6f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
        }

        static Material CriarOuAtualizar(string nome, Color cor)
        {
            string caminho = $"{Pasta}/Pal_{nome}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, caminho);
            }
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_Metallic", 0f);
            // Sem reflexo especular nem de ambiente: é isso que tira o "brilho de plástico".
            m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
