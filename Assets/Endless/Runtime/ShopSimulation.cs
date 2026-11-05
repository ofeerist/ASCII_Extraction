using System;
using System.Collections.Generic;
using System.Linq;

namespace Pawnshop.Endless
{
    public sealed class ShopSimulation
    {
        public const int IsolationSlot = 100;
        public RunState State { get; }
        public CustomerState Customer => State.phase == NightPhase.Reception && State.customerIndex < State.customers.Count ? State.customers[State.customerIndex] : null;
        public int Capacity => 8 + State.shelves * 4;
        public int MaxActions => 10 + State.workbench * 2;
        public int Capital => State.cash + State.stock.Sum(LiquidationValue);
        public double Difficulty => 1 + 0.8 * (1 - Math.Exp(-(State.night - 1) / 25.0));
        public bool CanClose => State.phase == NightPhase.Reception;

        public ShopSimulation(RunState state) { State = state ?? throw new ArgumentNullException(nameof(state)); }
        public static ShopSimulation NewRun(int seed)
        {
            var simulation = new ShopSimulation(new RunState { seed = seed, randomState = unchecked((uint)seed) | 1u });
            simulation.OpenNight();
            return simulation;
        }
        int Random(int min, int max)
        {
            uint x = State.randomState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            State.randomState = x;
            return min + (int)(x % (uint)(max - min));
        }
        public static int Value(ItemState item) => Math.Max(8, item.baseValue * item.condition / 100);
        public static int LiquidationValue(ItemState item) => Math.Max(1, Value(item) * 45 / 100);
        public int TestCost(Diagnostic test) => Math.Max(1, (test == Diagnostic.Loupe ? 4 : test == Diagnostic.Scales ? 5 : 9) - State.expert * 2);
        public int WrapCost => Math.Max(3, 12 - State.insulation * 3);
        public int LossBudget => 20 + (int)(20 * (Difficulty - 1));
        public string Title(ItemState item) => ShopCatalog.Items[item.template].name;
        public bool IsAdjacent(int a, int b) => a != IsolationSlot && b != IsolationSlot &&
            ((Math.Abs(a - b) == 1 && a / 4 == b / 4) || Math.Abs(a - b) == 4);
        public void Log(string message)
        {
            State.journal.Add($"N{State.night:000}  {message}");
            if (State.journal.Count > 120) State.journal.RemoveAt(0);
        }
        void Cash(int delta) => State.cash = (int)Math.Clamp((long)State.cash + delta, 0, 1000000000L);
        void OpenNight()
        {
            State.phase = NightPhase.Reception;
            State.actions = MaxActions;
            State.customerIndex = 0;
            State.report = new NightReport { openingCash = State.cash };
            State.due = 32 + (int)Math.Round(16 * (Difficulty - 1));
            State.favoredCategory = (Category)Random(0, 4);
            var events = new[] { "Collector's bulletin", "Quiet streets", "Payday", "Heavy rain", "The appraiser", "Late train" };
            var which = State.night == 1 ? 0 : Random(0, events.Length);
            State.eventName = events[which];
            State.eventDescription = $"Strong demand for {State.favoredCategory.ToString().ToLowerInvariant()}.";
            if (which == 3) { State.due += 4; State.eventDescription += " Heating surcharge: 4."; }
            if (which == 4) { State.actions += 2; State.eventDescription += " An appraiser lends a hand: +2 actions."; }
            if (which == 1) { State.due = Math.Max(20, State.due - 4); State.eventDescription += " Reduced utilities: -4 expenses."; }
            State.customers.Clear();
            var count = which == 5 ? 6 : 5;
            for (var i = 0; i < count; i++) State.customers.Add(GenerateCustomer(i));
            Log($"Opened. {count} visitors expected. Expenses due: {State.due}.");
        }
        CustomerState GenerateCustomer(int index)
        {
            var template = Random(0, ShopCatalog.Items.Length);
            var entry = ShopCatalog.Items[template];
            var cursed = Random(0, 100) < 18 + (int)(12 * (Difficulty - 1));
            if (State.night == 1 && index == 0) cursed = false;
            var item = new ItemState {
                id = State.nextId++, template = template, condition = Random(68, 101),
                baseValue = entry.value + Random(-8, 9), curse = cursed ? entry.curse : Curse.None,
                suspicious = cursed || Random(0, 100) < 12, slot = -1
            };
            var kind = (CustomerKind)Random(0, 4);
            var minimum = Value(item) * (kind == CustomerKind.Desperate ? 44 : kind == CustomerKind.Dealer ? 64 : 54) / 100;
            var ask = minimum + Random(10, 25);
            return new CustomerState {
                kind = kind, item = item, minimum = minimum, ask = ask,
                story = Story(kind), response = $"I am asking {ask}. What do you say?"
            };
        }
        static string Story(CustomerKind kind)
        {
            switch (kind)
            {
                case CustomerKind.Desperate: return "I need enough for the morning train. Please do not ask where I got it.";
                case CustomerKind.Dealer: return "A private estate sale. The family wanted the house empty before sunset.";
                case CustomerKind.Collector: return "It used to be the finest piece in my collection. My collection is smaller now.";
                default: return "It belongs here. I think you will understand that soon.";
            }
        }
        public string Diagnose(int itemId, Diagnostic test)
        {
            if (State.phase != NightPhase.Reception) return "Diagnostics are available during reception.";
            var item = Customer?.item.id == itemId ? Customer.item : State.stock.FirstOrDefault(x => x.id == itemId);
            if (item == null) return "Select an item.";
            if ((test == Diagnostic.Loupe && item.inspected) || (test == Diagnostic.Scales && item.weighed) || (test == Diagnostic.Occult && item.tested)) return "This test has already been performed. Its result will not change.";
            var cost = TestCost(test);
            if (State.actions < 1) return "No actions left tonight.";
            if (State.cash < cost) return "Not enough cash for this test.";
            State.actions--; Cash(-cost); State.report.tests += cost;
            string result;
            if (test == Diagnostic.Loupe)
            {
                item.inspected = true;
                result = $"Loupe: condition {item.condition}%. Appraised value {Value(item)}. Wear documented.";
            }
            else if (test == Diagnostic.Scales)
            {
                item.weighed = true;
                result = item.curse == Curse.Hunger ? "Scales: impossible weight confirmed. Hunger identified." : "Scales: no impossible weight. Hunger excluded.";
                if (item.curse == Curse.Hunger) Identify(item);
            }
            else
            {
                item.tested = true; Identify(item);
                result = item.curse == Curse.None ? "Occult test: curse excluded. Ordinary wear only." : $"Occult test: {ShopCatalog.CurseName(item.curse)} confirmed. {ShopCatalog.Rule(item.curse)}";
            }
            Log($"{Title(item)} — {result}"); return result;
        }
        void Identify(ItemState item)
        {
            item.identified = true;
            if (item.curse != Curse.None && !State.discoveries.Contains(item.curse)) State.discoveries.Add(item.curse);
        }
        public string UseArgument()
        {
            var customer = Customer;
            if (customer == null) return "No customer is waiting.";
            if (customer.accepted) return "A price is already agreed.";
            if (customer.usedArgument) return "You have already used your evidence.";
            if (!customer.item.inspected && !customer.item.identified && !customer.item.goldTested) return "Inspect or identify the item first.";
            customer.usedArgument = true;
            customer.minimum = Math.Max(8, customer.minimum - 8);
            customer.ask = Math.Max(customer.minimum, customer.ask - 8);
            customer.response = $"You have a point. I can lower the asking price to {customer.ask}.";
            return customer.response;
        }
        public string Offer(int amount)
        {
            var customer = Customer;
            if (customer == null) return "No customer is waiting.";
            if (customer.accepted) return "A price is already agreed. Complete or refuse the deal.";
            if (amount < 1 || amount > State.cash) return "Offer a positive amount you can afford.";
            if (FreeSlot() < 0) return "No storage space. Sell, dispose of an item, or refuse this deal.";
            if (amount >= customer.minimum)
            {
                customer.accepted = true; customer.acceptedPrice = amount;
                customer.response = $"Agreed. {amount}. Confirm the purchase when you are ready.";
                return customer.response;
            }
            customer.attempts--;
            if (customer.attempts == 0) { Log($"{customer.kind} left after three rejected offers."); State.customerIndex++; return "The visitor leaves. No money changed hands."; }
            customer.response = customer.attempts == 1 ? "Last chance. I will not wait much longer." : "Too low. Make me a serious offer.";
            return customer.response;
        }
        int FreeSlot()
        {
            for (var i = 0; i < Capacity; i++) if (State.stock.All(x => x.slot != i)) return i;
            return State.stock.All(x => x.slot != IsolationSlot) ? IsolationSlot : -1;
        }
        public string Buy()
        {
            var customer = Customer;
            if (customer == null) return "No customer is waiting.";
            var price = customer.accepted ? customer.acceptedPrice : customer.ask;
            if (State.cash < price) return "Not enough cash. Refusing costs nothing.";
            var slot = FreeSlot(); if (slot < 0) return "Storage is full.";
            var item = customer.item;
            item.cost = price; item.acquiredNight = State.night; item.slot = slot;
            item.askingPrice = Value(item); item.listed = false;
            State.stock.Add(item); Cash(-price); State.report.purchases += price;
            Log($"Bought {Title(item)} for {price}. List it for sale or store it safely.");
            State.customerIndex++; return "Purchased. The item is in storage, not yet listed for sale.";
        }
        public string Reject()
        {
            if (Customer == null) return "No customer is waiting.";
            Log($"Refused {Title(Customer.item)}. No charge."); State.customerIndex++; return "The visitor leaves.";
        }
        public string ListItem(int id, int price, bool listed)
        {
            if (State.phase != NightPhase.Reception) return "Set sale prices during reception.";
            var item = State.stock.FirstOrDefault(x => x.id == id);
            if (item == null) return "Item not found.";
            if (price < 1 || price > 100000) return "Choose a price between 1 and 100000.";
            item.askingPrice = price; item.listed = listed;
            return listed ? $"Listed for {price}. Sales resolve at closing." : "Removed from sale.";
        }
        public string Move(int id, int slot)
        {
            if (State.phase != NightPhase.Reception) return "Move items during reception.";
            var item = State.stock.FirstOrDefault(x => x.id == id);
            if (item == null || (slot != IsolationSlot && (slot < 0 || slot >= Capacity))) return "Invalid storage position.";
            if (item.slot == slot) return "The item is already there.";
            if (State.stock.Any(x => x.slot == slot)) return "Choose an empty position.";
            if (State.actions < 1) return "No actions left tonight.";
            State.actions--; item.slot = slot; return slot == IsolationSlot ? "Moved into isolation. This blocks contact, not Hunger or Whispering." : "Item moved.";
        }
        public string Wrap(int id)
        {
            if (State.phase != NightPhase.Reception) return "Wrap items before closing.";
            var item = State.stock.FirstOrDefault(x => x.id == id);
            if (item == null || item.protectedItem) return "Select an unwrapped item.";
            if (State.actions < 1 || State.cash < WrapCost) return "Wrapping needs one action and enough cash.";
            State.actions--; Cash(-WrapCost); State.report.protection += WrapCost; item.protectedItem = true;
            Log($"Wrapped {Title(item)}. Its curse is suppressed while wrapped."); return "Protective wrapping applied.";
        }
        public string Dispose(int id)
        {
            if (State.phase != NightPhase.Reception && State.phase != NightPhase.Settlement) return "Disposal is unavailable now.";
            var item = State.stock.FirstOrDefault(x => x.id == id);
            if (item == null) return "Item not found.";
            State.stock.Remove(item); Log($"Disposed of {Title(item)}. No refund; no future effects."); return "Item disposed of.";
        }
        public string Liquidate(int id)
        {
            if (State.phase != NightPhase.Reception && State.phase != NightPhase.Settlement) return "Liquidation is unavailable now.";
            var item = State.stock.FirstOrDefault(x => x.id == id);
            if (item == null) return "Item not found.";
            var value = LiquidationValue(item); State.stock.Remove(item); Cash(value); State.report.liquidation += value;
            Log($"Liquidated {Title(item)} for {value}."); return $"Received {value}.";
        }
        public int SaleChance(ItemState item)
        {
            if (!item.listed) return 0;
            var ratio = (double)item.askingPrice / Value(item);
            var boost = ShopCatalog.Items[item.template].category == State.favoredCategory ? 12 : 0;
            return Math.Clamp((int)Math.Round(100 - (ratio - 0.65) * 110) + boost, 0, 100);
        }
        public string Demand(ItemState item)
        {
            var chance = SaleChance(item);
            return !item.listed ? "Not listed" : chance >= 85 ? "Very strong" : chance >= 60 ? "Strong" : chance >= 35 ? "Uncertain" : chance > 0 ? "Weak" : "No buyers at this price";
        }
        int SaleRoll(ItemState item)
        {
            uint x = unchecked((uint)(State.seed ^ item.id * 73856093 ^ State.night * 19349663));
            x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15;
            return (int)(x % 100);
        }
        public string Forecast()
        {
            var expected = State.stock.Sum(x => x.listed ? x.askingPrice * SaleChance(x) / 100 : 0);
            var guarantees = State.stock.Where(x => SaleChance(x) == 100).Sum(x => x.askingPrice);
            return $"Cash {State.cash} • Expenses {State.due}\nSales estimate {expected} (not guaranteed); guaranteed sales {guarantees}.\nMaximum combined cash/item curse loss tonight: {LossBudget}.\nOrder: sales → curses on remaining stock → expenses.\nIf short, you can liquidate remaining goods before confirming bankruptcy.";
        }
        public string CloseNight()
        {
            if (!CanClose) return "This night is already closed.";
            State.customerIndex = State.customers.Count;
            State.distortion = false;
            foreach (var item in State.stock.ToArray())
                if (item.listed && SaleRoll(item) < SaleChance(item))
                {
                    Cash(item.askingPrice); State.report.sales += item.askingPrice; State.report.soldCount++;
                    State.stock.Remove(item); Log($"Sold {Title(item)} for {item.askingPrice}.");
                }
            int budget = LossBudget;
            foreach (var item in State.stock.OrderBy(x => x.id).ToArray())
            {
                if (item.protectedItem || item.curse == Curse.None) continue;
                if (item.curse == Curse.Whisper)
                {
                    State.distortion = true; Identify(item); Log($"{Title(item)} whispered after closing. Decorative text will be distorted next night.");
                }
                else if (item.curse == Curse.Hunger && budget > 0)
                {
                    var loss = Math.Min(budget, Math.Min(12, State.cash)); Cash(-loss); budget -= loss; State.report.curseLoss += loss;
                    Identify(item); Log($"{Title(item)} consumed {loss} cash. Isolation cannot block Hunger.");
                }
                else if (item.curse == Curse.Cold && budget > 0 && item.slot != IsolationSlot)
                {
                    var neighbor = State.stock.Where(x => x.id != item.id && IsAdjacent(item.slot, x.slot)).OrderBy(x => x.id).FirstOrDefault();
                    if (neighbor == null) continue;
                    var before = Value(neighbor);
                    var target = Math.Max(8, before - Math.Min(budget, 14));
                    neighbor.condition = Math.Max(10, (int)Math.Ceiling(target * 100.0 / neighbor.baseValue));
                    neighbor.condition = Math.Min(100, neighbor.condition);
                    var damage = Math.Max(0, before - Value(neighbor)); budget -= damage; State.report.damage += damage;
                    Identify(item); Log($"{Title(item)} chilled adjacent {Title(neighbor)}: appraisal fell by {damage}.");
                }
            }
            State.phase = NightPhase.Settlement;
            if (State.cash >= State.due) PayExpenses();
            return State.phase == NightPhase.Results ? "Night settled. Review the ledger before opening again." : "Expenses are due. Liquidate goods to cover the deficit, or confirm bankruptcy.";
        }
        public string PayExpenses()
        {
            if (State.phase != NightPhase.Settlement) return "No outstanding settlement.";
            if (State.cash < State.due) return $"Still short by {State.due - State.cash}.";
            Cash(-State.due); State.report.expenses = State.due;
            State.completedNights++;
            FinishReport(); State.phase = NightPhase.Results;
            Log($"Paid {State.due} expenses. Closing cash: {State.cash}. Cash change: {State.report.profit:+#;-#;0}.");
            return "Expenses paid.";
        }
        void FinishReport()
        {
            if (State.report.finalized) return;
            State.report.profit = State.cash - State.report.openingCash;
            State.totalProfit = (int)Math.Clamp((long)State.totalProfit + State.report.profit, -1000000000L, 1000000000L);
            State.peakCapital = Math.Max(State.peakCapital, Capital);
            State.report.finalized = true;
        }
        public string ConfirmBankruptcy()
        {
            if (State.phase != NightPhase.Settlement || State.cash >= State.due) return "Bankruptcy is only available when expenses cannot be paid.";
            FinishReport(); State.phase = NightPhase.Bankrupt; Log("Run ended. The debt remains unpaid."); return "The shop is closed.";
        }
        public string NextNight()
        {
            if (State.phase != NightPhase.Results) return "Settle this night first.";
            State.night++; OpenNight(); return "A new night begins.";
        }
        public int UpgradeLevel(Upgrade type) => type == Upgrade.Shelves ? State.shelves : type == Upgrade.Workbench ? State.workbench : type == Upgrade.Insulation ? State.insulation : State.expert;
        public int UpgradeCost(Upgrade type) => (type == Upgrade.Shelves ? 90 : type == Upgrade.Workbench ? 75 : type == Upgrade.Insulation ? 65 : 85) + UpgradeLevel(type) * 60;
        public string BuyUpgrade(Upgrade type)
        {
            if (State.phase != NightPhase.Reception && State.phase != NightPhase.Results) return "Upgrades are unavailable during settlement.";
            if (UpgradeLevel(type) >= 2) return "This improvement is already at its maximum level.";
            var cost = UpgradeCost(type); if (State.cash < cost) return "Not enough cash.";
            Cash(-cost);
            if (type == Upgrade.Shelves) State.shelves++;
            else if (type == Upgrade.Workbench) { State.workbench++; if (State.phase == NightPhase.Reception) State.actions += 2; }
            else if (type == Upgrade.Insulation) State.insulation++;
            else State.expert++;
            if (State.report.finalized) { State.report.profit -= cost; State.totalProfit -= cost; }
            State.report.upgrades += cost; Log($"Purchased {type} level {UpgradeLevel(type)} for {cost}."); return "Improvement installed.";
        }
    }
}
