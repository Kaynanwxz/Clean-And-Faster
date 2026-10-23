# Clean Browser 0.8.0

- Menu reconstruído como painel integrado compacto, com Navegar, Organizar e Mais. Zoom, aparência e atualização têm acesso fixo.
- Clicar fora ou pressionar Escape fecha apenas o menu. Não há descarte de ícones/controles dentro do evento nativo de fechamento. Comandos são despachados depois de esconder o painel.
- Aparência dividida em Visuais, Layout e Fundo. As opções são salvas imediatamente; cartões indicam seleção. Removida a mistura entre alteração imediata e botão Aplicar.
- Personalização, tema, abas, densidade, atalhos, cores, fundo, foco e animações agora usam o mesmo fluxo de atualização.
- Página inicial preserva os controles ao personalizar. Abrir Extensões sem um site não substitui a nova aba por uma página vazia.
- Deslize de entrada/saída dos painéis, transição das seções de aparência, entrada da nova aba e destaque suave dos atalhos. Temporizadores param ao concluir e respeitam redução de animações do Windows.
- Troca entre imagem própria e arte padrão corrigida; remoção limpa o arquivo criado pelo app.
- Diálogos de permissão e download agendados fora dos callbacks nativos do WebView2 para evitar travamentos por reentrada.
- Diagnóstico Windows ampliado com cliques reais fora do menu, Escape, perda de foco, vinte ciclos de abertura/fechamento, todos os comandos de menu, botões da barra e da IA, opções de aparência, cores inválidas, persistência, cancelamento de seletor de arquivos e cliques rápidos durante transições.

## Atualizar

Clique **Atualizar → Atualizar e reiniciar**. Não precisa extrair ZIP. Dados e perfil permanecem no mesmo local.

## Personalizar

**Menu → Aparência** abre as seções Visuais, Layout e Fundo. As mudanças são automáticas. A opção Restaurar retorna ao Copiloto Azul.

Os testes usam dados isolados, página local WebView2 e protocolo de IA simulado. Login Google real, chamadas pagas da IA e compatibilidade com todas as extensões continuam dependendo de credenciais/serviços e não são cobertos pelo diagnóstico local.
