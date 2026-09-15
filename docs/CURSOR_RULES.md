# CURSOR RULES v0.2

## Role

You are an implementation agent, not the owner of the game's architecture.

The source of truth is:
1. Game Design Bible
2. Technical Bible
3. Architecture documents
4. Decisions

## Before coding

Always:
1. inspect the repository;
2. read relevant docs;
3. identify dependencies;
4. check whether the requested change affects an existing contract.

## If architecture is unclear

Do not silently invent a permanent solution.

Use the smallest reversible solution and document the uncertainty.

## Never

- put simulation state only in Godot Nodes;
- let UI directly mutate simulation state;
- use display names as IDs;
- use array indexes as persistent IDs;
- create global mutable singleton state without a documented reason;
- put all AI into one class;
- put all game logic into one manager;
- hard-code content that should be data;
- use random values without controlled seed/context;
- make a temporary prototype hack without marking it.

## Always

- use stable IDs;
- separate definitions from runtime state;
- separate commands from events;
- keep systems cohesive;
- make state serializable;
- write tests for important rules;
- keep presentation replaceable;
- document non-obvious architectural decisions.

## Naming

Use clear domain names.

Prefer:
`CharacterNeedsSystem`

Avoid:
`Manager2`

Prefer:
`BuildBuildingCommand`

Avoid:
`DoThing`

## Tests

For each meaningful rule:
- create deterministic input;
- execute;
- assert resulting state.

## End-of-task report

Always report:
- files changed;
- functionality added;
- tests executed;
- test results;
- known limitations;
- next task.

## AI safety rule

If you discover that the requested task would require rewriting an established subsystem, stop and explain why before proceeding.

## Scope rule

Do not implement future features merely because their interfaces exist.

Interfaces should prepare the architecture; they should not create unnecessary unfinished gameplay.

## Development reporting

After every completed task, you MUST create/update the development report before considering the task finished.

The report is stored in `docs/DEVELOPMENT_LOG.md`. Do not create a second log at the repository root. Do not rely only on chat output: the repository report is the persistent source of development progress.

For every task, record:
1. **Task** — what was requested and the scope actually implemented.
2. **Done** — concrete files, systems, classes, data, or behavior that were added/changed.
3. **Working** — what was verified to work right now.
4. **Tests** — tests/checks executed and their exact result.
5. **Bugs found** — bugs discovered during implementation or testing.
6. **Bugs fixed** — bugs fixed during this task, including a short cause/fix description when useful.
7. **Known limitations / TODO** — anything intentionally incomplete, fragile, mocked, or requiring follow-up.
8. **Architecture decisions** — any new decision, changed assumption, or deviation from existing architecture.
9. **Next step** — the most appropriate next implementation task.

### Reporting rules

- Never claim something works unless it was actually tested or otherwise directly verified.
- Clearly distinguish **implemented**, **tested**, **partially working**, **not tested**, and **known broken**.
- Do not hide failed tests, temporary hacks, warnings, or unresolved bugs.
- If a bug was found and not fixed, record it explicitly instead of silently working around it.
- If a task changes an existing behavior, record what changed and why.
- Keep entries chronological and append new task entries; do not rewrite history to make the project look cleaner.
- If a task required no code changes, still create a report entry describing what was investigated and what was concluded.
- The report must remain concise enough to scan quickly, but detailed enough that another AI agent can understand the current project state without reconstructing the entire conversation.
- At the end of the task, provide the same report summary in the Cursor response so the user can quickly review it.

### Handoff requirement

`docs/DEVELOPMENT_LOG.md` is intended to be shared with ChatGPT when the user wants an external review of development progress.

When another agent continues the project:
1. read `docs/DEVELOPMENT_LOG.md`;
2. read the latest entry first;
3. inspect the relevant code/tests before changing anything;
4. do not assume an item marked "implemented" is "working" unless the report says it was verified.
