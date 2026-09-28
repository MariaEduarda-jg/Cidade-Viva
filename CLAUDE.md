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
Unity (3D URP), C#, plataforma-alvo Web (WebGL) e Desktop.

## Estado atual do projeto
Projeto recém-criado, ainda sem nenhum script ou cena estruturada. Vamos começar pela Sprint 1: estrutura de pastas, cena do Hub básica, personagem controlável, e navegação entre hub e minigames (mesmo que vazios/placeholder no início).