# Clean Browser 0.8.1

- Corrigido o tratamento de cliques fora do menu durante a ativação da janela e a animação de saída. O gesto de dispensa não deve acionar o botão ou o conteúdo que está por baixo.
- O menu é ocultado depois do processamento da troca de foco pelo Windows. Solicitações antigas de fechamento são ignoradas após uma nova abertura.
- Aparência e IA permanecem abertas ao clicar na página ou trocar de aplicativo; Configurações avançadas permanecem abertas ao clicar fora e dispensar listas de opções.
- Diagnóstico Windows ampliado com cliques nativos na barra da janela, durante a saída animada, sobre a barra de endereço e com Configurações abertas sobre Aparência. O diagnóstico também verifica se o clique externo chega indevidamente ao conteúdo WebView2.

## Atualizar

Clique **Atualizar → Atualizar e reiniciar**. Não precisa extrair ZIP. Histórico, favoritos, configurações e perfil de sites são preservados.

Os testes usam dados e páginas locais isolados. A cobertura e seus limites estão em `docs/VALIDACAO.md`.
