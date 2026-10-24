<div align="center">

# Clean Browser
### Navegador para Windows

Uma interface nativa em C# sobre o Microsoft Edge WebView2, com identidade azul-marinho inspirada na Grécia, dez visuais, atalhos editáveis, espaços e Clean AI.

[![Windows build](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml/badge.svg)](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml)
[![Release](https://img.shields.io/github/v/release/Kaynanwxz/Clean-And-Faster)](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest)

**[Baixar CleanBrowser.exe](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest/download/CleanBrowser.exe)** · **[Versões](https://github.com/Kaynanwxz/Clean-And-Faster/releases)** · **[Relatar problema](https://github.com/Kaynanwxz/Clean-And-Faster/issues/new/choose)**

</div>

> Prévia para Windows 10/11 x64. A compilação, os diagnósticos e o teste de atualização são executados pelo GitHub Actions. Consulte o resultado do workflow associado à release; esses testes não equivalem a uma auditoria de segurança ou compatibilidade completa.

## Instalar e abrir

1. Baixe **CleanBrowser.exe** pelo link acima.
2. Salve em uma pasta na qual seu usuário possa escrever e abra com dois cliques.
3. Mantenha o [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) instalado e atualizado.

O executável inclui o .NET; você não precisa instalar o SDK para usar. O WebView2 é separado e reaproveita o motor baseado em Chromium instalado no Windows. Isso reduz o pacote distribuído, mas não garante menor uso de RAM que outros navegadores. O executável ainda não tem assinatura Authenticode.

## Atualizar sem extrair arquivos

**Da versão 0.4.0 em diante:** clique em **Atualizar → Atualizar e reiniciar**. O app consulta as releases públicas deste repositório, baixa o executável, verifica o SHA-256, guarda uma cópia da versão anterior e reinicia. Não exige Vercel, token pessoal ou extração de ZIP.

**Se você já usa uma versão anterior:** abra **Menu → Configurações avançadas → Atualizações pelo GitHub**, preencha `Kaynanwxz/Clean-And-Faster`, salve e use o botão **Atualizar** ou o item **Verificar atualizações**. Esse caminho está disponível nas versões anteriores que já têm o atualizador. Se o aplicativo antigo não abrir, substitua apenas o exe com o navegador fechado.

Se configurou um servidor alternativo, use **Atualizar → Canal de atualização → Usar canal oficial** na nova versão. Salve trabalhos e formulários antes do reinício.

Os dados permanecem em `%LOCALAPPDATA%\KayBrowser`. A atualização não apaga histórico, favoritos, configurações ou perfil de sites. A meta é atualizar todo o código do aplicativo por troca do executável; a atualização do motor WebView2 continua sendo gerenciada pela Microsoft.

## Animações suaves (0.12.0)

Abas entram com expansão curta e saem com recolhimento; as abas vizinhas e o botão de nova aba acompanham o movimento. Fechar uma aba libera seu conteúdo imediatamente, sem esperar a animação. O menu abre com fade e deslocamento vertical discreto; fechar pelo botão usa uma saída breve. O clique externo continua dispensando o menu imediatamente, preservando foco e o próximo clique.

Maximizar, restaurar e minimizar usam os comandos e transições nativos do Windows, preservando posição, área útil e comportamento da barra de tarefas. A duração desse efeito é controlada pelo Windows. **Personalizar → Layout → Animações e transições** desativa os efeitos do aplicativo; a preferência do sistema por reduzir movimento também é respeitada. Não há animação contínua em repouso.

## Gravura clássica aprovada (0.11.1)

A arte padrão da nova aba é a **versão 1 aprovada**: Apolo, veleiro e mar em gravura clássica azul sobre papel claro. O PNG original aprovado está incorporado ao executável, sem recriação, conversão em pontos ou redução da opacidade no modo Arte. O enquadramento continua centralizado, proporcional e cobrindo a página.

Ao atualizar da 0.11.0, somente a escolha do pontilhismo é migrada uma vez para a gravura aprovada; os demais ajustes, imagens pessoais, abas, histórico e dados permanecem. O efeito anterior continua opcional em **Personalizar → Fundo → Pontilhismo anterior (opcional)**. A página em papel usa texto azul-marinho legível também quando a barra do navegador está escura; o tema da barra é preservado.

A imagem original tem 1672 × 941 pixels. Em telas maiores há escala proporcional; não se declara que o arquivo seja 4K. O efeito antigo em pontos, caso ativado, continua sendo desenhado no tamanho da página.

## Apps e visual (0.11.0)

Use o botão de quatro quadrados **Seus apps**, na barra, ou **Menu → Organizar → Seus apps**. Spotify, YouTube, Notion, Gmail e GitHub já aparecem no catálogo. Clique para abrir em uma aba do Clean Browser e faça login no próprio site. **Adicionar outro app** aceita nome e endereço HTTPS; o botão direito remove do catálogo. O catálogo não cria WebViews nem acessa os sites até você abrir um app. Reabrir procura a aba já existente no espaço atual. Você pode manter vários apps em abas, com o controle de memória normal do navegador, que preserva abas reproduzindo áudio.

As sessões usam o mesmo perfil WebView2 persistente do navegador. Remover do catálogo não faz logout nem apaga a conta; encerre a sessão pelo site quando quiser. Fechar o painel não fecha a aba nem para a música. Não há coleta de senha pelo catálogo, integração com API/IA, sincronização das contas ou controle do Spotify instalado no Windows.

O novo emblema mantém vela/estátua e azul-marinho, azul glacial e prata, com silhueta transparente e tamanhos Windows de 16 a 256 px. Executável, janela, abas e nova aba usam a mesma identidade. As superfícies ficaram mais neutras, preservando os temas e a cor de destaque escolhidos.

Na versão 0.11.0, a arte padrão passou a usar **pontilhismo** (agora opcional): pontos antialias são desenhados no tamanho físico da página, inclusive em 4K, com um cache compartilhado para reduzir trabalho e memória entre abas. A composição original é preservada. A fonte histórica continua em 1440 × 576: não se afirma que ela ganhou detalhes fotográficos; a nitidez nova vem da renderização dos pontos, sem ampliar um bitmap de pontinhos. Em **Personalizar → Fundo → Pontilhismo anterior (opcional)** você pode desativar o efeito. Fundos personalizados permanecem com seus pixels originais.

Validação local: 3.770 verificações lógicas; 30 verificações novas de apps/painéis/editor/abas/cookie de teste/remoção/pontilhismo, além das verificações existentes de IA e layout. Os sites públicos de Spotify, YouTube e Notion abriram no executável com perfil isolado, e o teste de disponibilidade Widevine retornou suporte no runtime instalado. Isso não testa login de contas pessoais, reprodução autenticada de músicas, edição de páginas privadas ou todos os provedores. O diagnóstico opcional `dist/CleanBrowser.exe --apps-check` consulta apenas páginas públicas em perfil temporário e grava app-compatibility.json. A CI de Windows e o smoke test do atualizador são obrigatórios antes de considerar a entrega concluída.

## Clean AI integrado (0.10.0)

Abra **Clean AI → Conectar IA**. Escolha OpenAI, OpenRouter ou Personalizada, informe o modelo e a chave da sua API, use **Testar conexão** e salve. O teste faz um pedido curto à API e pode consumir crédito. Endereços personalizados terminam em /responses ou /chat/completions; HTTP só é aceito no próprio computador. Não é preciso instalar outro aplicativo, extensão ou SDK.

Converse no painel, continue com perguntas sobre o mesmo assunto e use **Nova conversa** para limpar o contexto desta sessão. A conversa mantém até seis pares de mensagens e 32 mil caracteres em memória; não é salva no disco nem sincronizada. Mudar a conexão também limpa o contexto. A chave usa proteção do Windows e fica vinculada ao endereço escolhido, preservando chaves OpenAI existentes.

**Mostrar tela ou janela** abre o seletor nativo. Escolha a aba, uma janela específica ou um monitor, clique em **Capturar**, arraste na prévia se quiser recortar, confirme em **Usar esta imagem** e clique em **Enviar**. A captura é pontual; não há vídeo/gravação em segundo plano. Ela permanece anexada, visível, para perguntas seguintes até remover, trocar ou iniciar uma nova conversa. Remover impede os próximos envios; não retira o que já foi enviado ao provedor. Imagens não são salvas pelo recurso e são reduzidas proporcionalmente a até 1920 px para envio leve. Janelas protegidas, minimizadas ou com renderização incompatível podem mostrar captura vazia; confira a prévia ou use a aba/tela.

**Contexto → Usar texto da página e lista de abas** compartilha contexto textual opcional. **Permitir ações aprovadas no navegador** permite as ferramentas existentes de navegação; cada alteração continua sujeita à sua confirmação. A IA não controla outros aplicativos. Modelos precisam oferecer suporte às imagens/ferramentas escolhidas. Não inclui recursos de voz/vídeo ao vivo de outros produtos, nem conecta uma assinatura do ChatGPT como se fosse crédito de API.

## Recursos

| Área | Incluído |
|---|---|
| Navegação | Abas, voltar/avançar, pesquisa, nova aba e restauração dos endereços |
| Organização | Espaços com abas separadas, histórico, favoritos, lista de leitura, fontes e notas locais |
| Aparência | 10 visuais, abas superiores/laterais, modo foco, densidade, cores e imagem de fundo própria |
| Atalhos | Adicionar, editar, remover, arrastar, reordenar; lista própria ou sites mais visitados |
| Memória | Estimativa de RAM total, meta flexível, suspensão e descarregamento de abas |
| Extensões | Instalação local por pasta com `manifest.json`, ativação e remoção |
| Conta | Perfil Google por OAuth Desktop, com autorização no navegador padrão |
| Atualizações | GitHub Releases, progresso, SHA-256, cópia anterior e reinício |
| IA | Chat nativo com contexto, imagens/capturas e APIs OpenAI/compatíveis; ações opcionais com aprovação |

## Limitações conhecidas

- Compatibilidade de extensões depende do WebView2. Não há instalação direta da Chrome Web Store nem barra de popup de extensões.
- O login Google exige cadastrar credenciais OAuth Desktop em **Menu → Configurações avançadas**. Não inclui Chrome Sync nem sincronização de senhas, histórico ou favoritos na nuvem. Login Google dentro de sites pode ser recusado por ser um WebView.
- A meta de RAM não é um teto rígido. Abas com áudio ou proteção não são suspensas automaticamente. Descarregar uma aba pode perder formulários não salvos.
- Restaurar abas recupera endereços, sem garantir estado interno de páginas. A lista de downloads é da sessão atual.
- Não há modo anônimo, gerenciador próprio de senhas ou integração como navegador padrão nesta versão.
- Os testes de layout cobrem cenários definidos; uso diário, múltiplos monitores e escalas variadas continuam exigindo validação adicional.

## Personalizar

**Barra inferior:** fica oculta por padrão, inclusive em perfis existentes sem essa preferência. Ative **Personalizar → Layout → Mostrar barra inferior**. A escolha é salva; desativar recolhe a linha inteira. Tela cheia e modo foco a ocultam temporariamente.

**Fundo:** a arte aprovada do repositório tem 1440 × 576 pixels. O recorte permanece centralizado e proporcional. A versão 0.9.1 preserva a resolução de novas imagens personalizadas até 24 MP, sem reduzi-las a 1600 px. Não adiciona detalhe à arte pequena; para isso é necessária sua fonte em maior resolução.

Clique em **Personalizar** na nova aba ou em **Menu → Personalizar aparência**. O visual inicial é **Cristal do Egeu**, com tema e barra claros, abas no topo, espaçamento compacto, página Essencial e atalhos mais visitados. A colagem de Apolo/mar/vela cobre toda a página, centralizada e sem distorção. O painel Clean AI acompanha o tema escolhido. Selecione um dos dez visuais ou combine tema, barra clara/escura, abas laterais, cores, fundo, atalhos, densidade e animações. As escolhas são salvas automaticamente, com indicação da seleção ativa. Clicar fora de Aparência ou IA mantém a lateral aberta; use o botão de fechar do próprio painel. O clique fora do menu dispensa somente o menu. Use as seções **Visuais, Layout e Fundo**. **F8** alterna modo foco; **Ctrl+Shift+I** abre/fecha IA.

Na nova aba, **Adicionar atalho** cria um endereço HTTP/HTTPS. Botão direito permite editar, remover e mover; arraste para reordenar. Enter abre o selecionado; Delete remove; Ctrl+E edita. Google, GitHub e YouTube usam marcas vetoriais locais. Os demais favicons vêm de páginas já visitadas, sem consultas a serviços externos de ícones.

**Menu → Espaços de trabalho** cria, renomeia, abre e exclui espaços. Os endereços são restaurados por espaço se a restauração estiver ativa. Espaços organizam abas; compartilham o mesmo perfil de sites e cookies. Fontes são links guardados pelo usuário, sem verificação automática. Documentos são notas locais de texto, com importação TXT/Markdown e exportação TXT; não é um editor Office. O aspecto Cristal é uma composição visual sem desfoque contínuo.

## Validação

A preparação 0.9.1 passou por build Windows e 3.711 verificações de lógica, incluindo geometria de 1–21 itens (atalhos mais o botão Adicionar), três escalas, persistência e migração da barra inferior. Muitas dessas verificações são geométricas; a contagem não significa milhares de testes de integração.

A CI da implementação também passou no Windows com 359 verificações de interação: painéis, foco, clique seguinte, botões, extensão local e preferências. Consulte o [workflow validado](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/39996863911).

Consulte [a auditoria desta preparação](docs/AUDITORIA-0.9.1.md) para os resultados efetivamente executados e as pendências. O diagnóstico de entrada física depende de uma área de trabalho interativa em primeiro plano; renderização separada não comprova foco ou cliques.

## Tecnologia e estrutura

C# · .NET 10 · Windows Forms · Microsoft Edge WebView2 `1.0.4258.31`.

```text
src/                  Aplicativo Windows e atualizador
 tests/               Testes de lógica independentes de Windows
 Assets/Icons/         Ícones vetoriais editáveis
 ThirdParty/           Avisos de dependências
 docs/                 Uso, atualização, publicação e validação
 .github/workflows/    Compilação, layout Windows, release e teste de autoatualização
```

## Compilar

Com [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0):

```powershell
dotnet publish src/KayBrowser.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -o dist
```

Ou execute `Compilar-Windows.cmd`. Para desenvolvimento, use `dotnet run --project src/KayBrowser.csproj` no Windows.

## Testar

A CI executa lógica em Linux e a interface nativa no Windows. Os testes de lógica usam um diretório temporário; não aponte testes para dados pessoais.

```bash
dotnet run --project tests/Tests.csproj -c Release
```

No Windows, após publicar localmente:

```powershell
Start-Process dist/CleanBrowser.exe -ArgumentList '--layout-check' -Wait
Get-Content dist/layout-check.json
```

O diagnóstico abre uma janela isolada, verifica encaixe em três tamanhos, maximização/restauração, perda de foco, dez presets, abas laterais, IA, personalização, modo foco, troca de espaços, reordenação de atalhos e persistência de notas. As capturas usam DrawToBitmap dos controles do aplicativo, sem fotografar outras janelas. Conteúdo WebView2 é verificado pela fixture DOM e pode não aparecer nessas capturas. O teste de atualização da CI compila um cliente com versão anterior, baixa a release pública real, verifica o checksum, substitui o executável, confirma a cópia anterior e valida o reinício na versão nova.

Para validar renderização e a fixture WebView2 sem enviar entrada física à área de trabalho:

```powershell
Start-Process dist/CleanBrowser.exe -ArgumentList '--render-check' -Wait
Get-Content dist/layout-check.json
```

Esse modo registra explicitamente os cliques físicos como SKIPPED. Não substitui o teste de painéis, foco e clique seguinte.

## Publicar uma versão

1. Altere `<Version>` em `src/KayBrowser.csproj` e atualize `docs/RELEASE-NOTES.md`.
2. Envie a alteração para `main`.
3. O workflow testa, compila e publica uma release **somente quando a versão ainda não existe**.
4. Após publicar, executa o teste de atualização com o arquivo público. Confira todos os passos antes de anunciar a versão.

Mudanças sem incremento da versão não sobrescrevem releases existentes. Pull requests testam sem publicar. Consulte [o fluxo de publicação](docs/PUBLICACAO.md).

## Contribuir e relatar problemas

Veja [CONTRIBUTING.md](CONTRIBUTING.md). Ao relatar um problema visual, informe a versão do Clean Browser, versão do Windows, escala do monitor, tamanho da janela e passos para reproduzir. Evite incluir credenciais ou dados pessoais.

## Clean AI — funcionamento e limites

Abra pelo ícone de brilho ou **Ctrl+Shift+I**. As etapas de conexão, captura e conversa estão descritas na seção **Clean AI integrado (0.10.0)** acima.

- O contexto da página é opcional e limitado a 12 mil caracteres, 60 elementos e 30 abas do espaço ativo. Nenhum valor de formulário é extraído; imagens podem mostrar exatamente os pixels escolhidos pelo usuário, por isso existe prévia e recorte.
- As ferramentas do navegador só ficam disponíveis quando você as permite em **Contexto**. Abrir/selecionar abas, clicar, preencher campos comuns e rolar continuam exigindo aprovação. O modelo não recebe terminal, arquivos ou execução livre de JavaScript.
- A conversa mantém até seis pares de mensagens em memória; **Nova conversa** limpa histórico e imagem. A captura anexada é pontual e permanece visível até remover/trocar. Não há acesso remoto desta conversa do ChatGPT ao computador.
- São até seis rodadas de ferramentas e 1.800 tokens de saída por chamada, com **Parar** e prazo de quatro minutos por envio. Sem modelo embarcado, processo de IA local, SDK de agente pesado ou captura contínua.
- OpenAI Responses usa `store: false`. Isso não garante retenção zero; APIs compatíveis seguem suas próprias políticas. A chave é protegida por DPAPI e vinculada ao endereço escolhido, fora do repositório e de settings.json.
- Não inclui login de assinatura do ChatGPT, voz/vídeo ao vivo ou controle de outros aplicativos. A API e o modelo escolhidos precisam aceitar imagens/ferramentas para os recursos correspondentes.
- Extração DOM cobre o documento principal, sem iframe/shadow DOM. Páginas dinâmicas podem exigir nova leitura. Janelas protegidas, minimizadas ou incompatíveis podem impedir captura; confira a prévia ou use aba/tela.

Validação 0.10.0: 3.753 verificações lógicas; 25 verificações novas de conversa/imagem/captura de janela sintética/recorte/chaves; 359 verificações de interação Windows. Build e diagnóstico nativo passaram no [workflow de revisão](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/40006847679). As chamadas de IA usaram HTTP simulado e chaves fictícias: nenhuma chamada paga ou credencial pessoal foi usada, e compatibilidade com todos os provedores/janelas não é declarada. A publicação pelo app também exige o smoke test da release.

## Leveza e segurança

Os controles são Windows Forms: a interface e a IA não criam outro processo Chromium. Arte compartilhada decodificada uma vez, cache de favicons limitado em memória, transições curtas que param ao concluir e respeito à preferência de reduzir animações do Windows. O peso maior depende das páginas WebView2 abertas; não há promessa de teto rígido de RAM nem benchmark comparativo.

Os sites ficam com `AreHostObjectsAllowed=false` e `IsWebMessageEnabled=false`, pois não precisam da ponte de objetos/mensagens nativa. Os atalhos aceitam HTTP/HTTPS sem credenciais na URL. A chave de IA continua protegida por DPAPI; ações da IA exigem confirmação e contexto compartilhado é opcional. SHA-256 detecta corrupção do download, mas não substitui assinatura de código ou auditoria de segurança.

## Menu e interações da 0.8

O menu é um painel compacto integrado ao navegador, organizado em Navegar, Organizar e Mais. Clique fora ou pressione Escape para fechá-lo. Zoom, Aparência e Atualizar ficam no rodapé. A personalização salva imediatamente cada opção, sem botão Aplicar.

O diagnóstico Windows testa os comandos de menu, cliques fora repetidos, Escape, perda de foco, botões da barra e da IA, presets e opções de aparência, persistência, rejeição de cor inválida, cancelamento de seletor e conclusão/interrupção das animações. `interaction-check.json` e capturas ficam no artefato do Actions. Isso cobre os cenários descritos; não é garantia de ausência de todos os bugs. Operações reais de Google/API OpenAI exigem credenciais e não são simuladas como sucesso.
