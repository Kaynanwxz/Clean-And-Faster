# Clean Browser — auditoria da preparação 0.9.1

Data: 23/10/2026. Base: commit 2d26a42a398e897db312c5cb9fce2f0d6ac537a2 de Kaynanwxz/Clean-And-Faster (main, versão 0.9.0).

## Estado da entrega

Preparação 0.9.1 compilada localmente para Windows x64, autocontida, com WebView2 Runtime externo. Sem release 0.9.1 publicada e sem substituição do executável em uso pelo usuário. As alterações ficam em branch de revisão. README atualizado.

## Inspeção

O repositório usa C#/.NET 10, Windows Forms e Microsoft.Web.WebView2 1.0.4258.31; não usa uma interface web simulada como shell. CONTRIBUTING.md lido; nenhuma AGENTS.md encontrada na árvore do repositório. As releases 0.8.1 e 0.9.0 existem. O workflow 39942581662 da base terminou com sucesso nas tarefas de lógica, Windows e atualização; isso é histórico consultado, não evidência de validação desta preparação.

O checkout de origem estava limpo. O trabalho foi feito em cópia separada e mantém assets, credenciais configuradas, formato dos dados e caminho %LOCALAPPDATA%\KayBrowser. Testes usam diretórios temporários. O navegador já em uso em D:\Downloads não foi encerrado, substituído nem atualizado.

## Correções

- ShowBottomBar=false quando a preferência está ausente. Personalizar → Layout → Mostrar barra inferior salva imediatamente. A linha da tabela tem altura zero quando oculta; foco e tela cheia não apagam a preferência.
- A grade usa a contagem real dos atalhos exibidos, incluindo Mais visitados e Adicionar. Cada última linha incompleta é centralizada. Desenho e hit testing compartilham a geometria. O título da nova aba usa a largura real medida do texto.
- A arte aprovada apollo-sea.png tem 1440 × 576; seu hash/arquivo foi preservado. Cover já fazia recorte proporcional centralizado. Removida a redução a 1600 px das novas imagens personalizadas; importação e cache mantêm os pixels originais até 24 MP. Imagens antigas já reduzidas precisam ser reimportadas da fonte para recuperar detalhe.
- Nenhuma nova arte, paleta ou composição de referência foi inventada. As imagens 1, 2 e 3 da conversa anterior não foram recuperadas; o visual existente foi mantido.
- Google, GitHub e YouTube já usam desenho vetorial local. O build gera dez tamanhos ICO (16,20,24,32,40,48,64,96,128,256) a partir da marca existente, com margem e cantos transparentes. Janela e executável usam o ícone; aparência na barra de tarefas ainda depende de teste visual no desktop do usuário.
- Testes lógicos agora configuram seu próprio diretório temporário no Windows/Linux. Adicionado --render-check para separar renderização de entrada física. Capturas do diagnóstico usam controles da própria janela; WebView2 não aparece necessariamente em DrawToBitmap.

## Testes desta sessão

| Verificação | Resultado e limite |
|---|---|
| Build/publish do .exe x64 autocontido | PASS; warning NU1510 sobre ProtectedData redundante no framework, sem erro |
| Lógica | PASS: 3.711 verificações. Inclui URLs, persistência/recuperação, manifestos, IA com HTTP simulado e geometria. A maioria das novas verificações é geométrica, não integração |
| Grade | 1–21 itens totais (0–20 atalhos mais Adicionar), larguras 270/510/760/1200 e escalas 96/144/192 DPI; centralização de cada linha e ausência de sobreposição |
| Migração da barra | Perfil sem propriedade começa oculto; true e false persistem; tema, layout e atalho personalizado preservados |
| Entrada física --layout-check | FAIL na primeira dispensa externa do menu nas duas tentativas. Capturas iniciais mostraram outra janela em primeiro plano. Resultado inconclusivo quanto ao produto; NÃO declarar foco/menu corrigidos. Capturas que incluíam outra janela foram removidas |
| Renderização --render-check | PASS no .exe final: 900×600, 1320×860 e 1920×1080 a 96 DPI, maximizar/restaurar, layout com IA, dez presets, imagem 3840×2160 sem redução, barra inferior sem espaço vazio, persistência, espaços e fixture DOM real WebView2/DPAPI. Entrada física registrada SKIPPED; testes de foco aqui verificam estabilidade de layout, não o destino real dos cliques |
| Atualizador real | PASS em cópia 0.8.9 isolada → release pública 0.9.0; download, SHA-256, backup .previous, substituição e update-receipt.json com version=0.9.0/status=restarted |
| Integridade do arquivo atualizado | SHA-256 b1c3c09d9ac470de2ae3e8976e92c4785208ca79c38739009bb787f9982e60a0, igual ao checksum publicado |
| Rollback e cancelamento de atualização real | Falhas não foram injetadas nesta sessão; cobertura lógica e inspeção não substituem esse teste |
| Preservação dos dados | Persistência/migração em fixtures passou. Atualização não escreve histórico/favoritos/settings no instalador. Não foi executada atualização sobre o perfil pessoal do usuário |

## Auditoria dos recursos

| Recurso | Classificação | Alcance confirmado / pendência |
|---|---|---|
| Navegação e abas | Implementado; validação parcial | AddTab/CloseTab, WebView2, voltar/avançar/recarregar e restauração por espaço existem. Resolução de URLs testada. Testes físicos de botões não concluíram |
| Histórico | Implementado e testado na persistência | Record/SaveHistory, recuperação e limite de registros; fluxo completo da tela ainda pendente |
| Personalização | Implementado; validação parcial | Presets, tema, layouts, atalhos, imagem e barra inferior. Persistência lógica passou; teste de entrada física pendente |
| Login Google | Dependente de credenciais e validação externa | OAuth Desktop com PKCE/state e autorização no navegador padrão; salva nome/e-mail protegidos por DPAPI. Não faz Chrome Sync nem autentica automaticamente o usuário em todos os sites |
| Extensões | Implementado parcialmente | APIs reais de instalar pasta, listar, ativar e remover; cópia própria persistente. Sem Chrome Web Store nem barra de popups. Instalação de extensão real não revalidada nesta sessão |
| Controle de RAM | Implementado parcialmente | WorkingSet estimado do app/motor; TrySuspendAsync para abas ociosas sem áudio/proteção; descarregamento manual. Meta flexível, sem limite rígido; suspensão não medida nesta sessão |
| IA/OpenAI | Dependente de chave; validação parcial | Cliente real com chave própria e ações aprovadas. HTTP simulado passou; fixture DOM local no diagnóstico. Nenhuma chamada paga/live realizada |
| Atualização interna | Implementado e testado em cópia isolada | Botão, detecção, download, checksum, backup, substituição e reinício. GitHub ainda exige aliases KayBrowser.exe/KayBrowser.exe.sha256, presentes na release atual. Não testada atualização para 0.9.1, pois ela não foi publicada |
| Executável Windows | Implementado | .exe autocontido; requer WebView2. Não assinado com Authenticode |
| README e organização | Atualizado | Mantida estrutura existente; relatório e testes novos em docs/tests |

Ausentes no alcance atual: sincronização Google/Chrome, instalação direta de extensões da Chrome Web Store e teto rígido de RAM. Essas ausências são limites dos recursos existentes, sem interfaces fictícias adicionadas.

## Pendências concretas

1. Validar cliques fora, foco, clique seguinte e retorno à página em desktop interativo com Clean Browser em primeiro plano. O comportamento do código mantém Aparência/IA abertas; o menu é dispensável; Configurações avançadas são modais. Isso ainda precisa de comprovação física nesta preparação.
2. Fornecer a fonte aprovada do fundo acima de 1440 × 576 para melhorar o detalhe real da arte padrão. Reavaliar nitidez em janela/maximizada após trocar pela mesma arte em alta resolução.
3. Recuperar imagens aprovadas 1/2/3 antes de qualquer reformulação visual adicional.
4. Validar OAuth Google com cadastro Desktop, extensão compatível real, suspensão de abas sob carga e chamada OpenAI com chave. Nenhuma credencial foi solicitada ou gravada para esta auditoria.
5. Validar aparência do ícone na barra de tarefas (inclusive atalho fixado/cache do Windows), redimensionamento manual e múltiplos monitores.
6. Revisar e executar CI da branch; publicar 0.9.1 somente depois da revisão dos limites de interface. Nenhuma release nova foi criada automaticamente nesta sessão.

## Comandos

No Windows com .NET SDK 10 e WebView2 Runtime:

```powershell
dotnet build src/KayBrowser.csproj -c Release -p:NuGetAudit=false
dotnet run --project tests/Tests.csproj -c Release -p:NuGetAudit=false
dotnet publish src/KayBrowser.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -p:NuGetAudit=false -o dist
.\dist\CleanBrowser.exe
.\dist\CleanBrowser.exe --render-check
.\dist\CleanBrowser.exe --layout-check
```

Build, testes, publish e execução isolada foram usados nesta sessão. A execução normal sobre o perfil pessoal não foi usada para validação, pois já havia instância aberta. --layout-check exige primeiro plano e envia entrada física; --render-check não comprova foco/cliques. O nome de arquivo layout-check.json é compartilhado; copie os resultados de cada execução antes de iniciar outra.
