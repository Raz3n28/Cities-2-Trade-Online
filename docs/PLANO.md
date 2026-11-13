# Plano e arquitetura

## Objetivo

Cada jogador roda a **própria cidade** no Cities: Skylines II. O mod liga as cidades por um servidor pequeno para **enviar e receber recursos quase em tempo real**. Uma conexão externa por estrada/trem representa a cidade parceira, e um painel no jogo controla as trocas.

Fora do escopo: duas pessoas construindo **a mesma** cidade ao mesmo tempo (exigiria sincronizar a simulação inteira do jogo).

## Arquitetura

```
┌──────────── PC do jogador ────────────┐        ┌──── Servidor ────┐
│ Jogo (thread principal)               │        │ RelayServer      │
│   TradeNetworkSystem  ◄── fila ──┐    │  TCP   │  - quem está on  │
│   (aplica no jogo, 1x por frame) │    │◄──────►│  - repassa SEND  │
│ TradeClient (thread de rede) ────┘    │        │  - sem estado    │
└───────────────────────────────────────┘        └──────────────────┘
```

- **O servidor não guarda nada do jogo.** Cada cidade é dona dos próprios números. Isso deixa o servidor simples e barato, e impede que um bug do servidor estrague um save.
- **A rede nunca trava o jogo.** O `TradeClient` roda numa thread separada; o `TradeNetworkSystem` pega as mensagens na thread do jogo, no máximo 64 por frame.
- **Reconexão automática** a cada 5 s, e ping a cada 10 s. O servidor derruba quem ficar 30 s calado.
- **Protocolo em texto** (uma linha por mensagem) para ser fácil de ler nos logs e de testar com o cliente de teste.

## Protocolo (v1)

Cada mensagem é uma linha UTF-8: `TIPO<TAB>chave=valor<TAB>chave=valor`. Tabs e quebras de linha nos valores são escapados (`\t`, `\n`, `\\`). Código: `src/Shared/Protocol`.

| Tipo | Quem envia | Campos | Para quê |
|---|---|---|---|
| `HELLO` | cliente | `name`, `version`, `city` | Primeira mensagem. Versão diferente é recusada. |
| `WELCOME` | servidor | `id`, `name`, `motd` | Confirma a entrada. O nome pode ganhar sufixo se repetido. |
| `PLAYER_JOINED` | servidor | `id`, `name`, `city` | Alguém está online (também enviado para cada jogador que já estava). |
| `PLAYER_LEFT` | servidor | `id`, `name` | Alguém saiu. |
| `PING` / `PONG` | cliente / servidor | `t` | Mantém a conexão viva e mede atraso. |
| `SEND` | cliente | `to` (id ou `*`), `kind`, ... | Manda um conteúdo para um jogador ou todos. |
| `DELIVER` | servidor | `from`, `fromName`, `kind`, ... | O `SEND` entregue ao destino. |
| `BYE` | cliente | | Saída educada. |
| `ERROR` | servidor | `code`, `text` | `version_mismatch`, `hello_required`, `unknown_target`, `bad_message`, `server_full`. |

As fases seguintes usam `SEND`/`DELIVER` com novos valores de `kind`, sem mudar o servidor.

## Fases

Cada fase termina com uma versão para testar e passos simples no README.

### Fase 1: base (esta)
Mod carrega, opções (servidor, nome, conectar automático), conexão com reconexão, lista de jogadores, servidor de repasse, cliente de teste, teste automático no GitHub.

### Fase 2: troca de recursos
- Ler do jogo o estoque/produção dos recursos da cidade (sistemas de recursos e empresas do CS2) e escolher como "retirar" e "entregar" recursos de forma segura.
- Mensagens `kind=trade_offer`, `trade_accept`, `trade_ship`, `trade_receive`, com id único por envio para não duplicar se a conexão cair.
- Regras de jogo: limite por minuto, custo/frete, o que acontece se o amigo estiver offline (fila no remetente).
- Teste: dois PCs (ou jogo + cliente de teste) trocando um recurso e conferindo os números.

### Fase 3: conexão por estrada e trem
- Uma conexão externa (outside connection) de estrada e de trilho que representa a cidade parceira.
- Recursos enviados saem fisicamente por ela (caminhões/trens) e os recebidos chegam por ela.
- Investigar como o jogo cria e identifica conexões externas e como "marcar" uma delas como do parceiro.

### Fase 4: painel no jogo
- Painel (UI do jogo em React/TypeScript, usando o toolchain) com: jogadores online, botões de enviar recurso, histórico de trocas e status da conexão.
- Notificações quando chega recurso.
- Página do mod (Paradox Mods) com miniatura feita no Canva.

## Riscos conhecidos

- **APIs do jogo mudam a cada patch.** O código que mexe no jogo fica isolado em `src/Mod/Systems`, para corrigir rápido.
- **Fase 1 não foi testada no jogo por quem escreveu o código** (não há o jogo no ambiente de desenvolvimento). Servidor e cliente de rede foram testados de verdade; as partes do jogo (opções, `CityConfigurationSystem`, `OnGameLoadingComplete`) seguem o template oficial e precisam do teste do Paulo.
- **Segurança:** o servidor é para amigos. Não há senha nem criptografia ainda; não abra a porta para a internet inteira. Uma senha de sala pode entrar na Fase 2.
