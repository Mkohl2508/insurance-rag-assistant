# CLAUDE.md

## Language
- Everything in the repo is English: code, comments, XML docs, console/exception messages, LLM prompts, test data, docs.

## Commits
- Use Conventional Commits in English: `type(scope): summary` (types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`; scopes: `core`, `cli`).
- Keep commits atomic: one logical change per commit, each one builds and passes tests.

## Commands
- Build: `dotnet build insurance-assistant.sln`
- Test: `dotnet test insurance-assistant.sln`
