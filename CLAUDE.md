# Projeto: Cidade Viva

Jogo educativo 3D para crianças de 6 a 9 anos, parte do projeto de extensão Glotec (UnB) — linha Glotrânsito.

## Enredo
Uma tempestade alaga Brasília e leva embora toda a sinalização de trânsito. Uma família (pai, mãe, filho mais velho, filha, filho mais novo) se divide para ajudar a reconstruir a cidade.

## Estrutura do jogo
Hub central navegável (mapa estilizado de Brasília) com áreas representando minigames. Cada área começa "cinza/danificada" e fica colorida quando o minigame correspondente é completado.

## Minigames planejados
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
- Pastas: `Assets/Scenes`, `Scripts/{Core,Hub,Minigames}`, `Materials`, `Prefabs`, `Art`, `Audio`.
- `Hub.unity`: terreno 100x100, Player (`PlayerMovement` com setas/WASD + `HubSpawn`), câmera com `CameraFollow`, 9 áreas `Area_*` (cubos trigger com `AreaTrigger`, um material por área) numa grade de -20 a 20.
- `Minigame_Template.unity`: cena placeholder com câmera e botão "Voltar" (`VoltarAoHub.Voltar()`).
- Navegação: entrar numa área carrega `AreaTrigger.cenaDoMinigame` (hoje todas usam `Minigame_Template`). Ao voltar, `HubSpawn` põe o jogador no `PontoDeRetorno` (10, 1, -10), fora das áreas (o centro do mapa é a `Area_PedalSeguro`). As duas cenas estão em Build Settings.
- `Minigame_OndeEuVou.unity` (cópia do template, em Build Settings; `Area_OndeEuVou` aponta pra ela): Canvas com rua placeholder, 3 `PlacaArrastavel` (PARE/FAIXA/SEMAFORO) e 3 `AlvoPlaca`; `OndeEuVouManager` conta erros (100 − 10 por placa solta no alvo errado; soltar no vazio não conta) e chama `GameProgress` ao encaixar as 3. Scripts em `Scripts/Minigames/OndeEuVou`.
- `GameProgress` (`Scripts/Core`, classe estática): todo minigame chama `GameProgress.FinalizarMinigame(nomeDaArea, pontuacao)` ao terminar. Hoje só faz `Debug.Log`; a persistência entra na Sprint 2.
- Cenas e UI são editadas pelo Editor (unity-mcp), não à mão no YAML do `.unity`.

**Próximos passos sugeridos:**
1. ~~Instalar URP~~ — feito (2026-09-28).
2. Sprint 2 — progresso e recordes: estado salvo localmente (minigames jogados + recorde), `FinalizarMinigame(pontuacao)` padrão, áreas cinza→coloridas no Hub com recorde exibido, uma cena por minigame.
3. Sprint 3 — "Corre, Correndo!" (runner 3 faixas com escudos, base para "Pedal Seguro"); depois "Olho Vivo!" (hotspots, base para "Onde Eu Vou?").

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