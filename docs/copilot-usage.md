# GitHub Copilot usage

> **To complete before submission:** sections 2–4 must describe how *you* used Copilot during
> Assignment 2: real prompts you typed, what it saved you, and where it went wrong. They are left
> blank so that the report is an honest record. Section 1 describes the Copilot customization
> files that are in the repository.

## 1. Copilot customization files in this repository

| File | Purpose |
|---|---|
| `.github/copilot-instructions.md` | Repository-wide instructions: project map, multi-tenancy rules, roles, election-privacy rules, validation and UI conventions. Loaded by Copilot Chat for every request in this repository. |
| `.github/skills/multi-tenancy.skill.md` | How the tenant boundary is set in each host, plus a checklist for adding a tenant-aware feature. |
| `.github/skills/identity-authorization.skill.md` | Roles, tenant claims, Admin and API authorization, password policy. |
| `.github/skills/ef-core-migrations.skill.md` | How to add a SQL Server migration and the cascade-path rule. |
| `.github/prompts/scaffold-crud.prompt.md` | Reusable prompt that generates a contract, service, API controller and Blazor admin pages for an entity. |
| `.github/prompts/seed-data.prompt.md` | Reusable prompt for adding a seeded municipality. |
| `.github/agents/crud-reviewer.agent.md` | Review agent that checks tenant isolation, authorization, validation, uploads, error handling and privacy. |

## 2. How Copilot was used

_(Describe where you used inline completions, Copilot Chat, Edits/agent mode, the prompt files, and the reviewer agent.)_

## 3. Example prompts

| Prompt | What Copilot produced | What you changed |
|---|---|---|
| | | |

## 4. Productivity improvements and problems encountered

_(What was faster, what Copilot got wrong, for example tenant filters, Aspire APIs, or Blazor render modes, and how you caught it.)_
