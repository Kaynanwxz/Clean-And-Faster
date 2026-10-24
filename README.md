<div align="center">

# Clean Browser
### Navegador para Windows

Uma interface nativa em C# sobre o Microsoft Edge WebView2, com identidade azul-marinho inspirada na Gr�cia, dez visuais, atalhos edit�veis, espa�os e Clean AI.

[![Windows build](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml/badge.svg)](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml)
[![Release](https://img.shields.io/github/v/release/Kaynanwxz/Clean-And-Faster)](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest)

**[Baixar CleanBrowser.exe](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest/download/CleanBrowser.exe)** � **[Vers�es](https://github.com/Kaynanwxz/Clean-And-Faster/releases)** � **[Relatar problema](https://github.com/Kaynanwxz/Clean-And-Faster/issues/new/choose)**

</div>

> Pr�via para Windows 10/11 x64. A compila��o, os diagn�sticos e o teste de atualiza��o s�o executados pelo GitHub Actions. Consulte o resultado do workflow associado � release; esses testes n�o equivalem a uma auditoria de seguran�a ou compatibilidade completa.

## Instalar e abrir

1. Baixe **CleanBrowser.exe** pelo link acima.
2. Salve em uma pasta na qual seu usu�rio possa escrever e abra com dois cliques.
3. Mantenha o [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) instalado e atualizado.

O execut�vel inclui o .NET; voc� n�o precisa instalar o SDK para usar. O WebView2 � separado e reaproveita o motor baseado em Chromium instalado no Windows. Isso reduz o pacote distribu�do, mas n�o garante menor uso de RAM que outros navegadores. O execut�vel ainda n�o tem assinatura Authenticode.

## Atualizar sem extrair arquivos

**Da vers�o 0.4.0 em diante:** clique em **Atualizar  Atualizar e reiniciar**. O app consulta as releases p�blicas deste reposit�rio, baixa o execut�vel, verifica o SHA-256, guarda uma c�pia da vers�o anterior e reinicia. N�o exige Vercel, token pessoal ou extra��o de ZIP.

**Se voc� j� usa uma vers�o anterior:** abra **Menu  Configura��es avan�adas  Atualiza��es pelo GitHub**, preencha `Kaynanwxz/Clean-And-Faster`, salve e use o bot�o **Atualizar** ou o item **Verificar atualiza��es**. Esse caminho est� dispon�vel nas vers�es anteriores que j� t�m o atualizador. Se o aplicativo antigo n�o abrir, substitua apenas o exe com o navegador fechado.

Se configurou um servidor alternativo, use **Atualizar  Canal de atualiza��o  Usar canal oficial** na nova vers�o. Salve trabalhos e formul�rios antes do rein�cio.

Os dados permanecem em `%LOCALAPPDATA%\KayBrowser`. A atualiza��o n�o apaga hist�rico, favoritos, configura��es ou perfil de sites. A meta � atualizar todo o c�digo do aplicativo por troca do execut�vel; a atualiza��o do motor WebView2 continua sendo gerenciada pela Microsoft.

## Janela e transi��es revisadas (0.12.1)

A moldura usa o estado de borda real do Windows Forms, com �rea de trabalho calculada no monitor atual. F11 restaura o estado e o tamanho anteriores. As barras usam desenho com buffer para reduzir flashes no redimensionamento. A abertura de uma aba executa uma �nica ativa��o.

Abas usam uma curva gradual de 280 ms; o menu, 240 ms. Sess�es remotas deixam de desativar os efeitos automaticamente; a prefer�ncia de reduzir movimento do sistema e a op��o do aplicativo continuam respeitadas.

A valida��o Windows registra quadros intermedi�rios da janela sint�tica em primeiro plano (somente na CI), incluindo os comandos reais Ctrl+T/Ctrl+W e menu. Confere tamb�m �rea cliente, ciclos de minimizar/maximizar/restaurar, F11 e redimensionamento. Esses registros complementam os testes l�gicos; n�o equivalem a testar todo hardware ou todas as escalas de tela.

## Anima��es suaves (0.12.0)

Abas entram com expans�o curta e saem com recolhimento; as abas vizinhas e o bot�o de nova aba acompanham o movimento. Fechar uma aba libera seu conte�do imediatamente, sem esperar a anima��o. O menu abre com fade e deslocamento vertical discreto; fechar pelo bot�o usa uma sa�da breve. O clique externo continua dispensando o menu imediatamente, preservando foco e o pr�ximo clique.

Maximizar, restaurar e minimizar usam os comandos e transi��es nativos do Windows, preservando posi��o, �rea �til e comportamento da barra de tarefas. A dura��o desse efeito � controlada pelo Windows. **Personalizar  Layout  Anima��es e transi��es** desativa os efeitos do aplicativo; a prefer�ncia do sistema por reduzir movimento tamb�m � respeitada. N�o h� anima��o cont�nua em repouso.

## Gravura cl�ssica aprovada (0.11.1)

A arte padr�o da nova aba � a **vers�o 1 aprovada**: Apolo, veleiro e mar em gravura cl�ssica azul sobre papel claro. O PNG original aprovado est� incorporado ao execut�vel, sem recria��o, convers�o em pontos ou redu��o da opacidade no modo Arte. O enquadramento continua centralizado, proporcional e cobrindo a p�gina.

Ao atualizar da 0.11.0, somente a escolha do pontilhismo � migrada uma vez para a gravura aprovada; os demais ajustes, imagens pessoais, abas, hist�rico e dados permanecem. O efeito anterior continua opcional em **Personalizar  Fundo  Pontilhismo anterior (opcional)**. A p�gina em papel usa texto azul-marinho leg�vel tamb�m quando a barra do navegador est� escura; o tema da barra � preservado.

A imagem original tem 1672 � 941 pixels. Em telas maiores h� escala proporcional; n�o se declara que o arquivo seja 4K. O efeito antigo em pontos, caso ativado, continua sendo desenhado no tamanho da p�gina.

## Apps e visual (0.11.0)

Use o bot�o de quatro quadrados **Seus apps**, na barra, ou **Menu  Organizar  Seus apps**. Spotify, YouTube, Notion, Gmail e GitHub j� aparecem no cat�logo. Clique para abrir em uma aba do Clean Browser e fa�a login no pr�prio site. **Adicionar outro app** aceita nome e endere�o HTTPS; o bot�o direito remove do cat�logo. O cat�logo n�o cria WebViews nem acessa os sites at� voc� abrir um app. Reabrir procura a aba j� existente no espa�o atual. Voc� pode manter v�rios apps em abas, com o controle de mem�ria normal do navegador, que preserva abas reproduzindo �udio.

As sess�es usam o mesmo perfil WebView2 persistente do navegador. Remover do cat�logo n�o faz logout nem apaga a conta; encerre a sess�o pelo site quando quiser. Fechar o painel n�o fecha a aba nem para a m�sica. N�o h� coleta de senha pelo cat�logo, integra��o com API/IA, sincroniza��o das contas ou controle do Spotify instalado no Windows.

O novo emblema mant�m vela/est�tua e azul-marinho, azul glacial e prata, com silhueta transparente e tamanhos Windows de 16 a 256 px. Execut�vel, janela, abas e nova aba usam a mesma identidade. As superf�cies ficaram mais neutras, preservando os temas e a cor de destaque escolhidos.

Na vers�o 0.11.0, a arte padr�o passou a usar **pontilhismo** (agora opcional): pontos antialias s�o desenhados no tamanho f�sico da p�gina, inclusive em 4K, com um cache compartilhado para reduzir trabalho e mem�ria entre abas. A composi��o original � preservada. A fonte hist�rica continua em 1440 � 576: n�o se afirma que ela ganhou detalhes fotogr�ficos; a nitidez nova vem da renderiza��o dos pontos, sem ampliar um bitmap de pontinhos. Em **Personalizar  Fundo  Pontilhismo anterior (opcional)** voc� pode desativar o efeito. Fundos personalizados permanecem com seus pixels originais.

Valida��o local: 3.770 verifica��es l�gicas; 30 verifica��es novas de apps/pain�is/editor/abas/cookie de teste/remo��o/pontilhismo, al�m das verifica��es existentes de IA e layout. Os sites p�blicos de Spotify, YouTube e Notion abriram no execut�vel com perfil isolado, e o teste de disponibilidade Widevine retornou suporte no runtime instalado. Isso n�o testa login de contas pessoais, reprodu��o autenticada de m�sicas, edi��o de p�ginas privadas ou todos os provedores. O diagn�stico opcional `dist/CleanBrowser.exe --apps-check` consulta apenas p�ginas p�blicas em perfil tempor�rio e grava app-compatibility.json. A CI de Windows e o smoke test do atualizador s�o obrigat�rios antes de considerar a entrega conclu�da.

## Clean AI integrado (0.10.0)

Abra **Clean AI  Conectar IA**. Escolha OpenAI, OpenRouter ou Personalizada, informe o modelo e a chave da sua API, use **Testar conex�o** e salve. O teste faz um pedido curto � API e pode consumir cr�dito. Endere�os personalizados terminam em /responses ou /chat/completions; HTTP s� � aceito no pr�prio computador. N�o � preciso instalar outro aplicativo, extens�o ou SDK.

Converse no painel, continue com perguntas sobre o mesmo assunto e use **Nova conversa** para limpar o contexto desta sess�o. A conversa mant�m at� seis pares de mensagens e 32 mil caracteres em mem�ria; n�o � salva no disco nem sincronizada. Mudar a conex�o tamb�m limpa o contexto. A chave usa prote��o do Windows e fica vinculada ao endere�o escolhido, preservando chaves OpenAI existentes.

**Mostrar tela ou janela** abre o seletor nativo. Escolha a aba, uma janela espec�fica ou um monitor, clique em **Capturar**, arraste na pr�via se quiser recortar, confirme em **Usar esta imagem** e clique em **Enviar**. A captura � pontual; n�o h� v�deo/grava��o em segundo plano. Ela permanece anexada, vis�vel, para perguntas seguintes at� remover, trocar ou iniciar uma nova conversa. Remover impede os pr�ximos envios; n�o retira o que j� foi enviado ao provedor. Imagens n�o s�o salvas pelo recurso e s�o reduzidas proporcionalmente a at� 1920 px para envio leve. Janelas protegidas, minimizadas ou com renderiza��o incompat�vel podem mostrar captura vazia; confira a pr�via ou use a aba/tela.

**Contexto  Usar texto da p�gina e lista de abas** compartilha contexto textual opcional. **Permitir a��es aprovadas no navegador** permite as ferramentas existentes de navega��o; cada altera��o continua sujeita � sua confirma��o. A IA n�o controla outros aplicativos. Modelos precisam oferecer suporte �s imagens/ferramentas escolhidas. N�o inclui recursos de voz/v�deo ao vivo de outros produtos, nem conecta uma assinatura do ChatGPT como se fosse cr�dito de API.

## Recursos

| �rea | Inclu�do |
|---|---|
| Navega��o | Abas, voltar/avan�ar, pesquisa, nova aba e restaura��o dos endere�os |
| Organiza��o | Espa�os com abas separadas, hist�rico, favoritos, lista de leitura, fontes e notas locais |
| Apar�ncia | 10 visuais, abas superiores/laterais, modo foco, densidade, cores e imagem de fundo pr�pria |
| Atalhos | Adicionar, editar, remover, arrastar, reordenar; lista pr�pria ou sites mais visitados |
| Mem�ria | Estimativa de RAM total, meta flex�vel, suspens�o e descarregamento de abas |
| Extens�es | Instala��o local por pasta com `manifest.json`, ativa��o e remo��o |
| Conta | Perfil Google por OAuth Desktop, com autoriza��o no navegador padr�o |
| Atualiza��es | GitHub Releases, progresso, SHA-256, c�pia anterior e rein�cio |
| IA | Chat nativo com contexto, imagens/capturas e APIs OpenAI/compat�veis; a��es opcionais com aprova��o |

## Limita��es conhecidas

- Compatibilidade de extens�es depende do WebView2. N�o h� instala��o direta da Chrome Web Store nem barra de popup de extens�es.
- O login Google exige cadastrar credenciais OAuth Desktop em **Menu  Configura��es avan�adas**. N�o inclui Chrome Sync nem sincroniza��o de senhas, hist�rico ou favoritos na nuvem. Login Google dentro de sites pode ser recusado por ser um WebView.
- A meta de RAM n�o � um teto r�gido. Abas com �udio ou prote��o n�o s�o suspensas automaticamente. Descarregar uma aba pode perder formul�rios n�o salvos.
- Restaurar abas recupera endere�os, sem garantir estado interno de p�ginas. A lista de downloads � da sess�o atual.
- N�o h� modo an�nimo, gerenciador pr�prio de senhas ou integra��o como navegador padr�o nesta vers�o.
- Os testes de layout cobrem cen�rios definidos; uso di�rio, m�ltiplos monitores e escalas variadas continuam exigindo valida��o adicional.

## Personalizar

**Barra inferior:** fica oculta por padr�o, inclusive em perfis existentes sem essa prefer�ncia. Ative **Personalizar  Layout  Mostrar barra inferior**. A escolha � salva; desativar recolhe a linha inteira. Tela cheia e modo foco a ocultam temporariamente.

**Fundo:** a gravura de Apolo aprovada tem 1672 � 941 pixels. A imagem hist�rica opcional do pontilhismo tem 1440 � 576 pixels. O recorte permanece centralizado e proporcional. A vers�o 0.9.1 preserva a resolu��o de novas imagens personalizadas at� 24 MP, sem reduzi-las a 1600 px. N�o adiciona detalhe � arte pequena; para isso � necess�ria sua fonte em maior resolu��o.

Clique em **Personalizar** na nova aba ou em **Menu  Personalizar apar�ncia**. O visual inicial � **Cristal do Egeu**, com tema e barra claros, abas no topo, espa�amento compacto, p�gina Essencial e atalhos mais visitados. A colagem de Apolo/mar/vela cobre toda a p�gina, centralizada e sem distor��o. O painel Clean AI acompanha o tema escolhido. Selecione um dos dez visuais ou combine tema, barra clara/escura, abas laterais, cores, fundo, atalhos, densidade e anima��es. As escolhas s�o salvas automaticamente, com indica��o da sele��o ativa. Clicar fora de Apar�ncia ou IA mant�m a lateral aberta; use o bot�o de fechar do pr�prio painel. O clique fora do menu dispensa somente o menu. Use as se��es **Visuais, Layout e Fundo**. **F8** alterna modo foco; **Ctrl+Shift+I** abre/fecha IA.

Na nova aba, **Adicionar atalho** cria um endere�o HTTP/HTTPS. Bot�o direito permite editar, remover e mover; arraste para reordenar. Enter abre o selecionado; Delete remove; Ctrl+E edita. Google, GitHub e YouTube usam marcas vetoriais locais. Os demais favicons v�m de p�ginas j� visitadas, sem consultas a servi�os externos de �cones.

**Menu  Espa�os de trabalho** cria, renomeia, abre e exclui espa�os. Os endere�os s�o restaurados por espa�o se a restaura��o estiver ativa. Espa�os organizam abas; compartilham o mesmo perfil de sites e cookies. Fontes s�o links guardados pelo usu�rio, sem verifica��o autom�tica. Documentos s�o notas locais de texto, com importa��o TXT/Markdown e exporta��o TXT; n�o � um editor Office. O aspecto Cristal � uma composi��o visual sem desfoque cont�nuo.

## Valida��o

A prepara��o 0.9.1 passou por build Windows e 3.711 verifica��es de l�gica, incluindo geometria de 1-21 itens (atalhos mais o bot�o Adicionar), tr�s escalas, persist�ncia e migra��o da barra inferior. Muitas dessas verifica��es s�o geom�tricas; a contagem n�o significa milhares de testes de integra��o.

A CI da implementa��o tamb�m passou no Windows com 359 verifica��es de intera��o: pain�is, foco, clique seguinte, bot�es, extens�o local e prefer�ncias. Consulte o [workflow validado](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/39996863911).

Consulte [a auditoria desta prepara��o](docs/AUDITORIA-0.9.1.md) para os resultados efetivamente executados e as pend�ncias. O diagn�stico de entrada f�sica depende de uma �rea de trabalho interativa em primeiro plano; renderiza��o separada n�o comprova foco ou cliques.

## Tecnologia e estrutura

C# � .NET 10 � Windows Forms � Microsoft Edge WebView2 `1.0.4258.31`.

```text
src/                  Aplicativo Windows e atualizador
 tests/               Testes de l�gica independentes de Windows
 Assets/Icons/         �cones vetoriais edit�veis
 ThirdParty/           Avisos de depend�ncias
 docs/                 Uso, atualiza��o, publica��o e valida��o
 .github/workflows/    Compila��o, layout Windows, release e teste de autoatualiza��o
```

## Compilar

Com [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0):

```powershell
dotnet publish src/KayBrowser.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -o dist
```

Ou execute `Compilar-Windows.cmd`. Para desenvolvimento, use `dotnet run --project src/KayBrowser.csproj` no Windows.

## Testar

A CI executa l�gica em Linux e a interface nativa no Windows. Os testes de l�gica usam um diret�rio tempor�rio; n�o aponte testes para dados pessoais.

```bash
dotnet run --project tests/Tests.csproj -c Release
```

No Windows, ap�s publicar localmente:

```powershell
Start-Process dist/CleanBrowser.exe -ArgumentList '--layout-check' -Wait
Get-Content dist/layout-check.json
```

O diagn�stico abre uma janela isolada, verifica encaixe em tr�s tamanhos, maximiza��o/restaura��o, perda de foco, dez presets, abas laterais, IA, personaliza��o, modo foco, troca de espa�os, reordena��o de atalhos e persist�ncia de notas. As capturas usam DrawToBitmap dos controles do aplicativo, sem fotografar outras janelas. Conte�do WebView2 � verificado pela fixture DOM e pode n�o aparecer nessas capturas. O teste de atualiza��o da CI compila um cliente com vers�o anterior, baixa a release p�blica real, verifica o checksum, substitui o execut�vel, confirma a c�pia anterior e valida o rein�cio na vers�o nova.

Para validar renderiza��o e a fixture WebView2 sem enviar entrada f�sica � �rea de trabalho:

```powershell
Start-Process dist/CleanBrowser.exe -ArgumentList '--render-check' -Wait
Get-Content dist/layout-check.json
```

Esse modo registra explicitamente os cliques f�sicos como SKIPPED. N�o substitui o teste de pain�is, foco e clique seguinte.

## Publicar uma vers�o

1. Altere `<Version>` em `src/KayBrowser.csproj` e atualize `docs/RELEASE-NOTES.md`.
2. Envie a altera��o para `main`.
3. O workflow testa, compila e publica uma release **somente quando a vers�o ainda n�o existe**.
4. Ap�s publicar, executa o teste de atualiza��o com o arquivo p�blico. Confira todos os passos antes de anunciar a vers�o.

Mudan�as sem incremento da vers�o n�o sobrescrevem releases existentes. Pull requests testam sem publicar. Consulte [o fluxo de publica��o](docs/PUBLICACAO.md).

## Contribuir e relatar problemas

Veja [CONTRIBUTING.md](CONTRIBUTING.md). Ao relatar um problema visual, informe a vers�o do Clean Browser, vers�o do Windows, escala do monitor, tamanho da janela e passos para reproduzir. Evite incluir credenciais ou dados pessoais.

## Clean AI - funcionamento e limites

Abra pelo �cone de brilho ou **Ctrl+Shift+I**. As etapas de conex�o, captura e conversa est�o descritas na se��o **Clean AI integrado (0.10.0)** acima.

- O contexto da p�gina � opcional e limitado a 12 mil caracteres, 60 elementos e 30 abas do espa�o ativo. Nenhum valor de formul�rio � extra�do; imagens podem mostrar exatamente os pixels escolhidos pelo usu�rio, por isso existe pr�via e recorte.
- As ferramentas do navegador s� ficam dispon�veis quando voc� as permite em **Contexto**. Abrir/selecionar abas, clicar, preencher campos comuns e rolar continuam exigindo aprova��o. O modelo n�o recebe terminal, arquivos ou execu��o livre de JavaScript.
- A conversa mant�m at� seis pares de mensagens em mem�ria; **Nova conversa** limpa hist�rico e imagem. A captura anexada � pontual e permanece vis�vel at� remover/trocar. N�o h� acesso remoto desta conversa do ChatGPT ao computador.
- S�o at� seis rodadas de ferramentas e 1.800 tokens de sa�da por chamada, com **Parar** e prazo de quatro minutos por envio. Sem modelo embarcado, processo de IA local, SDK de agente pesado ou captura cont�nua.
- OpenAI Responses usa `store: false`. Isso n�o garante reten��o zero; APIs compat�veis seguem suas pr�prias pol�ticas. A chave � protegida por DPAPI e vinculada ao endere�o escolhido, fora do reposit�rio e de settings.json.
- N�o inclui login de assinatura do ChatGPT, voz/v�deo ao vivo ou controle de outros aplicativos. A API e o modelo escolhidos precisam aceitar imagens/ferramentas para os recursos correspondentes.
- Extra��o DOM cobre o documento principal, sem iframe/shadow DOM. P�ginas din�micas podem exigir nova leitura. Janelas protegidas, minimizadas ou incompat�veis podem impedir captura; confira a pr�via ou use aba/tela.

Valida��o 0.10.0: 3.753 verifica��es l�gicas; 25 verifica��es novas de conversa/imagem/captura de janela sint�tica/recorte/chaves; 359 verifica��es de intera��o Windows. Build e diagn�stico nativo passaram no [workflow de revis�o](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/40006847679). As chamadas de IA usaram HTTP simulado e chaves fict�cias: nenhuma chamada paga ou credencial pessoal foi usada, e compatibilidade com todos os provedores/janelas n�o � declarada. A publica��o pelo app tamb�m exige o smoke test da release.

## Leveza e seguran�a

Os controles s�o Windows Forms: a interface e a IA n�o criam outro processo Chromium. Arte compartilhada decodificada uma vez, cache de favicons limitado em mem�ria, transi��es curtas que param ao concluir e respeito � prefer�ncia de reduzir anima��es do Windows. O peso maior depende das p�ginas WebView2 abertas; n�o h� promessa de teto r�gido de RAM nem benchmark comparativo.

Os sites ficam com `AreHostObjectsAllowed=false` e `IsWebMessageEnabled=false`, pois n�o precisam da ponte de objetos/mensagens nativa. Os atalhos aceitam HTTP/HTTPS sem credenciais na URL. A chave de IA continua protegida por DPAPI; a��es da IA exigem confirma��o e contexto compartilhado � opcional. SHA-256 detecta corrup��o do download, mas n�o substitui assinatura de c�digo ou auditoria de seguran�a.

## Menu e intera��es da 0.8

O menu � um painel compacto integrado ao navegador, organizado em Navegar, Organizar e Mais. Clique fora ou pressione Escape para fech�-lo. Zoom, Apar�ncia e Atualizar ficam no rodap�. A personaliza��o salva imediatamente cada op��o, sem bot�o Aplicar.

O diagn�stico Windows testa os comandos de menu, cliques fora repetidos, Escape, perda de foco, bot�es da barra e da IA, presets e op��es de apar�ncia, persist�ncia, rejei��o de cor inv�lida, cancelamento de seletor e conclus�o/interrup��o das anima��es. `interaction-check.json` e capturas ficam no artefato do Actions. Isso cobre os cen�rios descritos; n�o � garantia de aus�ncia de todos os bugs. Opera��es reais de Google/API OpenAI exigem credenciais e n�o s�o simuladas como sucesso.

