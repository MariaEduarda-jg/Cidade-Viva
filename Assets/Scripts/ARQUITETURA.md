# Arquitetura dos minigames

> O que todo minigame precisa ter (cena, níveis, telas, som, regras de tela e checklist de teste)
> está em [`PADRAO_MINIGAME.md`](PADRAO_MINIGAME.md). Este arquivo explica como as peças funcionam.

Objetivo: dar pra trocar arte, cores e textos sem mexer na lógica dos jogos.

Cada minigame tem três partes:

| Parte | Onde fica | O que guarda |
|---|---|---|
| Lógica | `Assets/Scripts/Minigames/<Minigame>/` | Regras, pontuação, colisões. Herda de `MinigameBase`. |
| Configuração | `Assets/Configs/*.asset` (ScriptableObject) | Números de regra, cores, textos, posições dos hotspots. |
| Cena | `Assets/Scenes/Minigame_*.unity` | Formas, tamanhos e layout dos objetos; referências entre eles. |

## MinigameBase (`Scripts/Core/MinigameBase.cs`)

Todo manager de minigame herda de `MinigameBase`. Campos no Inspector:

- `nomeDaArea`: o mesmo nome da `AreaTrigger` no Hub (ex.: `"Onde Eu Vou?"`).
- `telaFimDeJogo`: a instância do prefab `UI_FimDeJogo` na cena.
- `niveis`: o asset de `LevelConfig` do minigame. `telaDeSelecao`: a instância de `UI_SelecaoDeNivel`.
- `pontosPara2Estrelas` / `pontosPara3Estrelas`: só valem pra minigame sem `LevelConfig`.

O que o manager usa:

- `NivelAtual` (1 a 5) e `Nivel` (o `NivelConfig` daquele nível; nunca é nulo). Leia `Nivel` no `Start`
  pra configurar a partida, ou sobrescreva `AplicarNivel(NivelConfig)`, chamado no `Awake` da base.
- `EscolhendoNivel`: `true` enquanto a seleção de níveis está aberta (o tempo fica parado,
  `Time.timeScale = 0`). Não comece nada que não dependa de `Time.deltaTime` enquanto for `true`.
- `FinalizarNivel(ResultadoDoNivel resultado, string titulo)`: a forma completa. O minigame diz se a
  criança `concluiu` e o que mediu (`pontuacao`, `erros`, `tempoGasto`, `tempoRestante`, `vidasRestantes`).
  A base calcula as estrelas pelo critério do nível, grava no `GameProgress` e mostra a tela de fim.
  `concluiu = false` (estourou erros ou tempo) não grava nada e não libera o próximo nível.
- `FinalizarJogo(int pontuacao[, string titulo])`: forma antiga, só com a pontuação; conta como concluído.
- `ReiniciarJogo()`: recarrega a cena no mesmo nível. `JogarNivel(n)` / `ProximoNivel()`: trocam de nível.
- `Awake()` (`protected virtual`): esconde a tela de fim, decide o nível e abre a seleção. Quem
  sobrescrever chama `base.Awake()`.

Só vale a primeira chamada de finalização (`Finalizado` fica `true`). Nenhum minigame chama
`GameProgress` direto; só o `MinigameBase`.

## Níveis (`LevelConfig`)

Cada minigame tem **5 níveis**, num asset em `Assets/Configs/Niveis/Niveis_<Minigame>.asset`
(classe `LevelConfig`, menu Create > Cidade Viva > Níveis de minigame). Cada item da lista é um `NivelConfig`:

| Campo | Pra que serve |
|---|---|
| `objetivo` | Frase curta pra criança (aparece na seleção de níveis). |
| `velocidade` | Multiplicador de velocidade (1 = normal). |
| `quantidadeDeObstaculos`, `quantidadeDeItens` | Quantos obstáculos / itens o nível tem. |
| `tempoEmSegundos` | Tempo limite (0 = sem limite). |
| `errosPermitidos` | Erros antes de precisar tentar de novo (-1 = sem limite). |
| `regrasExtras` | Regras ligadas no nível, por nome (ex.: `"contramao"`). Cada minigame define as suas; `Nivel.TemRegra("nome")`. |
| `extras` | Números específicos do minigame, por nome; `Nivel.Extra("nome", padrao)`. |
| `estrelas` | Critério das estrelas: `medida` (`Pontuacao`, `Erros`, `TempoGasto`, `TempoRestante`, `VidasRestantes`) e os limites `para2Estrelas` / `para3Estrelas`. Concluir vale 1. Em `Erros` e `TempoGasto` o limite é "no máximo"; nas outras, "pelo menos". |

Os campos são os mesmos pra todos; cada manager usa os que fazem sentido e ignora o resto.
A ideia da curva: nível 1 ensina a regra com calma, 2 e 3 aumentam o ritmo, 4 e 5 combinam regras.

**Como o nível chega no jogo.** Ao entrar na cena sem nível escolhido, o `MinigameBase` abre a
`UI_SelecaoDeNivel` e para o tempo. Escolher um nível guarda a escolha (estática, por cena) e **recarrega
a cena**; no novo `Awake` a base já sabe o nível e entrega o `NivelConfig`. "Tentar de novo" recarrega no
mesmo nível; "Próximo nível" faz o mesmo com o nível seguinte. Sair pro Hub (`VoltarAoHub.Voltar`) ou
entrar por uma `AreaTrigger` chama `MinigameBase.EsquecerNivelEscolhido()`, pra seleção voltar a aparecer.

**Desbloqueio e gravação.** Nível 1 sempre aberto; concluir o nível N libera o N+1
(`GameProgress.NivelDesbloqueado`). No perfil ficam, por minigame, `estrelasPorNivel` e `recordePorNivel`
(sempre o melhor resultado), além de `niveisConcluidos`, do total de `estrelas` e do `recorde` geral.
A área do Hub fica colorida a partir do nível 1 concluído.

**Estado de cada minigame.** O **Travessia Segura** e o **Corre, Correndo!** já leem o nível (o comentário no topo de `CorreManager.cs` lista regras e extras do Corre, que calcula as próprias estrelas sobrescrevendo `CalcularEstrelas`). O Travessia é o modelo do padrão novo (cena 3D,
`Ambiente_Padrao`, prefabs de arte, HUD com as peças de `MenuConstrutor`, seleção de níveis e fim de jogo
padronizados). O comentário no topo de `TravessiaManager.cs` lista o que cada campo do nível faz nele:
`quantidadeDeObstaculos` = pistas de carro, `velocidade`, `errosPermitidos` (vidas = erros + 1), as regras
`comecar_fora_da_faixa`, `semaforo`, `ciclista`, `ambulancia`, `imprevisiveis` e os extras
`intervaloDeCarros`, `tempoVerde`, `tempoVermelho`, `intervaloDaAmbulancia`; estrelas por `VidasRestantes`.
Os outros sete já têm o asset, a seleção e a tela de fim, mas ainda jogam igual em todos os níveis e usam
estrelas por `Pontuacao`; os números dos assets deles são a intenção de cada nível, pra quando forem refeitos.

**Pra adaptar um minigame:** ler `Nivel` no `Start` e aplicar; trocar `FinalizarJogo(pontuacao)` por
`FinalizarNivel(new ResultadoDoNivel { ... })`; terminar com `concluiu = false` quando estourar
`errosPermitidos` ou `tempoEmSegundos`; e ajustar a `medida` das estrelas no asset.

Prefabs e ligação nas cenas são gerados por `Assets/Editor/ArteCidadeViva/NiveisConstrutor.cs`
(menu **Cidade Viva > Níveis**). "Criar os LevelConfig que faltam" não sobrescreve assets já ajustados.

## Perfis e progresso (`Scripts/Core`)

**Os perfis são locais ao dispositivo (ou ao navegador, no WebGL). Não existe conta na nuvem**: não há
e-mail, senha nem servidor. Um perfil criado num computador não aparece em outro, e limpar os dados do
navegador (ou reinstalar o jogo) apaga os perfis. Isso é de propósito: crianças de 6 a 9 anos muitas
vezes não conseguem criar conta, e o jogo não coleta dado pessoal nenhum além do nome que a criança digita.

- **Perfil** (`Perfil.cs`) = nome (até 12 caracteres) + avatar (um de 8 bichinhos) + código secreto de
  3 figuras (escolhidas entre 6, na ordem, podendo repetir) + progresso por minigame.
- **Onde fica**: um arquivo JSON por perfil em `Application.persistentDataPath/perfis/perfil_<id>.json`.
  No Windows: `%USERPROFILE%\AppData\LocalLow\<empresa>\<produto>\perfis`. No WebGL esse caminho é um
  disco em memória espelhado no IndexedDB do navegador; `Perfis.Salvar` chama o plugin
  `Assets/Plugins/WebGL/CidadeVivaArquivos.jslib` (`FS.syncfs`) pra gravar de verdade.
- **Conteúdo do arquivo**: `id`, `nome`, `avatar`, `codigo` e a lista `minigames`, cada item com `id`
  (nome da área sem espaços/acentos, ex.: `OndeEuVou`), `niveisConcluidos`, `estrelas` (total), `recorde`
  (geral) e as listas `estrelasPorNivel` e `recordePorNivel`.
- **O código de figuras não é segurança**: fica em texto puro no arquivo. Serve pra uma criança não
  entrar sem querer no perfil da outra. Se a criança esquecer, um adulto pode abrir o arquivo e ler o
  `codigo` (índices das figuras: 0 estrela, 1 coração, 2 sol, 3 lua, 4 flor, 5 gota) ou apagar o arquivo.
- **`Perfis`** (classe estática): `Listar()`, `Criar(nome, avatar, codigo)`, `CodigoConfere`, `Entrar`,
  `EntrarComoVisitante()`, `Sair()`, `Ativo`, `SalvarAtivo()`.
- **Visitante** ("Jogar sem conta"): perfil só na memória, nunca vira arquivo. O progresso vale durante
  a sessão e some ao fechar o jogo ou ao trocar de perfil. Se alguma cena for aberta sem ninguém ter
  entrado (ex.: dar Play direto num minigame no Editor), `Perfis.Ativo` cria um Visitante sozinho.
- **`GameProgress`** (classe estática) é a única porta pro progresso, sempre do perfil ativo:
  `FinalizarNivel(nomeDaArea, nivel, pontuacao, estrelas, concluiu)`, `NivelDesbloqueado`,
  `ObterEstrelasDoNivel`, `ObterRecordeDoNivel`, `ObterRecorde`, `ObterEstrelas`, `ObterNiveisConcluidos`,
  `FoiCompletado`. Tudo guarda "o melhor até agora". Só o `MinigameBase` grava.
- **Estrelas e níveis**: ver a seção "Níveis (`LevelConfig`)" acima.
- **Migração**: o jogo antigo guardava um progresso único em `PlayerPrefs` (`Completado_<Id>`,
  `Recorde_<Id>`). O primeiro perfil criado no dispositivo herda esses dados, uma vez só
  (marca `Perfis_ProgressoMigrado` em `PlayerPrefs`); as chaves antigas não são apagadas.
- **Ícones**: `Assets/Configs/Icones_De_Perfil.asset` (`IconesDePerfil`). O perfil guarda índices dessas
  listas, então dá pra trocar a arte de um ícone, mas não a ordem.

Telas: `Menu_Inicial.unity` (primeira cena do Build Settings; script `MenuInicial` em `Scripts/Menu`;
botões "Entrar", "Criar meu perfil" e "Jogar sem conta") e, no Hub, o `PainelDoPerfil` (avatar, nome e
botão "Trocar de perfil"). As duas são geradas por `Assets/Editor/ArteCidadeViva/MenuConstrutor.cs`
(menu **Cidade Viva > Menu**).

## Tela de fim de jogo (`Assets/Prefabs/UI_FimDeJogo.prefab`)

Um prefab só, usado em todas as cenas. Script: `TelaFimDeJogo` (`Scripts/Core`).

- `Text_Nivel`: "Nível 2" (some em minigame sem níveis).
- `Text_Titulo`: título passado pelo minigame (ou `tituloPadrao`); "Quase! Tente de novo" se não concluiu.
- Três estrelas (`estrelas`): acesas conforme o resultado.
- `Text_Pontuacao`: pontuação + "Novo recorde!" ou o recorde do nível.
- `Button_VoltarAoHub` (`VoltarAoHub.Voltar()`), `Button_TentarDeNovo` (`TelaFimDeJogo.TentarDeNovo()` →
  `MinigameBase.ReiniciarJogo()`) e `Button_ProximoNivel` (`TelaFimDeJogo.ProximoNivel()`; só aparece se
  concluiu e existe nível seguinte).

Os textos fixos ("Pontuação: {0}", "Novo recorde!", "Recorde: {0}") são campos do `TelaFimDeJogo`
no prefab. Pra mudar o visual da tela em todos os minigames, edite o prefab.

## Configurações (`Assets/Configs`)

Criadas pelo menu **Create > Cidade Viva**. O manager de cada cena aponta pro seu asset.

| Asset | Classe | Usado por |
|---|---|---|
| `OndeEuVou_Config` | `OndeEuVouConfig` | `OndeEuVouManager` |
| `TravessiaSegura_Config` | `TravessiaConfig` | `TravessiaManager` |
| `ReciclaComigo_Config` | `ReciclaConfig` | `ReciclaManager` |
| `CorreCorrendo_Config` | `CorreConfig` | `CorreManager` |
| `PedalSeguro_Config` | `RunnerConfig` | `RunnerManager` (runner 2D antigo) |
| `SobeSobe_Config` | `SobeSobeConfig` | `SobeSobeManager` |
| `SemaforoMagico_Config` | `SemaforoConfig` | `SemaforoManager` |
| `OlhoVivo_Fase1_Rua` | `OlhoVivoFaseConfig` | `CenaOlhoVivo` (um asset por fase) |

O que cada uma guarda de apresentação:

- **Onde Eu Vou?**: cor e rótulo de cada placa (por `tipo`), cor e duração da piscada de acerto, título do fim.
- **Travessia Segura**: medidas da rua, velocidades-base, lista de carros e ambulância (prefabs), tempos do semáforo, pontuação, todas as dicas e os sons.
- **Recicla Comigo!**: itens de lixo (nome, tipo, cor), lixeiras (rótulo, cor, cor da tampa, por `tipo`), onde os itens surgem, textos.
- **Corre, Correndo!**: tipos de obstáculo (id, regra que libera, pulável/deslizável, comprimento, peso, aviso curto), medidas da pista e do pulo, moedas e escudos, pontuação, dicas e sons.
- **Runner antigo (Pedal Seguro)**: lista `objetos` (id, tipo, pontos, cor, rótulo), chances e espaçamento, cor da batida, textos.
- **Sobe, Sobe!**: cores do jogador, plataforma, moeda e água; textos; todos os números de dificuldade e física.
- **Semáforo Mágico**: dicas, parabéns, mensagens de erro, formatos dos contadores, cores do feedback, títulos.
- **Olho Vivo!**: pontos de erro e elementos certos (posição, tamanho, textos), instrução, cores, títulos, som de acerto.

### Como a config chega nos objetos da cena

A cena guarda a forma; a config guarda a aparência. A ligação é por um identificador:

- Runner: `RunnerObjeto.id` no modelo da cena = `RunnerObjetoConfig.id`. O `RunnerSpawner` aplica
  tipo, pontos, cor e rótulo em cada objeto que cria. Modelo sem entrada na config vira obstáculo
  e gera um aviso no console.
- Olho Vivo: `PontoDeErro.id` na fase = `VisualDeErro.id` na lista `visuais` da `CenaOlhoVivo`,
  que aponta pros objetos "errado" e "corrigido" da cena.
- Onde Eu Vou? e Recicla Comigo!: pelo `tipo` da placa / `tipoAceito` da lixeira.

As cores são aplicadas no `Start`. A cor que aparece no Editor fora do Play Mode é só a que ficou
salva na cena; a que vale no jogo é a da config.

## Como fazer

**Trocar cor ou texto**: edite o asset em `Assets/Configs`. Não precisa abrir script.

**Trocar a arte de um objeto**: edite o objeto (ou modelo inativo) na cena. Mantenha o componente
de lógica (`RunnerObjeto`, `Lixeira`, `PlacaArrastavel`...) e o `id`/`tipo`.

**Nova fase do Olho Vivo**: duplique `OlhoVivo_Fase1_Rua`, troque as listas, monte os visuais numa
cópia da cena e preencha `visuais` com os mesmos ids.

**Novo minigame**:

1. Copie `Minigame_Template.unity` e adicione ao Build Settings.
2. Crie o manager herdando de `MinigameBase` e, se tiver o que configurar, uma classe
   `ScriptableObject` de config com `[CreateAssetMenu]`.
3. Ponha uma instância de `UI_FimDeJogo` no Canvas (por último) e ligue em `telaFimDeJogo`.
4. Preencha `nomeDaArea` e chame `FinalizarJogo(pontuacao, titulo)` quando a partida acabar.
5. Aponte a `AreaTrigger` do Hub pra cena nova.

## Fora desta estrutura (por enquanto)

- Layout e tamanhos dos objetos ficam nas cenas, não nas configs.
- Textos fixos de instrução na tela (ex.: "Arraste cada placa para o lugar certo!") ainda são
  objetos `Text` das cenas.
- `RunnerEscudos`, `RunnerFaixas` e `SemaforoDaRua` leem cores e medidas dos próprios objetos da cena.
