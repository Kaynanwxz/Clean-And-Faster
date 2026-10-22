# Clean Browser 0.6.5

- Moldura própria preservada ao alternar o foco para outro aplicativo.
- Transições suaves nos botões, atalhos e indicador da aba selecionada.
- Painel lateral Clean AI com abertura animada e atalho Ctrl+Shift+I.
- IA pode explicar, resumir páginas, listar/abrir/selecionar abas e propor cliques, preenchimentos e rolagem. Ações exigem confirmação individual.
- Compartilhamento de páginas e abas opcional por tarefa. Valores de formulários e campos de senha não são extraídos. Chave própria da API OpenAI protegida pelo Windows (DPAPI), modelo configurável.
- Cancelamento, limites de etapas e de contexto. Sem modelo local, novo WebView ou loop de animação ocioso.
- Proteção contra inicializações concorrentes do WebView na mesma aba.
- Mantida compatibilidade do botão Atualizar das versões anteriores.

## Como usar

Clique no ícone de brilho na barra ou pressione Ctrl+Shift+I. Em **Configurar IA**, salve sua chave de API e um modelo compatível com Responses e ferramentas (padrão: `gpt-5.4-mini`). A cobrança ocorre na conta da API. Este assistente é separado da conversa no ChatGPT.

Para resumir uma página, marque **Compartilhar página e abas nesta tarefa** e peça: “Leia e resuma esta página em cinco tópicos”. Para navegar, peça: “Abra o GitHub em outra aba”. Aprove as ações que desejar. **Parar** cancela a tarefa; ações já executadas permanecem.

As tarefas não compartilham histórico de conversa entre pedidos. Iframes, shadow DOM e páginas que bloqueiam extração podem limitar o agente. O assistente não tem acesso ao sistema de arquivos, terminal ou execução de código livre.
