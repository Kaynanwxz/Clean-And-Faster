<div align="center">

# Clean And Faster
### Kay Browser para Windows

Uma interface própria em C# sobre o Microsoft Edge WebView2, com abas, personalização, histórico e atualizações pelo GitHub.

[![Windows build](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml/badge.svg)](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml)
[![Release](https://img.shields.io/github/v/release/Kaynanwxz/Clean-And-Faster)](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest)

**[Baixar KayBrowser.exe](https://github.com/Kaynanwxz/Clean-And-Faster/releases/latest/download/KayBrowser.exe)** · **[Versões](https://github.com/Kaynanwxz/Clean-And-Faster/releases)** · **[Relatar problema](https://github.com/Kaynanwxz/Clean-And-Faster/issues/new/choose)**

</div>

> Prévia para Windows 10/11 x64. A compilação, os diagnósticos e o teste de atualização são executados pelo GitHub Actions. Consulte o resultado do workflow associado à release; esses testes não equivalem a uma auditoria de segurança ou compatibilidade completa.

## Instalar e abrir

1. Baixe **KayBrowser.exe** pelo link acima.
2. Salve em uma pasta na qual seu usuário possa escrever e abra com dois cliques.
3. Mantenha o [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) instalado e atualizado.

O executável inclui o .NET; você não precisa instalar o SDK para usar. O WebView2 é separado e reaproveita o motor baseado em Chromium instalado no Windows. Isso reduz o pacote distribuído, mas não garante menor uso de RAM que outros navegadores. O executável ainda não tem assinatura Authenticode.

## Atualizar sem extrair arquivos

**Da versão 0.4.0 em diante:** clique em **Atualizar → Atualizar e reiniciar**. O app consulta as releases públicas deste repositório, baixa o executável, verifica o SHA-256, guarda uma cópia da versão anterior e reinicia. Não exige Vercel, token pessoal ou extração de ZIP.

**Se você já usa uma versão anterior:** abra **Menu → Personalizar → Atualizações pelo GitHub**, preencha `Kaynanwxz/Clean-And-Faster`, salve e use o botão **Atualizar** ou o item **Verificar atualizações**. Esse caminho está disponível nas versões anteriores que já têm o atualizador. Se o aplicativo antigo não abrir, substitua apenas o exe com o navegador fechado.

Se configurou um servidor alternativo, use **Atualizar → Canal de atualização → Usar canal oficial** na nova versão. Salve trabalhos e formulários antes do reinício.

Os dados permanecem em `%LOCALAPPDATA%\KayBrowser`. A atualização não apaga histórico, favoritos, configurações ou perfil de sites. A meta é atualizar todo o código do aplicativo por troca do executável; a atualização do motor WebView2 continua sendo gerenciada pela Microsoft.

## Recursos

| Área | Incluído |
|---|---|
| Navegação | Abas, voltar/avançar, pesquisa, nova aba e restauração dos endereços |
| Organização | Histórico pesquisável, favoritos e downloads da sessão |
| Aparência | Tema claro/escuro, cor de destaque, ícones próprios, zoom e tela cheia |
| Memória | Estimativa de RAM total, meta flexível, suspensão e descarregamento de abas |
| Extensões | Instalação local por pasta com `manifest.json`, ativação e remoção |
| Conta | Perfil Google por OAuth Desktop, com autorização no navegador padrão |
| Atualizações | GitHub Releases, progresso, SHA-256, cópia anterior e reinício |

## Limitações conhecidas

- Compatibilidade de extensões depende do WebView2. Não há instalação direta da Chrome Web Store nem barra de popup de extensões.
- O login Google exige cadastrar credenciais OAuth Desktop em **Personalizar**. Não inclui Chrome Sync nem sincronização de senhas, histórico ou favoritos na nuvem. Login Google dentro de sites pode ser recusado por ser um WebView.
- A meta de RAM não é um teto rígido. Abas com áudio ou proteção não são suspensas automaticamente. Descarregar uma aba pode perder formulários não salvos.
- Restaurar abas recupera endereços, sem garantir estado interno de páginas. A lista de downloads é da sessão atual.
- Não há modo anônimo, gerenciador próprio de senhas ou integração como navegador padrão nesta versão.
- Os testes de layout cobrem cenários definidos; uso diário, múltiplos monitores e escalas variadas continuam exigindo validação adicional.

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
XDG_DATA_HOME="$(mktemp -d -t kay-test-data-XXXXXXXX)" dotnet run --project tests/Tests.csproj -c Release
```

No Windows, após publicar localmente:

```powershell
Start-Process dist/KayBrowser.exe -ArgumentList '--layout-check' -Wait
Get-Content dist/layout-check.json
```

O diagnóstico abre uma janela isolada, verifica encaixe em três tamanhos, maximização/restauração, e salva capturas sem dados de usuário. O teste de atualização da CI compila um cliente com versão anterior, baixa a release pública real, verifica o checksum, substitui o executável, confirma a cópia anterior e valida o reinício na versão nova.

## Publicar uma versão

1. Altere `<Version>` em `src/KayBrowser.csproj` e atualize `docs/RELEASE-NOTES.md`.
2. Envie a alteração para `main`.
3. O workflow testa, compila e publica uma release **somente quando a versão ainda não existe**.
4. Após publicar, executa o teste de atualização com o arquivo público. Confira todos os passos antes de anunciar a versão.

Mudanças sem incremento da versão não sobrescrevem releases existentes. Pull requests testam sem publicar. Consulte [o fluxo de publicação](docs/PUBLICACAO.md).

## Contribuir e relatar problemas

Veja [CONTRIBUTING.md](CONTRIBUTING.md). Ao relatar um problema visual, informe a versão do Kay, versão do Windows, escala do monitor, tamanho da janela e passos para reproduzir. Evite incluir credenciais ou dados pessoais.
