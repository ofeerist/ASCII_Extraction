using System;
using System.IO;
using System.Linq;
using Pawnshop.Dialogue;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pawnshop.Endless
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class EndlessGame : MonoBehaviour
    {
        [SerializeField] StyleSheet theme;
        [SerializeField] Font terminalFont;
        [SerializeField] DialogueAsset[] visitorDialogues;
        RunStorage storage;
        ShopSimulation game;
        PlayerProfile profile;
        VisualElement root;
        string page = "Menu";
        string message;
        string confirmation;
        Action pending;
        int selected = -1;
        int offer;
        int price;
        int seed;
        AudioSource audioSource;
        AudioClip cue;
        public ShopSimulation Game => game;
        public bool IsReady => root != null && root.childCount > 0;

        void OnEnable()
        {
            storage = new RunStorage(Application.persistentDataPath);
            profile = storage.LoadProfile();
            message = storage.Notice;
            var run = storage.LoadRun();
            if (run != null) game = new ShopSimulation(run);
            if (!string.IsNullOrEmpty(storage.Notice)) message = storage.Notice;
            seed = Math.Abs(Guid.NewGuid().GetHashCode() % 999999);
            root = GetComponent<UIDocument>().rootVisualElement;
            if (theme != null) root.styleSheets.Add(theme);
            root.style.unityFontDefinition = FontDefinition.FromFont(terminalFont != null ? terminalFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            var samples = new float[4410];
            for (var i = 0; i < samples.Length; i++) samples[i] = (float)(Math.Sin(i * 2 * Math.PI * 440 / 44100) * (1.0 - (double)i / samples.Length) * 0.07);
            cue = AudioClip.Create("DeskBell", samples.Length, 1, 44100, false); cue.SetData(samples, 0);
            Draw();
        }
        void OnDisable() { Save(); if (cue != null) Destroy(cue); }
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() => Save();
        void Sound() { if (profile.sound && audioSource != null && cue != null) audioSource.PlayOneShot(cue); }
        void Save()
        {
            if (storage == null || profile == null) return;
            try
            {
                if (game != null) { storage.SaveRun(game.State); RunStorage.UpdateRecords(profile, game.State, game.Capital); }
                storage.SaveProfile(profile);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { message = "SAVE FAILED — keep the game open and check free disk space. " + e.Message; }
        }
        void Act(Func<string> action)
        {
            var previous = game?.Customer?.item.id;
            message = action();
            if (game?.Customer?.item.id != previous) { offer = game?.Customer?.ask ?? 0; Sound(); }
            Save(); Draw();
        }
        void Confirm(string text, Action action) { confirmation = text; pending = action; Draw(); }
        void Navigate(string destination) { page = destination; confirmation = null; pending = null; Draw(); }
        VisualElement Box(VisualElement parent, string classes = "card")
        { var box = new VisualElement(); foreach (var c in classes.Split(' ')) box.AddToClassList(c); parent.Add(box); return box; }
        Label Text(VisualElement parent, string text, string classes = "body")
        { var label = new Label(text); foreach (var c in classes.Split(' ')) label.AddToClassList(c); parent.Add(label); return label; }
        Button Button(VisualElement parent, string label, Action action, bool enabled = true, string classes = "")
        { var button = new Button(action) { text = label }; button.SetEnabled(enabled); foreach (var c in classes.Split(' ')) if (c.Length > 0) button.AddToClassList(c); parent.Add(button); return button; }
        IntegerField Number(VisualElement parent, string label, int value, Action<int> changed)
        { var field = new IntegerField(label) { value = value, isDelayed = false }; field.RegisterValueChangedCallback(e => changed(e.newValue)); parent.Add(field); return field; }
        void Draw()
        {
            if (root == null) return;
            root.Clear(); root.AddToClassList("screen");
            var header = Box(root, "header");
            Text(header, "PAWNSHOP OF THE DAMNED", "brand");
            Text(header, "BUY CAREFULLY. CLOSE BEFORE DAWN.", "eyebrow");
            if (page != "Menu" && game != null)
            {
                var s = game.State;
                var metrics = Box(root, "metrics");
                Text(metrics, $"NIGHT {s.night:000}", "metric"); Text(metrics, $"CASH {s.cash}", "metric"); Text(metrics, $"DUE {s.due}", "metric");
                Text(metrics, $"ACTIONS {s.actions}", "metric"); Text(metrics, $"STOCK {s.stock.Count}/{game.Capacity + 1}", "metric");
                var nav = Box(root, "nav");
                foreach (var item in new[] { "Counter", "Storage", "Ledger", "Improvements", "Codex", "Settings" })
                { var destination = item; Button(nav, item, () => Navigate(destination), true, page == item ? "active" : ""); }
                Button(nav, "Save & Menu", () => { Save(); Navigate("Menu"); });
            }
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("content"); root.Add(scroll);
            if (!string.IsNullOrEmpty(message)) Text(scroll, message, "notice");
            if (page == "Menu") DrawMenu(scroll);
            else if (game != null)
            {
                if (game.State.distortion && profile.effects && page != "Ledger" && page != "Settings") Text(scroll, "[ SIGNAL NOISE ]   THE SHELVES REMEMBER YOUR NAME   [ SIGNAL NOISE ]", "haunt");
                if (page == "Counter") DrawCounter(scroll);
                else if (page == "Storage") DrawStorage(scroll);
                else if (page == "Ledger") DrawLedger(scroll);
                else if (page == "Improvements") DrawUpgrades(scroll);
                else if (page == "Codex") DrawCodex(scroll);
                else if (page == "Settings") DrawSettings(scroll);
                DrawNightControls(scroll);
            }
            var footer = Box(root, "footer");
            Text(footer, game == null ? "NO TIMER • EVERY DEAL IS A CHOICE" : $"RUN SEED {game.State.seed} • BALANCE v{game.State.balanceVersion} • AUTOSAVE AFTER EACH ACTION", "muted");
            if (pending != null)
            {
                var shade = Box(root, "modal-shade"); var panel = Box(shade, "modal card");
                Text(panel, "CONFIRM", "section-title"); Text(panel, confirmation);
                var actions = Box(panel, "row");
                Button(actions, "Confirm", () => { var callback = pending; pending = null; confirmation = null; callback(); }, true, "primary");
                Button(actions, "Cancel", () => { pending = null; confirmation = null; Draw(); });
            }
        }
        void DrawMenu(VisualElement parent)
        {
            var hero = Box(parent, "hero card");
            Text(hero, "THE NIGHT SHIFT", "title");
            Text(hero, "The city sleeps. Your customers do not.\nInspect what they bring, negotiate a price, and decide what can safely stay until morning.", "lead");
            if (game != null) Button(hero, game.State.phase == NightPhase.Bankrupt ? "View Last Run" : $"Continue — Night {game.State.night}", () => { offer = game.Customer?.ask ?? 0; Navigate("Counter"); }, true, "primary");
            Number(hero, "Run seed", seed, value => seed = value);
            Button(hero, "Start New Run", () => {
                void StartRun() { game = ShopSimulation.NewRun(seed); selected = -1; offer = game.Customer.ask; message = "Welcome. Read the warning signs, keep your rent reserve, and refuse deals you cannot afford."; page = "Counter"; Save(); Sound(); Draw(); }
                if (game != null && game.State.phase != NightPhase.Bankrupt) Confirm("Replace the current run? Records and discovered curse rules will remain.", StartRun); else StartRun();
            });
            Text(hero, "HOW A NIGHT WORKS", "section-title");
            Text(hero, "1. Read the customer's story and the item's free warning sign.\n2. Spend actions on tests. Use verified evidence when bargaining.\n3. Buy or refuse. List purchases for sale in Storage.\n4. Isolate, wrap, liquidate or dispose of risky goods.\n5. Review the closing forecast. Sales happen before curses and expenses.\n6. Invest the surplus and open again. There is no final night.");
            var records = Box(parent);
            Text(records, "LOCAL RECORDS", "section-title");
            Text(records, $"Nights survived  {profile.bestNights}\nBest cumulative cash profit  {profile.bestProfit}\nHighest liquidation capital  {profile.bestCapital}\nRecord seed  {profile.recordSeed}");
            Button(records, "Quit", () => { Save(); Application.Quit(); });
        }
        void DrawCounter(VisualElement parent)
        {
            var s = game.State;
            var bulletin = Box(parent, "bulletin"); Text(bulletin, s.eventName.ToUpperInvariant() + " — " + s.eventDescription);
            if (s.phase != NightPhase.Reception) { DrawReport(parent); return; }
            var customer = game.Customer;
            if (customer == null) { Text(parent, "The waiting room is empty. Review your stock and close the shop.", "lead"); return; }
            var columns = Box(parent, "columns"); var left = Box(columns, "card column"); var right = Box(columns, "card column");
            Text(left, $"VISITOR {s.customerIndex + 1}/{s.customers.Count} — {customer.kind.ToString().ToUpperInvariant()}", "section-title");
            Text(left, customer.story, "quote");
            DrawConversation(left, customer);
            if (customer.accepted || customer.usedArgument || customer.attempts < 3) Text(left, customer.response, "lead");
            Text(left, $"Asking price: {customer.ask} • Offers remaining: {customer.attempts}");
            Number(left, "Your offer", offer, value => offer = value);
            var bargaining = Box(left, "row");
            Button(bargaining, "Make Offer", () => Act(() => game.Offer(offer)), !customer.accepted);
            Button(bargaining, "Use Diagnostic Evidence", () => Act(game.UseArgument), !customer.usedArgument && !customer.accepted && (customer.item.inspected || customer.item.identified));
            var total = customer.accepted ? customer.acceptedPrice : customer.ask;
            var deal = Box(left, "row");
            Button(deal, $"Buy for {total}", () => Confirm($"Buy {game.Title(customer.item)} for {total}?\nCash after purchase: {s.cash - total}. Expenses due tonight: {s.due}.\nThe item will enter storage unlisted.", () => Act(game.Buy)), s.cash >= total, "primary");
            Button(deal, "Refuse Deal", () => Act(game.Reject));
            DrawItem(right, customer.item, true);
        }
        void DrawConversation(VisualElement parent, CustomerState customer)
        {
            var index = (int)customer.kind;
            if (visitorDialogues == null || index >= visitorDialogues.Length || visitorDialogues[index] == null) return;
            if (!customer.conversationStarted)
            {
                Button(parent, "Ask About the Item", () => Act(() => { customer.conversationStarted = true; return "Listening costs no actions. A story is not verified evidence."; }));
                return;
            }
            var session = VisitorConversation.Restore(visitorDialogues[index], customer);
            if (session.Finished) return;
            var step = session.Current;
            if (step.kind == StepKind.Line)
            {
                Text(parent, step.GetText(), "quote");
                Button(parent, "Continue Conversation", () => Act(() => VisitorConversation.Advance(visitorDialogues[index], customer, -1)));
            }
            else if (step.kind == StepKind.Choice)
            {
                for (var i = 0; i < step.answers.Length; i++)
                {
                    var answer = i;
                    Button(parent, step.GetAnswer(i), () => Act(() => VisitorConversation.Advance(visitorDialogues[index], customer, answer)));
                }
            }
        }
        void DrawItem(VisualElement parent, ItemState item, bool diagnostics)
        {
            Text(parent, game.Title(item).ToUpperInvariant(), "section-title");
            Text(parent, ShopCatalog.Items[item.template].description, "quote");
            Text(parent, ShopCatalog.Sign(item), item.suspicious ? "warning" : "muted");
            var value = ShopSimulation.Value(item);
            Text(parent, item.inspected ? $"Verified condition: {item.condition}% • Appraised value: {value}" : $"Unverified appraisal range: {value * 75 / 100}–{value * 125 / 100}. Use the loupe to narrow it.");
            Text(parent, item.identified ? $"Verified: {ShopCatalog.CurseName(item.curse)}\n{ShopCatalog.Rule(item.curse)}" : "Curse status: NOT VERIFIED. A warning can also be an ordinary defect.");
            if (item.weighed && !item.identified) Text(parent, "Verified scales result: Hunger excluded.", "muted");
            if (diagnostics && game.State.phase == NightPhase.Reception)
            {
                var row = Box(parent, "row");
                foreach (Diagnostic test in Enum.GetValues(typeof(Diagnostic)))
                {
                    var performed = test == Diagnostic.Loupe ? item.inspected : test == Diagnostic.Scales ? item.weighed : item.tested;
                    var cost = game.TestCost(test);
                    Button(row, performed ? $"{test} ✓" : $"{test} · {cost}", () => Act(() => game.Diagnose(item.id, test)), !performed && game.State.actions > 0 && game.State.cash >= cost);
                }
                Text(parent, "Each test costs one action. Results are fixed and recorded in the protected ledger.", "muted");
            }
        }
        void DrawStorage(VisualElement parent)
        {
            Text(parent, "STORAGE & SALES", "title");
            Text(parent, "Select a slot to inspect its item. Cold touch crosses horizontal and vertical edges, never diagonals. The isolation slot has no neighbors.");
            var grid = Box(parent, "stock-grid");
            for (var i = 0; i < game.Capacity; i++) SlotButton(grid, i);
            var isolation = Box(parent, "row"); SlotButton(isolation, ShopSimulation.IsolationSlot);
            var item = game.State.stock.FirstOrDefault(x => x.id == selected);
            if (item == null) { Text(parent, "Select an occupied slot. Purchased items must be listed before buyers can purchase them.", "muted"); return; }
            var panel = Box(parent); DrawItem(panel, item, true);
            if (item.identified && item.curse == Curse.Cold && !item.protectedItem)
            {
                var neighbors = game.State.stock.Where(x => x.id != item.id && game.IsAdjacent(item.slot, x.slot)).OrderBy(x => x.id).ToArray();
                Text(panel, neighbors.Length == 0 ? "Cold touch: no occupied neighboring slots." : $"Cold touch target if both remain after sales: {game.Title(neighbors[0])} in slot {neighbors[0].slot + 1}.", "warning");
            }
            Text(panel, $"Paid: {item.cost} • Immediate liquidation: {ShopSimulation.LiquidationValue(item)}\nSale price: {item.askingPrice} • Demand: {game.Demand(item)}\nProtection: {(item.protectedItem ? "Wrapped — curse suppressed" : "None")}");
            if (game.State.phase == NightPhase.Reception)
            {
                Number(panel, "Sale price", price, value => price = value);
                var row = Box(panel, "row");
                Button(row, "List / Update Price", () => Act(() => game.ListItem(item.id, price, true)));
                Button(row, "Unlist", () => Act(() => game.ListItem(item.id, item.askingPrice, false)), item.listed);
                Button(row, $"Wrap · {game.WrapCost} + 1 action", () => Act(() => game.Wrap(item.id)), !item.protectedItem && game.State.actions > 0 && game.State.cash >= game.WrapCost);
                Text(panel, "MOVE TO EMPTY SLOT — 1 ACTION", "section-title"); var targets = Box(panel, "row");
                for (var i = 0; i < game.Capacity; i++)
                { var slot = i; if (game.State.stock.All(x => x.slot != slot)) Button(targets, (slot + 1).ToString(), () => Act(() => game.Move(item.id, slot)), game.State.actions > 0); }
                if (game.State.stock.All(x => x.slot != ShopSimulation.IsolationSlot)) Button(targets, "Isolation", () => Act(() => game.Move(item.id, ShopSimulation.IsolationSlot)), game.State.actions > 0);
            }
            var available = game.State.phase == NightPhase.Reception || game.State.phase == NightPhase.Settlement;
            var exits = Box(panel, "row");
            Button(exits, $"Liquidate Now · +{ShopSimulation.LiquidationValue(item)}", () => Confirm($"Sell {game.Title(item)} immediately for {ShopSimulation.LiquidationValue(item)}?", () => Act(() => game.Liquidate(item.id))), available);
            Button(exits, "Dispose · No Refund", () => Confirm($"Permanently dispose of {game.Title(item)}? Its purchase cost will not be refunded.", () => Act(() => game.Dispose(item.id))), available, "danger");
        }
        void SlotButton(VisualElement parent, int slot)
        {
            var item = game.State.stock.FirstOrDefault(x => x.slot == slot);
            var position = slot == ShopSimulation.IsolationSlot ? "ISOLATION" : $"SLOT {slot + 1:00}";
            Button(parent, item == null ? position + "\n[ empty ]" : position + "\n" + game.Title(item) + "\n" + (item.listed ? $"Listed {item.askingPrice}" : "Not listed"), () => { if (item != null) { selected = item.id; price = item.askingPrice; Draw(); } }, item != null, "slot" + (item?.id == selected ? " active" : ""));
        }
        void DrawLedger(VisualElement parent)
        {
            Text(parent, "PROTECTED LEDGER", "title");
            Text(parent, "Verified facts and recorded transactions. This page is never distorted.", "muted");
            DrawReport(parent);
            var history = Box(parent);
            foreach (var line in game.State.journal.AsEnumerable().Reverse()) Text(history, line, "ledger-line");
        }
        void DrawReport(VisualElement parent)
        {
            var s = game.State; var r = s.report; var panel = Box(parent);
            Text(panel, s.phase == NightPhase.Bankrupt ? "THE SHOP IS CLOSED" : s.phase == NightPhase.Settlement ? "PAYMENT REQUIRED" : "NIGHT ACCOUNT", "section-title");
            Text(panel, $"Opening cash        {r.openingCash}\nPurchases           -{r.purchases}\nDiagnostics         -{r.tests}\nProtection          -{r.protection}\nImprovements        -{r.upgrades}\nSales               +{r.sales} ({r.soldCount} items)\nLiquidation         +{r.liquidation}\nCurse cash loss      -{r.curseLoss}\nExpenses paid       -{r.expenses}\nCurrent cash        {s.cash}\nItem value damage   {r.damage}\nLiquidation capital {game.Capital}");
            if (r.finalized) Text(panel, $"Cash change this night: {r.profit:+#;-#;0} • Total cash profit: {s.totalProfit:+#;-#;0}\nNights survived: {s.completedNights}", "lead");
            if (s.phase == NightPhase.Settlement) Text(panel, $"Due: {s.due}. Shortfall: {Math.Max(0, s.due - s.cash)}. Remaining stock can be liquidated for {s.stock.Sum(ShopSimulation.LiquidationValue)}.", "warning");
        }
        void DrawUpgrades(VisualElement parent)
        {
            Text(parent, "SHOP IMPROVEMENTS", "title");
            var descriptions = new[] { "Four additional shelf positions per level.", "Two additional actions each night per level.", "Protective wrapping costs 3 less per level.", "Each diagnostic costs 2 less per level (minimum 1)." };
            foreach (Upgrade upgrade in Enum.GetValues(typeof(Upgrade)))
            {
                var panel = Box(parent); var level = game.UpgradeLevel(upgrade); var cost = game.UpgradeCost(upgrade);
                Text(panel, upgrade.ToString().ToUpperInvariant() + $"   {level}/2", "section-title"); Text(panel, descriptions[(int)upgrade]);
                Button(panel, level == 2 ? "Fully Improved" : $"Buy Improvement · {cost}", () => Act(() => game.BuyUpgrade(upgrade)), level < 2 && game.State.cash >= cost && (game.State.phase == NightPhase.Reception || game.State.phase == NightPhase.Results));
            }
        }
        void DrawCodex(VisualElement parent)
        {
            Text(parent, "FIELD NOTES", "title");
            Text(parent, "Discoveries persist between runs. They grant knowledge, never starting cash or permanent stat bonuses.");
            foreach (var curse in new[] { Curse.Cold, Curse.Whisper, Curse.Hunger })
            {
                var panel = Box(parent); var known = profile.discoveries.Contains(curse) || game.State.discoveries.Contains(curse);
                Text(panel, known ? ShopCatalog.CurseName(curse).ToUpperInvariant() : "UNIDENTIFIED PHENOMENON", "section-title");
                Text(panel, known ? ShopCatalog.Rule(curse) : "Use diagnostics or record an observed effect to unlock this entry.");
            }
        }
        void DrawSettings(VisualElement parent)
        {
            Text(parent, "SETTINGS", "title");
            var sound = new Toggle("Desk sounds") { value = profile.sound };
            sound.RegisterValueChangedCallback(e => { profile.sound = e.newValue; Save(); }); parent.Add(sound);
            var effects = new Toggle("Decorative horror distortion") { value = profile.effects };
            effects.RegisterValueChangedCallback(e => { profile.effects = e.newValue; Save(); }); parent.Add(effects);
            Text(parent, "No real-time timer. Reading is free. Financial values, controls and verified facts are always protected. Settings and records are saved locally.", "muted");
        }
        void DrawNightControls(VisualElement parent)
        {
            var s = game.State; var panel = Box(parent, "card closing");
            if (s.phase == NightPhase.Reception)
            {
                Text(panel, "BEFORE DAWN", "section-title"); Text(panel, game.Forecast());
                if (game.Customer != null) Text(panel, $"Closing now sends away {s.customers.Count - s.customerIndex} remaining visitors for free.", "muted");
                if (s.stock.Any(x => !x.listed)) Text(panel, "Some goods are not listed. They cannot sell tonight.", "warning");
                if (s.stock.Any(x => x.suspicious && !x.protectedItem)) Text(panel, "Unwrapped goods with warning signs remain in storage. Verify their risks before closing.", "warning");
                Button(panel, "Close Shop & Resolve Night", () => Confirm(game.Forecast() + "\nProceed with closing?", () => { Act(game.CloseNight); page = "Counter"; Draw(); }), true, "primary");
            }
            else if (s.phase == NightPhase.Settlement)
            {
                Button(panel, "Manage Stock / Liquidate", () => Navigate("Storage"));
                Button(panel, $"Pay Expenses · {s.due}", () => Act(game.PayExpenses), s.cash >= s.due, "primary");
                Button(panel, "Declare Bankruptcy", () => Confirm($"End this run after {s.completedNights} survived nights?\nYou still have {s.stock.Sum(ShopSimulation.LiquidationValue)} in liquidation value. This decision is final for this run.", () => Act(game.ConfirmBankruptcy)), s.cash < s.due, "danger");
            }
            else if (s.phase == NightPhase.Results) Button(panel, $"Open Night {s.night + 1}", () => { Act(game.NextNight); page = "Counter"; Draw(); }, true, "primary");
            else Button(panel, "Return to Menu", () => Navigate("Menu"), true, "primary");
        }
    }
}
