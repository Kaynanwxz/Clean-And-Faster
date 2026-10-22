# Contribuir

Abra uma issue com o problema ou uma proposta objetiva. Para código, use uma branch e um pull request para `main`. Preserve os comportamentos existentes e os dados em `%LOCALAPPDATA%\KayBrowser`.

- Use .NET 10 SDK e mantenha as versões de dependências explícitas.
- Rode os testes de lógica em dados temporários e o diagnóstico de layout no Windows.
- Para atualizar o app, incremente a versão do projeto e descreva o comportamento em `docs/RELEASE-NOTES.md`.
- Nunca envie credenciais OAuth pessoais, tokens, cookies, dados de navegação, `bin/` ou `obj/`.
- Alterações no atualizador devem validar cancelamento, integridade, origem do download, restauração e preservação dos dados.
