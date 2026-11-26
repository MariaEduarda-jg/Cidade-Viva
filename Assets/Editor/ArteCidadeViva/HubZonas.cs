using UnityEngine;
using M = CidadeViva.EditorArte.MapaDoHubConstrutor;

namespace CidadeViva.EditorArte
{
    // As duas zonas do Hub (2026-11-26). Tudo em coordenadas do mundo: +X = leste, +Z = norte.
    // Cada peça de chão e cada prédio também vai pro desenho do mapa (MapaDoHubConstrutor).
    //
    // Medidas principais (simplificadas: ver "Hub fiel a Brasília" no CLAUDE.md):
    //   Eixão (Eixo Rodoviário) em x = 0; Eixinho L em x = 20; ciclovia em x de 25 a 27,2; calçada até 29.
    //   Superquadra: x de 29 a 84 (a borda leste é a beira do lago), z de -31 a 26.
    //   Entrequadra: rua em z de -37 a -31 e o comércio local ao sul dela (lojas de frente pro norte).
    //   Eixo Monumental: centro em z = 100 (pistas em 82-90 e 110-118, gramado no meio); Rodoviária em x = 0;
    //   Torre de TV em x = -55; Museu (48) e Catedral (82) ao sul; ministérios de x = 104 a 168; Congresso
    //   em x = 198 e a Praça dos Três Poderes atrás (x de 212 a 262).
    public static partial class HubConstrutor
    {
        // Por onde o jogador anda. Os lados são o meio das paredes invisíveis (múltiplos de 0,5 m).
        static readonly Rect[] Caminhavel =
        {
            Rect.MinMaxRect(24f, -42f, 86f, 28f),     // superquadra, entrequadra, varanda do comércio e beira do lago
            Rect.MinMaxRect(24f, 28f, 31f, 82f),      // calçada e ciclovia do Eixinho até o Eixo Monumental
            Rect.MinMaxRect(-75f, 82f, 178f, 118f),   // Eixo Monumental (gramado e pistas), da Torre de TV ao Congresso
            Rect.MinMaxRect(170f, 118f, 262f, 128f),  // passeio que contorna o Congresso pelo norte
            Rect.MinMaxRect(212f, 72f, 262f, 128f),   // Praça dos Três Poderes
        };

        static void Zonas(Transform cidade)
        {
            Chao(cidade);
            Pinturas(cidade);
            Superquadra(cidade);
            ComercioLocal(cidade);
            Fundo(cidade);
            Monumentos(cidade);
            Rotulos();
        }

        // ------------------------------------------------------------------ chão

        static void Caixa(Transform pai, string nome, string mat, float x0, float z0, float x1, float z1, float y, float h, Color? mapa = null)
        {
            var go = F.Caixa(pai, nome, mat, (x0 + x1) / 2f, y, (z0 + z1) / 2f, Mathf.Abs(x1 - x0), h, Mathf.Abs(z1 - z0));
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mapa.HasValue) M.Ret(x0, z0, x1, z1, mapa.Value);
        }

        static void Via(Transform pai, string nome, float x0, float z0, float x1, float z1) =>
            Caixa(pai, nome, "Asfalto", x0, z0, x1, z1, -.03f, .04f, M.Via);

        static void Chao(Transform cidade)
        {
            var chao = F.Grupo("Chao", cidade).transform;
            F.Plano(chao, "Gramado_Externo", "Verde_Claro", F.V(80, -.03f, 30), 900f, 900f)
                .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Lago Paranoá: a leste da superquadra e da Praça dos Três Poderes; ao sul, entre a Asa Sul e o Lago Sul.
            var lago = F.Grupo("Lago_Paranoa", chao).transform;
            Caixa(lago, "Agua", "Agua", 86f, -45f, 420f, 44f, -.02f, .03f, M.Agua);
            Caixa(lago, "Agua", "Agua", 100f, -260f, 200f, -45f, -.02f, .03f, M.Agua);
            Caixa(lago, "Agua", "Agua", 200f, -60f, 420f, -45f, -.02f, .03f, M.Agua);
            Caixa(lago, "Agua", "Agua", 275f, 44f, 420f, 320f, -.02f, .03f, M.Agua);
            Caixa(lago, "Margem", "Areia", 84f, -45f, 86f, 30f, -.01f, .03f, M.Hex("E9DDB5"));
            Caixa(lago, "Margem", "Areia", 98f, -100f, 100f, -45f, -.01f, .03f, M.Hex("E9DDB5"));

            // Zona da superquadra.
            Caixa(chao, "Grama_Da_Quadra", "Verde", 29f, -31f, 84f, 26f, -.025f, .03f, M.Grama);
            Via(chao, "Eixao", -6f, -100f, 6f, 175f);
            Caixa(chao, "Eixao_Canteiro", "Verde", -.6f, -100f, .6f, 88f, -.02f, .06f);
            Caixa(chao, "Eixao_Canteiro", "Verde", -.6f, 112f, .6f, 175f, -.02f, .06f);
            Via(chao, "Eixinho_W", -24f, -100f, -16f, 82f);
            Via(chao, "Eixinho_L", 16f, -100f, 24f, 82f);
            Caixa(chao, "Meio_Fio", "Cinza_Claro", 24f, -100f, 25f, 82f, 0f, .1f);
            Caixa(chao, "Ciclovia", "Terracota", 25f, -100f, 27.2f, 82f, 0f, .06f, M.Ciclovia);
            Caixa(chao, "Calcada_Do_Eixinho", "Cinza_Claro", 27.2f, -100f, 29f, 82f, 0f, .08f, M.Calcada);
            Caixa(chao, "Calcada_Do_Ponto", "Cinza_Claro", 12.5f, 9f, 16f, 23f, 0f, .08f, M.Calcada);
            // Via de acesso da quadra (sai do Eixinho no Cruzamento Central) e o estacionamento.
            Via(chao, "Via_De_Acesso", 24f, -3f, 44f, 3f);
            Via(chao, "Estacionamento", 32f, 3f, 44f, 8.5f);
            // Entrequadra: a rua do comércio local, do Eixinho até a beira do lago.
            Via(chao, "Rua_Da_Entrequadra", 24f, -37f, 86f, -31f);
            Caixa(chao, "Varanda_Do_Comercio", "Cinza_Claro", 26f, -42.5f, 86f, -37f, 0f, .08f, M.Calcada);
            // Caminhos de pedestre dentro da quadra.
            Caixa(chao, "Caminho_Leste_Oeste", "Cinza_Claro", 29f, -4.5f, 84f, -2.5f, 0f, .05f, M.Calcada);
            Caixa(chao, "Caminho_Norte_Sul", "Cinza_Claro", 49f, -31f, 51f, 13f, 0f, .05f, M.Calcada);
            Caixa(chao, "Caminho_Do_Norte", "Cinza_Claro", 29f, 10f, 41f, 12f, 0f, .05f, M.Calcada);
            Tesourinha(chao);

            // Zona monumental.
            Caixa(chao, "Eixo_Gramado", "Verde", -75f, 90f, 178f, 110f, -.025f, .03f, M.Grama);
            Via(chao, "Eixo_Pista_Sul", -75f, 82f, 178f, 90f);
            Via(chao, "Eixo_Pista_Norte", -75f, 110f, 178f, 118f);
            Caixa(chao, "Praca_Dos_Tres_Poderes_Piso", "Branco_Concreto", 212f, 72f, 262f, 128f, 0f, .05f, M.Calcada);
            Caixa(chao, "Passeio_Do_Congresso_Piso", "Cinza_Claro", 170f, 118f, 212f, 128f, 0f, .05f, M.Calcada);
            Caixa(chao, "Esplanada_Piso", "Cinza_Claro", 92f, 76f, 170f, 82f, 0f, .05f, M.Calcada);
            Caixa(chao, "Esplanada_Piso", "Cinza_Claro", 92f, 118f, 168f, 124f, 0f, .05f, M.Calcada);
        }

        // Tesourinha: a alça em laço que liga o Eixinho à passagem por baixo do Eixão (simplificada: sem desnível).
        static void Tesourinha(Transform chao)
        {
            var t = F.Grupo("Tesourinha", chao).transform;
            var centro = new Vector3(10.5f, 0, -46f);
            const float raio = 4.2f;
            const int pedacos = 24;
            for (int i = 0; i < pedacos; i++)
            {
                float a = i * Mathf.PI * 2f / pedacos;
                var p = centro + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * raio;
                var go = F.Cubo(t, "Asfalto_Da_Alca", "Asfalto", F.V(p.x, -.005f, p.z), F.V(3f, .04f, 2f * Mathf.PI * raio / pedacos + .25f), F.V(0, -a * Mathf.Rad2Deg, 0));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Caixa(t, "Asfalto_Da_Ligacao", "Asfalto", 14.6f, -47.5f, 16f, -44.5f, -.005f, .04f);
            Caixa(t, "Asfalto_Da_Passagem", "Asfalto", 6f, -47.5f, 6.4f, -44.5f, -.005f, .04f);
            // Boca da passagem por baixo do Eixão (só a sombra escura, de cada lado).
            Caixa(t, "Passagem_Sob_O_Eixao", "Petroleo_Escuro", -6f, -47.5f, 6f, -44.5f, .012f, .01f, M.Hex("5B6670"));
            M.Anel(centro.x, centro.z, raio, 3f, M.Via);
            M.Ret(14.6f, -47.5f, 16f, -44.5f, M.Via);
        }

        static void Pinturas(Transform cidade)
        {
            var p = F.Grupo("Pintura_Das_Vias", cidade).transform;
            void Traco(float x, float z, float lx, float lz) => F.Cubo(p, "Traco", "Branco", F.V(x, .016f, z), F.V(lx, .012f, lz));
            for (float z = -98f; z <= 173f; z += 4f)
            {
                if (z > 80f && z < 120f) continue;
                Traco(-3f, z, .16f, 1.6f); Traco(3f, z, .16f, 1.6f);
                if (z < 80f) { Traco(-20f, z, .16f, 1.6f); if (Mathf.Abs(z) > 9f) Traco(20f, z, .16f, 1.6f); }
            }
            for (float x = -73f; x <= 176f; x += 4f)
            {
                if (x > -22f && x < 26f) continue;
                Traco(x, 86f, 1.6f, .16f); Traco(x, 114f, 1.6f, .16f);
            }
            for (float x = 30f; x <= 84f; x += 4f) Traco(x, -34f, 1.6f, .16f);
            // Tracejado da ciclovia.
            for (float z = -98f; z <= 80f; z += 1.75f)
                if (Mathf.Abs(z) > 3.5f && (z < -37.5f || z > -30.5f)) F.Cubo(p, "Ciclovia_Tracejado", "Branco", F.V(26.1f, .066f, z), F.V(.1f, .01f, .9f));
            // Faixas de pedestre do Cruzamento Central: atravessando o Eixinho dos dois lados da via de acesso.
            foreach (float z in new[] { -5f, 5f })
                for (int i = 0; i < 8; i++)
                    F.Cubo(p, "Listra", "Branco", F.V(16.9f + i * .9f, .018f, z), F.V(.45f, .014f, 2.2f));
            // Linhas de parada e vagas do estacionamento.
            Traco(22f, -6.6f, 3.6f, .3f); Traco(18f, 6.6f, 3.6f, .3f);
            for (float x = 32f; x <= 44f; x += 3f) Traco(x, 6f, .12f, 4.5f);
            foreach (var r in p.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ zona da superquadra

        static void Superquadra(Transform cidade)
        {
            var q = F.Grupo("Superquadra", cidade).transform;

            // Blocos residenciais sobre pilotis: dá pra passar por baixo (colisor só nos pilares, no saguão e na escada).
            Bloco(q, F.V(56f, 0, 20f), 180f);
            Bloco(q, F.V(62f, 0, -15f), 0f);

            // Escola Classe dentro da quadra, de frente pro meio dela.
            var escola = P(q, "Predios/Escola", F.V(38.4f, 0, -19f), 90);
            M.RetGirado(escola.transform.position + new Vector3(1f, 0, 0), new Vector2(14.8f, 9.5f), 90, M.Escola);
            P(q, "Props/Placa_Escola", F.V(44.6f, .05f, -9.4f), 90);
            P(q, "Props/Cerca_Pequena", F.V(44.8f, .05f, -27f), 90);

            Parquinho(q, F.V(62f, 0, 4f));

            // Cinturão verde em volta da quadra e algumas árvores dentro (só o tronco segura o jogador).
            string[] arvores = { "Arvore_Comum_1", "Arvore_Comum_2", "Arvore_Comum_3", "Arvore_Simples_Grande", "Arvore_Betula" };
            int k = 0;
            foreach (float x in new[] { 36f, 43f, 50f, 57f, 64f, 71f, 78f })
                P(q, "Natureza/" + arvores[k++ % arvores.Length], F.V(x, 0, 30.5f), x * 7f, .95f);
            foreach (float z in new[] { 23f, 17f, -16f, -22f, -27.5f })
                P(q, "Natureza/" + arvores[k++ % arvores.Length], F.V(82.3f, 0, z), z * 11f, .9f);
            foreach (var pos in new[] { F.V(37f, 0, 23.5f), F.V(79f, 0, -27f), F.V(45f, 0, 7.5f), F.V(70f, 0, -6.5f) })
                P(q, "Natureza/" + arvores[k++ % arvores.Length], pos, pos.x * 5f, .85f);
            foreach (var pos in new[] { F.V(57.5f, 0, -1f), F.V(41f, 0, -6f) })
                P(q, "Natureza/Arbusto_2", pos, 0, .8f);

            // Postes e bancos dos caminhos.
            foreach (float x in new[] { 38f, 58f, 76f })
                P(q, "Props/PosteKK", F.V(x, .05f, -5.2f), 0);
            P(q, "Props/PosteKK", F.V(51.6f, .05f, -18f), -90);
            P(q, "Props/Banco_De_Praca", F.V(54f, .05f, -5.4f), 0);
            P(q, "Props/Lixeira_De_Rua", F.V(47.6f, .05f, -5.3f), 0);

            // Carros no estacionamento.
            P(q, "Carros/Carro_SUV", F.V(33.5f, .02f, 5.8f), 0);
            P(q, "Carros/Carro_Compacto_Azul", F.V(39.5f, .02f, 5.8f), 180);
            P(q, "Carros/Van_Azul", F.V(42.5f, .02f, 5.8f), 0);

            // Ponto de ônibus do outro lado do Eixinho já faz parte da Rua Viva; aqui só um poste na calçada do ponto.
            P(q, "Props/PosteKK", F.V(13.2f, .08f, 21.5f), 90);

            // Trânsito no Eixão (parado, só pra cidade parecer viva).
            P(q, "Carros/Carro_Sedan", F.V(3f, .02f, -22f), 0);
            P(q, "Carros/Onibus", F.V(-3f, .02f, 34f), 180);
            P(q, "Carros/Carro_Taxi", F.V(3f, .02f, 58f), 0);
            P(q, "Carros/Carro_Hatch", F.V(-3f, .02f, -60f), 180);
            P(q, "Carros/Carro_Van", F.V(-18f, .02f, 10f), 180);
        }

        // Bloco da superquadra (30 x 12 m, 6 andares sobre pilotis). Colisores próprios: o corpo (só acima de 2,8 m,
        // pra câmera não ficar dentro), os pilares, o saguão e a caixa de escada. Assim a criança passa por baixo.
        static void Bloco(Transform pai, Vector3 pos, float giro)
        {
            var b = P(pai, "Predios/Brasilia_Bloco_Superquadra", pos, giro);
            var corpo = b.AddComponent<BoxCollider>();
            corpo.center = new Vector3(0, 9f, -.9f);
            corpo.size = new Vector3(30.6f, 12.4f, 12.4f);
            foreach (Transform t in b.GetComponentsInChildren<Transform>())
            {
                if (t.name == "Pilotis") t.gameObject.AddComponent<CapsuleCollider>();
                else if (t.name == "Saguao" || t.name == "Caixa_De_Escada") t.gameObject.AddComponent<BoxCollider>();
            }
            M.RetGirado(pos + Quaternion.Euler(0, giro, 0) * new Vector3(0, 0, -.9f), new Vector2(30.6f, 12.4f), giro, M.Bloco);
        }

        // Parquinho: caixa de areia, escorregador, balanço e gangorra (formas da paleta).
        static void Parquinho(Transform pai, Vector3 centro)
        {
            var g = F.Grupo("Parquinho", pai, centro).transform;
            F.Caixa(g, "Areia_Do_Parquinho", "Areia", 0, 0, 0, 9f, .12f, 7f);
            foreach (float x in new[] { -4.6f, 4.6f })
                F.Caixa(g, "Borda", "Terracota", x, 0, 0, .25f, .25f, 7.2f);
            foreach (float z in new[] { -3.6f, 3.6f })
                F.Caixa(g, "Borda", "Terracota", 0, 0, z, 9.4f, .25f, .25f);
            // Escorregador.
            foreach (float x in new[] { -3.3f, -2.3f })
                foreach (float z in new[] { 1.4f, 2.4f })
                    F.Cilindro(g, "Escorregador_Pe", "Petroleo", F.V(x, .9f, z), F.V(.12f, 1.8f, .12f));
            F.Caixa(g, "Escorregador_Topo", "Mostarda", -2.8f, 1.7f, 1.9f, 1.2f, .12f, 1.2f);
            F.Cubo(g, "Escorregador_Rampa", "Vermelho", F.V(-2.8f, .95f, -.15f), F.V(.9f, .08f, 3.2f), F.V(-30, 0, 0));
            // Balanço.
            foreach (float x in new[] { .6f, 3.8f })
            {
                F.Barra(g, "Balanco_Perna", "Petroleo_Claro", F.V(x, .1f, .9f), F.V(x, 2.3f, 2f), .14f);
                F.Barra(g, "Balanco_Perna", "Petroleo_Claro", F.V(x, .1f, 3.1f), F.V(x, 2.3f, 2f), .14f);
            }
            F.Barra(g, "Balanco_Travessa", "Petroleo_Claro", F.V(.6f, 2.3f, 2f), F.V(3.8f, 2.3f, 2f), .14f);
            foreach (float x in new[] { 1.5f, 2.9f })
            {
                F.Barra(g, "Balanco_Corrente", "Cinza_Escuro", F.V(x - .25f, 2.3f, 2f), F.V(x - .25f, .55f, 2f), .03f);
                F.Barra(g, "Balanco_Corrente", "Cinza_Escuro", F.V(x + .25f, 2.3f, 2f), F.V(x + .25f, .55f, 2f), .03f);
                F.Caixa(g, "Balanco_Assento", "Laranja", x, .5f, 2f, .65f, .06f, .3f);
            }
            // Gangorra.
            F.Caixa(g, "Gangorra_Base", "Petroleo", 1.8f, 0, -2f, .3f, .45f, .3f);
            F.Cubo(g, "Gangorra_Tabua", "Rosa", F.V(1.8f, .45f, -2f), F.V(3.4f, .08f, .35f), F.V(0, 0, 8));
            P(g, "Props/Banco_De_Praca", F.V(-5.6f, .02f, 2.6f), 90);
            M.Ret(centro.x - 4.6f, centro.z - 3.6f, centro.x + 4.6f, centro.z + 3.6f, M.Hex("EBD9A6"));
        }

        // Comércio local, ao sul da rua da entrequadra: uma fileira de lojas de frente pro norte. A oficina é a loja
        // maior (montada pela própria área); aqui ficam a padaria e as outras lojas.
        static void ComercioLocal(Transform cidade)
        {
            var c = F.Grupo("Comercio_Local", cidade).transform;
            void Loja(string prefab, float x, float z, float largura, float fundo)
            {
                P(c, "Predios/" + prefab, F.V(x, .08f, z), 0);
                M.Ret(x - largura / 2f, z - fundo / 2f, x + largura / 2f, z + fundo / 2f, M.Comercio);
            }
            Loja("Comercio_A", 29.8f, -45.8f, 5.4f, 5.6f);
            Loja("Padaria", 52.5f, -46.2f, 7.2f, 7.3f);
            Loja("Comercio_E", 62.5f, -45f, 9.8f, 6f);
            Loja("Predio_Comercial_Loja", 72.6f, -46.4f, 8.4f, 8.5f);
            Loja("Comercio_B", 80.8f, -44.9f, 5.8f, 5.6f);
            M.Ret(32.7f, -50.5f, 47.3f, -42.5f, M.Comercio); // a oficina
            foreach (float x in new[] { 49.6f, 67.6f, 77.4f })
                P(c, "Props/PosteKK", F.V(x, .08f, -37.6f), 180);
            P(c, "Props/Banco", F.V(57.5f, .08f, -41.6f), 0);
            P(c, "Props/Lixeira_De_Rua", F.V(66.8f, .08f, -41.6f), 0);
            P(c, "Personagens/Idoso", F.V(55.6f, .08f, -40.6f), 200);
            P(c, "Natureza/Cachorro_Pug", F.V(56.6f, .08f, -40.2f), 160);
            P(c, "Personagens/Mini_Mulher_1", F.V(70.5f, .08f, -40.4f), 170, .9f);
        }

        // Fundo: outras superquadras (as da Asa Sul do outro lado do Eixão e as do sul), o Lago Sul e a Ponte JK.
        static void Fundo(Transform cidade)
        {
            var f = F.Grupo("Fundo", cidade).transform;
            foreach (var (pos, giro) in new[] { (F.V(-42f, 0, 40f), 90f), (F.V(-42f, 0, 2f), 90f), (F.V(-42f, 0, -36f), 90f),
                                               (F.V(-72f, 0, 22f), 90f), (F.V(-72f, 0, -18f), 90f), (F.V(48f, 0, -72f), 0f), (F.V(82f, 0, -76f), 0f) })
            {
                P(f, "Predios/Brasilia_Bloco_Superquadra", pos, giro);
                M.RetGirado(pos, new Vector2(30.6f, 12.4f), giro, M.Bloco);
            }
            var sorteio = new System.Random(11);
            string[] arvores = { "Arvore_Comum_1", "Arvore_Comum_2", "Arvore_Simples_Grande", "Arvore_Betula" };
            for (int i = 0; i < 26; i++)
            {
                var pos = i < 14 ? F.V(-30f - (float)sorteio.NextDouble() * 55f, 0, -80f + (float)sorteio.NextDouble() * 150f)
                                 : F.V(35f + (float)sorteio.NextDouble() * 60f, 0, -55f - (float)sorteio.NextDouble() * 40f);
                if (Mathf.Abs(pos.x + 42f) < 8f || Mathf.Abs(pos.x + 72f) < 8f) pos.x -= 12f;
                P(f, "Natureza/" + arvores[sorteio.Next(arvores.Length)], pos, sorteio.Next(360), .9f);
            }

            // Ponte JK ao fundo, atravessando o lago da Asa Sul até o Lago Sul (a mesma das Obras, bem maior).
            var jk = P(f, "Obras/Ponte_JK", F.V(150f, 0, -82f), 90);
            jk.name = "Ponte_JK_Ao_Fundo";
            jk.transform.localScale = new Vector3(2.4f, 5f, 7f); // 100 m de comprimento, 16 m de largura

            M.Linha(M.Hex("F4F4F4"), 4f, new Vector2(100f, -82f), new Vector2(200f, -82f));
            foreach (float x in new[] { 125f, 150f, 175f }) M.Anel(x, -82f, 8f, 1.6f, M.Hex("FFFFFF"));
            // Lago Sul: a outra margem, com árvores.
            Caixa(f, "Margem_Do_Lago_Sul", "Areia", 200f, -62f, 330f, -60f, -.01f, .03f);
            for (int i = 0; i < 14; i++)
                P(f, "Natureza/" + arvores[i % arvores.Length], F.V(206f + i * 8.5f, 0, -68f - (i % 3) * 7f), i * 40, 1.2f);
        }

        // ------------------------------------------------------------------ zona monumental

        static void Monumentos(Transform cidade)
        {
            var m = F.Grupo("Monumentos", cidade).transform;

            Rodoviaria(m, F.V(0, 0, 100f));
            TorreDeTV(m, F.V(-55f, 0, 100f));

            // A leste da Rodoviária, do lado sul do Eixo: o Museu Nacional e depois a Catedral.
            var museu = P(m, "Predios/Brasilia_Museu_Cupula", F.V(48f, 0, 64f), 0);
            ColisorInteiro(museu);
            M.Circ(48f, 64f, 13.5f, M.Monumento);
            var catedral = P(m, "Predios/Brasilia_Catedral", F.V(82f, 0, 68f), 0);
            ColisorInteiro(catedral);
            M.Circ(82f, 68f, 8.5f, M.Monumento);

            // Esplanada dos Ministérios: duas fileiras iguais de blocos, dos dois lados do Eixo.
            var esplanada = F.Grupo("Esplanada_Dos_Ministerios", m).transform;
            foreach (float x in new[] { 100f, 115f, 130f, 145f, 160f })
                foreach (float z in new[] { 63f, 137f })
                    Ministerio(esplanada, F.V(x, 0, z));
            OtimizacaoWebGL.Combinar(esplanada, "Esplanada");

            // Congresso no fim da Esplanada, de frente pro oeste; a Praça dos Três Poderes atrás.
            var congresso = P(m, "Predios/Brasilia_Congresso", F.V(198f, 0, 100f), -90);
            ColisorInteiro(congresso);
            M.RetGirado(F.V(193.5f, 0, 100f), new Vector2(44f, 27f), -90, M.Monumento);
            M.Ret(194f, 96f, 202f, 104f, M.Hex("E1E1DA"));

            var planalto = P(m, "Predios/Brasilia_Palacio_Arcos", F.V(238f, 0, 145f), 180);
            ColisorInteiro(planalto);
            M.Ret(223f, 129f, 253f, 153f, M.Monumento);
            var stf = P(m, "Predios/Brasilia_Palacio_Arcos", F.V(238f, 0, 58f), 0, .7f);
            ColisorInteiro(stf);
            M.Ret(227.5f, 52.4f, 248.5f, 69.2f, M.Monumento);
            Mastro(m, F.V(232f, 0, 100f));

            // Placas pra reconhecer cada lugar (lidas de quem passeia pelo Eixo).
            PlacaDeLugar(m, F.V(48f, 0, 80.6f), 180f, "Museu Nacional", "Eixo Monumental");
            PlacaDeLugar(m, F.V(80f, 0, 80.6f), 180f, "Catedral", "Eixo Monumental");
            PlacaDeLugar(m, F.V(91f, 0, 100f), 90f, "Esplanada dos Ministérios", "Eixo Monumental");
            PlacaDeLugar(m, F.V(174f, 0, 92f), 90f, "Congresso Nacional", "Praça dos Três Poderes atrás");
            PlacaDeLugar(m, F.V(22.5f, 0, 103f), -90f, "Rodoviária", "Eixo Rodoviário x Eixo Monumental");
            PlacaDeLugar(m, F.V(-46f, 0, 104f), -90f, "Torre de TV", "Eixo Monumental");
            PlacaDeLugar(m, F.V(214f, 0, 108f), 90f, "Praça dos Três Poderes", "Planalto, Congresso e STF");

            // Pouco movimento no Eixo, pra não parecer abandonado.
            P(m, "Carros/Carro_SUV", F.V(62f, .02f, 84f), 90);
            P(m, "Carros/Carro_Hatch", F.V(128f, .02f, 112f), -90);
            P(m, "Carros/Onibus_Urbano", F.V(-30f, .02f, 112f), -90);
            P(m, "Personagens/Mini_Homem_6", F.V(140f, .05f, 96f), 200, .9f);
            P(m, "Personagens/Mini_Mulher_4", F.V(141f, .05f, 96.6f), 160, .9f);
            P(m, "Personagens/Idosa", F.V(236f, .05f, 92f), 30);
        }

        // Rodoviária: plataforma alta sobre pilares, por cima do Eixão (as pistas passam por baixo; a criança também).
        static void Rodoviaria(Transform pai, Vector3 centro)
        {
            var r = F.Grupo("Rodoviaria", pai, centro).transform;
            F.Caixa(r, "Plataforma", "Branco_Concreto", 0, 4.2f, 0, 40f, 1f, 22f);
            F.Caixa(r, "Plataforma_Borda", "Cinza", 0, 3.9f, 0, 40.4f, .3f, 22.4f);
            F.Caixa(r, "Pavilhao", "Vidro", 0, 5.2f, 0, 28f, 3f, 12f);
            F.Caixa(r, "Pavilhao_Laje", "Branco_Concreto", 0, 8.2f, 0, 30f, .4f, 14f);
            F.Caixa(r, "Faixa_Azul", "Petroleo", 0, 4.4f, -11.05f, 40f, .5f, .1f);
            foreach (float x in new[] { -16f, -9f, 9f, 16f })
                foreach (float z in new[] { -8f, 0f, 8f })
                    F.Cilindro(r, "Pilar", "Branco_Concreto", F.V(x, 2.1f, z), F.V(.9f, 4.2f, .9f));
            // O que fica no alto ganha colisor (a câmera não entra na plataforma); os pilares ganham o deles depois.
            foreach (var mr in r.GetComponentsInChildren<MeshRenderer>())
                if (mr.bounds.min.y > 3f) mr.gameObject.AddComponent<BoxCollider>();
            P(r, "Carros/Onibus_Urbano", F.V(12.5f, .02f, 3f), 0);
            P(r, "Carros/Onibus", F.V(-12.5f, .02f, -3f), 180);
            P(r, "Personagens/Mini_Homem_1", F.V(10f, .05f, -6f), 200, .9f);
            P(r, "Personagens/Pedestre_Mulher", F.V(-9.5f, .05f, 6.5f), 20);
            M.Ret(centro.x - 20f, centro.z - 11f, centro.x + 20f, centro.z + 11f, M.Monumento);
            M.Ret(centro.x - 14f, centro.z - 6f, centro.x + 14f, centro.z + 6f, M.Hex("BFE0EE"));
        }

        // Torre de TV: base em tripé, mastro alto, mirante com vidro e a antena vermelha e branca.
        static void TorreDeTV(Transform pai, Vector3 centro)
        {
            var t = F.Grupo("Torre_De_TV", pai, centro).transform;
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f + .5f;
                F.Barra(t, "Perna", "Branco_Concreto", F.V(Mathf.Cos(a) * 7f, 0, Mathf.Sin(a) * 7f), F.V(0, 14f, 0), 1.1f);
            }
            F.Caixa(t, "Plataforma", "Branco_Concreto", 0, 13.6f, 0, 6f, .8f, 6f);
            F.Caixa(t, "Mastro", "Cinza_Claro", 0, 14.4f, 0, 1.8f, 30f, 1.8f);
            F.Cilindro(t, "Mirante", "Vidro", F.V(0, 29f, 0), F.V(6f, 2.2f, 6f));
            F.Cilindro(t, "Mirante_Laje", "Branco_Concreto", F.V(0, 30.3f, 0), F.V(6.6f, .4f, 6.6f));
            F.Cilindro(t, "Mirante_Piso", "Branco_Concreto", F.V(0, 27.7f, 0), F.V(6.6f, .4f, 6.6f));
            for (int i = 0; i < 6; i++)
                F.Cilindro(t, "Antena", i % 2 == 0 ? "Vermelho" : "Branco", F.V(0, 45.4f + i * 2f, 0), F.V(.5f, 2f, .5f));
            M.Circ(centro.x, centro.z, 4f, M.Monumento);
            M.Circ(centro.x, centro.z, 1.6f, M.Hex("C0392B"));
        }

        // Um ministério: bloco de vidro verde de 10 x 26 m, com as pontas e as faixas brancas.
        static void Ministerio(Transform pai, Vector3 pos)
        {
            var g = F.Grupo("Ministerio", pai, pos).transform;
            F.Caixa(g, "Corpo", "Vidro", 0, 0, 0, 10f, 13f, 26f);
            foreach (float z in new[] { -13.2f, 13.2f })
                F.Caixa(g, "Empena", "Branco_Concreto", 0, 0, z, 10.6f, 13.4f, .6f);
            for (int i = 0; i < 4; i++)
                F.Caixa(g, "Faixa", "Branco_Concreto", 0, 1.2f + i * 3.2f, 0, 10.2f, .5f, 26.2f);
            F.Caixa(g, "Laje", "Branco_Concreto", 0, 13f, 0, 10.6f, .4f, 27f);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 6.7f, 0);
            col.size = new Vector3(10.6f, 13.4f, 27f);
            M.Ret(pos.x - 5.3f, pos.z - 13.5f, pos.x + 5.3f, pos.z + 13.5f, M.Monumento);
        }

        // Mastro da Bandeira, na Praça dos Três Poderes.
        static void Mastro(Transform pai, Vector3 pos)
        {
            var g = F.Grupo("Mastro_Da_Bandeira", pai, pos).transform;
            F.Cilindro(g, "Base", "Branco_Concreto", F.V(0, .4f, 0), F.V(3f, .8f, 3f));
            F.Cilindro(g, "Mastro", "Cinza_Claro", F.V(0, 14f, 0), F.V(.5f, 28f, .5f));
            F.Cubo(g, "Bandeira", "Verde", F.V(3.3f, 25.5f, 0), F.V(6f, 4f, .08f));
            F.Cubo(g, "Bandeira_Losango", "Mostarda", F.V(3.3f, 25.5f, -.03f), F.V(2.6f, 2.6f, .08f), F.V(0, 0, 45));
            F.Cilindro(g, "Bandeira_Circulo", "Petroleo", F.V(3.3f, 25.5f, -.08f), F.V(1.7f, .06f, 1.7f), F.V(90, 0, 0));
            M.Circ(pos.x, pos.z, 1.8f, M.Hex("5E8F3E"));
        }

        // ------------------------------------------------------------------ nomes no mapa

        static void Rotulos()
        {
            // O Eixão segue pras pontas das asas, curvando pro oeste (só no desenho do mapa).
            M.Linha(M.Via, 12f, new Vector2(0f, 150f), new Vector2(-4f, 160f), new Vector2(-12f, 172f));
            M.Linha(M.Via, 12f, new Vector2(0f, -78f), new Vector2(-5f, -90f), new Vector2(-12f, -102f));
            M.Escrever("Lago Paranoá", 165f, 15f, 36, true);
            M.Escrever("Ponte JK", 150f, -68f, 28);
            M.Escrever("Lago Sul", 248f, -82f, 28);
            M.Escrever("Eixo Monumental", -30f, 96f, 26);
            M.Escrever("Esplanada", 130f, 100f, 28);
            M.Escrever("Congresso", 192f, 72f, 26);
            M.Escrever("Praça dos\nTrês Poderes", 237f, 112f, 26);
            M.Escrever("Rodoviária", 0f, 125f, 26);
            M.Escrever("Torre de TV", -55f, 117f, 26);
            M.Escrever("Catedral", 92f, 53f, 26);
            M.Escrever("Museu", 46f, 45f, 26);
            M.Escrever("Eixão", -14f, -74f, 26);
            M.Escrever("Asa Sul", -56f, -50f, 36);
            M.Escrever("Asa Norte", -56f, 150f, 36);
            M.Escrever("Superquadra", 56f, 35f, 28);
            M.Escrever("Comércio local", 60f, -58f, 26);
            M.Escrever("Tesourinha", -2f, -58f, 26);
        }
    }
}
