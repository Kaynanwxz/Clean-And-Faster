# Publicação e atualizações

Canal oficial: `Kaynanwxz/Clean-And-Faster`. A Vercel não participa deste fluxo.

A branch `main` executa `.github/workflows/windows.yml`. Após os testes de lógica e de layout Windows, o workflow lê a versão do projeto, cria a release `vVERSAO` com `gh release create` e anexa `KayBrowser.exe`, `KayBrowser.exe.sha256` e um ZIP opcional. A tag aponta para o commit exato da compilação.

O `GITHUB_TOKEN` temporário é fornecido pelo próprio Actions, com `contents: write` limitado ao job de release. O aplicativo distribuído não contém token e lê somente releases públicas. Nunca coloque um token pessoal no exe.

Releases existentes são preservadas. Para corrigir uma release, publique uma versão maior; não substitua silenciosamente um executável já distribuído. O botão compara a versão do assembly com a tag da última release estável e só oferece versões maiores.

O teste pós-publicação compila um cliente de teste com versão anterior e usa a release pública real. Ele não abre sites nem usa dados pessoais. Logs e arquivos `layout-check.json`/`update-receipt.json` são preservados como artefatos de Actions. Esse teste simula uma versão anterior com o mesmo código de atualização: não testa todas as migrações possíveis de versões históricas.

A release é publicada antes do teste de atualização de ponta a ponta, pois ele precisa do download público. Se esse último teste falhar, trate a release como não validada, corrija o problema e publique uma versão maior. O README nunca deve afirmar sucesso sem conferir a execução correspondente.

No Windows, o instalador guarda `KayBrowser.exe.previous`, espera o processo atual encerrar, substitui somente o exe e reinicia. Não pede administrador. Arquivos em `%LOCALAPPDATA%\KayBrowser` permanecem no lugar. Se o executável estiver em uma pasta sem permissão de escrita, a atualização falha antes de fechar o app.
