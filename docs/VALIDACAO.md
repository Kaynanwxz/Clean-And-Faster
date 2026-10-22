# Validação da versão 0.4.0

Commit do aplicativo: `88a87ec2c7cdfc4135e1a5c905661eabc2a65938`.

[Execução e logs do GitHub Actions](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/39815572101) · [Release publicada](https://github.com/Kaynanwxz/Clean-And-Faster/releases/tag/v0.4.0).

## Resultados confirmados em 22/10/2026

- 55 verificações de lógica passaram localmente e no job Ubuntu: navegação, persistência, recuperação de JSON, validação do canal e manifestos de atualização.
- Aplicativo compilado para Windows x64 no executor **Windows Server 2025**, com .NET SDK 10.0.402.
- Diagnóstico nativo de encaixe passou a 96 DPI (100%), com janela 900 × 600, janela maior limitada pelo desktop do executor a 1044 × 788, maximização e restauração. As capturas geradas foram inspecionadas: ícones, barra de endereço e títulos ficaram dentro das barras.
- Uma cópia de teste com versão 0.3.1 consultou a release pública 0.4.0, baixou o exe, conferiu o SHA-256, preservou `KayBrowser.exe.previous`, substituiu o executável e reiniciou.
- O processo reiniciado gravou `{"version":"0.4.0","status":"restarted"}`. O hash instalado foi comparado ao binário publicado e o log registrou `PASS: GitHub API -> release download -> checksum -> replacement -> restart`.
- Download público também conferido fora do Actions: **52.102.650 bytes**, SHA-256 `8d4f80bd72d50eb7b52f3d71fee4a3dd8b506dda1106b03928e7d6c0b944a735`.

Os arquivos `layout-check.json`, capturas de layout e `update-receipt.json` ficam nos artefatos da execução acima. Em caso de atraso no painel de Actions, os logs e os artefatos são a evidência da execução; não deduza falha apenas de um status antigo.

## Limites

O cliente de versão anterior usado no teste foi compilado a partir do código atual com a versão do assembly alterada. Isso valida o mecanismo de troca/reinício, mas não cobre toda versão histórica ou migração de dados possível. As versões anteriores já distribuídas permitem informar o repositório em Personalizar e usar o atualizador existente.

Não foram testados nesta rodada: login Google com credenciais reais, todas as extensões, sites de uso diário, escalas de 125/150/200%, múltiplos monitores ou todas as políticas corporativas do Windows. Não houve auditoria independente de segurança. O teste confirma o fluxo de atualização em um ambiente Windows isolado, não garante ausência de qualquer falha em qualquer computador.
