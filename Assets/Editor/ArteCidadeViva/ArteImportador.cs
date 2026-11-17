using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Liga cada FBX de Assets/Art à paleta (remap de materiais no importador) e gera o prefab
    // em Assets/Prefabs/Art/<Categoria>, já na escala do jogo (1 unidade = 1 metro).
    public static class ArteImportador
    {
        public const string PastaPrefabs = "Assets/Prefabs/Art";

        class Item
        {
            public string categoria, kit, arquivo, prefab, trocas;
            public string ext = "fbx", partes = "";
            public float escala, giroY, tamanho;
            public bool animado, gltf, autor, rente;
            public string CaminhoModelo => $"Assets/Art/{categoria}/Modelos/{kit}/{arquivo}.{ext}";
            public string CaminhoPrefab => $"{PastaPrefabs}/{categoria}/{prefab}.prefab";

            // Tem esqueleto e clipes: o prefab sai com Animator e um controlador pronto.
            public Item Anim() { animado = true; return this; }
            // Gira o modelo pra frente dele ficar em +Z.
            public Item Gira(float graus) { giroY = graus; return this; }
            // Modelo glTF (Sketchfab): mantém materiais e texturas do autor.
            public Item Gltf() { gltf = true; ext = "gltf"; return this; }
            // glTF binário (.glb), com as texturas embutidas.
            public Item Glb() { gltf = true; ext = "glb"; return this; }
            // Modelo .obj (o .mtl e as texturas ficam na mesma pasta).
            public Item Obj() { ext = "obj"; return this; }
            // Mantém as cores do autor; só os materiais citados em "trocas" vão pra paleta.
            public Item Autor() { autor = true; return this; }
            // O arquivo traz várias peças juntas: o prefab fica só com os filhos diretos citados (separados por ;).
            public Item Partes(string nomes) { partes = nomes; return this; }
            // Em vez de escala fixa, ajusta pra maior dimensão ficar com este tamanho em metros
            // (pacotes avulsos vêm cada um numa unidade diferente).
            public Item Mede(float metros) { tamanho = metros; return this; }
            // Peça chata que fica deitada no chão (papéis, manchas): sobe um pouquinho e não faz sombra.
            public Item Rente() { rente = true; return this; }
        }

        public const string PastaAnimacoes = "Assets/Art/Animacoes";

        // Escalas: ladrilho de rua = 8 m; casa de 2 andares ~6 m; carro ~3,5 m; adulto 1,7 m.
        const float EscalaRua = 8f, EscalaCasa = 5.5f, EscalaCarro = 1.3f, EscalaLixo = 0.8f, EscalaArvore = 1.8f;

        static Item I(string categoria, string kit, string arquivo, string prefab, float escala, string trocas = "")
        {
            return new Item { categoria = categoria, kit = kit, arquivo = arquivo, prefab = prefab, escala = escala, trocas = trocas };
        }

        static readonly Item[] Itens =
        {
            // ---------- Ruas ----------
            I("Ruas", "KenneyRoads", "road_straight", "Rua_Reta", EscalaRua),
            I("Ruas", "KenneyRoads", "road_crossing", "Rua_FaixaDePedestre", EscalaRua),
            I("Ruas", "KenneyRoads", "road_crossroad", "Rua_Cruzamento", EscalaRua),
            I("Ruas", "KenneyRoads", "road_crossroadPath", "Rua_Cruzamento_ComFaixas", EscalaRua),
            I("Ruas", "KenneyRoads", "road_intersection", "Rua_Entroncamento", EscalaRua),
            I("Ruas", "KenneyRoads", "road_intersectionPath", "Rua_Entroncamento_ComFaixas", EscalaRua),
            I("Ruas", "KenneyRoads", "road_bend", "Rua_Curva", EscalaRua),
            I("Ruas", "KenneyRoads", "road_bendSidewalk", "Rua_Curva_Calcada", EscalaRua),
            I("Ruas", "KenneyRoads", "road_curve", "Rua_CurvaLonga", EscalaRua),
            I("Ruas", "KenneyRoads", "road_end", "Rua_Fim", EscalaRua),
            I("Ruas", "KenneyRoads", "road_endRound", "Rua_Fim_Redondo", EscalaRua),
            I("Ruas", "KenneyRoads", "road_roundabout", "Rua_Rotatoria", EscalaRua),
            I("Ruas", "KenneyRoads", "road_square", "Rua_Asfalto", EscalaRua),
            I("Ruas", "KenneyRoads", "road_side", "Rua_Acostamento", EscalaRua),
            I("Ruas", "KenneyRoads", "tile_low", "Calcada_Piso", EscalaRua),
            I("Ruas", "QuaterniusDowntown", "Sidewalk_Straight_3m", "Calcada_Reta", 1f),
            I("Ruas", "QuaterniusDowntown", "Sidewalk_Corner_Round_3m", "Calcada_Canto", 1f),
            I("Ruas", "QuaterniusDowntown", "Sidewalk_NoCurb_3m", "Calcada_SemMeioFio", 1f),
            I("Ruas", "QuaterniusDowntown", "Decal_Crosswalk", "Pintura_FaixaDePedestre", 1f),
            I("Ruas", "QuaterniusDowntown", "Decal_Bikelane", "Pintura_Ciclovia", 1f),

            // ---------- Props ----------
            I("Props", "KenneyRoads", "construction_barrier", "Barreira_Obra", EscalaRua, "pavement=Mostarda"),
            I("Props", "KenneyRoads", "construction_light", "Sinalizador_Obra", EscalaRua),
            I("Props", "KenneyRoads", "construction_pylon", "Cone_Pequeno", EscalaRua, "pavement=Branco"),
            I("Props", "KenneyRoads", "light_curved", "Poste_Curvo", EscalaRua, "pavement=Cinza_Escuro;_defaultMat=Cinza_Escuro"),
            I("Props", "KenneyRoads", "light_curvedDouble", "Poste_Duplo", EscalaRua, "pavement=Cinza_Escuro;_defaultMat=Cinza_Escuro"),
            I("Props", "KenneyRoads", "light_square", "Poste_Reto", EscalaRua, "pavement=Cinza_Escuro"),
            I("Props", "KenneyCarKit", "cone", "Cone", 1.2f),
            I("Props", "KenneyCarKit", "cone-flat", "Cone_Caido", 1.2f),
            I("Props", "KenneyCarKit", "box", "Caixa_Papelao", 1f),
            I("Props", "KenneyFoodKit", "barrel", "Barril", 1.3f),
            I("Props", "QuaterniusDowntown", "Prop_Bollard", "Pilarete", 1f, "MI_Trim_MetalConcrete=Cinza_Escuro"),
            I("Props", "QuaterniusDowntown", "Prop_Planter_Single", "Canteiro", 1f),
            I("Props", "QuaterniusDowntown", "Sidewalk_Planter", "Canteiro_Calcada", 1f),
            I("Props", "QuaterniusDowntown", "Prop_ManholeCover", "Tampa_Bueiro", 1f),
            I("Props", "QuaterniusDowntown", "Prop_Drain", "Ralo", 1f),
            I("Props", "KenneySuburban", "fence_small", "Cerca_Pequena", EscalaCasa),
            I("Props", "KenneySuburban", "fence_medium", "Cerca_Media", EscalaCasa),
            I("Props", "KenneySuburban", "fence_large", "Cerca_Grande", EscalaCasa),

            // ---------- Carros ----------
            I("Carros", "KenneyCarKit", "sedan", "Carro_Sedan", EscalaCarro),
            I("Carros", "KenneyCarKit", "sedan-sports", "Carro_Esportivo", EscalaCarro),
            I("Carros", "KenneyCarKit", "hatchback-sports", "Carro_Hatch", EscalaCarro),
            I("Carros", "KenneyCarKit", "suv", "Carro_SUV", EscalaCarro),
            I("Carros", "KenneyCarKit", "suv-luxury", "Carro_SUV_Luxo", EscalaCarro),
            I("Carros", "KenneyCarKit", "taxi", "Carro_Taxi", EscalaCarro),
            I("Carros", "KenneyCarKit", "van", "Carro_Van", EscalaCarro),
            I("Carros", "KenneyCarKit", "truck", "Carro_Picape", EscalaCarro),
            I("Carros", "KenneyCarKit", "delivery", "Carro_Entregas", EscalaCarro),
            I("Carros", "KenneyCarKit", "police", "Carro_Policia", EscalaCarro),
            I("Carros", "KenneyCarKit", "ambulance", "Ambulancia", EscalaCarro),
            I("Carros", "KenneyCarKit", "firetruck", "Caminhao_Bombeiros", EscalaCarro),
            I("Carros", "KenneyCarKit", "garbage-truck", "Caminhao_Lixo", EscalaCarro),

            // ---------- Prédios (casas) ----------
            I("Predios", "KenneySuburban", "house_type01", "Casa_01", EscalaCasa, "_defaultMat=Mostarda_Clara;roof=Petroleo"),
            I("Predios", "KenneySuburban", "house_type03", "Casa_02", EscalaCasa, "_defaultMat=Branco;roof=Terracota;border=Cinza_Claro"),
            I("Predios", "KenneySuburban", "house_type06", "Casa_03", EscalaCasa, "_defaultMat=Verde_Claro;roof=Cinza_Escuro"),
            I("Predios", "KenneySuburban", "house_type09", "Casa_04", EscalaCasa, "_defaultMat=Petroleo_Claro;roof=Mostarda"),
            I("Predios", "KenneySuburban", "house_type12", "Casa_05", EscalaCasa, "_defaultMat=Areia;roof=Verde_Escuro"),
            I("Predios", "KenneySuburban", "house_type15", "Casa_06", EscalaCasa, "_defaultMat=Terracota;roof=Cinza_Escuro"),
            I("Predios", "KenneySuburban", "house_type18", "Casa_07", EscalaCasa, "_defaultMat=Branco;roof=Petroleo;border=Cinza_Claro"),
            I("Predios", "KenneySuburban", "house_type21", "Casa_08", EscalaCasa, "_defaultMat=Mostarda_Clara;roof=Terracota"),

            // ---------- Natureza ----------
            I("Natureza", "QuaterniusNature", "CommonTree_1", "Arvore_Comum_1", EscalaArvore),
            I("Natureza", "QuaterniusNature", "CommonTree_2", "Arvore_Comum_2", EscalaArvore, "Green=Verde_Claro"),
            I("Natureza", "QuaterniusNature", "CommonTree_3", "Arvore_Comum_3", EscalaArvore, "Green=Verde_Musgo"),
            I("Natureza", "QuaterniusNature", "BirchTree_1", "Arvore_Betula", EscalaArvore, "Green=Verde_Claro;DarkGreen=Verde"),
            I("Natureza", "QuaterniusNature", "PineTree_1", "Arvore_Pinheiro", EscalaArvore, "Green=Verde_Escuro"),
            I("Natureza", "QuaterniusNature", "Willow_1", "Arvore_Salgueiro", EscalaArvore, "DarkGreen=Verde_Musgo"),
            I("Natureza", "QuaterniusNature", "PalmTree_1", "Palmeira_1", EscalaArvore),
            I("Natureza", "QuaterniusNature", "PalmTree_2", "Palmeira_2", EscalaArvore),
            I("Natureza", "QuaterniusNature", "PalmTree_3", "Palmeira_3", EscalaArvore),
            I("Natureza", "KenneySuburban", "tree_large", "Arvore_Simples_Grande", EscalaCasa, "roof=Verde;wood=Marrom"),
            I("Natureza", "KenneySuburban", "tree_small", "Arvore_Simples_Pequena", EscalaCasa, "roof=Verde_Escuro;wood=Marrom"),
            I("Natureza", "QuaterniusNature", "Bush_1", "Arbusto_1", 0.8f),
            I("Natureza", "QuaterniusNature", "Bush_2", "Arbusto_2", 0.8f, "Green=Verde_Claro"),
            I("Natureza", "QuaterniusNature", "BushBerries_1", "Arbusto_Frutinhas", 0.8f, "Green=Verde_Escuro"),
            I("Natureza", "QuaterniusNature", "Flowers", "Flores", 0.6f),
            I("Natureza", "QuaterniusNature", "Grass", "Grama_Alta", 0.5f, "Green=Verde_Claro"),
            I("Natureza", "QuaterniusNature", "Grass_2", "Grama_Alta_2", 0.5f, "Green=Verde_Musgo"),
            I("Natureza", "QuaterniusNature", "Grass_Short", "Grama_Baixa", 0.6f),
            I("Natureza", "QuaterniusNature", "Plant_1", "Planta_1", 0.8f),
            I("Natureza", "QuaterniusNature", "Plant_3", "Planta_2", 0.8f),
            I("Natureza", "QuaterniusNature", "Rock_1", "Pedra_1", 1f),
            I("Natureza", "QuaterniusNature", "Rock_2", "Pedra_2", 1f, "Rock=Cinza_Claro"),
            I("Natureza", "QuaterniusNature", "Rock_3", "Pedra_3", 1f),
            I("Natureza", "QuaterniusNature", "Rock_Moss_1", "Pedra_Musgo", 1f, "Green=Verde_Musgo"),
            I("Natureza", "QuaterniusNature", "Lilypad", "Vitoria_Regia", 0.6f),
            I("Natureza", "QuaterniusNature", "TreeStump", "Toco", 1f),
            I("Natureza", "QuaterniusNature", "WoodLog", "Tronco_Caido", 1f),

            // ---------- Lixo e reciclagem ----------
            I("Lixo_Reciclagem", "KenneyFoodKit", "soda-bottle", "Garrafa_Plastica", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "bottle-ketchup", "Frasco_Plastico", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "styrofoam", "Marmita_Isopor", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "candy-bar-wrapper", "Embalagem_Doce", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "chocolate-wrapper", "Embalagem_Chocolate", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "soda-can", "Lata_Refrigerante", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "soda-can-crushed", "Lata_Amassada", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "can", "Lata_Conserva", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "can-open", "Lata_Aberta", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "carton", "Caixa_De_Leite", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "carton-small", "Caixa_De_Suco", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "bag", "Saco_De_Papel", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "pizza-box", "Caixa_De_Pizza", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "cup-coffee", "Copo_De_Cafe", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "fries-empty", "Embalagem_Batata", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "glass", "Copo_De_Vidro", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "wine-white", "Garrafa_De_Vidro", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "honey", "Pote_De_Vidro_1", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "peanut-butter", "Pote_De_Vidro_2", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "plate-broken", "Prato_Quebrado", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "banana", "Banana", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "apple-half", "Maca_Mordida", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "fish-bones", "Espinha_De_Peixe", EscalaLixo),
            I("Lixo_Reciclagem", "KenneyFoodKit", "egg-half", "Casca_De_Ovo", EscalaLixo),

            // ================= Segunda leva de pacotes =================

            // ---------- Personagens animados (Quaternius): a família e figurantes ----------
            I("Personagens", "QuaterniusCharacters", "Casual_Male", "Familia_Pai_Animado", .52f, "Shirt=Petroleo;Pants=Cinza_Escuro;Hair=Cabelo_Preto;Skin=Pele_2").Anim(),
            I("Personagens", "QuaterniusCharacters", "Casual_Female", "Familia_Mae_Animado", .51f, "Shirt=Mostarda;Pants=Petroleo_Escuro;Hair=Marrom_Escuro;Skin=Pele_3").Anim(),
            I("Personagens", "QuaterniusCharacters", "Casual2_Male", "Familia_FilhoMaisVelho_Animado", .485f, "Shirt=Verde;Pants=Petroleo_Escuro;Hair=Cabelo_Preto;Skin=Pele_2").Anim(),
            I("Personagens", "QuaterniusCharacters", "Casual2_Female", "Familia_Filha_Animado", .39f, "Shirt=Petroleo_Claro;Pants=Mostarda;Hair=Marrom_Escuro;Skin=Pele_3").Anim(),
            I("Personagens", "QuaterniusCharacters", "Casual3_Male", "Familia_FilhoMaisNovo_Animado", .33f, "Shirt=Mostarda_Clara;Pants=Verde_Escuro;Hair=Cabelo_Preto;Skin=Pele_2").Anim(),
            I("Personagens", "QuaterniusCharacters", "Casual3_Female", "Pedestre_Mulher", .51f, "Shirt=Rosa;Pants=Cinza_Escuro;Hair=Marrom;Skin=Pele_1").Anim(),
            I("Personagens", "QuaterniusCharacters", "Worker_Male", "Trabalhador", .52f, "Skin=Pele_3").Anim(),
            I("Personagens", "QuaterniusCharacters", "Worker_Female", "Trabalhadora", .51f, "Skin=Pele_1;Hair=Cabelo_Preto").Anim(),
            I("Personagens", "QuaterniusCharacters", "Doctor_Male_Young", "Medico", .52f, "Skin=Pele_1;Hair=Marrom").Anim(),
            I("Personagens", "QuaterniusCharacters", "Doctor_Female_Young", "Medica", .51f, "Skin=Pele_3;Hair=Cabelo_Preto").Anim(),
            I("Personagens", "QuaterniusCharacters", "OldClassy_Male", "Idoso", .46f, "Skin=Pele_2").Anim(),
            I("Personagens", "QuaterniusCharacters", "OldClassy_Female", "Idosa", .45f, "Skin=Pele_1").Anim(),

            // ---------- Mini personagens animados (Kenney): figurantes ----------
            I("Personagens", "KenneyMiniCharacters", "character-female-a", "Mini_Mulher_1", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-female-b", "Mini_Mulher_2", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-female-c", "Mini_Mulher_3", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-female-d", "Mini_Mulher_4", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-female-e", "Mini_Mulher_5", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-female-f", "Mini_Mulher_6", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-a", "Mini_Homem_1", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-b", "Mini_Homem_2", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-c", "Mini_Homem_3", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-d", "Mini_Homem_4", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-e", "Mini_Homem_5", 2.2f).Anim(),
            I("Personagens", "KenneyMiniCharacters", "character-male-f", "Mini_Homem_6", 2.2f).Anim(),
            I("Props", "KenneyMiniCharacters", "wheelchair", "Cadeira_De_Rodas", 2.2f),
            I("Props", "KenneyMiniCharacters", "aid-cane-blind", "Bengala_Branca", 2.2f),
            I("Props", "KenneyMiniCharacters", "aid-crutch", "Muleta", 2.2f),

            // ---------- Cachorros animados (Quaternius) ----------
            I("Natureza", "QuaterniusAnimals", "Husky", "Cachorro_Husky", .2f,
                "Material=Cinza_Escuro;Material.001=Cinza_Claro;Material.006=Grafite;Material.003=Branco;Material.002=Grafite").Anim(),
            I("Natureza", "QuaterniusAnimals", "ShibaInu", "Cachorro_Caramelo", .17f,
                "Main=Laranja;Main_Light=Areia;Black=Grafite;Eyes_White=Branco;Eyes_Pupil=Marrom_Escuro;Eyes_Black=Grafite").Anim(),
            I("Natureza", "QuaterniusFarmAnimals", "Pug", "Cachorro_Pug", .12f, "Beige=Marrom_Claro;Brown=Marrom_Escuro").Anim(),

            // ---------- Cidade KayKit (ladrilho de 2 -> 8 m) ----------
            I("Predios", "KayKitCity", "building_A", "Predio_KayKit_A", 4f),
            I("Predios", "KayKitCity", "building_B", "Predio_KayKit_B", 4f),
            I("Predios", "KayKitCity", "building_C", "Predio_KayKit_C", 4f),
            I("Predios", "KayKitCity", "building_D", "Predio_KayKit_D", 4f),
            I("Predios", "KayKitCity", "building_E", "Predio_KayKit_E", 4f),
            I("Predios", "KayKitCity", "building_F", "Predio_KayKit_F", 4f),
            I("Predios", "KayKitCity", "building_G", "Predio_KayKit_G", 4f),
            I("Predios", "KayKitCity", "building_H", "Predio_KayKit_H", 4f),
            I("Predios", "KayKitCity", "watertower", "Caixa_Dagua", 4f),
            I("Ruas", "KayKitCity", "road_straight", "RuaKK_Reta", 4f),
            I("Ruas", "KayKitCity", "road_straight_crossing", "RuaKK_FaixaDePedestre", 4f),
            I("Ruas", "KayKitCity", "road_corner", "RuaKK_Esquina", 4f),
            I("Ruas", "KayKitCity", "road_corner_curved", "RuaKK_Curva", 4f),
            I("Ruas", "KayKitCity", "road_junction", "RuaKK_Cruzamento", 4f),
            I("Ruas", "KayKitCity", "road_tsplit", "RuaKK_Entroncamento", 4f),
            I("Ruas", "KayKitCity", "base", "RuaKK_Quadra", 4f),
            I("Carros", "KayKitCity", "car_hatchback", "CarroKK_Hatch", 4f),
            I("Carros", "KayKitCity", "car_sedan", "CarroKK_Sedan", 4f),
            I("Carros", "KayKitCity", "car_stationwagon", "CarroKK_Perua", 4f),
            I("Carros", "KayKitCity", "car_taxi", "CarroKK_Taxi", 4f),
            I("Carros", "KayKitCity", "car_police", "CarroKK_Policia", 4f),
            I("Props", "KayKitCity", "bench", "BancoKK", 4f),
            I("Props", "KayKitCity", "box_A", "CaixaKK_A", 4f),
            I("Props", "KayKitCity", "box_B", "CaixaKK_B", 4f),
            I("Props", "KayKitCity", "dumpster", "Cacamba_De_Lixo", 4f),
            I("Props", "KayKitCity", "firehydrant", "Hidrante", 4f),
            I("Props", "KayKitCity", "streetlight", "PosteKK", 4f),
            I("Props", "KayKitCity", "trafficlight_A", "SemaforoKK_A", 4f),
            I("Props", "KayKitCity", "trafficlight_B", "SemaforoKK_B", 4f),
            I("Props", "KayKitCity", "trafficlight_C", "SemaforoKK_C", 4f),
            I("Props", "KayKitCity", "trash_A", "Saco_De_Lixo_A", 4f),
            I("Props", "KayKitCity", "trash_B", "Saco_De_Lixo_B", 4f),
            I("Natureza", "KayKitCity", "bush", "ArbustoKK", 4f),

            // ---------- Prédios comerciais (Kenney) ----------
            I("Predios", "KenneyCommercial", "building-a", "Comercio_A", 6f),
            I("Predios", "KenneyCommercial", "building-b", "Comercio_B", 6f),
            I("Predios", "KenneyCommercial", "building-c", "Comercio_C", 6f),
            I("Predios", "KenneyCommercial", "building-d", "Comercio_D", 6f),
            I("Predios", "KenneyCommercial", "building-e", "Comercio_E", 6f),
            I("Predios", "KenneyCommercial", "building-f", "Comercio_F", 6f),
            I("Predios", "KenneyCommercial", "building-g", "Comercio_G", 6f),
            I("Predios", "KenneyCommercial", "building-h", "Comercio_H", 6f),
            I("Predios", "KenneyCommercial", "building-i", "Comercio_I", 6f),
            I("Predios", "KenneyCommercial", "building-j", "Comercio_J", 6f),
            I("Predios", "KenneyCommercial", "building-k", "Comercio_K", 6f),
            I("Predios", "KenneyCommercial", "building-l", "Comercio_L", 6f),
            I("Predios", "KenneyCommercial", "building-m", "Comercio_M", 6f),
            I("Predios", "KenneyCommercial", "building-n", "Comercio_N", 6f),
            I("Predios", "KenneyCommercial", "building-skyscraper-a", "Arranha_Ceu_A", 6f),
            I("Predios", "KenneyCommercial", "building-skyscraper-b", "Arranha_Ceu_B", 6f),
            I("Predios", "KenneyCommercial", "building-skyscraper-c", "Arranha_Ceu_C", 6f),
            I("Predios", "KenneyCommercial", "building-skyscraper-d", "Arranha_Ceu_D", 6f),
            I("Predios", "KenneyCommercial", "building-skyscraper-e", "Arranha_Ceu_E", 6f),
            I("Props", "KenneyCommercial", "detail-awning", "Toldo", 6f),
            I("Props", "KenneyCommercial", "detail-awning-wide", "Toldo_Largo", 6f),
            I("Props", "KenneyCommercial", "detail-parasol-a", "Guarda_Sol_A", 6f),
            I("Props", "KenneyCommercial", "detail-parasol-b", "Guarda_Sol_B", 6f),

            // ---------- Transporte público (Quaternius) ----------
            I("Carros", "QuaterniusTransport", "Bus", "Onibus", 1.8f,
                "Material=Grafite;Top=Branco;Bottom=Petroleo;Bumper=Cinza_Escuro;Windows=Vidro;Lights=Mostarda_Clara;Details=Cinza").Gira(-90),
            I("Carros", "QuaterniusTransport", "SchoolBus", "Onibus_Escolar", 1.8f,
                "Wheel=Grafite;Yellow=Mostarda;Bumper=Cinza_Escuro;Windows=Vidro;Lights=Branco;Details=Grafite").Gira(-90),
            I("Carros", "QuaterniusTransport", "Train", "Metro", 1.6f, "Outside=Cinza_Claro;Black=Grafite;Windows=Vidro;Top=Petroleo"),
            I("Carros", "QuaterniusTransport", "Bicycle", "Bicicleta_Q1", .31f, "Wheel=Grafite;Bike=Vermelho;Handle=Cinza_Escuro;Material.003=Grafite"),
            I("Carros", "QuaterniusTransport", "SquareFrameBicycle", "Bicicleta_Q2", .31f,
                "Material.002=Grafite;Material.001=Petroleo;Material.005=Cinza_Escuro;Material.003=Grafite"),
            I("Props", "QuaterniusTransport", "TrafficLight", "Semaforo_Q", 2.1f, "TrafficLight=Grafite;Green=Luz_Verde;Red=Luz_Vermelha;Yellow=Luz_Amarela"),
            I("Props", "QuaterniusTransport", "TrafficSign1", "Placa_Q1", 1.6f, "Border=Branco;Sign=Vermelho;Pole=Cinza").Gira(90),
            I("Props", "QuaterniusTransport", "TrafficSign2", "Placa_Q2", 1.6f, "Border=Grafite;Sign=Mostarda;Pole=Cinza"),
            I("Props", "QuaterniusTransport", "TrafficSign3", "Placa_Q3", 1.6f, "Border=Branco;Sign=Petroleo;Pole=Cinza"),

            // ---------- Móveis (Kenney) pra enchente e cenas de casa ----------
            I("Enchente", "KenneyFurniture", "bedSingle", "Cama_Solteiro", .2f),
            I("Enchente", "KenneyFurniture", "bedDouble", "Cama_Casal", .2f),
            I("Enchente", "KenneyFurniture", "loungeSofa", "Sofa_Kenney", .2f, "carpet=Petroleo_Claro;carpetDarker=Petroleo"),
            I("Enchente", "KenneyFurniture", "loungeChair", "Poltrona", .2f, "carpet=Mostarda;carpetDarker=Laranja"),
            I("Enchente", "KenneyFurniture", "chairCushion", "Cadeira_Estofada", .2f),
            I("Enchente", "KenneyFurniture", "table", "Mesa_Kenney", .2f),
            I("Enchente", "KenneyFurniture", "tableCoffee", "Mesa_De_Centro", .2f),
            I("Enchente", "KenneyFurniture", "bookcaseOpen", "Estante", .2f),
            I("Enchente", "KenneyFurniture", "books", "Livros", .2f),
            I("Enchente", "KenneyFurniture", "cabinetTelevision", "Rack", .2f),
            I("Enchente", "KenneyFurniture", "televisionModern", "Televisao", .2f),
            I("Enchente", "KenneyFurniture", "televisionVintage", "Televisao_Antiga", .2f),
            I("Enchente", "KenneyFurniture", "radio", "Radio", .2f),
            I("Enchente", "KenneyFurniture", "kitchenFridge", "Geladeira", .2f),
            I("Enchente", "KenneyFurniture", "kitchenStove", "Fogao", .2f),
            I("Enchente", "KenneyFurniture", "kitchenSink", "Pia_Cozinha", .2f),
            I("Enchente", "KenneyFurniture", "washer", "Maquina_De_Lavar", .2f),
            I("Enchente", "KenneyFurniture", "toilet", "Vaso_Sanitario", .2f),
            I("Enchente", "KenneyFurniture", "bathtub", "Banheira", .2f),
            I("Enchente", "KenneyFurniture", "bathroomSink", "Pia_Banheiro", .2f),
            I("Enchente", "KenneyFurniture", "lampRoundFloor", "Abajur", .2f),
            I("Enchente", "KenneyFurniture", "pottedPlant", "Vaso_De_Planta", .2f),
            I("Enchente", "KenneyFurniture", "cardboardBoxClosed", "Caixa_Fechada", .2f),
            I("Enchente", "KenneyFurniture", "cardboardBoxOpen", "Caixa_Aberta", .2f),
            I("Enchente", "KenneyFurniture", "trashcan", "Lixeira_De_Casa", .2f),
            I("Enchente", "KenneyFurniture", "bear", "Ursinho", .2f),
            I("Enchente", "KenneyFurniture", "rugRectangle", "Tapete", .2f),
            I("Enchente", "KenneyFurniture", "pillow", "Almofada", .2f),

            // ---------- Props realistas (CityEnvPack1, licença não identificada) ----------
            I("Props", "CityEnvPack1", "ATM", "Caixa_Eletronico", 1f, "Mat=Tex_ATM"),
            I("Props", "CityEnvPack1", "CityMailBox", "Caixa_De_Correio", 1f, "Mat=Tex_CityMailBox"),
            I("Props", "CityEnvPack1", "Payphone", "Telefone_Publico", 1f, "Mat=Tex_Payphone"),

            // ---------- Brasília e props em glTF (Sketchfab, CC-BY 4.0) ----------
            I("Predios", "Sketchfab/Catedral_De_Brasilia", "Catedral_De_Brasilia", "BSB_Catedral_Realista", 1.8f).Gltf(),
            I("Predios", "Sketchfab/Palacio_Do_Planalto", "Palacio_Do_Planalto", "BSB_Palacio_Do_Planalto_Realista", .7f).Gltf(),
            I("Predios", "Sketchfab/Maquete_De_Brasilia", "Maquete_De_Brasilia", "BSB_Maquete_Da_Cidade", .0001f).Gltf(),
            I("Props", "Sketchfab/Ponto_De_Onibus", "Ponto_De_Onibus", "Ponto_De_Onibus", 1.8f).Gltf(),
            I("Props", "Sketchfab/Lixeira_Sketchfab", "Lixeira_Sketchfab", "Lixeira_Com_Tampa", .55f).Gltf(),

            // ================= Terceira leva: a cidade em caos depois da enchente =================
            // Pacotes avulsos, cada um numa unidade: o tamanho vem de Mede(metros), não da escala.

            // ---------- Sinalização de obra, inteira e quebrada (MilkAndBanana, CC0) ----------
            I("Enchente", "MilkAndBanana", "Broken_Traffic_Cone", "Cone_Quebrado_1", 1f).Glb().Mede(.6f),
            I("Enchente", "MilkAndBanana", "Broken_Traffic_Cone_002", "Cone_Quebrado_2", 1f).Glb().Mede(.6f),
            I("Enchente", "MilkAndBanana", "Broken_Traffic_Cone_005", "Cone_Quebrado_3", 1f).Glb().Mede(.6f),
            I("Enchente", "MilkAndBanana", "Traffic_Cone", "Cone_Obra_1", 1f).Glb().Mede(.7f),
            I("Enchente", "MilkAndBanana", "Traffic_Cone_003", "Cone_Obra_2", 1f).Glb().Mede(.7f),
            I("Enchente", "MilkAndBanana", "Chipped_Road_Block", "Bloco_Concreto_Lascado_1", 1f).Glb().Mede(1.1f),
            I("Enchente", "MilkAndBanana", "Chipped_Road_Block_002", "Bloco_Concreto_Lascado_2", 1f).Glb().Mede(1.1f),
            I("Enchente", "MilkAndBanana", "Road_Block", "Bloco_Concreto", 1f).Glb().Mede(1.1f),
            I("Enchente", "MilkAndBanana", "Small_Road_Block", "Marco_De_Concreto", 1f).Glb().Mede(1f),
            I("Enchente", "MilkAndBanana", "Crush_Barrier", "Defensa_Metalica_1", 1f).Glb().Mede(1.8f),
            I("Enchente", "MilkAndBanana", "Crush_Barrier_001", "Defensa_Metalica_2", 1f).Glb().Mede(1.8f),
            I("Enchente", "MilkAndBanana", "Manhole_Cover", "Bueiro_Tampa_1", 1f).Glb().Mede(.9f),
            I("Enchente", "MilkAndBanana", "Manhole_Cover_001", "Bueiro_Tampa_2", 1f).Glb().Mede(.9f),
            I("Enchente", "MilkAndBanana", "Manhole_Cover_002", "Bueiro_Grade", 1f).Glb().Mede(.9f),
            I("Enchente", "MilkAndBanana", "Streetlights", "Poste_De_Luz_Simples", 1f).Glb().Mede(7f),
            I("Enchente", "MilkAndBanana", "Streetlights_001", "Poste_De_Luz_Duplo", 1f).Glb().Mede(7f),
            I("Enchente", "MilkAndBanana", "Warning_Triangle", "Triangulo_De_Sinalizacao", 1f).Glb().Mede(.6f),
            I("Enchente", "MilkAndBanana", "Water_Hydrant", "Hidrante_1", 1f).Glb().Mede(.9f),
            I("Enchente", "MilkAndBanana", "Water_Hydrant_001", "Hidrante_2", 1f).Glb().Mede(.9f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Large", "Cavalete_Grande_1", 1f).Glb().Mede(4f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Large_004", "Cavalete_Grande_2", 1f).Glb().Mede(4f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Large_006", "Cavalete_Grande_3", 1f).Glb().Mede(4f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Medium", "Cavalete_Medio_1", 1f).Glb().Mede(2.4f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Medium_005", "Cavalete_Medio_2", 1f).Glb().Mede(2.4f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Small", "Cavalete_Pequeno_1", 1f).Glb().Mede(1.3f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_Small_002", "Cavalete_Pequeno_2", 1f).Glb().Mede(1.6f),
            I("Enchente", "MilkAndBanana", "Wood_Road_Block_XSmall", "Cavalete_Mini", 1f).Glb().Mede(1f),

            // ---------- Estragos da enchente (Quaternius avulsos, CC0) ----------
            I("Enchente", "QuaterniusAvulsos", "Barrier_Trash", "Barricada_De_Entulho", 1f,
                "Wood=Marrom;Wood_Light=Marrom_Claro;Sack=Areia;Black=Grafite;Grey=Cinza_Escuro;Red=Vermelho").Mede(3.2f),
            I("Enchente", "QuaterniusAvulsos", "TrashContainer_Open", "Cacamba_Transbordando", 1f,
                "Oil=Marrom_Escuro;Black=Grafite;Green=Verde_Escuro;Grey=Cinza_Escuro;DarkGrey=Grafite").Mede(2.6f),
            I("Enchente", "QuaterniusAvulsos", "Debris_Papers_2", "Papeis_Espalhados", 1f, "White=Branco").Mede(1.2f).Rente(),
            I("Enchente", "QuaterniusAvulsos", "DeadTree_5", "Arvore_Seca_Grande", 1f, "Bark_DeadTree=Marrom_Escuro").Mede(6f),
            I("Enchente", "QuaterniusAvulsos", "DeadTrees", "Arvore_Seca_1", 1f, "NormalTree_Bark=Marrom").Partes("DeadTree_1").Mede(5f),
            I("Enchente", "QuaterniusAvulsos", "DeadTrees", "Arvore_Seca_2", 1f, "NormalTree_Bark=Marrom").Partes("DeadTree_3").Mede(4f),
            I("Enchente", "QuaterniusAvulsos", "DeadTrees", "Arvore_Seca_3", 1f, "NormalTree_Bark=Marrom").Partes("DeadTree_4").Mede(3.6f),
            I("Enchente", "QuaterniusAvulsos", "DeadTrees", "Arbusto_Seco", 1f, "NormalTree_Bark=Marrom").Partes("DeadTree_5").Mede(1.6f),

            // ---------- Ruínas (Destroyed City Assets, licença não identificada): um arquivo, várias peças ----------
            I("Enchente", "CidadeDestruida", "Destroyed_City_Assets", "Ruina_De_Predio_1", 1f).Partes("Colonne_1;FACADE_3").Mede(11f),
            I("Enchente", "CidadeDestruida", "Destroyed_City_Assets", "Ruina_De_Predio_2", 1f).Partes("Colonne_2;FACADE_2").Mede(11f),
            I("Enchente", "CidadeDestruida", "Destroyed_City_Assets", "Ruina_De_Predio_3", 1f).Partes("Colonne_3;FACADE").Mede(11f),
            I("Enchente", "CidadeDestruida", "Destroyed_City_Assets", "Entulho_De_Concreto", 1f).Partes("Débris;Rocks").Mede(4.5f),
            I("Enchente", "CidadeDestruida", "Destroyed_City_Assets", "Asfalto_Quebrado", 1f).Partes("Route").Mede(8f),

            // ---------- Buraco e pedaços de rua (City Pack, licença não identificada) ----------
            I("Enchente", "CityPack", "FloorHole", "Buraco_No_Chao", 1f, "pavement=Asfalto;dirt=Marrom_Escuro").Mede(1.8f),
            I("Enchente", "CityPack", "RoadBits", "Pedacos_De_Rua", 1f, "citybits_texture=Atlas_KayKit").Mede(6f).Rente(),

            // ---------- Lixo espalhado (pasta "lixos", licença não identificada) ----------
            I("Lixo_Reciclagem", "LixosAvulsos", "BolsaN1", "Saco_De_Lixo_Preto_1", 1f).Autor().Mede(.7f),
            I("Lixo_Reciclagem", "LixosAvulsos", "BolsaN2", "Saco_De_Lixo_Preto_2", 1f).Autor().Mede(.6f),
            I("Lixo_Reciclagem", "LixosAvulsos", "BolsaNa", "Saco_De_Lixo_Laranja", 1f).Autor().Mede(.7f),
            I("Lixo_Reciclagem", "LixosAvulsos", "BolsaA", "Saco_De_Lixo_Verde", 1f).Autor().Mede(.7f),
            I("Lixo_Reciclagem", "LixosAvulsos", "BagEBox", "Caixa_Com_Saco", 1f).Autor().Mede(.6f),
            I("Lixo_Reciclagem", "LixosAvulsos", "BoxEBottles", "Caixa_Com_Garrafas", 1f).Autor().Mede(.6f),
            I("Lixo_Reciclagem", "LixosAvulsos", "StripedBox", "Caixa_Listrada", 1f).Autor().Mede(.6f),
            I("Lixo_Reciclagem", "LixosAvulsos", "NormalRed", "Garrafa_Pet", 1f).Autor().Mede(.32f),
            I("Lixo_Reciclagem", "LixosAvulsos", "CrushedBottle", "Garrafa_Pet_Amassada", 1f).Autor().Mede(.3f),
            I("Lixo_Reciclagem", "LixosAvulsos", "TwistedBottle", "Garrafa_Pet_Torcida", 1f).Autor().Mede(.3f),
            I("Lixo_Reciclagem", "LixosAvulsos", "FlatBottle", "Garrafa_Pet_Achatada", 1f).Autor().Mede(.3f),
            I("Lixo_Reciclagem", "LixosAvulsos", "DwarfBottle", "Garrafa_Pet_Pequena", 1f).Autor().Mede(.2f),
            I("Lixo_Reciclagem", "LixosAvulsos", "Tissue1", "Papel_Amassado_1", 1f).Autor().Mede(.22f),
            I("Lixo_Reciclagem", "LixosAvulsos", "Tissue2", "Papel_Amassado_2", 1f).Autor().Mede(.22f),
            I("Lixo_Reciclagem", "LixosAvulsos", "RottenApple", "Maca_Podre", 1f).Autor().Mede(.22f),
            I("Lixo_Reciclagem", "LixosAvulsos", "Platano", "Casca_De_Banana_Aberta", 1f, "lambert2=Mostarda;Tallo=Marrom_Escuro").Mede(.3f),
            I("Lixo_Reciclagem", "LixosAvulsos", "PlasticBlueFBX", "Balde_De_Lixo_Azul", 1f, "lambert4=Petroleo").Mede(.75f),
            I("Lixo_Reciclagem", "LixosAvulsos", "PlasticGreenFBX", "Balde_De_Lixo_Verde", 1f, "lambert4=Verde").Mede(.75f),
            I("Lixo_Reciclagem", "LixosAvulsos", "PlasticGrayFBX", "Balde_De_Lixo_Cinza", 1f, "lambert4=Cinza").Mede(.75f),
            I("Lixo_Reciclagem", "LixosAvulsos", "PlasticBeigeFBX", "Balde_De_Lixo_Bege", 1f, "lambert6=Areia").Mede(.75f),
            I("Lixo_Reciclagem", "LixosAvulsos", "FilledTrashcan", "Lixeira_Transbordando", 1f).Autor().Mede(1.1f),
            I("Lixo_Reciclagem", "PolyGoogle", "Rubbish", "Pilha_De_Lixo", 1f).Glb().Mede(2.2f),

            // ---------- Props de rua (City Pack) ----------
            I("Props", "CityPack", "TrashContainer", "Cacamba_Verde", 1f, "DarkGrey=Grafite;Green=Verde_Escuro;Grey=Cinza_Escuro").Mede(2.2f),
            I("Props", "CityPack", "TrashCan/TrashCan", "Lixeira_Redonda", 1f, "BlackTrashCan02=Cinza_Escuro").Obj().Mede(.9f),
            I("Props", "CityPack", "TrashBag/TrashBag", "Saco_De_Lixo_Amarrado", 1f).Obj().Autor().Mede(.7f),
            I("Props", "CityPack", "Cone/Cone", "Cone_Listrado", 1f).Obj().Autor().Mede(.75f),
            I("Props", "CityPack", "FireHydrant/FireHydrant", "Hidrante_Vermelho", 1f, "red=Vermelho").Obj().Mede(.9f),
            I("Props", "CityPack", "Bench/Bench", "Banco_De_Praca", 1f).Obj().Autor().Mede(1.8f),
            I("Props", "CityPack", "BusStop/BusStop", "Ponto_De_Onibus_Azul", 1f).Obj().Autor().Mede(3.8f),
            I("Props", "CityPack", "BusStopSign", "Placa_De_Ponto_De_Onibus", 1f).Autor().Mede(2.8f),
            I("Props", "CityPack", "Billboard/Billboard", "Outdoor", 1f).Obj().Autor().Gira(90).Mede(6f),
            I("Props", "CityPack", "AirConditioner/AirConditioner", "Ar_Condicionado", 1f).Obj().Autor().Mede(.9f),
            I("Props", "CityPack", "Fence/Fence", "Alambrado", 1f, "defaultMat=Cinza").Obj().Mede(3f),
            I("Props", "CityPack", "FencePiece", "Muro_Com_Grade", 1.8f).Autor(),
            I("Props", "CityPack", "FenceEnd", "Muro_Com_Grade_Ponta", 1.8f).Autor(),
            I("Props", "CityPack", "FlowerPot4", "Vaso_De_Folhagem", 1f, "VaseFlowerPot4=Terracota;GroundFlowerPot4=Marrom_Escuro;PlantFlowerPot4=Verde").Mede(1.1f),
            I("Props", "CityPack", "FlowerPot7", "Vaso_De_Laranjeira", 1f,
                "VaseFlowerPot7=Cinza_Escuro;GroundFlowerPot7=Marrom_Escuro;TrunkFlowerPot7=Marrom;LeafFlowerPot7=Verde_Escuro;FruitFlowerPot7=Laranja").Mede(1.2f),
            I("Props", "CityPack", "PlanterAndBushes", "Floreira_De_Madeira", 1f).Autor().Mede(2.2f),
            I("Props", "CityPack", "ManholeCover", "Bueiro_Fechado", 1f, "concrete=Cinza;metal_cover=Grafite").Mede(.9f),
            I("Props", "CityPack", "PowerBox", "Caixa_De_Energia", 1f).Autor().Mede(1.3f),
            I("Props", "CityPack", "WashingLine", "Varal", 1f, "metal=Cinza;black_plastic=Grafite").Mede(2.2f),
            I("Props", "PolyGoogle", "Broom/Broom", "Vassoura", 1f).Obj().Autor().Mede(1.4f),

            // ---------- Veículos, prédios e árvore (City Pack) ----------
            I("Carros", "CityPack", "NormalCar1/NormalCar1", "Carro_Sedan_Azul", 1f).Obj().Autor().Mede(3.7f),
            I("Carros", "CityPack", "NormalCar2/NormalCar2", "Carro_Compacto_Azul", 1f).Obj().Autor().Mede(3.3f),
            I("Carros", "CityPack", "SportsCar2/SportsCar2", "Carro_Cupe_Branco", 1f).Obj().Autor().Mede(3.6f),
            I("Carros", "CityPack", "SUV/SUV", "Carro_SUV_Branco", 1f).Obj().Autor().Mede(3.9f),
            I("Carros", "CityPack", "Cop/Cop", "Viatura_De_Policia", 1f).Obj().Autor().Mede(3.6f),
            I("Carros", "CityPack", "Motorcycle/Motorcycle", "Moto", 1f).Obj().Autor().Mede(2f),
            I("Carros", "CityPack", "Bus/Bus", "Onibus_Urbano", 1f).Obj().Autor().Mede(9f),
            I("Carros", "CityPack", "Van/Van", "Van_Azul", 1f).Obj().Autor().Mede(4.4f),
            I("Carros", "CityPack", "Bike/Bike", "Bicicleta_Azul", 1f).Obj().Autor().Mede(1.7f),
            I("Predios", "CityPack", "BrownBuilding", "Predio_Tijolo_Marrom", 1f).Autor().Mede(14f),
            I("Predios", "CityPack", "Building3_Big", "Predio_Tijolo_Grande", 1f).Autor().Mede(16f),
            I("Predios", "CityPack", "BuildingGreen", "Predio_Tijolo_Verde", 4f).Autor(),
            I("Predios", "CityPack", "GBBlank", "Predio_Tijolo_Verde_Liso", 4f).Autor(),
            I("Predios", "CityPack", "PizzaCorner", "Predio_Tijolo_Verde_Esquina", 4f).Autor(),
            I("Predios", "CityPack", "BuildingRed1", "Predio_Tijolo_Vermelho", 4f).Autor(),
            I("Predios", "CityPack", "RBBlank", "Predio_Tijolo_Vermelho_Liso", 4f).Autor(),
            I("Predios", "CityPack", "BuildingRedCorner", "Predio_Tijolo_Vermelho_Esquina", 4f).Autor(),
            I("Predios", "CityPack", "Greenhouse/Greenhouse", "Estufa", 1f, "greenhouse_1=Branco;greenhouse_glass=Vidro").Obj().Mede(3f),
            I("Natureza", "CityPack", "Tree/Tree", "Arvore_LowPoly", 1f).Obj().Autor().Mede(5f),
            I("Natureza", "QuaterniusAvulsos", "TwistedTree_3", "Arvore_Retorcida", 1f, "Bark_TwistedTree=Marrom_Escuro;Leaves_TwistedTree=Verde").Mede(6f),

            // ================= Quarta leva: o interior do carro (Passeio de Carro, 2026-11-04) =================
            // Origem e licença a confirmar com a usuária (ver CREDITOS.md). Painel, volante e fivela vão pra paleta;
            // o rádio (glb) fica com as cores do autor. Do painel saem duas peças (o painel e o câmbio);
            // do "clip belt" fica só a fivela (Cube.001): a tira do cinto é feita pelo jogo, pra poder esticar.
            I("Carros", "InteriorDoCarro", "Painel_CarDash", "Interior_Painel", 1f).Obj().Partes("Dash_1_Retopo1").Gira(-90f).Mede(1.5f),
            I("Carros", "InteriorDoCarro", "Painel_CarDash", "Interior_Cambio", 1f).Obj().Partes("polySurface14").Gira(-90f).Mede(.5f),
            I("Carros", "InteriorDoCarro", "Volante_Shutt", "Interior_Volante", 1f).Mede(.38f),
            I("Carros", "InteriorDoCarro", "Radio_Carro", "Interior_Radio", 1f).Glb().Mede(.2f),
            I("Carros", "InteriorDoCarro", "Fivela_ClipBelt", "Interior_Fivela", 1f).Partes("Cube.001").Mede(.12f),
        };

        // Nome do material no FBX -> cor da paleta, por pacote.
        static readonly Dictionary<string, Dictionary<string, string>> PadraoPorKit = new Dictionary<string, Dictionary<string, string>>
        {
            ["KenneyRoads"] = new Dictionary<string, string>
            {
                ["pavement"] = "Cinza_Claro", ["asphalt"] = "Asfalto", ["asphaltEdge"] = "Asfalto_Escuro", ["line"] = "Branco",
                ["cone"] = "Laranja", ["light"] = "Mostarda_Clara", ["_defaultMat"] = "Cinza",
            },
            ["KenneySuburban"] = new Dictionary<string, string>
            {
                ["border"] = "Branco", ["window"] = "Vidro", ["door"] = "Petroleo_Escuro", ["_defaultMat"] = "Areia",
                ["roof"] = "Petroleo", ["wood"] = "Marrom_Claro",
            },
            ["QuaterniusNature"] = new Dictionary<string, string>
            {
                ["Green"] = "Verde", ["DarkGreen"] = "Verde_Escuro", ["Wood"] = "Marrom", ["LightWood"] = "Marrom_Claro",
                ["White"] = "Branco", ["Black"] = "Grafite", ["Berry"] = "Vermelho", ["Cyan"] = "Petroleo_Claro",
                ["Yellow"] = "Mostarda", ["Pink"] = "Rosa", ["Rock"] = "Cinza", ["Coconuts"] = "Marrom_Escuro",
                ["Mushroom_Top"] = "Terracota", ["Mushroom_Bottom"] = "Branco",
            },
            ["QuaterniusDowntown"] = new Dictionary<string, string>
            {
                ["MI_Trim_MetalConcrete"] = "Cinza_Claro", ["MI_Dirt"] = "Verde_Musgo", ["MI_Ornaments"] = "Cinza_Escuro",
                ["MI_StreetDecals"] = "Pintura_Via",
            },
            ["KenneyCarKit"] = new Dictionary<string, string> { ["colormap"] = "Atlas_Carros" },
            ["KenneyFoodKit"] = new Dictionary<string, string> { ["colormap"] = "Atlas_Comida" },
            ["KenneyCommercial"] = new Dictionary<string, string> { ["colormap"] = "Atlas_Comercial" },
            ["KenneyMiniCharacters"] = new Dictionary<string, string> { ["colormap"] = "Atlas_MiniPersonagens" },
            ["KayKitCity"] = new Dictionary<string, string> { ["citybits_texture"] = "Atlas_KayKit" },
            ["KenneyFurniture"] = new Dictionary<string, string>
            {
                ["wood"] = "Marrom_Claro", ["woodDark"] = "Marrom", ["carpet"] = "Terracota", ["carpetDarker"] = "Vermelho",
                ["carpetWhite"] = "Branco", ["fur"] = "Marrom_Claro", ["glass"] = "Vidro", ["lamp"] = "Mostarda_Clara",
                ["metal"] = "Cinza_Claro", ["metalDark"] = "Cinza_Escuro", ["metalLight"] = "Branco", ["metalMedium"] = "Cinza",
                ["plant"] = "Verde", ["_defaultMat"] = "Branco",
            },
            ["QuaterniusCharacters"] = new Dictionary<string, string>
            {
                ["Skin"] = "Pele_2", ["Face"] = "Grafite", ["Hair"] = "Marrom_Escuro", ["Shirt"] = "Cinza_Claro", ["Pants"] = "Petroleo_Escuro",
                ["Belt"] = "Marrom_Escuro", ["Vest"] = "Laranja", ["Hat"] = "Mostarda", ["Main"] = "Branco", ["Brown"] = "Marrom_Escuro",
                ["Black"] = "Petroleo", ["Detail"] = "Terracota",
            },
            ["QuaterniusAnimals"] = new Dictionary<string, string>(),
            ["QuaterniusFarmAnimals"] = new Dictionary<string, string>(),
            ["QuaterniusTransport"] = new Dictionary<string, string>(),
            ["CityEnvPack1"] = new Dictionary<string, string>(),
            ["CityPack"] = new Dictionary<string, string>(),
            ["InteriorDoCarro"] = new Dictionary<string, string>
            {
                ["Dash_1_Retopo1Mat"] = "Petroleo_Escuro", ["polySurface14Mat"] = "Grafite",
                ["steering Leather"] = "Grafite", ["steering Base"] = "Cinza_Escuro", ["steering Strut"] = "Cinza", ["Logo"] = "Mostarda",
                ["CLIP"] = "Cinza_Claro", ["CLIP.2"] = "Cinza", ["STRAP"] = "Petroleo",
            },
            ["QuaterniusAvulsos"] = new Dictionary<string, string>(),
            ["LixosAvulsos"] = new Dictionary<string, string>(),
            ["CidadeDestruida"] = new Dictionary<string, string>
            {
                ["Ciment_Brisé"] = "Cinza_Claro", ["Ciment_Brisé2"] = "Cinza", ["Ciment_Brisé3"] = "Areia",
                ["Métal_Rouillé"] = "Terracota", ["Rock_Apo"] = "Cinza_Escuro", ["Route_Brisée"] = "Asfalto_Escuro",
            },
        };

        public static void PrepararTexturas()
        {
            ArtePaleta.GarantirPasta(ArtePaleta.PastaTexturas);
            GerarAtlas("Assets/Art/Carros/Modelos/KenneyCarKit/Textures/colormap.png", $"{ArtePaleta.PastaTexturas}/Atlas_Carros.png");
            GerarAtlas("Assets/Art/Lixo_Reciclagem/Modelos/KenneyFoodKit/Textures/colormap.png", $"{ArtePaleta.PastaTexturas}/Atlas_Comida.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ArtePaleta.ConfigurarTextura($"{ArtePaleta.PastaTexturas}/Atlas_Carros.png", false, false, 512, true);
            ArtePaleta.ConfigurarTextura($"{ArtePaleta.PastaTexturas}/Atlas_Comida.png", false, false, 512, true);
            ArtePaleta.MatTextura("Atlas_Carros", $"{ArtePaleta.PastaTexturas}/Atlas_Carros.png", false);
            ArtePaleta.MatTextura("Atlas_Comida", $"{ArtePaleta.PastaTexturas}/Atlas_Comida.png", false);

            // Segunda leva: o comercial do Kenney entra na paleta; KayKit e mini personagens mantêm as cores do autor.
            GerarAtlas($"{ArtePaleta.PastaTexturas}/Fonte_Comercial.png", $"{ArtePaleta.PastaTexturas}/Atlas_Comercial.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var (mat, arquivo) in new[] { ("Atlas_Comercial", "Atlas_Comercial"), ("Atlas_KayKit", "Fonte_KayKit"), ("Atlas_MiniPersonagens", "Fonte_MiniPersonagens") })
            {
                ArtePaleta.ConfigurarTextura($"{ArtePaleta.PastaTexturas}/{arquivo}.png", false, false, 1024, true);
                ArtePaleta.MatTextura(mat, $"{ArtePaleta.PastaTexturas}/{arquivo}.png", false);
            }
            foreach (var nome in new[] { "ATM", "CityMailBox", "Payphone" })
            {
                string caminho = $"Assets/Art/Props/Modelos/CityEnvPack1/{nome}_Base_Color.png";
                ArtePaleta.ConfigurarTextura(caminho, false, true, 1024, false);
                ArtePaleta.MatTextura("Tex_" + nome, caminho, false);
            }

            const string pinturas = "Assets/Art/Ruas/Modelos/QuaterniusDowntown/Textures/T_Street_Decals.png";
            ArtePaleta.ConfigurarTextura(pinturas, true, true, 1024, false);
            ArtePaleta.MatTextura("Pintura_Via", pinturas, true);

            const string tex = "Assets/Art/Props/Texturas";
            foreach (var nome in new[] { "placa_pare", "placa_faixa_pedestre", "placa_escola", "placa_ciclovia",
                         "rotulo_papel", "rotulo_plastico", "rotulo_vidro", "rotulo_organico", "letreiro_escola", "letreiro_padaria" })
            {
                ArtePaleta.ConfigurarTextura($"{tex}/{nome}.png", true, true, 512, false);
                ArtePaleta.MatTextura("Tex_" + nome, $"{tex}/{nome}.png", true);
            }
            foreach (var nome in new[] { "semaforo_pedestre_pare", "semaforo_pedestre_siga" })
            {
                ArtePaleta.ConfigurarTextura($"{tex}/{nome}.png", false, true, 128, false);
                ArtePaleta.MatTextura("Tex_" + nome, $"{tex}/{nome}.png", false, true);
            }
            AssetDatabase.SaveAssets();
        }

        // Sem argumentos refaz tudo; com nomes de pacote ("CityPack", "MilkAndBanana"...) refaz só os itens deles.
        static bool Entra(Item item, string[] kits) => kits.Length == 0 || System.Array.IndexOf(kits, item.kit) >= 0;

        public static void RemapearMateriais(params string[] kits)
        {
            var feitos = new HashSet<string>();
            foreach (var item in Itens)
            {
                // O mesmo arquivo pode render vários prefabs (Partes): o importador só precisa passar uma vez.
                if (item.gltf || !Entra(item, kits) || !feitos.Add(item.CaminhoModelo)) continue;
                var imp = AssetImporter.GetAtPath(item.CaminhoModelo) as ModelImporter;
                if (imp == null) { Debug.LogError("[Arte] Modelo não encontrado: " + item.CaminhoModelo); continue; }

                var nomes = new HashSet<string>();
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(item.CaminhoModelo))
                    if (o is Material m) nomes.Add(m.name);
                foreach (var par in imp.GetExternalObjectMap())
                    if (par.Key.type == typeof(Material)) nomes.Add(par.Key.name);

                var trocas = item.autor ? new Dictionary<string, string>() : new Dictionary<string, string>(PadraoPorKit[item.kit]);
                foreach (var troca in item.trocas.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = troca.Split('=');
                    trocas[kv[0]] = kv[1];
                }

                foreach (var nome in nomes)
                {
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), nome);
                    imp.RemoveRemap(id);
                    if (!trocas.TryGetValue(nome, out var cor))
                    {
                        if (item.autor) continue;
                        Debug.LogWarning($"[Arte] {item.arquivo}: material '{nome}' sem cor definida, usando Cinza.");
                        cor = "Cinza";
                    }
                    imp.AddRemap(id, ArtePaleta.Mat(cor));
                }

                imp.importCameras = false;
                imp.importLights = false;
                if (item.animado) ConfigurarAnimacao(imp);
                else
                {
                    imp.importAnimation = false;
                    imp.animationType = ModelImporterAnimationType.None;
                }
                imp.SaveAndReimport();
            }
        }

        static void ConfigurarAnimacao(ModelImporter imp)
        {
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = true;
            var clipes = imp.defaultClipAnimations;
            foreach (var c in clipes)
            {
                // "CharacterArmature|Walk" -> "Walk"
                int barra = c.name.LastIndexOf('|');
                if (barra >= 0) c.name = c.name.Substring(barra + 1);
                string n = c.name.ToLowerInvariant();
                c.loopTime = n.StartsWith("idle") || n.StartsWith("walk") || n.StartsWith("run") || n == "gallop" || n == "sprint"
                             || n == "eating" || n == "sit" || n == "drive" || n.StartsWith("wheelchair-move");
            }
            imp.clipAnimations = clipes;
        }

        // Controlador simples: estado inicial "Locomocao" (parado/andando/correndo pelo parâmetro float
        // "Velocidade", de 0 a 1) e um estado por clipe restante, pra tocar com animator.Play("Nome").
        static RuntimeAnimatorController CriarControlador(Item item)
        {
            var clipes = new List<AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(item.CaminhoModelo))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) clipes.Add(c);
            if (clipes.Count == 0) return null;

            AnimationClip Achar(params string[] nomes)
            {
                foreach (var nome in nomes)
                    foreach (var c in clipes)
                        if (c.name.ToLowerInvariant() == nome) return c;
                return null;
            }

            ArtePaleta.GarantirPasta(PastaAnimacoes);
            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{PastaAnimacoes}/{item.prefab}.controller");
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            var maquina = ctrl.layers[0].stateMachine;

            var parado = Achar("idle");
            var andando = Achar("walk");
            var correndo = Achar("run", "sprint", "gallop");
            if (parado != null)
            {
                var estado = ctrl.CreateBlendTreeInController("Locomocao", out var arvore);
                arvore.blendParameter = "Velocidade";
                arvore.useAutomaticThresholds = false;
                arvore.AddChild(parado, 0f);
                if (andando != null) arvore.AddChild(andando, .5f);
                if (correndo != null) arvore.AddChild(correndo, 1f);
                maquina.defaultState = estado;
            }
            foreach (var c in clipes)
            {
                if (c == parado || c == andando || c == correndo) continue;
                maquina.AddState(c.name).motion = c;
            }
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        public static void GerarPrefabs(params string[] kits)
        {
            foreach (var item in Itens)
            {
                if (!Entra(item, kits)) continue;
                var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(item.CaminhoModelo);
                if (modelo == null) continue;
                ArtePaleta.GarantirPasta(Path.GetDirectoryName(item.CaminhoPrefab).Replace('\\', '/'));

                var raiz = new GameObject(item.prefab);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
                inst.name = "Modelo";
                inst.transform.SetParent(raiz.transform, false);
                inst.transform.localScale *= item.escala;
                if (item.giroY != 0f) inst.transform.Rotate(0, item.giroY, 0, Space.World);

                if (item.partes != "")
                {
                    // Pra tirar filhos é preciso soltar a instância do modelo; as malhas continuam vindo do arquivo.
                    PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var manter = new HashSet<string>(item.partes.Split(';'));
                    for (int i = inst.transform.childCount - 1; i >= 0; i--)
                        if (!manter.Contains(inst.transform.GetChild(i).name))
                            Object.DestroyImmediate(inst.transform.GetChild(i).gameObject);
                }
                if (item.tamanho > 0f)
                {
                    var m = Limites(inst);
                    inst.transform.localScale *= item.tamanho / Mathf.Max(m.size.x, m.size.y, m.size.z);
                }

                if (item.animado)
                {
                    var animator = inst.GetComponent<Animator>();
                    if (animator == null) animator = inst.AddComponent<Animator>();
                    animator.runtimeAnimatorController = CriarControlador(item);
                    animator.applyRootMotion = false;
                }

                // Base no chão; fora da natureza, também centraliza (vários FBX vêm com o pivô deslocado).
                // Nas plantas o pivô fica onde o autor pôs: no tronco, com a raiz um pouco enterrada.
                // Quem tem esqueleto também fica como veio: o pivô já é entre os pés.
                if (item.categoria != "Natureza" && !item.animado)
                {
                    var rends = inst.GetComponentsInChildren<Renderer>();
                    var b = Limites(inst);
                    bool rente = item.rente || item.arquivo.StartsWith("Decal_");
                    float y = rente ? 0.03f : 0f;
                    inst.transform.position += new Vector3(-b.center.x, y - b.min.y, -b.center.z);
                    if (rente)
                        foreach (var r in rends) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                PrefabUtility.SaveAsPrefabAsset(raiz, item.CaminhoPrefab);
                Object.DestroyImmediate(raiz);
            }
            if (kits.Length == 0 || System.Array.IndexOf(kits, "Lama") >= 0) GerarChaoDeLama();
            AssetDatabase.SaveAssets();
        }

        static Bounds Limites(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return b;
        }

        // Mancha de lama com poças (4 m de diâmetro) pra pôr por cima do asfalto nas áreas ainda alagadas.
        static void GerarChaoDeLama()
        {
            const string textura = "Assets/Art/Enchente/Texturas/Poca/lama_com_pocas.jpg";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(textura) == null) return;
            ArtePaleta.ConfigurarTextura(textura, false, true, 1024, false);
            var mat = ArtePaleta.MatTextura("Lama_Com_Pocas", textura, false);

            var raiz = new GameObject("Chao_De_Lama");
            // Disco bem fino em vez de quadrado, pra mancha não ter cantos.
            var chao = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chao.name = "Modelo";
            Object.DestroyImmediate(chao.GetComponent<Collider>());
            chao.transform.SetParent(raiz.transform, false);
            chao.transform.localPosition = new Vector3(0, 0.015f, 0);
            chao.transform.localScale = new Vector3(4, 0.005f, 4);
            var rend = chao.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{PastaPrefabs}/Enchente/Chao_De_Lama.prefab");
            Object.DestroyImmediate(raiz);
        }

        // Os kits de carros e de comida usam um atlas de cores. Em vez de trocar o material por parte,
        // o atlas é repintado: cada bloco de cor vira a cor da paleta mais parecida, mantendo o degradê.
        static void GerarAtlas(string origem, string destino)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(origem));
            int w = src.width, h = src.height;
            var px = src.GetPixels();
            const int bw = 32, bh = 128;
            for (int by = 0; by < h / bh; by++)
                for (int bx = 0; bx < w / bw; bx++)
                {
                    Color soma = Color.clear;
                    for (int y = 0; y < bh; y++)
                        for (int x = 0; x < bw; x++)
                            soma += px[(by * bh + y) * w + bx * bw + x];
                    Color media = soma / (bw * bh);
                    if (media.r + media.g + media.b < 0.05f) continue;

                    Color alvo = CorDaPaleta(media);
                    Color.RGBToHSV(media, out _, out _, out float vMedio);
                    for (int y = 0; y < bh; y++)
                        for (int x = 0; x < bw; x++)
                        {
                            int i = (by * bh + y) * w + bx * bw + x;
                            Color.RGBToHSV(px[i], out _, out _, out float v);
                            float k = Mathf.Clamp(v / Mathf.Max(vMedio, 0.01f), 0.88f, 1.1f);
                            px[i] = new Color(Mathf.Clamp01(alvo.r * k), Mathf.Clamp01(alvo.g * k), Mathf.Clamp01(alvo.b * k), 1f);
                        }
                }
            var saida = new Texture2D(w, h, TextureFormat.RGBA32, false);
            saida.SetPixels(px);
            saida.Apply();
            File.WriteAllBytes(destino, saida.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(saida);
        }

        static Color CorDaPaleta(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float g = h * 360f;
            bool azulado = g >= 190f && g < 265f;

            if (azulado && v > 0.85f && s > 0.08f && s < 0.35f) return ArtePaleta.Cor("Vidro");
            if (s < 0.12f || (azulado && s < 0.3f))
            {
                // Neutros: rampa cinza-azulada, do grafite ao branco.
                if (v > 0.9f) return Color.Lerp(ArtePaleta.Cor("Cinza_Claro"), ArtePaleta.Cor("Branco"), Mathf.InverseLerp(0.9f, 1f, v));
                if (v > 0.7f) return Color.Lerp(ArtePaleta.Cor("Cinza"), ArtePaleta.Cor("Cinza_Claro"), Mathf.InverseLerp(0.7f, 0.9f, v));
                if (v > 0.45f) return Color.Lerp(ArtePaleta.Cor("Cinza_Escuro"), ArtePaleta.Cor("Cinza"), Mathf.InverseLerp(0.45f, 0.7f, v));
                return Color.Lerp(ArtePaleta.Cor("Grafite"), ArtePaleta.Cor("Cinza_Escuro"), Mathf.InverseLerp(0.2f, 0.45f, v));
            }
            if (g < 10f || g >= 340f) return ArtePaleta.Cor("Vermelho");
            if (g < 40f)
            {
                if (s < 0.3f) return ArtePaleta.Cor("Areia");
                if (v < 0.5f) return ArtePaleta.Cor("Marrom_Escuro");
                if (v < 0.8f) return ArtePaleta.Cor("Marrom");
                if (s < 0.5f) return ArtePaleta.Cor("Marrom_Claro");
                if (s < 0.65f) return ArtePaleta.Cor("Terracota");
                return ArtePaleta.Cor("Laranja");
            }
            if (g < 70f) return s < 0.6f ? ArtePaleta.Cor("Mostarda_Clara") : ArtePaleta.Cor("Mostarda");
            if (g < 170f) return v < 0.65f ? ArtePaleta.Cor("Verde_Escuro") : ArtePaleta.Cor("Verde");
            if (g < 200f) return ArtePaleta.Cor("Petroleo_Claro");
            if (g < 290f) return (v < 0.8f || g > 235f) ? ArtePaleta.Cor("Petroleo_Escuro") : ArtePaleta.Cor("Petroleo");
            return ArtePaleta.Cor("Rosa");
        }
    }
}
