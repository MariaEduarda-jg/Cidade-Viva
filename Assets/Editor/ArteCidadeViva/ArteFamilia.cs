using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // A família do jogo em primitivas. Sem esqueleto: braços, pernas e cabeça são grupos
    // com o pivô na articulação (ombro, quadril, pescoço), pra dar pra animar girando por código.
    public static class ArteFamilia
    {
        enum Cabelo { Curto, Comprido, MariaChiquinha, Topete, Bone }

        class Ficha
        {
            public string nome;
            public float altura;
            public float cabeca;      // diâmetro da cabeça em fração da altura
            public float pernas;      // comprimento das pernas em fração da altura
            public string pele, cabelo, blusa, calca, sapato;
            public Cabelo penteado;
            public bool vestido, bermuda, mangaCurta, bigode, mochila;
        }

        public static void GerarPrefabs()
        {
            Montar(new Ficha { nome = "Familia_Pai", altura = 1.78f, cabeca = .185f, pernas = .47f, pele = "Pele_2", cabelo = "Cabelo_Preto",
                blusa = "Petroleo", calca = "Cinza_Escuro", sapato = "Grafite", penteado = Cabelo.Curto, bigode = true });
            Montar(new Ficha { nome = "Familia_Mae", altura = 1.66f, cabeca = .19f, pernas = .47f, pele = "Pele_3", cabelo = "Marrom_Escuro",
                blusa = "Mostarda", calca = "Mostarda", sapato = "Terracota", penteado = Cabelo.Comprido, vestido = true, mangaCurta = true });
            Montar(new Ficha { nome = "Familia_FilhoMaisVelho", altura = 1.58f, cabeca = .2f, pernas = .46f, pele = "Pele_2", cabelo = "Cabelo_Preto",
                blusa = "Verde", calca = "Petroleo_Escuro", sapato = "Branco", penteado = Cabelo.Topete, mangaCurta = true, mochila = true });
            Montar(new Ficha { nome = "Familia_Filha", altura = 1.28f, cabeca = .225f, pernas = .44f, pele = "Pele_3", cabelo = "Marrom_Escuro",
                blusa = "Petroleo_Claro", calca = "Petroleo_Claro", sapato = "Mostarda", penteado = Cabelo.MariaChiquinha, vestido = true, mangaCurta = true });
            Montar(new Ficha { nome = "Familia_FilhoMaisNovo", altura = 1.08f, cabeca = .245f, pernas = .42f, pele = "Pele_2", cabelo = "Cabelo_Preto",
                blusa = "Mostarda_Clara", calca = "Verde_Escuro", sapato = "Branco", penteado = Cabelo.Bone, bermuda = true, mangaCurta = true });
            AssetDatabase.SaveAssets();
        }

        static void Montar(Ficha f)
        {
            float H = f.altura;
            float hd = H * f.cabeca;
            float perna = H * f.pernas;
            float queixo = H - hd;
            float ombro = queixo - .025f * H;
            float larguraTronco = .27f * H, fundoTronco = .18f * H;

            var r = F.Grupo(f.nome).transform;

            // Tronco
            var corpo = F.Grupo("Corpo", r).transform;
            float alturaTronco = ombro - perna + .06f * H;
            F.Capsula(corpo, "Tronco", f.blusa, F.V(0, perna + alturaTronco / 2f - .03f * H, 0), F.V(larguraTronco, alturaTronco, fundoTronco));
            F.Cilindro(corpo, "Pescoco", f.pele, F.V(0, queixo + .005f * H, 0), F.V(.095f * H, .08f * H, .095f * H));
            if (f.vestido)
                F.M(corpo, "Saia", "Saia", f.blusa, F.V(0, perna - .05f * H, 0), F.V(.4f * H, .3f * H, .32f * H));
            else
                F.Cilindro(corpo, "Quadril", f.calca, F.V(0, perna + .01f * H, 0), F.V(larguraTronco * .97f, .09f * H, fundoTronco * .97f));
            if (f.mochila)
            {
                F.Cubo(corpo, "Mochila", "Laranja", F.V(0, ombro - .17f * H, -fundoTronco / 2f - .035f * H), F.V(.2f * H, .23f * H, .09f * H));
                F.Cubo(corpo, "Mochila_Bolso", "Mostarda", F.V(0, ombro - .21f * H, -fundoTronco / 2f - .085f * H), F.V(.14f * H, .1f * H, .02f * H));
            }

            // Pernas (pivô no quadril)
            foreach (int lado in new[] { -1, 1 })
            {
                var p = F.Grupo(lado < 0 ? "Perna_E" : "Perna_D", r, F.V(lado * .075f * H, perna, 0)).transform;
                float coberto = f.vestido ? 0f : (f.bermuda ? .42f : 1f);
                float fimDaPerna = perna - .04f * H;
                if (coberto > 0f)
                    F.Cilindro(p, "Calca", f.calca, F.V(0, -fimDaPerna * coberto / 2f, 0), F.V(.118f * H, fimDaPerna * coberto, .118f * H));
                if (coberto < 1f)
                    F.Cilindro(p, "Canela", f.pele, F.V(0, -fimDaPerna * (1f + coberto) / 2f, 0), F.V(.092f * H, fimDaPerna * (1f - coberto), .092f * H));
                F.Esfera(p, "Sapato", f.sapato, F.V(0, -perna + .038f * H, .03f * H), F.V(.125f * H, .076f * H, .21f * H));
            }

            // Braços (pivô no ombro, levemente abertos)
            float braco = .34f * H;
            float manga = f.mangaCurta ? .38f : 1f;
            foreach (int lado in new[] { -1, 1 })
            {
                var b = F.Grupo(lado < 0 ? "Braco_E" : "Braco_D", r,
                    F.V(lado * (larguraTronco / 2f + .012f * H), ombro - .045f * H, 0), F.V(0, 0, lado * 9f)).transform;
                F.Esfera(b, "Ombro", f.blusa, Vector3.zero, Vector3.one * .105f * H);
                F.Cilindro(b, "Manga", f.blusa, F.V(0, -braco * manga / 2f, 0), F.V(.095f * H, braco * manga, .095f * H));
                if (manga < 1f)
                    F.Cilindro(b, "Antebraco", f.pele, F.V(0, -braco * (1f + manga) / 2f, 0), F.V(.078f * H, braco * (1f - manga), .078f * H));
                F.Esfera(b, "Mao", f.pele, F.V(0, -braco - .02f * H, 0), Vector3.one * .095f * H);
            }

            // Cabeça (pivô no centro)
            var c = F.Grupo("Cabeca", r, F.V(0, H - hd / 2f, 0)).transform;
            F.Esfera(c, "Rosto", f.pele, Vector3.zero, F.V(hd * .96f, hd, hd * .96f));
            foreach (int lado in new[] { -1, 1 })
            {
                F.Esfera(c, "Olho", "Grafite", F.V(lado * .19f * hd, .03f * hd, .425f * hd), F.V(.105f * hd, .125f * hd, .06f * hd));
                F.Esfera(c, "Orelha", f.pele, F.V(lado * .47f * hd, -.02f * hd, 0), F.V(.1f * hd, .17f * hd, .13f * hd));
            }
            F.Esfera(c, "Nariz", f.pele, F.V(0, -.09f * hd, .47f * hd), Vector3.one * .1f * hd);
            if (f.bigode)
                F.Capsula(c, "Bigode", f.cabelo, F.V(0, -.18f * hd, .445f * hd), F.V(.07f * hd, .3f * hd, .07f * hd), F.V(0, 0, 90));
            else
                F.Esfera(c, "Boca", "Terracota", F.V(0, -.24f * hd, .405f * hd), F.V(.2f * hd, .05f * hd, .06f * hd));

            F.M(c, "Cabelo", "Semiesfera", f.cabelo, F.V(0, .07f * hd, -.02f * hd), F.V(hd * 1.06f, hd * 1.1f, hd * 1.06f), F.V(-24, 0, 0));
            switch (f.penteado)
            {
                case Cabelo.Comprido:
                    F.Esfera(c, "Cabelo_Costas", f.cabelo, F.V(0, -.3f * hd, -.27f * hd), F.V(hd * 1.02f, hd * 1.2f, hd * .62f));
                    F.Esfera(c, "Coque", f.cabelo, F.V(0, .5f * hd, -.3f * hd), Vector3.one * .36f * hd);
                    break;
                case Cabelo.MariaChiquinha:
                    foreach (int lado in new[] { -1, 1 })
                    {
                        F.Esfera(c, "Chiquinha", f.cabelo, F.V(lado * .6f * hd, .1f * hd, -.1f * hd), F.V(.34f * hd, .4f * hd, .34f * hd));
                        F.Esfera(c, "Elastico", "Mostarda", F.V(lado * .47f * hd, .14f * hd, -.1f * hd), Vector3.one * .15f * hd);
                    }
                    break;
                case Cabelo.Topete:
                    F.Esfera(c, "Topete", f.cabelo, F.V(0, .47f * hd, .2f * hd), F.V(.62f * hd, .28f * hd, .42f * hd), F.V(-15, 0, 0));
                    break;
                case Cabelo.Bone:
                    F.M(c, "Bone", "Semiesfera", "Vermelho", F.V(0, .13f * hd, 0), F.V(hd * 1.14f, hd * 1.12f, hd * 1.14f), F.V(-12, 0, 0));
                    F.Cilindro(c, "Bone_Aba", "Vermelho", F.V(0, .2f * hd, .55f * hd), F.V(.8f * hd, .04f * hd, .62f * hd), F.V(-4, 0, 0));
                    F.Esfera(c, "Bone_Botao", "Branco", F.V(0, .68f * hd, -.12f * hd), Vector3.one * .1f * hd);
                    break;
            }

            F.Salvar(r.gameObject, "Personagens");
        }
    }
}
