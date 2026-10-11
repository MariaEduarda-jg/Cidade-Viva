# Padrão de minigame — Cidade Viva

Todo minigame novo ou refeito segue este padrão. O modelo é o **Travessia Segura**
(`Assets/Scripts/Minigames/TravessiaSegura` e `Assets/Editor/ArteCidadeViva/TravessiaConstrutor.cs`):
na dúvida, abra e copie de lá.

Detalhes de API (campos e métodos do `MinigameBase`, `LevelConfig`, perfis) estão em
[`ARQUITETURA.md`](ARQUITETURA.md). Este arquivo diz **o que** todo minigame precisa ter.

Público: crianças de 6 a 9 anos. Sessões curtas, controles simples, erro sem punição pesada.

---

## 1. Cena

- **3D**, com o prefab `Assets/Prefabs/Ambiente_Padrao.prefab` (sol, céu, pós-processamento).
  Não criar luz, skybox nem Volume próprios.
- **Cenário feito com os prefabs de `Assets/Prefabs/Art`** (ruas, prédios, carros, natureza, props,
  personagens). **Nada de cubo, esfera ou retângulo colorido fazendo papel de objeto do jogo.**
  - Exceção aceita: superfícies planas e pinturas de chão (asfalto, faixa de pedestre, tracejado,
    ciclovia, marcadores), feitas de chapas finas com material da paleta, quando o tamanho muda
    com o nível e não existe prefab que sirva.
  - Faltou um objeto? Primeiro procure em `Assets/Art` e na cena `Galeria_Assets`. Se não existir,
    acrescente ao pipeline de arte (`ArteImportador.cs` / `ArteFormas.cs`), não improvise na cena.
- **Materiais**: só os de `Assets/Materials/Paleta` (`Pal_*`, URP/Lit fosco). Nunca `Standard`
  nem `Default-Material`.
- **Personagens**: os animados de `Assets/Prefabs/Art/Personagens` (controlador com o estado
  `Locomocao` e o float `Velocidade`: 0 parado, 0,5 andando, 1 correndo; outros clipes por
  `animator.CrossFade("Nome")`).
- **Câmera**: fixa ou com movimento simples, sempre mostrando o que a criança precisa ver pra
  decidir. Nada importante pode ficar atrás da interface (na Travessia a câmera se enquadra
  sozinha conforme a largura da rua: `TravessiaManager.EnquadrarCamera`).
- **A cena é gerada por script**, num `XxxConstrutor.cs` em `Assets/Editor/ArteCidadeViva`, com
  item de menu em **Cidade Viva > Minigames**. Não montar nem ajustar a cena à mão: o próximo
  "Reconstruir" apaga. Carregue assets (configs, sprites) **depois** de
  `EditorSceneManager.NewScene`, senão as referências ficam nulas.
- O que muda com o nível (largura da rua, quantidade de coisas) é montado em tempo de jogo, a
  partir de **modelos inativos** da cena (`Modelo_*`), copiados com `Instantiate`.
- A cena entra no Build Settings e a `AreaTrigger` do Hub aponta pra ela. O `nomeDaArea` do
  manager é igual ao `areaName` da `AreaTrigger`.

## 2. Código

- O manager **herda de `MinigameBase`** e fica em `Assets/Scripts/Minigames/<Minigame>/`.
- Separe por responsabilidade, como na Travessia: manager (regras), jogador (movimento e
  animação), "mundo" (trânsito, itens...), HUD (só apresentação), config.
- **Config do minigame** (`XxxConfig : ScriptableObject`, asset em `Assets/Configs`): tudo que vale
  pra todos os níveis — medidas, velocidades-base, pontuação, **todos os textos** e **todos os sons**.
  Nenhum texto de tela nem número de regra fica escrito no script.
- **Config dos níveis** (`LevelConfig`, seção 3): só o que muda de um nível pro outro.
- O manager lê `Nivel` no `Start`, não começa nada enquanto `EscolhendoNivel` for `true`, e termina
  com `FinalizarNivel(new ResultadoDoNivel { concluiu, pontuacao, erros, vidasRestantes, ... })`.
- **Nunca** chamar `GameProgress`, `Perfis` nem `PlayerPrefs` direto no minigame.
- Toda ação do jogador é um **método público** do manager (`Andar()`, `Olhar()`...), chamado tanto
  pelo teclado quanto pelos botões da tela. Isso também é o que permite testar por script.
- Exponha o estado da partida em propriedades só de leitura (`Posicao`, `Vidas`...) e as regras em
  métodos consultáveis (na Travessia, `MotivoPraNaoSair()` devolve a dica ou `null`).
- No topo do manager, um comentário listando o que cada campo do nível faz naquele minigame.
- Comentários e nomes em português, no estilo dos scripts existentes.

## 3. Níveis

- **5 níveis** num asset `Assets/Configs/Niveis/Niveis_<Minigame>.asset`, definidos em
  `NiveisConstrutor.cs`.
- **A dificuldade sobe de verdade**, e não só a velocidade:

  | Nível | Papel | Travessia Segura |
  |---|---|---|
  | 1 | Ensina a regra, com calma. Quase impossível perder. | 1 pista, carros lentos |
  | 2 | Mesma regra, um pouco mais de ritmo ou uma coisa a mais. | 2 pistas; ir até a faixa |
  | 3 | Entra uma regra nova. | semáforo de pedestres |
  | 4 | Exige atenção: mais coisas ao mesmo tempo. | 3 pistas + ciclovia, sem semáforo |
  | 5 | Combina as regras. Desafiador, mas justo pra 8–9 anos. | semáforo + ciclista + ambulância + carros imprevisíveis |

- Use os campos comuns do `NivelConfig` (`velocidade`, `quantidadeDeObstaculos`,
  `quantidadeDeItens`, `tempoEmSegundos`, `errosPermitidos`); o que for só daquele minigame vai em
  `regrasExtras` (por nome) e `extras` (números por nome).
- Cada nível tem um **`objetivo`**: uma frase curta, pra criança, dizendo o que há de novo.
- **Estrelas (1 a 3)** por um critério que a criança entende e que está na tela: vidas que
  sobraram, erros, tempo. Concluir já vale 1. Evite critério por "pontuação" abstrata.
- **Desbloqueio em sequência** e gravação são do `MinigameBase`; o minigame não cuida disso.
- **Justo** quer dizer: dá pra concluir o nível 5 sem sorte, o perigo é sempre visível antes de
  acontecer, e existe tempo de reação de criança (na Travessia a ambulância surge longe e com sirene).

## 4. Telas padronizadas

Instâncias dos prefabs, postas no Canvas pelo construtor e ligadas no manager:

- **`UI_SelecaoDeNivel`** (`telaDeSelecao`): aparece ao entrar no minigame. 5 botões com número,
  estrelas ganhas ou cadeado, a frase do próximo nível e "Voltar ao Hub".
- **`UI_FimDeJogo`** (`telaFimDeJogo`): nível, 3 estrelas, pontuação e recorde, e os botões
  "Voltar ao Hub", "Tentar de novo" e "Próximo nível". Sem concluir: "Quase! Tente de novo".

Não criar tela própria de início, de níveis ou de fim. Pra mudar o visual delas, mude os prefabs
(gerados por `NiveisConstrutor.cs`), e a mudança vale pra todos os minigames.

Durante a partida, o HUD tem sempre um botão de **voltar ao Hub** (`VoltarAoHub.Voltar`).

## 5. Progresso

- Tudo é gravado no **perfil ativo**, pelo `MinigameBase`: estrelas e recorde por nível.
- **Visitante também joga tudo**: o progresso dele vale na sessão (níveis liberam, estrelas
  aparecem) e some ao fechar o jogo. O minigame não trata o Visitante de forma diferente.
- Abrir a cena direto no Editor funciona: sem perfil, o jogo entra como Visitante.
- Não concluir um nível não grava nada e não apaga o que já foi ganho.

## 6. Som e partículas

- **Som em toda ação e em todo resultado**: ação do jogador (passo, toque), ação bloqueada,
  acerto, erro, concluir. Sons de aviso pro perigo (buzina, sirene).
- Sons **amigáveis, nunca assustadores**. Erro é um som curto e leve, não um alarme.
- Clipes ficam em `Assets/Audio/Interface` e `Assets/Audio/Efeitos` e são referenciados **na
  config** do minigame. Um `AudioSource` no manager, com `PlayOneShot`.
- **Partículas leves**: confete ao concluir e um efeito pequeno no erro. Poucas partículas, curta
  duração, cores da paleta, material `Assets/Materials/Particula.mat`
  (veja `TravessiaConstrutor.Particulas`).
- O jogo tem que ser jogável **sem som** (sala de aula): todo aviso sonoro tem um aviso visual junto.

## 7. Interface

- **Montada com as peças de `MenuConstrutor.cs`**: `NovoCanvas`, `Botao`, `Imagem`, `Texto`.
  Canvas em Screen Space Overlay, referência 1920×1080.
- **Fontes**: `Baloo2` no texto normal e `LilitaOne` em títulos e botões (é o que `Texto` faz com
  `FontStyle.Bold`). Não usar a fonte padrão do Unity nem negrito sintético.
- **Cores**: só as da paleta (`ArtePaleta.Cor("Nome")`). Convenção dos botões:

  | Cor | Uso |
  |---|---|
  | `Verde_Escuro` | ação principal, seguir, confirmar |
  | `Mostarda` | ação de atenção (olhar, tentar de novo) |
  | `Petroleo_Claro` | navegação (setas, voltar ao Hub) |
  | `Cinza` | ação secundária |
  | `Vermelho` | perigo, apagar, aviso urgente |

- **Ícones** em `Assets/Art/UI/Perfis` (desenhados por script). Estrela = vida e recompensa.

## 8. Regras de tela (6 a 9 anos)

- **Botões grandes**: ação principal com pelo menos 300×150 na tela de referência; nenhum botão
  menor que 120×120. Ficam embaixo, ao alcance, e não cobrem o personagem.
- **Ícone + palavra** nos botões principais. Quem ainda lê devagar entende pelo ícone.
- **Poucos controles**: 2 ações principais, no máximo 5 botões na tela. Tudo que o botão faz, o
  teclado também faz (setas + uma tecla de ação).
- **Textos curtos**: uma frase, até umas 8 palavras, no imperativo e positiva
  ("Atravesse na faixa de pedestre!"). Uma dica por vez, no mesmo lugar da tela.
- **Feedback visual imediato e grande** pra tudo: o que deu certo, o que deu errado, o que está
  bloqueado e por quê. Cor não é o único sinal (vermelho/verde vêm com forma ou ícone diferente).
- **Ação proibida não é erro**: o jogo não deixa, toca um som leve e **explica o que fazer**
  ("Primeiro olhe para os dois lados!"). Não tira vida nem ponto.
- **Erro nunca é punitivo demais**:
  - 3 vidas, mostradas como estrelas; errar tira uma e devolve a criança a um ponto seguro.
  - Reação cômica, sem violência (tropeço, cambalhota). Nada de sangue, grito ou tela vermelha.
  - Sem vidas: "Quase! Tente de novo", com "Tentar de novo" a um toque. Nunca "Você perdeu".
  - Nada do que já foi conquistado se perde.
- **Sem pressa artificial**: o tempo só conta depois da primeira ação; limite de tempo só quando
  faz parte do que o nível ensina.
- **O conteúdo educativo é a própria regra do jogo** (olhar antes de atravessar, esperar o sinal),
  e não um texto pra ler.

## 9. Checklist de teste

Testar em Play Mode, com um piloto por script que chama os métodos públicos do manager e grava o
relatório em arquivo (não editar nada em `Assets/` enquanto o piloto roda: o Unity recompila e o
teste morre). Abrir a cena do minigame direto, pra rodar como Visitante e não criar perfil.

**Entrada e níveis**
- [ ] A seleção de níveis aparece ao entrar, com o tempo parado.
- [ ] Só o nível 1 está aberto num perfil novo; clicar num nível trancado não faz nada.
- [ ] Cada nível carrega com os parâmetros do `LevelConfig` (conferir os valores no jogo).
- [ ] Mudar um número no asset muda o jogo, sem mexer em script.

**Regras**
- [ ] Cada ação proibida é bloqueada e mostra a dica certa, sem tirar vida.
- [ ] Cada regra extra do nível funciona (uma verificação por regra).
- [ ] Errar de propósito: reação, perde 1 vida, volta ao ponto seguro, HUD atualiza.
- [ ] Perder todas as vidas: "Quase! Tente de novo", sem estrelas, sem "Próximo nível", e o
      progresso anterior não muda.

**Concluir**
- [ ] Os 5 níveis são concluídos em sequência pelo piloto, sem depender de sorte.
- [ ] As estrelas batem com o critério (testar 3, 2 e 1 estrela pelo menos uma vez).
- [ ] "Próximo nível" avança; no nível 5 o botão some. "Tentar de novo" repete o mesmo nível.
- [ ] Concluir o nível N libera o N+1 na seleção.

**Progresso**
- [ ] Voltar ao Hub e entrar de novo: a seleção reaparece com as estrelas certas.
- [ ] O letreiro da área no Hub mostra recorde, nível e estrelas, e a área fica colorida.
- [ ] Com um perfil de verdade, o arquivo JSON guarda `estrelasPorNivel`; com outro perfil, o
      progresso é separado. (Apagar os perfis de teste depois.)

**Tela e som**
- [ ] Foto da tela em cada nível: o personagem e o que importa não ficam atrás dos botões.
- [ ] Textos com acento aparecem certos e cabem nos painéis.
- [ ] Nenhum erro ou aviso no console durante a partida.
- [ ] **Uma pessoa jogou** com teclado e com mouse/toque, ouviu os sons e achou o nível 5 justo.
      O piloto não substitui isso: ele enxerga o que a criança não enxerga.
- [ ] Build WebGL abre, roda com desempenho aceitável e o progresso sobrevive a fechar a aba.

## 10. Ao terminar

- Atualizar o `CLAUDE.md` (o item do minigame) e, se a arquitetura mudou, o `ARQUITETURA.md`.
- Novo som, fonte ou modelo de terceiros: registrar em `Assets/Art/CREDITOS.md`, com a licença.
- Se o padrão mudou por causa do minigame novo, atualizar este arquivo.
