PAWNSHOP OF THE DAMNED — ENDLESS MODE

PLAY
Open Tools > Pawnshop > Open Endless Mode, then press Play.
The standalone scene is Assets/Endless/EndlessShop.unity.
Start New Run accepts a reproducible integer seed. Continue restores the current phase.
Use the mouse to navigate, the wheel to scroll, and the keyboard to enter prices.
There is no real-time timer. Reading, conversations and navigation cost no actions.

COMPLETE NIGHT LOOP
Read a visitor's story and the item's free warning. Diagnostics consume cash and one action.
The loupe reveals condition and appraisal. Scales identify or exclude Hunger.
The occult test identifies any curse or confirms that the item is ordinary.
Tests never reroll. Ordinary defects can imitate a warning, but cursed goods always have one.

Make up to three price offers. Verified evidence lowers the visitor's threshold once.
An accepted offer does not transfer money until the purchase is confirmed.
Refusal is free. Purchased goods enter storage unlisted.
In Storage, select an item, enter its sale price, and choose List / Update Price.
Demand depends on price versus appraisal and the favored category. Unsold goods remain.
Liquidation pays the displayed, discounted amount immediately.

Storage has four columns and an additional isolation position.
Only orthogonal neighbors count. Moving or wrapping an item consumes one action.
Cold touch damages one adjacent item after closing; empty space and isolation block contact.
Whispering affects decorative text next night; the ledger and financial controls remain readable.
Hunger consumes up to 12 cash per closing and is not blocked by isolation.
Wrapping suppresses the stored item's curse. Disposal is free but gives no refund.
Combined cash and appraisal damage from curses has a visible nightly cap.

Closing shows a forecast and asks for confirmation. Resolution order is fixed:
listed sales, remaining curses, expenses. An underfunded shop enters settlement.
Liquidate goods and pay, or explicitly confirm bankruptcy. There is no automatic restart.
Results show the cash account and causes of losses. Buy improvements or open the next night.
There is no designed final night; difficulty approaches 1.8 instead of rising indefinitely.

CONTENT
10 item templates, 3 curses, 4 visitor archetypes, 3 diagnostics,
4 improvements with two levels each, daily demand and six bulletin variants,
protected ledger, persistent discoveries, local records, optional desk sounds
and decorative horror text. English is the current content language.

DIALOGUE AUTHORING AND LOCALIZATION
Assets/Endless/Dialogues contains four editable Graph Toolkit .dialogue assets.
They compile to runtime DialogueAssets and are assigned to the Endless Shop component.
Optional conversations use the same DialogueSession as the dialogue editor preview.
Conversation actions are saved with each visitor; resuming cannot charge money.
Economy actions remain explicit ShopSimulation operations, separate from narrative text.

Every authored line and answer has a stable localization key and an English fallback.
Keep keys unchanged when revising wording. IDialogueTextResolver is the adapter point
for future Unity Localization String Tables. Translate narrative by key, preserve
numeric prices as data, and keep a separate UI table for interface text.
No language switch or translated tables are included in this English build.
The generated English UI currently uses literal strings; extracting these into that
UI table is required before adding another language.

SAVE FILES
RunStorage uses Application.persistentDataPath:
endless-run.json and endless-profile.json, each with a .bak recovery copy.
The envelope includes a version and SHA-256 checksum. Writes use a flushed temporary
file and atomic replacement. Invalid primary files fall back to the previous backup
with a visible notice. This checksum detects damage; it is not an anti-cheat mechanism.
The balance version is 1. Unsupported versions are rejected rather than guessed.

Each successful UI action autosaves. Closing, buying, paying and next-night transitions
are phase guarded. Records and discovered curse rules persist; upgrades and starting
cash reset with a new run. Save failures are shown in the interface.

BUILD
Tools > Pawnshop > Build Endless for Linux
Tools > Pawnshop > Build Endless for Windows
Each builds only EndlessShop into Builds/Endless/<platform>.
Use the entire output folder, not just the executable. The matching Unity platform
module must be installed. Build summaries are written under Temp/endless-build-*.json.
The editor's unrelated sample scene is not included in these standalone builds.

VALIDATION
Pawnshop.Endless.Tests covers deterministic generation, bargaining, diagnostic costs,
storage adjacency, curse budgets, wrapping, sales order, phase transitions, liquidation,
bankruptcy, upgrades, every save phase, backup recovery, records, and graph resumption.
A conservative strategy runs 200 nights on each of three seeds with JSON reloads between nights.
A separate late-night check opens night 10001 and checks the difficulty plateau.
The dialogue assembly contains additional graph, session and editor layout regressions.
The UI smoke flow runs a complete first night and verifies the exact saved cash on night two.
These checks establish functional correctness; human playtesting is still needed to tune
pacing, variety and difficulty. This is the playable P0 endless MVP, not a Steam release.

FONT
UI/Fonts/NotoSansMono-Regular.ttf: Copyright 2022 The Noto Project Authors.
Distributed under the SIL Open Font License 1.1; see UI/Fonts/LICENSE.txt.
