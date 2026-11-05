CURRENT DEFAULT: QUEST MODE
Open Tools > Pawnshop > Open Quest Mode, then Play.
The house now launches a mouse-driven 3D Russian quest with fixed camera views and four day phases.
See Assets/Endless/QUEST_README.txt for gameplay, saves and validation status.
Disable PhysicalShop.questMode only to use the legacy controls documented below.

PAWNSHOP OF THE DAMNED — THE 3D HOUSE

Open Tools > Pawnshop > Open 3D House, then press Play.
Open Endless Mode and the build commands also select HouseShop when it exists.
The gameplay scene is Assets/Endless/HouseShop.unity, copied from the existing
Assets/Scenes/SampleScene.unity. The source house is preserved.

CONTROLS
WASD: walk. Mouse: look. E: use the object under the crosshair.
Escape: leave an interaction or pause. Reading and walking have no time limit.
The front door, desk, phone, ledger, shelf tags and rear rug are interactive.

VISITORS
A visitor approaches the doorstep and knocks. Answer the door to talk.
Take Item for Inspection lends the object without charging money.
Return to the door after testing to use verified evidence, offer a price, buy or refuse.
Refusal retrieves the borrowed item. Purchase transfers ownership exactly once.
Visitors use the existing editable dialogue graphs. Their current choices are saved.
The current visitor models are procedural stand-ins, ready to replace with authored characters.

SUPPLIER AND INVENTORY
Use the phone on the desk to order tools, consumables and house improvements.
Magnifying glass and UV flashlight are reusable purchases.
Gold reagent is used once per completed gold test.
Milk, salt and vodka are stackable goods shared by the inventory and ritual systems.
Crackers are also available as trade goods. HouseState.TryExchange provides an atomic
exchange operation for future quests and barter; no quests are added to endless.
A successful purchase is delivered immediately and recorded in the ledger.

DESK EXAMINATION
Carry an item to the desk and press E to place it. Press E again to examine it.
Right drag rotates the object. The wheel adjusts zoom.
1: free rotation. 2: magnifying glass. 3: UV flashlight. 4: gold reagent.
Hold left mouse on each visible marker. Rotate the object to reach hidden regions.
Magnification checks three regions and reveals condition and appraisal.
UV checks three regions and identifies supernatural residue or excludes a curse.
Gold reagent requires one complete application at the test patch.
The displayed gold reaction is a fictional game mechanic, not a real assay procedure.
Each completed examination costs one action. Reusable tools have no per-use cash fee.
Starting, interrupting or repeating a completed test cannot charge again.
The Expert improvement reduces the time required to hold on each region.
ASCII post-processing is temporarily disabled while examining or performing a ritual
so the surface markers and physical reactions remain readable.

PHYSICAL STORAGE
Carry owned goods to numbered shelf tags, then press E to place them.
Press E on a stored item or its tag to set a sale price, pick it up, wrap it,
liquidate it or dispose of it. The first eight positions are available initially.
Shelf improvements unlock four positions per level, up to sixteen.
The other bookcase holds the isolation position. Logical neighbors are the four-column
grid on the main shelf; isolation has no contact neighbors.
Items at the desk, on the rug or in your hands must be shelved before closing.

RITUAL RUG
Carry a purchased item to the rear rug. E places it; E again begins the ritual.
Hold left mouse and trace every section of the salt ring: one salt packet.
Hold over the offering bowl to pour milk: one carton.
Rotate the item and wipe all three marked regions: one vodka bottle and one action.
The completed ritual removes the item's curse. Each completed stage is saved.
Leaving the view preserves progress; picking up the item cancels the arrangement
without refunding components already used. Borrowed items cannot be cleansed.

CLOSING
Use the ledger beside the phone to review purchases, supplies, sales and losses.
Closing requires confirmation. Sales resolve before curses and expenses.
If cash is short, liquidate goods from their physical shelf tags before bankruptcy.
Open the next night from the ledger. There is no fixed final night.

SAVES AND AUTHORING
The same versioned run and profile files are used as the terminal mode.
Older saves gain an empty physical inventory and default player position on first use.
House locations, owned supplies, inspection facts and completed ritual stages persist.
The 2D EndlessShop scene remains available as a reference; use HouseShop for gameplay.
Station anchors and references are serialized on the Physical Pawnshop object.
HouseSceneBuilder creates a copy only when HouseShop does not already exist.
New tool rules and trade inventory are separate from first-person input and visuals.

VALIDATION
EditMode tests cover atomic supply purchases and exchanges, ownership, physical
placement before closing, surface-trace requirements, consumption exactly once,
fixed test results, ritual stages, interrupted rituals and save round trips.
The live scene smoke test drives the actual mouse/raycast path for examinations
and the ritual, then lists the item, settles the night and checks the next-night save.

FONT
Noto Sans Mono: Copyright 2022 The Noto Project Authors, SIL OFL 1.1.
See FONT-LICENSE.txt in builds or UI/Fonts/LICENSE.txt in the project.
