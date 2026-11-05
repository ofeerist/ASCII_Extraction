# Dialogue editor

Unity 6000.3.22f1 with embedded Graph Toolkit 0.4.0-exp.2. Keep
`Packages/com.unity.graphtoolkit` in version control: it contains compatibility
fixes and readable node input sizing. See its `LOCAL_PATCHES.md`.

## Authoring

Create a graph through **Assets > Create > Pawnshop > Dialogue Graph**.
Double-click the asset, then double-click empty canvas space to search for nodes.
Connect each output to exactly one input. The graph needs one Start and at least
one End. Save the graph before testing.

| Node | Behavior |
| --- | --- |
| Start | Entry point |
| Line | Speaker and English source text; waits for Continue |
| Choice | 2–6 answers; Answer0 is the first answer |
| Condition | Compare a Blackboard variable; True and False outputs |
| Set Variable | Set a value, or Add/Subtract an integer |
| Command | Request a gameplay action; wait for explicit completion |
| End | Finish the conversation |

All six answer fields remain stored when the answer count changes. Only the
first N answers are used. For conditions and assignments, only the value field
matching the selected type is used.

Declare Boolean, Integer or String variables in the Blackboard. Enter the exact
name in Condition/Set Variable. Unknown names and mismatched types prevent playback.
`global.*` variables persist in the supplied run context. Other variables reset
for each conversation. Variables dragged onto the canvas are not executable steps.

## Preview

Open **Window > Pawnshop > Dialogue Preview**. Select an asset, click
**Prepare / Restart**, adjust variables, then **Start Conversation**.
The preview saves and compiles a snapshot. Prepare again after graph edits.
**Reset Run** also clears global values. Commands are simulated, not executed.

The WatchCustomer sample branches between an inspected watch and an uninspected
watch, then requests `OpenBargain`. It demonstrates dialogue logic, not an actual
price change or item inspection mechanic.

## Runtime integration

Use one DialoguePlayer for sequential customers in a run. Assign a graph asset,
then call Begin(). The component does not render UI.

- onChanged: read Session.Current and display the line or answer buttons.
- Continue(): advance a line or acknowledge completion of a command.
- Choose(index): select a zero-based answer.
- onCommand(name, argument): handle the action, then call Continue().
- onFinished / onError: handle completion or failure.
- ResetRun(): clear the conversation and global variables.

For a custom controller, create DialogueSession instances with a shared global
value dictionary. Set local item facts before Start(). Transactions belong to
gameplay code, not to independent variable edits in the dialogue.

## Localization

The graph stores English source strings and optional stable translation keys.
Select a Line/Choice node and use the Graph Inspector to set Text localization
key, Speaker localization key or Answer N localization key. Keys are independent
of node position, text wording, variable names and execution indices.

Use names such as `dialogue.watch_customer.greeting`,
`dialogue.watch_customer.inspect` and `characters.visitor.name`. Keep keys stable
when rewriting English. A copied line that becomes a different translation unit
needs a new key. Shared speaker-name keys are intentional.

Runtime UI should call step.GetText(resolver), step.GetSpeaker(resolver) and
step.GetAnswer(index, resolver). Implement IDialogueTextResolver to supply the
selected language. Without a resolver, an empty key, or a missing translation
(return null), the English source is used. Session logic never depends on locale.
The preview currently displays English only.

### Planned translation workflow

1. Use a Unity Localization String Table Collection for dialogue and another for
   character names. An adapter will implement IDialogueTextResolver using tables
   loaded before conversation playback. The package and adapter are not installed yet.
2. Export keys, English source and context (graph, speaker, answer/line) to CSV.
   Import translations into tables, never into graph topology.
3. Track an English-source hash in the export so edited source marks existing
   translations for review without changing their keys.
4. Validate missing keys, conflicting English text under one key, empty translations
   and placeholder mismatches before release. This translation audit is planned.
5. Introduce named placeholders and Smart Strings only when needed. Avoid building
   player-facing sentences by concatenating separately translated fragments.
6. Test long translations, font coverage and wrapping in the final gameplay UI.

Translation tables, CSV tooling, locale selection, asynchronous table loading and
translation audits are not implemented. The current change provides the data
fields and runtime resolver boundary so they can be added without rebuilding graphs.

## Current limits and tests

Conversation save/resume, subgraphs and conditional visibility of individual
answers are not implemented. Automatic execution stops after 1024 steps to prevent
cycles from freezing the game or editor.

Run EditMode tests in `Pawnshop.Dialogue.Tests`. They cover branching, run/local
scope, commands, validation failures, cycles, imported sample playback, graph
framing and localization fallback.

References for the planned adapter:
[Unity String Tables](https://docs.unity.cn/Packages/com.unity.localization@1.4/manual/StringTables.html)
and [CSV import/export](https://docs.unity.cn/Packages/com.unity.localization@1.4/manual/CSV.html).
