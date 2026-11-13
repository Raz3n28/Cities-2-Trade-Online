# Cities 2 Trade Online

Mod para **Cities: Skylines II** em que cada jogador constrói a própria cidade e as cidades **enviam e recebem recursos entre si quase em tempo real**, por meio de um pequeno servidor. Mais para frente, uma ligação por **estrada e trem** vai representar a cidade do amigo, e um **painel dentro do jogo** vai mostrar tudo.

> **Estado atual: Fase 1 (base).** O mod carrega no jogo, tem página de opções, conecta no servidor e vê quem está online. A troca de recursos chega na Fase 2. O plano completo está em [docs/PLANO.md](docs/PLANO.md).

## Como funciona (resumo)

```
 Cidade do Paulo (mod)  ──┐                    ┌──  Cidade do amigo (mod)
                          ├──  Servidor  ──────┤
 Cliente de teste (PC)  ──┘   (repassa msgs)   └──  ...
```

- Cada cidade continua sendo um jogo normal, salvo no seu PC.
- O **servidor** só repassa mensagens entre os jogadores. Ele não guarda as cidades.
- Um dos jogadores (ou um PC/servidor qualquer) deixa o servidor aberto. Os outros conectam pelo IP dele.

## O que tem nesta pasta

| Pasta | O que é |
|---|---|
| `src/Mod` | O mod (C#), usando o toolchain oficial de mods do jogo |
| `src/Server` | O servidor de troca |
| `src/TestClient` | Um "jogador falso" de texto, para testar o servidor sem abrir o jogo |
| `src/Shared/Protocol` | A "língua" que o mod e o servidor falam (usada pelos dois) |
| `scripts` | Atalhos `.bat` para Windows |
| `docs/PLANO.md` | Arquitetura e próximas fases |

---

## Teste 1: servidor sem abrir o jogo (5 minutos)

Serve para confirmar que a parte de rede funciona.

1. No GitHub, abra a aba **Actions** do repositório, clique na execução mais recente de **Server** (com ✓ verde) e baixe o arquivo **TradeServer-windows** no fim da página.
2. Descompacte numa pasta qualquer.
3. Dê dois cliques em **start-server.bat**. Deve aparecer `Servidor ouvindo na porta 7777`. Se o Windows perguntar sobre o firewall, clique em **Permitir**.
4. Dê dois cliques em **test-client.bat**, digite um nome (ex.: `Ana`) e aperte Enter duas vezes.
5. Abra **outro** test-client.bat com outro nome (ex.: `Bia`).
6. Na janela da Bia, digite `/lista`: a Ana deve aparecer. Digite `/enviar * oi` e veja o "oi" chegar na janela da Ana.

Comandos do cliente de teste: `/lista`, `/enviar <id ou *> <texto>`, `/ping`, `/sair`.

## Teste 2: o mod dentro do jogo

### Preparar o PC (só uma vez)

1. Abra o Cities: Skylines II e vá em **Opções > Modding** (pode aparecer como "Modding" ou "Mods").
2. Clique em **Instalar** no toolchain de modding e espere terminar todos os itens (fica tudo com ✓). Ele instala o .NET e as ferramentas que o mod precisa.
3. Feche o jogo e **reinicie o PC** (para o Windows enxergar as ferramentas novas).
4. Baixe este repositório: no GitHub, botão verde **Code > Download ZIP**, e descompacte. (Ou use o GitHub Desktop, que facilita pegar as atualizações.)

### Compilar e instalar o mod

1. Na pasta do repositório, entre em `scripts` e dê dois cliques em **build-mod.bat**.
2. Deve terminar com **OK!**. O toolchain já copia o mod para a pasta de mods do jogo.
   - Se der erro, mande um print da janela aqui no chat.

### Testar

1. Deixe o **start-server.bat** aberto (Teste 1).
2. Abra o jogo, vá em **Opções > Cities 2 Trade Online**:
   - **Endereço do servidor:** `127.0.0.1:7777` (no mesmo PC) ou `IP-do-amigo:7777`.
   - **Nome do jogador:** seu nome.
   - Clique em **Conectar**. O **Status** deve mudar para `Conectado`.
3. Na janela do servidor deve aparecer `<seu nome> ... entrou`.
4. Abra um **test-client.bat** e use `/lista`: seu nome do jogo aparece. Faça `/enviar * oi`.
5. Confira o log do mod em:
   `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\TradeOnline.log`
   (cole esse caminho na barra do Explorador de Arquivos). Deve ter as linhas `Conectado como ...`, `Jogador online: ...` e `Recebido de ...`.

Se marcar **Conectar automaticamente**, o mod conecta sozinho quando a cidade termina de carregar.

## Jogar com um amigo pela internet

O jeito mais fácil é os dois instalarem um programa de "rede local virtual", como o **Radmin VPN** ou o **Tailscale** (grátis). Quem abre o servidor passa o IP que aparece no programa, e o amigo usa `esse-IP:7777` no mod. Assim não precisa mexer no roteador.

## O que mandar de volta depois de testar

- Se o Teste 1 funcionou (sim/não).
- Se o mod compilou, e se apareceu em Opções.
- Se conectou, e o arquivo `TradeOnline.log` (ou um print), principalmente se algo deu errado.

## Para quem programa

- Servidor e cliente de teste: .NET 8. `./tests/smoke-test.sh` sobe o servidor e dois clientes e confere a troca de mensagens (roda também no GitHub Actions a cada push).
- Mod: `net472`, compila com o toolchain oficial (`CSII_TOOLPATH`). O protocolo é compartilhado como código-fonte, então o mod não leva DLL extra.
- Protocolo e decisões de arquitetura: [docs/PLANO.md](docs/PLANO.md).
