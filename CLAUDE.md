# Projeto: Cidade Viva

Jogo educativo 3D para crianças de 6 a 9 anos, parte do projeto de extensão Glotec (UnB) — linha Glotrânsito.

## Nova direção (2026-10-11): o jogo ensina os verbetes do Glotrânsito

**Prazo final: 30/11/2026.** Ponto de retorno antes da mudança: tag git `antes-do-glotransito` (commit `48521db`).

O jogo passa a ensinar os **38 verbetes do glossário Glotrânsito** (<https://glotransito.com>). A enchente levou embora o trânsito de Brasília, e **cada verbete é uma peça a reconstruir**: ao reconstruir a peça, a criança aprende a palavra e a definição dela. Público: crianças de 6 a 9 anos.

**Regra que não tem exceção: a definição de um verbete é sempre copiada do glossário, nunca reescrita.** Nada de resumir, simplificar, corrigir, "melhorar" ou completar uma definição, nem em tela, nem em fala, nem em dica, nem no JSON. O texto vem de `Verbete.definicao` exatamente como está em `Assets/Data/verbetes.json` (inclusive pontuação e espaços). Se a definição precisar mudar, quem muda é o glossário; aqui só se extrai de novo.

**Grupos de verbetes** (`GrupoDeVerbete`):
- `Veiculos` (14): ambulância, automóvel, bicicleta, bicicleta elétrica, bonde, caminhão de bombeiro, caminhão trator, micro-ônibus, motocicleta, motoneta, ônibus, quadriciclo, reboque, viatura.
- `ViasEEstruturas` (14): via, via rural, via urbana, calçada, faixa de pedestre, ciclovia, ciclofaixa, ponte, túnel, viaduto, passarela, passagem subterrânea, retorno, estacionamento.
- `Sinais` (5): semáforo, semáforo de pedestre, sinal verde, sinal amarelo, sinal vermelho.
- `PessoasESeguranca` (4): ciclista, passageiro, cinto de segurança, buzina.
- `Situacao` (1): engarrafamento.

**4 mecânicas planejadas** (feitas: a Oficina de Veículos, com os 14 veículos, e as Obras da Cidade, com os 14 verbetes em dois modos; faltam a Central de Sinais e a Rua Viva):
1. **Oficina de Veículos** — montar o veículo seguindo a definição (grupo `Veiculos`). **Feita em 2026-10-11**, ver o item próprio logo abaixo.
2. **Obras da Cidade** — escolher a estrutura certa para ligar dois pontos (grupo `ViasEEstruturas`). **Feita (partes 1 e 2) em 2026-10-11/12**, ver os itens próprios abaixo.
3. **Central de Sinais** — controlar semáforos de veículos e de pedestres (grupo `Sinais`).
4. **Rua Viva** — passageiro, ciclista, buzina e engarrafamento (grupos `PessoasESeguranca` e `Situacao`).

**Progresso: Álbum Glotrânsito** — uma figurinha por verbete, com a definição e a ilustração do glossário (cena `Album_Glotransito`, abaixo).

**Apresentação dos verbetes** (2026-10-11; reaproveitável por todas as mecânicas):
- **Cartão do verbete**: `CartaoVerbete.Mostrar(idDoVerbete, aoFechar)` (`Scripts/Core`) abre o prefab `Assets/Prefabs/UI_CartaoVerbete.prefab` por cima de qualquer cena (Canvas próprio, `sortingOrder` 500; cria um `EventSystem` se a cena não tiver). Mostra a palavra, a ilustração (ou o quadro cinza "desenho em breve" se o verbete não tiver), a definição literal de `Verbete.definicao`, "Ouvir" e "Continuar". Se o verbete ainda não estava no perfil, desbloqueia (`GameProgress.DesbloquearVerbete`) e mostra o selo "Novo!". Entrada: o cartão voa em arco até o centro, com brilho e estrelinhas (imagens de UI animadas por código, em tempo real, então funciona com `Time.timeScale = 0`). Há uma segunda forma, `Mostrar(id, aoFechar, origemNaTela)`, pra o cartão sair de um ponto da tela. Só um cartão por vez: abrir outro fecha o anterior (e chama o `aoFechar` dele).
- **Narração**: interface `IVozNarradora` (`Disponivel`, `Falar`, `Parar`), entregue por `Narracao.Voz`. `VozDoNavegador` (WebGL) usa o `speechSynthesis` do navegador pelo plugin `Assets/Plugins/WebGL/CidadeVivaVoz.jslib`, com voz pt-BR (ou outra voz em português, se não houver); sem voz em português, `Disponivel` é falso e o botão "Ouvir" some, sem erro (as vozes carregam depois da página, então o cartão confere por 4 s). `VozDoWindows` (Editor e build de Desktop no Windows, desde 2026-10-11) usa a voz de leitura do Windows (System.Speech, voz em português; esta máquina tem a "Microsoft Maria Desktop", pt-BR) num PowerShell escondido, e no Editor também escreve `narração: <texto>` no Console; confere a voz ao iniciar (leva ~1 s, então `Disponivel` começa falso). `VozSilenciosa` ficou só pra outras plataformas. **A narração no navegador ainda não foi testada num build WebGL de verdade, e ninguém ouviu ainda a voz do Windows** (o piloto só confirma que ela foi chamada).
- **Cena `Album_Glotransito`** (em Build Settings; script `AlbumGlotransito` em `Scripts/Album`): 5 abas na ordem do `GrupoDeVerbete`, grade de 7 figurinhas por linha (`FigurinhaDoAlbum`, copiadas do `Modelo_Figurinha` inativo), "X de 38 verbetes" com barra no topo e botão "Voltar" (`VoltarAoHub`). Bloqueada = silhueta cinza com "?" e nome "???" (tocar só balança e explica embaixo); desbloqueada = ilustração e palavra (tocar abre o cartão). Lê do perfil ativo, inclusive Visitante, e se refaz quando `GameProgress.VerbetesMudaram` dispara.
- **Hub**: botão "Álbum" fixo no canto de cima à direita (`UI_Hub/Botao_Album`, `BotaoDoAlbum` em `Scripts/Hub`), com o contador "X/38"; é criado junto com o painel do perfil em `MenuConstrutor.PainelDoHub`.
- **Geração**: `AlbumConstrutor.cs` (menu **Cidade Viva > Verbetes > Reconstruir cartão de verbete e cena do álbum**) refaz o prefab e a cena; não editar os dois à mão. Os dois Canvas usam `ScreenMatchMode.Expand`, pra nada ser cortado fora do 16:9.
- **Áreas dos verbetes no Hub** (2026-10-11; as 9 áreas de minigame continuam no centro). A primeira versão pôs as quatro áreas em "bolsos" colados por fora da cidade e **a usuária não gostou: cada área tem de ficar num lugar que faça sentido com a mecânica dela**. O mapa foi então ampliado pro norte (`HubConstrutor`: `ChaoDosBairrosNovos`, `BairrosNovos`, tabela `AreasDeVerbetes`): um **Eixo** (duas pistas com calçadão no meio, x = 0, do centro até o Congresso, que foi pra z = 112) cruzado por uma **Avenida** (z = 60, de x = -88 a 60), com ciclovia no lado sul do trecho oeste. `Area_CruzamentoCentral` (0; 50; grupo Sinais): marcador no calçadão, de frente pro cruzamento Eixo x Avenida, que ganha um semáforo por pista quando arrumado. `Area_RuaViva` (-45; 48; PessoasESeguranca + Situacao): pracinha de onde se vê a ciclovia com a família, a Avenida engarrafada (fila de carros, sempre lá) e o ponto de ônibus com passageiros. `Area_Oficina` (-76; 70; Veiculos; carrega `Minigame_Oficina`, `areaName = "Oficina de Veículos"`): pátio com galpão de duas vagas no fim da Avenida. `Area_ObrasDaCidade` (46; 69; ViasEEstruturas; carrega `Minigame_Obras`): rua que acaba no rio; do outro lado a rua segue até o túnel do morro; arrumada, a ponte volta. As duas sem cena (Cruzamento Central e Rua Viva) têm `AreaTrigger.emBreve = true`: não carregam nada, só mostram "<nome>: em breve!" pelo `AvisoDoHub` (painel `UI_Hub/Aviso_Em_Breve`, criado em `MenuConstrutor.PainelDoHub`). Cada área tem `Visual`, quatro grupos `Caos_1..4` (lixo; lama e plantas secas; veículos virados e placas caídas; entulho), quatro `Ordem_1..4` (flores; árvores; tudo no lugar; pássaros e toque final), `Luz` (point light) e `Letreiro` (`LetreiroDeVerbetes`: nome + "X de Y verbetes"). O `ChaosController` (`Scripts/Hub`) conta os verbetes desbloqueados do(s) grupo(s) no perfil ativo, escuta `GameProgress.VerbetesMudaram` e aplica a faixa (0 = 0–25%, 1 = 25–50%, 2 = 50–75%, 3 = 75–100%, 4 = completo): `Caos_i` fica ligado enquanto faixa < i, `Ordem_i` liga quando faixa >= i; a saturação do `Visual` (`SaturacaoDaArea`) e a intensidade da luz acompanham o progresso aos poucos. O céu é um só pra cena inteira, então não muda por área. **Por onde se anda**: a lista de retângulos `Caminhavel` do `HubConstrutor` (centro, Eixo, Avenida, pracinha, rua do rio, pátio); `Limites` gera sozinho as paredes invisíveis na fronteira da união deles e um chão por retângulo (o `Terreno` só vai até ±50). Regra de câmera: nada alto a menos de ~6 m ao sul de um caminho, porque a câmera fica atrás do jogador. Feitos de formas da paleta por falta de modelo: galpão da oficina, morro, boca do túnel, rio; a ponte é a `Obras/Ponte_JK` (gerada pelo `ObrasConstrutor`, que por isso precisa rodar antes do `HubConstrutor` numa máquina nova).
- **Teste sem jogar** (`TesteMenu.cs`, só em Play Mode): **Cidade Viva > Teste > Desbloquear verbete aleatório** (abre o cartão de um verbete ainda bloqueado, pelo mesmo caminho do jogo) e **Bloquear todos os verbetes de novo**. **Cidade Viva > Teste > Caos do Hub > <área> > 0% / 50% / 100%** finge um progresso naquela área do Hub (`ChaosController.Simular`, não mexe no perfil) e **Voltar ao progresso real** desfaz. Atenção: com um perfil de verdade ativo, isso grava no arquivo do perfil.

**Oficina de Veículos** (`Minigame_Oficina.unity`, 2026-10-11; em Build Settings; entra-se pela `Area_Oficina` do Hub). No padrão do jogo (`MinigameBase`, `LevelConfig`, `UI_SelecaoDeNivel`, `UI_FimDeJogo`, `Ambiente_Padrao`), cena gerada por `OficinaConstrutor.cs` (menu **Cidade Viva > Minigames > Reconstruir a cena da Oficina de Veículos**; não editar à mão). `nomeDaArea = "Oficina de Veículos"`.
- **Como joga**: a tela mostra a palavra e a definição literal do verbete, com a ilustração do glossário. **A definição só é lida em voz alta quando a criança aperta "Ouvir"** (pedido da usuária: o jogo vai rodar em sala cheia, e leitura automática vira barulho; não voltar a narrar sozinho). A criança arrasta peças das duas prateleiras até a bancada, onde encaixam no chassi; peça que a definição não pede volta pra prateleira com um balão ("A definição não fala de sirene. Tente outra peça!" ou "A definição diz: "duas rodas". Já está completo!"). Não existe derrota: erro, dica e tempo estourado só custam estrela. Veículo completo vai pra uma pista pequena, onde a criança dirige (setas/WASD ou botões de segurar) e pega 3 moedas; depois abre `CartaoVerbete.Mostrar`, que põe o verbete no álbum. Teclado na montagem: ←/→ escolhem a peça, Enter/Espaço levam pra bancada, D = dica, O = ouvir.
- **Receitas**: `VeiculoReceita` (ScriptableObject), um asset por veículo em `Assets/Resources/Veiculos/Receita_<id>.asset`, carregados por `Resources.LoadAll`. **Pra acrescentar um veículo basta criar outro asset lá** (Create > Cidade Viva > Oficina: receita de veículo), com `idDoVerbete`, `nivel` (em qual nível aparece), `ordem`, `chassi` (`DuasRodas` ou `QuatroRodas`) e os `itens`. **A receita sai SOMENTE da definição do verbete**: cada item guarda o `trechoDaDefinicao` que justifica a peça (e `verbeteDoTrecho` quando a definição cita outro verbete, como "Bicicleta com motor"), mais `quantidade`, `ouMais` ("quatro ou mais") e `tambemAceita`. Não entra peça que a definição não menciona (bicicleta não tem assento nem guidão aqui). O menu **Cidade Viva > Verbetes > Conferir receitas da Oficina com as definições** (rodado também a cada reconstrução) acusa trecho que não exista letra por letra na definição. O construtor só cria as 4 receitas se faltarem: não sobrescreve receita revisada.
- **Os 14 veículos estão na Oficina** (2026-10-11; receitas pendentes de revisão da coordenação; a lista completa, com o trecho que justifica cada peça, sai do campo `observacao` de cada asset e do menu de conferência). Distribuição (tabela `Distribuicao` do `OficinaConstrutor`; o menu **Cidade Viva > Verbetes > Aplicar a distribuição padrão dos veículos pelos níveis** reaplica): **1** bicicleta, motocicleta, automóvel; **2** quadriciclo, ônibus, ambulância; **3** viatura, caminhão de bombeiro, bonde; **4** caminhão trator, reboque; **5** bicicleta elétrica, motoneta, micro-ônibus. O que entrou de novo na receita (`VeiculoReceita`/`ItemDaReceita`):
  - `basePadrao`: peça que a definição NÃO cita mas o veículo precisa pra existir (4 rodas e 1 motor da ambulância, do caminhão de bombeiro e do caminhão trator; 4 rodas do bonde). Sem trecho; o relatório e a `observacao` marcam como "base padrão, não vem da definição". **O caminhão de bombeiro não tem nenhuma peça vinda da definição.**
  - `opcional`: a definição diz "pode ter ou não" (cobertura do quadriciclo): encaixa sem erro, não é exigida.
  - `proibidas`: peças que a definição exclui, com o trecho (reboque: motor, "sem motor"; bonde: motor comum, "com motor elétrico"). Balão: `A definição diz: "sem motor". Essa peça não serve.`
  - `comoEhCitado`: a motoneta é "Tipo de moto" e o glossário não tem o verbete "moto"; motor e rodas vêm da definição de motocicleta (que termina com "Ver moto").
  - `andaEmTrilhos` (bonde: na pista segue os trilhos, sem volante), `puxadoPor` (reboque → `caminhao_trator`: na pista ele fica parado até a criança apertar **Engatar**; o caminhão trator dá ré, prende, e é ele que ela dirige, com o reboque atrás), `velocidadeNaPista` (motoneta 0,7: "mais devagar"), `variante` ("Motoneta": assento de pernas juntas).
  - Chassi novo `UmEixo` (reboque) e peça nova `Cobertura`. Peças sem desenho no modelo (trilho, engate, cobertura) continuam à mostra depois de encaixadas.
  - **Modelos**: ambulância, caminhão de bombeiro e ônibus usam os de `Prefabs/Art/Carros`; o bonde usa o `Metro` e o micro-ônibus usa o `Carro_Van` (substitutos); quadriciclo, reboque, caminhão trator e motoneta são de formas simples (`Oficina/Pronto_*`), provisórios até aparecer modelo melhor.
  - Pendências conhecidas: o ônibus ("quatro ou seis rodas") também deixa terminar com 5; o passo de engatar é um botão, não um arrasto.
- **As 4 primeiras receitas**: bicicleta = 1 pedal ("se move quando a pessoa pedala") + 2 rodas ("possui duas rodas"); bicicleta elétrica = 2 rodas e 1 pedal (da definição de bicicleta, citada em "Bicicleta com motor") + 1 motor ("com motor"); motocicleta = 1 motor ("com motor") + 2 rodas ("duas rodas"); automóvel = 1 motor ("com motor") + 4 rodas ou mais ("com quatro ou mais rodas"). Onde a definição diz só "motor", o encaixe aceita o motor comum e o elétrico.
- **Ajustes de 2026-10-11, depois de a usuária jogar**: (1) rodas do chassi de quatro rodas — o encaixe é escolhido pela distância medida ao longo do veículo e os 4 cantos enchem antes do par do meio (`ChassiDaOficina.EncaixeLivre`), então ficam sempre 2 de cada lado (antes juntavam 3 do lado de cá, porque as de lá ficam escondidas atrás); (2) o nível 5 deixou de repetir motocicleta e automóvel: ganhou a **viatura** (`Receita_viatura`: 1 motor "com motor" + 4 rodas "quatro rodas" + 1 luzes "tem luzes que acendem e apagam"; "faz um barulho" não virou sirene), também pendente de revisão; (3) **veículo pronto**: `VeiculoReceita.modeloPronto` (+ `escalaDoModeloPronto`) é o prefab do veículo inteiro que substitui a estrutura da bancada na hora do teste na pista (bicicleta = `Bicicleta_Q1`, bicicleta elétrica = `Oficina/Pronto_Bicicleta_Eletrica`, motocicleta = `Moto`, automóvel = `Carro_Sedan`, viatura = `Carro_Policia`); é só aparência, não muda a receita, e receita sem modelo continua com a estrutura; `giroDoModeloPronto` acerta a frente do modelo (as bicicletas e a `Moto` vêm com a frente pra −Z e usam 180, senão andam de ré; os carros do Kenney já vêm pra +Z) — **ao ligar um modelo novo, conferir a frente**; (4) o painel da definição mostra a **ilustração do glossário** à esquerda; (5) **nome da peça**: uma etiqueta aparece em cima da peça com o mouse em cima, com o dedo segurando ou escolhida pelo teclado (`OficinaHUD.Etiqueta`). **Faltam modelos** pra veículo pronto de: motoneta, quadriciclo, bonde, caminhão trator, micro-ônibus e reboque (ambulância, caminhão de bombeiro e ônibus já existem em `Prefabs/Art/Carros`).
- **Molde do veículo na bancada** (2026-10-11, a pedido da usuária: a estrutura genérica "não parecia com nada"): quando a receita tem `modeloPronto`, a bancada mostra o próprio modelo do veículo todo cinza (`ChassiDaOficina.VestirMolde`, material `Pal_Cinza_Claro`), ajustado pra `OficinaConfig.comprimentoDoVeiculoNaBancada` (2,1 m; `escalaDoModeloPronto` virou só um ajuste fino). Os encaixes são reposicionados medindo o modelo (rodas nas rodas, assento em cima, luzes na frente...). **Cada peça encaixada "pinta" a parte correspondente do veículo**: a peça solta some e, em volta do encaixe, o modelo aparece com a cor de verdade (shader `CidadeViva/Molde` em `Assets/Art/Shaders/Molde.shader`, material `Assets/Materials/Molde.mat`; o `ChassiDaOficina.Fundir` passa as esferas pintadas por `_MoldeEsferas`/`_MoldeQuantidade`, globais, até 12; o raio de cada tipo de peça está em `RaioDaPintura`). A usuária pediu isso porque a peça grudada por cima do veículo cinza "não parecia completar o desenho". Ao completar, `RevelarMolde` devolve os materiais originais do modelo (tudo colorido); é esse mesmo objeto que vai pra pista. A estrutura genérica (barra/plataforma) só aparece em receita sem modelo. **Peças com formato de duas rodas**: `PecaDaOficina.visualDeDuasRodas` (filho `Visual_DuasRodas`) — roda fina com raios, selim e farol redondo — usado quando `chassi == "DuasRodas"`. **Na pista o painel da definição some** (cobria o fundo da pista).
- **Extras aceitos e peças erradas** (decisão da usuária em 2026-10-11, depois de estranhar o assento ser recusado): `VeiculoReceita.extrasAceitos` lista as peças que o veículo tem na vida real mas a definição não cita. Elas entram na prateleira (nos níveis com peças erradas), **encaixam sem contar erro**, não são exigidas pra completar, e o recado diz "Encaixou! A definição não fala de assento, mas pode ter." **Essa lista não vem do glossário, é escolha da equipe**: bicicleta e bicicleta elétrica = assento; motocicleta = assento, luzes; automóvel = assento, luzes, carroceria; viatura = assento, sirene, carroceria. As peças erradas passaram a ser só coisas que o veículo não tem: foram criadas `Asa`, `Helice`, `Ancora`, `VelaDeBarco` e `Remo` (nenhum verbete pede), e a ordem em `OficinaConfig.ordemDasPecasErradas` é asa, trilho, hélice, âncora, vela de barco, remo, sirene, pedal, motor (pula o que a receita pede ou aceita como extra). Balão da errada: "Bicicleta elétrica não tem asa. Tente outra peça!". O `engate` não é oferecido como errado. Como o veículo fica pronto assim que as peças exigidas encaixam, extra só entra antes disso.
- **Peças** (`TipoDePeca`): roda, pedal, motor, motor elétrico, trilho, sirene, luzes, carroceria, engate, assento. Não havia prefab de nenhuma em `Assets/Art`, então são formas da paleta geradas pelo construtor em `Assets/Prefabs/Art/Oficina` (`Peca_*`, `Chassi_DuasRodas`, `Chassi_QuatroRodas`, `Bancada`, `Prateleira`). Os dois chassis têm encaixe pra todos os tipos de peça (o de quatro rodas tem 6 encaixes de roda).
- **Níveis** (`Niveis_Oficina`; os veículos de cada nível são as receitas com aquele `nivel`, ver a distribuição acima): 1 = só as peças certas e dica sempre visível (`dica_visivel`); 2 = +3 peças erradas, botão "Dica"; 3 = +4 erradas, sem dica (`sem_dica`); 4 = +5 erradas, 60 s por veículo; 5 = +4 erradas, peças parecidas (`pecas_parecidas`: roda sobrando e os dois motores) e 60 s por veículo. Um nível sem receita própria revisa os últimos veículos dos níveis anteriores (extra `fases_de_revisao`). `quantidadeDeItens` = peças erradas; `tempoEmSegundos` = tempo por veículo, que só começa na primeira peça tocada.
- **Estrelas** (`OficinaManager.CalcularEstrelas`): três critérios mostrados no canto da tela — sem erro, no tempo, sem usar dica. 3 estrelas = os três; 2 = dois deles; 1 = concluiu. Pontuação = 100 por veículo − 10 por erro (mínimo 40).
- **Scripts** em `Scripts/Minigames/Oficina`: `OficinaManager` (regras; ações públicas `PegarPeca`, `ColocarPeca`, `PedirDica`, `OuvirDefinicao`), `OficinaPista` e `BotaoDaPista` (teste), `OficinaHUD`, `PecaDaOficina`, `EncaixeDaOficina`, `ChassiDaOficina`, `VeiculoReceita`, `OficinaConfig` (`Assets/Configs/Oficina_Config.asset`: textos, sons, pontuação). Som novo sintetizado pelo construtor: `Assets/Audio/Efeitos/motor_ligando.wav`.
- **Testado** por piloto em Play Mode (os 5 níveis em sequência, 3/2/1 estrelas, recusas, dica, relógio, pista, cartão e álbum). **Não testado**: arrastar com mouse/toque (o piloto chama `ColocarPeca` direto) e build WebGL.

**Obras da Cidade, parte 1** (`Minigame_Obras.unity`, 2026-10-11; em Build Settings; entra-se pela `Area_ObrasDaCidade` do Hub; `nomeDaArea = "Obras da Cidade"`). No padrão do jogo, cena gerada por `ObrasConstrutor.cs` (menu **Cidade Viva > Minigames > Reconstruir a cena das Obras da Cidade**; não editar à mão). Verbetes desta parte: ponte, túnel, viaduto, passarela, passagem subterrânea e faixa de pedestre.
- **Como joga**: cada nível tem de 2 a 4 problemas. Um problema é um lote de 20 x 26 m com o caminho cortado por um obstáculo (rio, morro ou via) e alguém esperando pra passar (veículo, pedestre ou animal). A criança toca numa estrutura da barra de baixo (ícone = ilustração do glossário), o que abre à esquerda a **definição literal** com o botão "Ouvir" (narração só ao apertar), e depois toca no lugar que brilha em cima do obstáculo. A obra sobe peça por peça, com poeira e som, e quem passa testa: se serve, atravessa, comemora e (na primeira vez de cada estrutura na partida) abre o `CartaoVerbete`; se não serve, para antes, faz "ué?" e um balão explica com um trecho da definição (`A definição de túnel diz: "através de outras vias, morros e montanhas". Aqui é um rio!`), a obra some e ela tenta de novo. Não existe derrota: erro, dica e tempo estourado só custam estrela. Câmera elevada que acompanha o problema da vez; arrastar com o mouse (ou Q/E) gira um pouco. Teclado: 1 a 4 escolhem a estrutura, Enter/Espaço constroem, Tab troca de problema, D = dica, O = ouvir.
- **Regras das estruturas** (`ObrasConfig.regras`, `RegraDaEstrutura`; **derivadas SOMENTE das definições**, cada uma com os dois trechos literais que a justificam): ponte = água; veículos, pedestres e animais. Túnel = outras vias e morros; veículos ou pessoas (não animal). Viaduto = outras vias; veículos. Passarela = vias; pedestres. Passagem subterrânea = via; pedestre ou animal. Faixa de pedestre = via; pedestre (os carros param antes da faixa e só então ele atravessa: "quando o veículo para"). Quem decide se a estrutura serve em jogo são essas regras, não o campo `correta` do problema.
- **Problemas**: `ObraProblema` (ScriptableObject), um asset por problema em `Assets/Resources/Obras/Problema_<id>.asset`, com `nivel`, `ordem`, `obstaculo`, `ruaComum` (só aparência: rua estreita em vez de avenida; as definições não distinguem as duas), `quemPassa`, `nomeDeQuemPassa`, `modeloDeQuemPassa`, `opcoes`, `correta`, `trechoDaDefinicao` e `rotaDaAmbulancia`. **Regra de ouro: entre as opções só UMA pode servir.** Por isso o túnel nunca é oferecido junto com o viaduto, a passarela, a passagem ou a faixa quando o obstáculo é via e passa veículo ou pedestre (ele também serviria), e pedestre atravessando via nunca tem duas entre passarela, passagem, faixa e túnel. O menu **Cidade Viva > Verbetes > Conferir problemas das Obras com as definições** (rodado a cada reconstrução; grava a lista em `Temp/obras_problemas.txt`) acusa problema com zero ou duas opções que servem, `correta` diferente da regra ou trecho que não exista letra por letra na definição. O construtor só cria os problemas e as regras se faltarem: não sobrescreve o que foi revisado. **Os 13 problemas estão pendentes de revisão da coordenação.**
- **Níveis** (`Niveis_Obras`): 1 = rio/ponte e morro/túnel, 2 opções, dica sempre visível (`dica_visivel`); 2 = avenida com táxi (viaduto) e com menina (passarela), 2 opções; 3 = avenida com cachorro (passagem subterrânea) e rua com vovô (faixa de pedestre), 3 opções; 4 = três problemas com 4 opções parecidas (rio/moça, morro/menino, avenida/caminhonete); 5 = 180 s pro nível inteiro, três trechos em fila (rio, morro, avenida) que a mesma ambulância atravessa na ordem até o hospital, mais um cachorro atravessando a rua ao lado. O relógio só começa na primeira escolha e para com o cartão aberto. Estrelas (`ObrasManager.CalcularEstrelas`): sem erros, no tempo, sem dica (3 = os três; 2 = dois; 1 = concluiu). Pontuação = 100 por problema − 10 por erro (mínimo 40).
- **Scripts** em `Scripts/Minigames/Obras`: `ObrasManager` (regras; ações públicas `EscolherOpcao`/`EscolherEstrutura`, `TocarNoLocal`, `Construir`, `SelecionarProblema`, `PedirDica`, `OuvirDefinicao`; consultas `Serve`, `Explicacao`, `MotivoPraNaoEscolher`), `ObrasHUD`, `BotaoDeEstrutura`, `ObraEstrutura` (peças que sobem e o caminho da travessia), `ObraTransito` (carros da via, param pra faixa), `ObraLocal` (lugar destacado), `ObraProblema`, `ObrasConfig` (`Assets/Configs/Obras_Config.asset`: regras, medidas, câmera, textos, sons).
- **Arte**: não havia modelo de nenhuma estrutura, então ponte, túnel, viaduto, passarela, passagem subterrânea, faixa, morro, rio, vias e hospital são formas da paleta geradas pelo construtor em `Assets/Prefabs/Art/Obras` (`Estrutura_<id>`, `Obstaculo_*`, `Lote`, `Local_Da_Obra`, `Hospital`); quem passa, carros, casas e árvores são prefabs de arte. **A ponte tem o desenho da Ponte JK** (pedido da usuária em 2026-10-11): três arcos brancos cruzando o tabuleiro em diagonal, com cabos, feita por `ObrasConstrutor.MontarPonteJK`; o prefab `Obras/Ponte_JK` é a mesma ponte sem o script, usada na área das Obras do Hub.
- **Testado** por piloto em Play Mode (os 5 níveis em sequência; 3, 2 e 1 estrela; todas as opções erradas do nível 4 com o balão de cada uma; construir sem escolher; ordem da ambulância; cartões e os 6 verbetes desbloqueados; Console sem erros). **Não testado**: tocar com mouse/dedo no botão e no lugar destacado (o piloto chama os métodos direto), girar a câmera arrastando, ouvir sons e narração, e build WebGL. A parte 2 (os outros 8 verbetes) está no item seguinte.

**Obras da Cidade, parte 2: modo "Planejar a rua"** (2026-10-12; mesma cena `Minigame_Obras`, mesma barra, mesmo `ObraProblema`). Verbetes: via, via rural, via urbana, calçada, ciclovia, ciclofaixa, retorno e estacionamento.
- **Dois modos na mesma cena**: `ObrasManager.Modo` (estático; 0 = "Pontes e Passagens", 1 = "Planejar a rua"). No `Awake`, antes da base, o manager troca `niveis` e `nomeDaArea` pelos do modo (`niveisPorModo`, `areasPorModo`): cada modo tem os seus 5 níveis (`Niveis_Obras` e `Niveis_ObrasRuas`) e o seu progresso no perfil ("Obras da Cidade" e "Planejar a rua"). A seleção de níveis ganhou duas abas no alto (`Canvas/Abas_Dos_Modos`, por cima do prefab `UI_SelecaoDeNivel`, que não mudou); tocar numa aba chama `TrocarModo(i)`, que recarrega a cena na seleção do outro modo. `ObraProblema.modo` diz de qual modo é o problema.
- **Situações** (valores novos do enum `ObstaculoDaObra`, sempre no fim): `CampoECidade` (fazenda de um lado, cidade do outro), `DentroDaCidade`, `LugarQualquer` (lago e mata: nem cidade nem fazenda), `BeiraDaRua` (rua pronta com carros; a obra vai na beira direita, em `ObrasConfig.xDaBeira`), `SeparadoDoTransito` (parque, com a rua dos carros ao longe), `MudarDeSentido` (avenida de duas pistas com canteiro) e `GuardarVeiculos` (loja com terreno vazio e três carros). `QuemPassaNaObra` ganhou `Ciclista`. Cada situação tem um cenário próprio (`Obras/Situacao_<nome>`, em `ObrasManager.modelosDeSituacao`); os enfeites do lote das pontes ficam no grupo `Enfeites`, desligado neste modo. `ObraProblema.enunciado` é a frase do alto da tela.
- **Regras** (também em `ObrasConfig.regras`, com os trechos literais): via = qualquer ligação ("para se movimentarem de um lugar para o outro"); veículos, pessoas e animais. Via rural = fazenda e cidade ("liga propriedades rurais às cidades"). Via urbana = dentro da cidade ("dentro da cidade, que fica próxima a casas, prédios e lojas"). Calçada = beira da rua ("Parte de uma rua"); pedestres ou animais. Ciclovia = longe dos carros ("está em via separada do trânsito dos veículos"); ciclista. Ciclofaixa = beira da rua ("está ao lado da via dos outros veículos"); bicicletas. Retorno = mudar de sentido; veículo. Estacionamento = guardar veículos; veículo. **O que NÃO vem direto das definições** (está na `observacao` de cada problema e no relatório da conferência): quem passa na via rural e na via urbana é herdado da definição de via (`RegraDaEstrutura.verbeteDoTrechoDeQuem = "via"`); o ciclista conta como veículo na via porque a definição de bicicleta começa com "Veículo"; o cenário "nem cidade nem fazenda"; a leitura de "parte de uma rua"/"ao lado da via" como beira de rua com carros; a avenida com canteiro do retorno; "temporariamente" (estacionamento) não aparece no jogo; e a **aparência** das peças (terra e cerca na via rural, meio-fio e poste na via urbana) é escolha do jogo. Regra de ouro: a via geral nunca é oferecida junto com a via rural ou a urbana onde elas servem.
- **Diferença ciclovia x ciclofaixa na obra pronta**: a ciclofaixa é uma faixa vermelha colada no asfalto, no mesmo nível, separada dos carros só por linha branca e tachões; a ciclovia é uma pista própria entre dois canteiros com arbustos e placa, e o problema dela fica no parque, com os carros lá longe. O nível 3 põe as duas lado a lado.
- **Travessias especiais**: `ObraEstrutura.caminhoCompleto` (o caminho já vai até onde quem passa termina: calçada, ciclofaixa e ciclovia percorrem o lote inteiro; no retorno o carro sobe pela pista de cá, vira na abertura do canteiro e volta pela de lá) e `ObraEstrutura.vagas` (estacionamento: os dois carros de `Figurantes` do cenário também vão cada um pra sua vaga, `ObrasManager.Guardar`).
- **Níveis** (`Niveis_ObrasRuas`): 1 = fazenda/via rural e cidade/via urbana, 2 opções, dica visível; 2 = via geral (cachorro, lago e mata) e calçada (vovó); 3 = ciclofaixa (rua) e ciclovia (parque); 4 = retorno, estacionamento e calçada pra cachorro, 4 opções; 5 = 200 s, quatro obras (via rural, via urbana, calçada pra menina e ciclofaixa pro ciclista, estes dois em lotes vizinhos da mesma situação de rua). **Os 13 problemas deste modo estão pendentes de revisão da coordenação.**
- **Arte**: tudo de formas da paleta em `Assets/Prefabs/Art/Obras` (`Estrutura_<id>`, `Situacao_<nome>`), mais o `Ciclista` (bicicleta + menino parado em cima: falta modelo de ciclista pedalando). A ciclovia não tem ilustração no glossário, então o botão e o painel dela ficam sem desenho.
- **Testado** por piloto (modo das pontes de novo no nível 1; troca de aba; os 5 níveis de Planejar a rua, com todas as opções erradas dos níveis 3 e 4 e o balão de cada uma; retorno termina na outra pista virado pro sentido contrário; os três carros ficam nas vagas; os 8 verbetes desbloqueados; Console sem erros). **Não testado**: toque de verdade nas abas, nos botões e no lugar destacado; sons; tempo estourado; build WebGL. Em janela quase quadrada o HUD se sobrepõe (foi pensado pra 16:9).

**Situação dos minigames antigos** (nenhum foi apagado nem alterado nesta etapa):
- **Concluídos, a reaproveitar:** Corre, Correndo! e Travessia Segura.
- **A substituir:** Onde Eu Vou?, Recicla Comigo!, Pedal Seguro, Sobe, Sobe!, Semáforo Mágico, Olho Vivo! e o Guarda o Sinal (que nunca chegou a ser criado).

**O que já existe da nova direção:**
- `Assets/Data/verbetes.json`: os 38 verbetes extraídos do site em 2026-10-11 (`id` sem acento, `palavra`, `definicao`, `grupo`, `urlDaIlustracao`, `arquivoDaIlustracao`, `semIlustracao`). O site é uma aplicação que carrega os dados depois de abrir; a lista veio da mesma consulta pública que a página inicial faz (`https://glotransito.com/api/apps/68fbc460d6abe1f1a7afda6c/entities/Entry`).
- `Assets/Art/Verbetes/`: 36 ilustrações do glossário, com o `id` como nome (35 `.png` e `bonde.jpg`), importadas como sprite em 1024 px. **Sem ilustração no site: `ciclovia` e `passageiro`** (`semIlustracao: true`); não inventar imagem pra eles sem combinar com a equipe.
- `Verbete` e `VerbeteDatabase` (`Scripts/Core`): `VerbeteDatabase.Instancia` (asset `Assets/Resources/VerbeteDatabase.asset`, com o JSON e os sprites) dá `Todos`, `Buscar(palavraOuId)` (ignora maiúsculas, acentos e separadores) e `ListarPorGrupo(grupo)`; `verbete.ilustracao` é o sprite (nulo se não houver).
- Perfil: `Perfil.verbetes` (lista de `ProgressoDoVerbete { id, desbloqueado }`) vai no `perfil_<id>.json`; perfis antigos, sem o campo, carregam com a lista vazia. O jogo usa `GameProgress.DesbloquearVerbete(id)`, `VerbeteDesbloqueado(id)` e `ContarVerbetesDesbloqueados()`; o evento `GameProgress.VerbetesMudaram` avisa as telas.
- Menu **Cidade Viva > Verbetes > Atualizar banco de verbetes** (`VerbetesConstrutor.cs`): refaz o asset depois de trocar o JSON ou as imagens.

O que está descrito daqui pra baixo é o jogo como foi construído até 2026-10-11 (hub, perfis, níveis, arte, os 8 minigames). Continua valendo como descrição do que existe no projeto; onde conflitar com esta seção, vale esta.

## Enredo
Uma tempestade alaga Brasília e leva embora toda a sinalização de trânsito. Uma família (pai, mãe, filho mais velho, filha, filho mais novo) se divide para ajudar a reconstruir a cidade.

## Estrutura do jogo
Hub central navegável (mapa estilizado de Brasília) com áreas representando minigames. Cada área começa "cinza/danificada" e fica colorida quando o minigame correspondente é completado.

## Minigames planejados (direção antiga; ver a situação de cada um em "Nova direção")
1. Onde Eu Vou? — drag-and-drop de placas de trânsito pro lugar certo
2. Travessia Segura — atravessar a rua com timing, estilo Frogger simplificado
3. Recicla Comigo! — separar lixo em lixeiras corretas (orgânico, plástico, papel, vidro)
4. Corre, Correndo! — endless runner estilo Subway Surfers, 3 faixas, desvio de obstáculos
5. Pedal Seguro — mesmo estilo runner, tema ciclovia
6. Sobe, Sobe! — plataforma vertical infinita fugindo da água que sobe
7. Semáforo Mágico — jogo de ritmo: verde=correr, amarelo=devagar, vermelho=parar
8. Guarda o Sinal — jogador controla o semáforo pra organizar o trânsito
9. Olho Vivo! — achar erros/situações de risco escondidas numa cena (cinto, lixo, sinalização)

## Estilo visual
Cartoon colorido e fofo, mas não "bebê demais" — referência tipo Toca Boca / Pixar leve. Elementos estilizados de Brasília (arquitetura modernista simplificada).

## Público-alvo
Crianças de 6 a 9 anos — controles simples, sem punição pesada em erros, sessões curtas.

## Stack técnica
Unity 6 (6000.6.0f1), C#, plataforma-alvo Web (WebGL) e Desktop. Render pipeline: **URP 17.6.0** (`com.unity.render-pipelines.universal`); pipeline asset em `Assets/Settings/URP_PipelineAsset.asset` (+ `URP_Renderer.asset`), atribuído em Graphics e em todos os níveis de Quality. Materiais novos devem usar `Universal Render Pipeline/Lit` (cor em `_BaseColor`), nunca `Standard`/`Default-Material` (ficam rosa). UI com UGUI (`com.unity.ugui`); input pelo Input Manager antigo (`Input.GetAxisRaw`).

## Estado atual do projeto
**Sprint 1 concluída.**
- Pastas: `Assets/Scenes`, `Scripts/{Core,Hub,Minigames}`, `Configs`, `Materials`, `Prefabs`, `Art`, `Audio`.
- **Padrão de minigame: leia `Assets/Scripts/PADRAO_MINIGAME.md` antes de criar ou refazer qualquer minigame.** É a regra do que todo minigame precisa ter: cena 3D gerada por um `XxxConstrutor.cs` com `Ambiente_Padrao` e prefabs de `Assets/Prefabs/Art` (sem formas geométricas simples), manager herdando de `MinigameBase`, `LevelConfig` com 5 níveis de dificuldade crescente, `UI_SelecaoDeNivel` e `UI_FimDeJogo` com estrelas, progresso no perfil ativo (inclusive Visitante), sons e partículas, UI com a paleta e as fontes do jogo feita com as peças de `MenuConstrutor`, regras de tela pra 6–9 anos (botões grandes, textos curtos, ação proibida explicada em vez de punida, erro leve) e o checklist de teste. O modelo é o Travessia Segura (o Corre, Correndo! também já segue o padrão); os outros seis minigames ainda estão no formato antigo (2D em Canvas) e precisam ser refeitos nesse padrão.
- **Arquitetura dos minigames** (detalhes em `Assets/Scripts/ARQUITETURA.md`): todo manager herda de `MinigameBase` (`Scripts/Core`; campos `nomeDaArea` e `telaFimDeJogo`; `FinalizarJogo(pontuacao[, titulo])` registra no `GameProgress` e mostra a tela de fim; `ReiniciarJogo()` recarrega a cena). Regras, cores e textos ficam em ScriptableObjects em `Assets/Configs` (um por minigame; classes `*Config` na pasta de cada minigame, menu Create > Cidade Viva); a cena guarda só formas, layout e referências. Todas as cenas usam o prefab `Assets/Prefabs/UI_FimDeJogo.prefab`. Os valores citados abaixo (velocidades, pontos, textos) hoje moram nos assets de config, não nos scripts.
- `Hub.unity` (reconstruído em 2026-10-10 como mini-Brasília, pelo menu **Cidade Viva > Hub > Reconstruir a cidade do Hub**, script `Assets/Editor/ArteCidadeViva/HubConstrutor.cs`; a lógica não mudou): terreno 100x100, Player (`PlayerMovement` com setas/WASD + `HubSpawn`; a cápsula ficou invisível e o visual é o filho `Visual` = `Familia_FilhoMaisVelho_Animado`, animado por `AnimacaoDoJogador`), câmera com `CameraFollow` (desde 2026-10-12 gira em volta do jogador com o mouse, a pedido da usuária: `Giro`, volta inteira, e `Inclinacao`, de -8 a 65 graus. Clicar no cenário prende o mouse no jogo (mexer pros lados gira sem parar, pra cima e pra baixo inclina) e Esc solta, pra usar os botões; com o mouse solto, mexer pros lados ainda gira e o cursor parado na borda esquerda/direita continua girando. A linha `UI_Hub/Dica_Do_Olhar` (`DicaDoOlhar`) ensina isso. Não gira com o cursor em cima da interface, fora da janela, com o jogo pausado ou com o cartão do verbete aberto, e o mouse é solto ao sair do Hub. Sensibilidades no componente: `sensibilidadeDoGiro` 2, `sensibilidadeDaInclinacao` 1,4, `giroNaBorda` 70. As teclas andam em relação ao olhar e o personagem, parado, vira pra onde o mouse aponta; começa olhando pra +Z), 9 áreas `Area_*` numa grade de -20 a 20, nas mesmas posições. Cada `Area_*` continua sendo o objeto com `AreaTrigger` + `BoxCollider` trigger (agora 2,6 m, com o cubo invisível) e ganhou: filho `Visual` (marcador redondo no chão + cenário temático feito de prefabs: placas no Onde Eu Vou?, faixa de pedestre na Travessia, lixeiras no Recicla, pista com obstáculos no Corre, ciclovia no Pedal, plataformas sobre água no Sobe, semáforo gigante no Semáforo Mágico, cruzamento no Guarda o Sinal, lupa no Olho Vivo), filho `Letreiro` (placa com nome e recorde, `LetreiroDaArea`, sempre colorida) e o componente `SaturacaoDaArea`. No `Start`, `AreaTrigger` consulta `GameProgress.FoiCompletado(areaName)` e chama `SaturacaoDaArea.Aplicar(1 ou saturacaoSemCompletar)`: o `Visual` fica cinza ou colorido por `MaterialPropertyBlock` (cor dessaturada; onde há textura, troca pela cópia cinza de `Assets/Art/Texturas_Paleta/Cinza`), sem trocar objeto nem material. **Caos da enchente** (2026-10-11): cada `Area_*` tem também os filhos `Caos` (ligado até completar: placas e semáforos caídos, lama, entulho, lixo, árvores secas nos cantos do quarteirão e estragos nas ruas em volta) e `Arrumado` (ligado depois de completar: placas e semáforos no lugar, árvores verdes, flores), trocados pelo componente `CaosDaArea` (`Scripts/Hub`; `Aplicar(completado)`, chamado no `AreaTrigger.Start` junto com a saturação). Os dois ficam fora do `Visual`, então não ficam cinza. No `HubConstrutor`, cada função de área recebe `(v, c, a)` = Visual/Caos/Arrumado; `CantosDoQuarteirao` e `EstragosNaRua` montam a parte comum a todas. Na cena salva o `Arrumado` fica desligado (cidade no estado inicial). O resto da cidade fica no objeto `Cidade` (gerado; não editar à mão): asfalto e quarteirões de 13 m com ruas de 7 m (eixos em ±10 e ±30), faixas, trânsito parado, prédios em volta, Congresso/Catedral/Museu de primitivas ao fundo (norte) e `Limites` (paredes invisíveis em ±37,5).
- `Minigame_Template.unity`: cena placeholder com câmera e botão "Voltar" (`VoltarAoHub.Voltar()`).
- Navegação: entrar numa área carrega `AreaTrigger.cenaDoMinigame` (hoje todas usam `Minigame_Template`). Ao voltar, `HubSpawn` põe o jogador no `PontoDeRetorno` (10, 1, -10), fora das áreas (o centro do mapa é a `Area_PedalSeguro`). As duas cenas estão em Build Settings.
- `Minigame_OndeEuVou.unity` (cópia do template, em Build Settings; `Area_OndeEuVou` aponta pra ela): Canvas com rua placeholder, 3 `PlacaArrastavel` (PARE/FAIXA/SEMAFORO) e 3 `AlvoPlaca`; `OndeEuVouManager` conta erros (100 − 10 por placa solta no alvo errado; soltar no vazio não conta) e chama `FinalizarJogo` ao encaixar as 3. Cor/rótulo das placas vêm da `OndeEuVou_Config` (por `tipo`). Scripts em `Scripts/Minigames/OndeEuVou`.
- `Minigame_TravessiaSegura.unity` (**refeito em 3D em 2026-10-10**; em Build Settings; `Area_TravessiaSegura` aponta pra ela): primeiro minigame no padrão novo e modelo pros próximos. Cena montada por `Assets/Editor/ArteCidadeViva/TravessiaConstrutor.cs` (menu **Cidade Viva > Minigames**; não editar a cena à mão) com `Ambiente_Padrao`, prefabs de arte e câmera elevada e inclinada. A rua corre em X; a criança (`Familia_Filha_Animado`) vai da calçada de cá (-Z) à de lá (+Z) em passos: posição 0 = calçada, 1..N = pistas, N+1 = chegada; na calçada anda de lado (coluna -1/0/+1; a faixa é a 0). Regras (`TravessiaManager`): só sai da calçada na faixa, depois de "Olhar" (setas vermelhas/verdes por pista, pelo tempo até o próximo veículo chegar na faixa), com o bonequinho verde se houver semáforo e sem ambulância chegando; veículo que encosta = tropeço cômico, volta ao início e perde 1 das 3 vidas (estrelas); 0 vidas = "Quase! Tente de novo". Scripts em `Scripts/Minigames/TravessiaSegura`: `TravessiaManager`, `TravessiaTrafego` (pistas, veículos, parar no sinal, distância entre carros, `Atingiu`, `TempoAteAFaixa`, `AmbulanciaChegando`), `TravessiaVeiculo`, `TravessiaSemaforo` (Vermelho→Fechando→Verde→Piscando), `TravessiaJogador` (passo, olhar, tropeço, comemoração), `TravessiaHUD`, `TravessiaConfig` (o que vale pra todos os níveis: medidas, velocidades-base, textos, sons). A largura da rua muda com o nível: asfalto, pinturas (copiadas dos `Modelo_*` inativos) e o grupo `Lado_De_La` são ajustados no `Start`, e a câmera se enquadra sozinha. Níveis (`Niveis_TravessiaSegura`): 1 = 1 pista lenta; 2 = 2 pistas, começa fora da faixa; 3 = 2 pistas + semáforo; 4 = 3 pistas + ciclovia, sem semáforo; 5 = 3 pistas + ciclovia + semáforo + ambulância (não para no sinal) + carros imprevisíveis. Regras extras: `comecar_fora_da_faixa`, `semaforo`, `ciclista`, `ambulancia`, `imprevisiveis`; extras: `intervaloDeCarros`, `tempoVerde`, `tempoVermelho`, `intervaloDaAmbulancia`. Estrelas por `VidasRestantes`. Controles: ↑/W/Espaço andar, ↓/S voltar um passo, ←/→ andar pela calçada, E/L/Enter olhar; os mesmos em botões grandes na tela.
- `Minigame_ReciclaComigo.unity` (cópia do template, em Build Settings; `Area_ReciclaComigo` aponta pra ela): 4 `Lixeira` embaixo (PAPEL azul, PLASTICO vermelha, VIDRO verde, ORGANICO marrom); `ReciclaManager` instancia um `ItemLixo` por vez a partir do `ItemModelo` inativo da cena (lista de itens e aparência das lixeiras na `ReciclaComigo_Config`), em x aleatório no topo. Lixeira errada devolve o item ao topo e marca `errou`; certa faz o item encolher e sumir. Após 10 descartes: pontuação = 100 × acertos de primeira / 10. Scripts em `Scripts/Minigames/ReciclaComigo`.
- `Minigame_CorreCorrendo.unity` (**refeito em 3D em 2026-10-11**; em Build Settings; `Area_CorreCorrendo` aponta pra ela): segundo minigame no padrão novo. Cena montada por `Assets/Editor/ArteCidadeViva/CorreConstrutor.cs` (menu **Cidade Viva > Minigames**; não editar a cena à mão): câmera atrás da criança (`Familia_FilhoMaisNovo_Animado`), que fica parada em z = 0 enquanto a rua de bairro vem na direção dela — 5 trechos de 24 m (asfalto de 3 faixas em x = −2,4/0/2,4, calçadas, casas, árvores, postes) que a `CorrePista` rola e devolve ao fim da fila, com neblina curta escondendo o reaparecimento. Scripts em `Scripts/Minigames/CorreCorrendo`: `CorreManager` (regras), `CorrePista` (cenário, geração por fileiras e movimento dos objetos), `CorreObjeto` (dados de cada objeto), `CorreJogador` (troca de faixa, pulo, deslize, batida cômica, queda, comemoração), `CorreHUD`, `CorreConfig` (`CorreCorrendo_Config`: tipos de obstáculo com o aviso curto de cada um, medidas, pontuação, textos, sons). Obstáculos são modelos inativos (`Cenario/Modelos/Modelo_<id>`) ligados por `id` à config: `buraco` e `poca` (dá pra pular), `cones`, `barreira` e `carro` (só desviar), `galho` (só deslizar por baixo), `carro_em_movimento` (vem de frente pelo meio e muda pra uma faixa do lado, com buzina). Toda fileira deixa um jeito de passar (faixa livre, ou obstáculo pulável/deslizável quando as 3 faixas estão ocupadas). Moedas na faixa livre (ou no alto, sobre obstáculo pulável); escudo extra aparece de tempos em tempos se faltar algum. 3 escudos: cada batida gasta um (rodopio, pisca, perde o embalo, mostra "Opa! <motivo>"); bater sem escudo = "Quase! Tente de novo". O motivo de cada obstáculo aparece na dica do HUD nas 2 primeiras vezes que o tipo chega perto e em toda batida. Níveis (`Niveis_CorreCorrendo`, meta em metros): 1 = 150 m, só buracos e cones, lento; 2 = 200 m, + poças e carros parados; 3 = 250 m, + barreiras e galhos, duas faixas bloqueadas; 4 = 300 m, chuva e asfalto molhado (só visual) + fileiras cheias; 5 = 350 m, rápido, sequências coladas e carros em movimento. Regras extras: `pocas`, `carros`, `barreiras`, `galhos`, `duas_faixas`, `fileira_cheia`, `sequencias`, `carros_em_movimento`, `chuva`; extras: `metaDeDistancia`, `espacoEntreObstaculos`, `fracaoDeMoedas`. Estrelas calculadas no `CorreManager.CalcularEstrelas`: 1 por chegar, +1 por chegar com pelo menos 1 escudo, +1 por pegar a maioria das moedas (60%). Pontuação = 100 + 10 por moeda + 30 por escudo. Controles: ←/→ ou A/D trocam de faixa, ↑/W/Espaço pula, ↓/S desliza; os mesmos em botões grandes. A pasta `Scripts/Minigames/Runner` (runner 2D antigo) continua existindo porque o Pedal Seguro ainda usa (e o Semáforo Mágico usa o `RunnerEscudos`): `RunnerFaixas`, `RunnerJogador`, `RunnerObjeto`, `RunnerSpawner`, `RunnerEscudos`, `RunnerManager` com `RunnerConfig`.
- `Minigame_PedalSeguro.unity` (cópia do CorreCorrendo antigo, 2D em Canvas, em Build Settings; `Area_PedalSeguro` aponta pra ela): mesma base de runner, ciclovia verde-clara, obstáculos com rótulo (BURACO, PEDESTRE distraído com celular, ANIMAL), power-up CAPACETE (ciano, ~8%) que liga um `Outline` ciano no jogador até segurar uma batida. `nomeDaArea = "Pedal Seguro"`.
- `Minigame_SobeSobe.unity` (cópia do template, em Build Settings; `Area_SobeSobe` aponta pra ela): plataforma vertical infinita em Canvas. Scripts em `Scripts/Minigames/SobeSobe`. Plataformas, jogador e água ficam dentro de `Mundo`, que desce conforme a câmera (só sobe) acompanha o jogador. `SobeJogador`: pulo automático ao cair sobre plataforma (só quica descendo), ←/→ controla o X. `SobeSobeManager`: gera plataformas acima a partir do `Modelo_Plataforma` inativo (30% com `Modelo_Moeda` em cima, +20 pts), remove as de baixo. Nível sobe a cada 15 s (máx. 6): água 80 px/s +12 por nível, largura 240 −15 por nível, distância 160–220 +10 por nível (pulo alcança ~330). A água nunca fica mais de 300 px abaixo do topo da plataforma mais alta já pisada (aparece no pé da tela; parado, pega em ~4 s). Câmera mantém o ponto mais alto do jogador a +150 do centro. Fim: "Você caiu!" ou "A água te alcançou!"; pontuação = altura máxima / 10 + moedas.
- `Minigame_SemaforoMagico.unity` (cópia do template, em Build Settings; `Area_SemaforoMagico` aponta pra ela): rua vista de lado, carro parado na tela em x=−550 e a rua (faixas + `SemaforoDaRua`) se move pra esquerda. Um semáforo por vez; pivô do `SemaforoDaRua` = faixa de parada. 3 vidas no topo (reaproveita `RunnerEscudos`), feedback + barra de tempo acima do carro, pontos/combo/contador no canto, legenda e botões "↑ SEGUIR"/"PARAR" embaixo. `SemaforoManager` (`Scripts/Minigames/SemaforoMagico`, estados Dirigindo→Decidindo→Freando/Parado→Passando): o sinal acende quando a faixa está a `velocidade × janela` do carro (janela 2,5 s −0,08 por semáforo, mín. 1 s; velocidade 320 +12 por semáforo, máx. 600). Verde = ↑/W/`Correr()`; amarelo = não apertar (o carro desacelera sozinho; acerto ao chegar na faixa); vermelho = espaço/`Parar()` (freia até a faixa, espera 1,5 s, abre verde e segue). Tecla errada ou chegar à faixa sem reagir = erro (−1 vida, zera combo); no vermelho o carro sempre para na faixa. Os 3 primeiros semáforos são verde/amarelo/vermelho com dica (tutorial), depois aleatórios. Fim com 0 vidas ou 20 semáforos; pontuação = 10 × acertos + 5 × combo máximo.
- `Minigame_OlhoVivo.unity` (cópia do template, em Build Settings; `Area_OlhoVivo` aponta pra ela): sistema reaproveitável de "achar o erro" em Canvas. Scripts em `Scripts/Minigames/OlhoVivo`. `Cena_Rua` é uma rua desenhada com formas simples de UI (prédios, calçada, rua com uma faixa de pedestre, ciclovia), sem arte final, com 4 erros em meio a coisas certas. A fase fica no asset `OlhoVivo_Fase1_Rua` (`OlhoVivoFaseConfig`), com duas listas de `posicao`/`tamanho` relativos ao centro da `Cena_Rua`: `pontosDeErro` (`PontoDeErro`: `id`, `descricao`, `explicacao`) e `elementosCertos` (`ElementoCerto`: `descricao`, `mensagem`), mais tempo, pontuação, textos e cores. Na cena, `CenaOlhoVivo.visuais` liga cada `id` aos objetos `visualErrado`/`visualCorrigido` (opcionais). Pra outra fase: outro asset + outra cena com os visuais. Erros atuais: carro em cima da faixa (vira carro antes da linha), criança sem cinto no carro azul (aparece o cinto), placa PARE de cabeça para baixo (desvira), pedestre no celular na mesma faixa, atrás do carro (guarda o celular). Certos: motorista de cinto no carro azul (que já passou da faixa), placa 40, lixeira, criança de mão dada, ciclista de capacete. No `Start` cria um `HotspotOlhoVivo` invisível por item a partir do `Modelo_Hotspot` inativo (certos primeiro, erros por cima). Clicar num erro: troca o visual, liga o selo "OK" no canto do hotspot, toca o som de acerto (`somDeAcerto` opcional; sem clipe, um "plim" gerado por código) e mostra a explicação em verde por 3 s. Clicar numa coisa certa só mostra o recado ("Isso está certo!"), sem penalidade; clicar fora ou num erro já corrigido não faz nada. Cronômetro de 30 s; fim ao achar todos (espera 2 s pra última explicação) ou ao zerar o tempo. Pontuação = 15 × erros achados + 40 × fração do tempo que sobrou (máx. 100), via `TelaFimDeJogo`.
- `TelaFimDeJogo` (`Scripts/Core`, prefab `Assets/Prefabs/UI_FimDeJogo.prefab`, usado nos 8 minigames): só apresentação. `Mostrar(dono, titulo, pontuacao, recorde, novoRecorde)` é chamado pelo `MinigameBase`; mostra `Text_Titulo` e `Text_Pontuacao` (pontuação + "Novo recorde!"/recorde); botões "Tentar de novo" (`TentarDeNovo()` → `MinigameBase.ReiniciarJogo()`) e "Voltar ao Hub".
- **Perfis locais** (2026-10-10; detalhes em `Assets/Scripts/ARQUITETURA.md`): sem conta na nuvem. `Menu_Inicial.unity` é a primeira cena do Build Settings (`MenuInicial`, `Scripts/Menu`): "Entrar" (lista os perfis do dispositivo e pede o código de 3 figuras), "Criar meu perfil" (nome até 12 caracteres por teclado na tela, um de 8 bichinhos, código de 3 entre 6 figuras) e "Jogar sem conta" (Visitante, só em memória, com o aviso "Seu progresso não será guardado"). `Perfis` (`Scripts/Core`, estática) guarda um JSON por perfil em `Application.persistentDataPath/perfis/perfil_<id>.json` (no WebGL sincroniza com o IndexedDB pelo plugin `Assets/Plugins/WebGL/CidadeVivaArquivos.jslib`); sem perfil ativo, `Perfis.Ativo` cria um Visitante. No Hub, `UI_Hub/Painel_Do_Perfil` (`PainelDoPerfil`) mostra avatar, nome e o botão "Trocar de perfil". Menu e painel são gerados por `MenuConstrutor.cs` (menu **Cidade Viva > Menu**). Ao testar em Play Mode, apagar depois os perfis de teste em `persistentDataPath/perfis` e zerar `Perfis_ProgressoMigrado` no `PlayerPrefs` se a migração tiver ido pra um perfil de teste.
- `GameProgress` (`Scripts/Core`, classe estática): lê e grava sempre no perfil ativo (não usa mais `PlayerPrefs`, exceto pra migrar o progresso antigo pro primeiro perfil criado). Só o `MinigameBase` chama `GameProgress.FinalizarNivel(nomeDaArea, nivel, pontuacao, estrelas, concluiu)`. Por minigame guarda `estrelasPorNivel` e `recordePorNivel` (sempre o melhor), mais `niveisConcluidos`, o total de `estrelas` e o `recorde` geral, com `<Id>` = nome da área sem espaços/pontuação/acentos (ex.: "Onde Eu Vou?" → `OndeEuVou`). Leitura: `NivelDesbloqueado`, `ObterEstrelasDoNivel`, `ObterRecordeDoNivel`, `ObterRecorde`, `ObterEstrelas`, `ObterNiveisConcluidos`, `FoiCompletado` (= nível 1 concluído).
- **Níveis** (2026-10-10; detalhes em `Assets/Scripts/ARQUITETURA.md`): cada minigame tem 5 níveis num asset `LevelConfig` em `Assets/Configs/Niveis/Niveis_<Minigame>.asset` (por nível: `objetivo`, `velocidade`, `quantidadeDeObstaculos`, `tempoEmSegundos`, `quantidadeDeItens`, `errosPermitidos`, `regrasExtras`, `extras` e o critério de `estrelas` por Pontuacao/Erros/TempoGasto/TempoRestante/VidasRestantes). Ao entrar no minigame, o `MinigameBase` abre o prefab `UI_SelecaoDeNivel` (5 botões com estrelas e cadeado; `Time.timeScale = 0` enquanto escolhe); escolher recarrega a cena já com `NivelAtual`/`Nivel` definidos. Níveis liberam em sequência. O manager lê `Nivel` no `Start` e termina com `FinalizarNivel(ResultadoDoNivel)` (`concluiu = false` = "Quase! Tente de novo", não grava). `UI_FimDeJogo` mostra nível, 3 estrelas e os botões "Voltar ao Hub", "Tentar de novo" e "Próximo nível". **Só o Travessia Segura e o Corre, Correndo! já aplicam o nível** (ver os itens deles acima); os outros seis têm asset, seleção e tela de fim, mas jogam igual nos 5 níveis (estrelas por pontuação) até serem refeitos. Prefabs, assets e ligação nas cenas são gerados por `NiveisConstrutor.cs` (menu **Cidade Viva > Níveis**).
- Cenas e UI são editadas pelo Editor (unity-mcp), não à mão no YAML do `.unity`.
- **Fontes da interface** (2026-10-10): `Assets/Art/UI/Fontes/Baloo2.ttf` (texto normal) e `LilitaOne-Regular.ttf` (títulos e botões), as duas OFL e com acentos. As telas geradas (menu, seleção de níveis, fim de jogo, HUD da Travessia, letreiros do Hub) usam as duas via `MenuConstrutor.Texto`; não usar negrito sintético (`FontStyle.Bold`) com elas, porque aperta os espaços. Peças de UI reutilizáveis (`Botao`, `Imagem`, `Texto`, `NovoCanvas`) ficam em `MenuConstrutor.cs`. Sons sintetizados por script (buzina, sirene, tropeço, campainha de bicicleta, pulo, deslizar, moeda e a música `Audio/Musica/corre_correndo.wav`) em `Assets/Audio/Efeitos/*.wav`.
- Ao gerar cenas por script: carregar assets (configs, sprites) **depois** de `EditorSceneManager.NewScene`, porque trocar de cena descarrega os assets soltos e as referências viram nulas.
- **Look do jogo** (2026-10-10): prefab `Assets/Prefabs/Ambiente_Padrao.prefab`, presente em todas as cenas: `Sol` (direcional quente, sombras suaves), `Volume_Global` (perfil `Assets/Settings/Ambiente_Padrao_Volume.asset`: tonemapping neutro, color grading leve, bloom e vinheta discretos) e o script `AmbientePadrao` (`Scripts/Core`), que aplica nos `RenderSettings` o céu em degradê (`Assets/Materials/Ceu_Degrade.mat`, shader `CidadeViva/CeuDegrade`), luz ambiente em gradiente e neblina (desligada na Galeria). No URP: SSAO adicionado ao `URP_Renderer`, sombras suaves e distância de sombra 80 no pipeline asset. Câmeras com pós-processamento e FXAA ligados e fundo = skybox. Gerado por `AmbienteConstrutor.cs` (menu **Cidade Viva > Ambiente**); pra mudar o look, edite o script e rode o menu. Obs.: o projeto está em espaço de cor **Gamma**; os minigames são Canvas em Screen Space Overlay, então neles o pós-processamento só afeta o fundo.
- **Arte 3D** (2026-10-10; ainda não usada nas cenas do jogo, que seguem com placeholders): modelos de terceiros em `Assets/Art/<Categoria>/Modelos/<Pacote>`, créditos e licenças em `Assets/Art/CREDITOS.md` (maioria CC0 de Kenney/Quaternius/KayKit; os do Sketchfab são **CC-BY 4.0, crédito obrigatório** na tela de créditos; o `CityEnvPack1` está sem licença identificada). Prefabs prontos em `Assets/Prefabs/Art/{Ruas,Predios,Carros,Natureza,Lixo_Reciclagem,Props,Personagens,Enchente}` (`Enchente` = móveis arrastados pela água e tudo que é estrago/caos), em metros (ladrilho de rua 8 m, adulto ~1,75 m, carro ~3,5 m), frente em +Z, sem colliders. Paleta única em `Assets/Materials/Paleta` (`Pal_*.mat`, URP/Lit fosco): a maioria dos FBX tem os materiais remapeados pra ela no importador; KayKit, mini personagens e os glTF mantêm as cores do autor (a equipe aceitou misturar estilos, inclusive modelos mais realistas). **Personagens animados**: `Familia_*_Animado`, figurantes (`Pedestre_Mulher`, `Trabalhador(a)`, `Medico/Medica`, `Idoso/Idosa`, `Mini_*`) e cachorros (`Cachorro_Husky/Caramelo/Pug`) têm `Animator` com controlador em `Assets/Art/Animacoes`: estado inicial `Locomocao` (blend por float `Velocidade`: 0 parado, 0,5 andando, 1 correndo; o Pug só tem parado) e um estado por clipe extra (`animator.Play("Jump")`, `"PickUp"`, `"sit"` etc.). As versões de primitivas (`Familia_Pai` etc.) continuam lá, sem animação. Brasília: versões de primitivas (`Brasilia_*`) e realistas em glTF (`BSB_*`, incluindo `BSB_Maquete_Da_Cidade`, um mapa 3D do Plano Piloto com 135 mil triângulos). glTF é importado pelo pacote `com.unity.cloud.gltfast` (6.20.0), cujos materiais usam shader graph próprio, não URP/Lit. UI do Kenney (botões/ícones em 5 cores, fontes) em `Assets/Art/UI/KenneyUIPack`; sons em `Assets/Audio/{Interface,Efeitos}`. **Terceira leva — cidade em caos** (2026-10-11, de `C:\dev\Assets_Baixados\novos2`; +105 prefabs, ainda não usados em nenhuma cena): servem pra montar a cidade bagunçada do começo do jogo, que vai se arrumando conforme os minigames são completados. Em `Prefabs/Art/Enchente`: `Cone_Quebrado_1..3`, `Cone_Obra_1/2`, `Bloco_Concreto`, `Bloco_Concreto_Lascado_1/2`, `Marco_De_Concreto`, `Defensa_Metalica_1/2`, `Bueiro_Tampa_1/2`, `Bueiro_Grade`, `Poste_De_Luz_Simples/Duplo`, `Triangulo_De_Sinalizacao`, `Hidrante_1/2`, `Cavalete_Grande_1..3`, `Cavalete_Medio_1/2`, `Cavalete_Pequeno_1/2`, `Cavalete_Mini` (MilkAndBanana, CC0, em GLB), `Barricada_De_Entulho`, `Cacamba_Transbordando`, `Papeis_Espalhados`, `Arvore_Seca_Grande`, `Arvore_Seca_1..3`, `Arbusto_Seco` (Quaternius, CC0), `Ruina_De_Predio_1..3`, `Entulho_De_Concreto`, `Asfalto_Quebrado`, `Buraco_No_Chao`, `Pedacos_De_Rua` e `Chao_De_Lama` (quad de 4 m com textura de lama e poças). Em `Lixo_Reciclagem`: sacos de lixo, caixas, garrafas PET amassadas, papel amassado, maçã podre, baldes de lixo coloridos, `Lixeira_Transbordando`, `Pilha_De_Lixo`. Mais props de rua, 9 veículos (inclui `Moto`, `Onibus_Urbano`, `Viatura_De_Policia`), 8 `Predio_Tijolo_*`, `Estufa` e 2 árvores. Boa parte veio **sem licença identificada** (City Pack, pasta "lixos", Destroyed City Assets, textura de poça) e 2 modelos são CC-BY do Poly by Google: está tudo listado em `CREDITOS.md` pra confirmar antes de distribuir. Na tabela `Itens`, esses pacotes avulsos usam os modificadores novos: `.Obj()`/`.Glb()` (formato), `.Mede(metros)` (tamanho pela maior dimensão, em vez de escala), `.Autor()` (mantém as cores do autor; só o que estiver em `trocas` vai pra paleta), `.Partes("A;B")` (prefab só com alguns filhos de um arquivo que traz várias peças) e `.Rente()` (peça deitada no chão, sem sombra). `ArteImportador.RemapearMateriais(kits...)` e `GerarPrefabs(kits...)` aceitam nomes de pacote pra refazer só uma parte. Atenção em OBJ: o Unity não acha `.mtl` nem textura com espaço no nome. Tudo de modelo/prefab é gerado pelos scripts de `Assets/Editor/ArteCidadeViva` (menu **Cidade Viva > Arte**; a lista de modelos, escalas e cores fica na tabela `Itens` de `ArteImportador.cs`): pra mudar cor, escala ou forma, edite o script e rode o menu, não o prefab à mão (seria sobrescrito). `Assets/Scenes/Galeria_Assets.unity` mostra todos os prefabs; não está em Build Settings.

**Próximos passos sugeridos:**
1. ~~Instalar URP~~ — feito (2026-09-28).
2. Sprint 2 — progresso e recordes: estado salvo localmente (minigames jogados + recorde), `FinalizarMinigame(pontuacao)` padrão, áreas cinza→coloridas no Hub com recorde exibido, uma cena por minigame.
3. Sprint 3 — "Corre, Correndo!" (runner 3 faixas com escudos, base para "Pedal Seguro"); depois "Olho Vivo!" (hotspots, base para "Onde Eu Vou?").

**Com a nova direção (Glotrânsito), as duas listas abaixo deixam de ser tarefas:** o "Guarda o Sinal" não será criado como estava planejado (o tema dele vai pra Central de Sinais) e os seis minigames antigos serão substituídos, não refeitos. As mecânicas novas seguem o `Assets/Scripts/PADRAO_MINIGAME.md`.

~~**Falta criar:** o 9º minigame, "Guarda o Sinal"~~ (`Area_GuardaOSinal` ainda aponta pro `Minigame_Template`).

~~**Falta refazer no padrão novo:** Onde Eu Vou?, Recicla Comigo!, Pedal Seguro, Sobe, Sobe!, Semáforo Mágico e Olho Vivo!.~~ Feitos e a reaproveitar: Travessia Segura e Corre, Correndo!.

**Em aberto com a equipe:** GDD diz 10 minigames mas lista 9 (+ variante Reciclomania); GDD sugere hub por toque/clique, o atual é personagem andando com setas.


GDD DO JOGO: 
NOME A DEFINIR 
GAME DESIGN DOCUMENT 
V 1.0 
EQUIPE GLOTEC
 
 
 
 
GDD – "Nome a definir" 
GAME DESIGN DOCUMENT — V1.0 
1. VISÃO GERAL DO JOGO 
1.1 High Concept 
Jogo educativo 3D para crianças de 6 a 9 anos, ambientado em Brasília, onde uma família 
precisa reconstruir a cidade depois de uma tempestade que fez tudo alagar e levou embora toda 
a sinalização de trânsito. O jogo é estruturado como um hub central com uma coleção de 
minigames temáticos, jogáveis de forma contínua e repetida, cada um ensinando um conceito 
de trânsito ou meio ambiente através de mecânicas curtas, divertidas e replayáveis. 
1.2 Game Overview 
Uma família, pai, mãe, filho mais velho, filha e filho mais novo, está em Brasília quando 
uma tempestade repentina (chuva forte, raios, trovões) causa um alagamento generalizado. A 
água leva embora placas, semáforos e sinalização da cidade inteira. A família se divide para 
ajudar a reorganizar diferentes áreas afetadas, e o jogador vai completando minigames, reunidos 
num hub central, para restaurar a cidade aos poucos. Cada minigame é independente e pode ser 
jogado quantas vezes o jogador quiser, incentivando repetição e superação de recordes pessoais. 
1.3 Título do Jogo 
Nome a definir. (provisório) 
1.4 O que faz dele único? 
• Ambientação em Brasília, com elementos reconhecíveis pelo público local. 
• Enredo emocional e concreto (tempestade/enchente), sem vilão fantasioso nem 
mitologia forçada. 
• Família como protagonista, dando múltiplos pontos de identificação para 
crianças pequenas. 
• Estrutura em hub de minigames curtos, feita para repetição e sessões curtas (ideal 
para oficina de 45 min ou uso em casa). 
• Cobertura combinada de trânsito (CTB) e meio ambiente na mesma experiência. 
4 
• Cada minigame tem identidade visual própria (ícone/"logo") dentro do hub, 
reforçando reconhecimento e escolha pela criança. 
1.5 Principais Gameplay Features 
• Hub central navegável com ícones de cada minigame. 
• 10 minigames curtos, cada um com mecânica própria e replayável. 
• Sistema de recordes pessoais por minigame. 
• Cutscenes curtas de introdução e conclusão amarrando a narrativa entre os 
minigames. 
• Progressão visual da cidade conforme minigames vão sendo completados. 
• Sistema de login/perfil, permitindo que o jogador retome o progresso salvo em 
qualquer sessão (A DEFINIR). 
2. PÚBLICO-ALVO E PLATAFORMAS 
2.1 Público-alvo 
Crianças de 6 a 9 anos, ensino fundamental I, rede pública de Brasília (território do projeto 
Glotec). 
2.2 Plataforma 
Web (HTML5) e Desktop como prioridade; Android como possibilidade futura. 
2.3 Gênero 
Coletânea de minigames educativos (hub + minigames), com elementos de ação, reflexo, 
puzzle e observação. 
2.4 Modelo de negócio 
Não comercial, uso educativo gratuito dentro do projeto de extensão Glotec, distribuído 
para escolas parceiras. 
5 
3. ENREDO E NARRATIVA 
3.1 Estilo da narrativa 
Realista e emocional, sem vilão nem elemento sobrenatural, o conflito nasce de um evento 
natural (tempestade/enchente), com tom afetuoso e familiar, adequado para 6-9 anos. 
3.2 A História 
Um dia comum em Brasília é interrompido por uma tempestade repentina: chuva forte, 
raios e trovões alagam ruas e avenidas. Quando a água baixa, a família percebe que a enchente 
levou embora as placas, semáforos e faixas da cidade, tudo ficou bagunçado e inseguro. Pai, 
mãe e os três filhos decidem se dividir para ajudar a reorganizar diferentes partes da cidade, 
cada um cuidando de um tipo de problema (trânsito, meio ambiente, segurança), até que a 
cidade volte a ficar bonita e segura para todo mundo. 
3.3 Estrutura de Vídeos Introdutórios 
• Vídeo de abertura (1-2 min): mostra a família no dia comum, a tempestade 
chegando, a enchente levando as placas, fecha com a decisão da família de ajudar. Estilo 
animação simples/storybook narrado. 
• Vídeos de capítulo (20-40s): ao desbloquear uma nova área/tema no hub, um 
vídeo curto apresenta o problema daquela área específica (ex: "Nossa, essa rua ficou 
sem nenhum sinal!"), preparando o jogador para o(s) minigame(s) daquele tema. 
• Falas do guia dentro do próprio jogo: em vez de vídeo educativo tradicional, 
um personagem-guia (pode ser o irmão mais velho ou npcs trabalhadores) aparece com 
falas curtas (10-15s) explicando o "porquê" antes de cada minigame, funcionando como 
a própria introdução da mecânica. 
• Vídeo de celebração (10-20s): ao completar os minigames de uma área, cena 
curta mostrando aquela parte da cidade "voltando ao normal", recompensa emocional 
visível. 
3.4 Personagens — Família 
• Pai: conduz cenas ligadas a decisões de trânsito de adulto/condução seguro e 
meio ambiente. 
• Mãe: também conduz cenas ligadas a decisões de trânsito, organização e meio 
ambiente. 
6 
• Filho mais velho: guia/ajudante do jogador, dá as dicas nos minigames. 
• Filha: personagem intermediária, aparece em minigames de observação e 
cuidado ambiental. 
• Filho mais novo: personagem mais infantil, protagonista dos minigames mais 
simples/fofos (bichinhos, reciclagem). 
4. ESTRUTURA DO JOGO (HUB + MINIGAMES) 
4.1 O Hub 
Uma versão em miniatura de Brasília (estilizada, não realista), navegável de forma 
simples (tocar/clicar nas áreas), onde cada região da cidade abriga um ou dois minigames. Cada 
minigame tem um ícone/"logo" próprio visível no hub, com cor e mascote específicos, 
facilitando a criança reconhecer e escolher o que quer jogar de novo. Áreas ainda não visitadas 
aparecem acinzentadas; áreas com minigames já jogados ficam coloridas e "vivas". 
4.2 Lista de Minigames 
1. "Onde Eu Vou?" 
Mecânica: arrastar e soltar placas de trânsito para o lugar certo da rua (semáforo, faixa, placa 
de pare). 
Ensina: reconhecimento de sinalização básica. 
Ritmo: calmo, sem pressão de tempo, feedback sonoro e visual positivo. 
2. "Travessia Segura" 
Mecânica: guiar o personagem atravessando a rua, olhando os dois lados, esperando os 
carrinhos passarem, usando a faixa de pedestre, ritmo tipo Frogger simplificado e gentil. 
Ensina: hábito de atravessar com segurança. 
3. "Recicla Comigo!" 
Mecânica: separar itens de lixo em lixeiras corretas (orgânico, plástico, papel, vidro), 
cronometrado, sem punição pesada em caso de erro. 
Ensina: separação de resíduos/reciclagem. 
Variante opcional  "Reciclomania": versão com mecânica de arremesso (puxar e soltar, tipo 
Angry Birds) para acertar a lixeira certa. 
7 
4. "Corre, Correndo!" 
Mecânica: endless runner estilo Subway Surfers , 3 faixas, pulos e desvios de obstáculos 
(buraco, poça, carrinho parado), moedas/itens colecionáveis, sistema de "escudo" (3 batidas). 
Ensina: atenção e desvio de riscos na rua, de forma lúdica e com adrenalina. 
5. "Pedal Seguro" 
Mecânica: mesma estrutura de runner, agora numa ciclovia, com obstáculos específicos de 
ciclista (pedestre distraído, buraco, animal) e power-up de capacete que dá proteção extra. 
Ensina: segurança ao andar de bicicleta e uso de equipamento (capacete). 
6. "Sobe, Sobe!" 
Mecânica: plataforma vertical infinita estilo Sonic/Doodle Jump, personagem pula 
automaticamente de superfície em superfície fugindo da água que sobe continuamente; não 
pode cair nem demorar demais numa plataforma. Pontuação por altura alcançada. 
Ensina: conecta diretamente com o tema da enchente, urgência e atenção. 
7. "Semáforo Mágico" 
Mecânica: jogo de ritmo/reação corporal, verde = correr, amarelo = andar devagar/se preparar, 
vermelho 
= parar e virar "estátua", velocidade cresce a cada rodada. 
Ensina: significado ativo de cada cor do semáforo (não só "obedecer", e sim entender a ação 
certa de cada cor). 
8. "Guarda o Sinal" 
para 
Mecânica: a criança assume o papel de operadora do semáforo, trocando as cores no momento 
certo 
deixar 
carrinhos 
e 
pedestres 
passarem 
sem 
batida. 
Ensina: o porquê da sequência das cores e como o sistema de trânsito funciona na prática. 
9. "Olho Vivo!" 
Mecânica: cena de rua/bairro/casa com vários personagens fazendo coisas do dia a dia, 3-4 
erros escondidos e exagerados de forma cômica (carro em cima da faixa, criança sem cinto, lixo 
jogado no chão, torneira aberta). A criança toca nos erros antes do tempo acabar (ou em modo 
livre, sem tempo); cada acerto gera correção visual e uma explicação curta em áudio/balão de 
8 
fala. 
Ensina: múltiplos temas de trânsito e meio ambiente na mesma mecânica reaproveitável, cenas 
variam por fase (rua, escola, parque, casa, depois da chuva), permitindo cobrir cinto de 
segurança, lixo, água, sinalização, etc., sem multiplicar sistemas diferentes. 
4.3 Sistema de Recorde e Replay 
Cada minigame salva a melhor pontuação/tempo do jogador localmente, exibida no hub 
junto ao ícone do minigame, incentivando o jogador a voltar e tentar superar o próprio recorde, 
é o principal motor de jogo contínuo, já que não há progressão obrigatória em sequência linear. 
5. JOGABILIDADE — REGRAS GERAIS 
5.1 Objective, Challenge, Reward 
Objective: ajudar a família a restaurar a cidade jogando os minigames disponíveis no hub. 
Challenge: cada minigame tem seu próprio tipo de desafio (reflexo, atenção, raciocínio, 
timing), 
sempre calibrado para não frustrar a faixa etária de 6-9 anos. 
Reward: recorde pessoal salvo, transformação visual da área no hub, avanço narrativo (vídeos 
de capítulo e celebração), sensação de "cidade ficando bonita de novo". 
5.2 Vidas e Punição 
Modelo suave em todos os minigames: erro nunca gera "game over" definitivo sem 
chance imediata de tentar de novo; em runners (Corre Correndo, Pedal Seguro) usa-se 
"escudos" em vez de vidas tradicionais; em jogos calmos (Onde Eu Vou, Olho Vivo modo livre) 
não há punição, só repetição. 
5.3 Câmera e Controles 
Visual 3D simples ou 2.5D (dependendo da viabilidade de produção), controles baseados 
em toque/clique e poucas teclas (setas + 1 botão de ação), pensados para crianças pequenas sem 
exigir combinações complexas. 
6. ARTE E ESTÉTICA 
Essas são apenas sugestões, a decidir ainda com a equipe. 
9 
6.1 Estilo 
Visual cartoon, colorido e fofo, referências a desenhos infantis.  
6.2 Direção de arte 
Contraste visual entre áreas ainda cinzentas/danificadas e áreas restauradas/coloridas no 
hub, reforçando progresso. Cada minigame tem paleta de cor e mascote/ícone próprios para 
fácil reconhecimento. 
6.3 Elementos de Brasília 
Referências estilizadas (não hiper-realistas) a elementos reconhecíveis da cidade, 
adaptando ruas, praças e paisagem local ao tema de cada minigame. 
7. ÁUDIO 
Também apenas sugestões. 
7.1 Estilo musical 
Trilha alegre e leve, com variações por minigame (mais calma nos jogos de observação, 
mais animada nos runners). 
7.2 Efeitos sonoros 
Sons de acerto/erro bem definidos e amigáveis (nunca assustadores), voz do guia/mascote 
com falas curtas, sons ambiente por tema (chuva, trânsito, natureza). 
8. PROGRAMAÇÃO 
8.1 Engine 
Unity (ou engine mais leve conforme necessidade de prazo, a definir). 
8.2 Sistemas principais a programar 
• Sistema de hub com navegação entre minigames. 
• Sistema de recorde/pontuação local por minigame. 
• Sistema de cena com "hotspots" clicáveis (reaproveitável para Onde Eu Vou, 
Olho Vivo e variações). 
10 
• Sistema de runner (movimento automático + desvio, reaproveitável para Corre 
Correndo e Pedal Seguro). 
• Sistema de plataforma vertical infinita (Sobe, Sobe!). 
• Sistema de ritmo/reação por estímulo (Semáforo Mágico). 
• Sistema de simulação simples de semáforo/tráfego (Guarda o Sinal). 
8.3 Reaproveitamento técnico 
Muitos minigames compartilham a mesma base técnica (sistema de hotspots, sistema de 
runner), o que reduz bastante o esforço de programação e é uma decisão pensada diretamente 
para caber no prazo e na equipe reduzida. 
9. PRODUÇÃO — ROADMAP SUGERIDO 
9.1 Protótipo (curto prazo / primeira entrega) 
Hub básico funcional + 2-3 minigames completos representando tipos diferentes de 
mecânica (ex: Travessia Segura, Corre Correndo, Olho Vivo), com pelo menos o vídeo de 
abertura pronto. 
9.2 Expansão 
Adição gradual dos demais minigames, vídeos de capítulo, sistema de recorde completo, 
refinamento de arte. 
9.3 Versão completa 
Todos os 9-10 minigames funcionando, cidade totalmente restaurável, todos os vídeos 
narrativos implementados. 
11 