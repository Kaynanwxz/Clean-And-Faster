# Validação da versão 0.8

[Execuções e resultados no Windows](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml) · [Correção e revisão](https://github.com/Kaynanwxz/Clean-And-Faster/pull/1)

O diagnóstico usa o mesmo ciclo de eventos do aplicativo, com dados isolados. A release só é publicada depois dos testes de lógica e da interface passarem. A atualização pública é verificada depois da publicação.

| Área | Cenários verificados |
|---|---|
| Menu | 21 comandos; cliques fora na página inicial e em WebView2 real; Escape; perda de foco; 20 reaberturas; cliques rápidos |
| Aparência | 10 presets pelos cartões; tema, abas, barra, densidade, 7 layouts, 3 modos de atalhos, foco, animações, cores, rejeição de cor inválida, restaurar e persistência |
| Imagens | Cancelar seletor, importar PNG real, voltar à arte padrão, reutilizar a imagem escolhida e remover arquivo próprio |
| Diálogos | Criar/editar/cancelar atalho; criar/renomear/abrir/excluir espaço; criar/editar/remover nota; salvar fonte; filtrar/apagar histórico; remover favorito |
| Navegação | Voltar, avançar e recarregar páginas locais reais, estrela de favoritos, zoom e botão início |
| Memória e janela | Proteger/desproteger e descarregar WebView real; minimizar, maximizar, restaurar e fechar janela de teste |
| Extensões | Instalar uma extensão local de teste pela API real; desativar, reativar e remover pelos botões |
| Download | Iniciar download real de teste e cancelar o seletor sem gravar arquivo ou travar |
| IA | Botões do painel, salvar modelo, remover chave, parar tarefa, leitura/preenchimento/clique com confirmação, referência expirada e contexto por espaço |
| Movimento | Menu, lateral e seções; fim dos temporizadores; abrir/fechar rapidamente; redução de animações |

Os arquivos `interaction-check.json`, `interaction-progress.log`, `layout-check.json` e capturas reais ficam no artefato **Windows-build-and-layout**. Os 115 testes de lógica incluem navegação, armazenamento, migração, atualização e protocolo da IA.

A instalação de extensão usa uma fixture controlada; isso não comprova compatibilidade com todas as extensões. Seletores nativos são exercitados pelo cancelamento; importação de imagem usa o mesmo caminho de processamento com um PNG de teste. Login Google e chamadas pagas OpenAI não usam credenciais reais nesta validação. A interface é executada a 96 DPI no Windows Server do Actions; múltiplos monitores e demais escalas não são cobertos. Esses resultados cobrem os cenários descritos, sem garantia de ausência de todos os bugs.

---

# Validação da família 0.7

[Execução confirmada da 0.7.0](https://github.com/Kaynanwxz/Clean-And-Faster/actions/runs/39867960481) · [Execuções e logs atuais](https://github.com/Kaynanwxz/Clean-And-Faster/actions/workflows/windows.yml)

## Evidência confirmada em 22/10/2026

- 115 verificações de lógica passaram localmente e na CI Ubuntu: navegação, persistência, JSON danificado, canais e manifestos de atualização, protocolo da IA, dez presets, atalhos e migração de dados antigos para espaços.
- A 0.7.0 compilou no Windows Server 2025 com .NET SDK 10.0.402 e passou no diagnóstico nativo a 96 DPI: janela 900 × 600, janela maior limitada pelo desktop do executor a 1044 × 788, maximização/restauração, perda/retorno de foco e painel IA.
- Os dez visuais, abas laterais, painel de aparência, foco, troca de espaços, reordenação de atalhos e persistência da biblioteca passaram no diagnóstico Windows. Capturas do aplicativo foram inspecionadas.
- Fixture WebView2 isolada confirmou leitura sem valores de formulário, preenchimento/clique com diálogo de aprovação, rejeição de elemento expirado e armazenamento/remoção de chave por DPAPI.
- Um cliente com versão de assembly 0.3.1 baixou a release pública 0.7.0, verificou SHA-256, preservou `.previous`, substituiu o executável e reiniciou. Recibo: `{"version":"0.7.0","status":"restarted"}`. O hash instalado correspondeu ao arquivo publicado; log `PASS: GitHub API -> release download -> checksum -> replacement -> restart`.

## Cobertura adicional da 0.7.1

O diagnóstico também verifica cancelamento da tarefa ao trocar de espaço, contexto de IA restrito ao espaço ativo, ponte nativa de mensagens/objetos desativada, compartilhamento do papel de parede em memória e limites dos controles da nova aba. Consulte a execução associada à release para o resultado final; a publicação depende do diagnóstico passar e é seguida pelo teste de atualização real.

## Limites

O cliente antigo usado no teste é compilado a partir do código atual com a versão do assembly alterada. Isso valida o mecanismo de troca/reinício, sem cobrir todo binário histórico. As versões anteriores já distribuídas permitem informar o repositório e usar o atualizador existente.

Não foram usados credenciais Google ou chave de API OpenAI reais. O protocolo da IA usa HTTP simulado; o teste de DOM usa WebView2 real com página local. Não houve consumo real de API nem auditoria independente de segurança. Fontes da biblioteca não são automaticamente verificadas.

Não foram validados todos os sites/extensões, monitores múltiplos, escalas de 125/150/200% ou políticas corporativas. Não há benchmark que comprove menor consumo que Chrome/Edge nem garantia de teto rígido de RAM. Os dados de teste são isolados dos dados pessoais.

---

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
