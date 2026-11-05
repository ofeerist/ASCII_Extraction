using System;
using System.Linq;

namespace Pawnshop.Endless
{
    public sealed class HouseRules
    {
        public ShopSimulation Shop { get; }
        public HouseState State => Shop.State.house;
        public HouseRules(ShopSimulation shop)
        {
            Shop = shop;
            shop.State.house ??= new HouseState();
            State.inventory ??= new System.Collections.Generic.List<SupplyStack>();
            Synchronize();
        }
        public ItemState Find(int id) => id <= 0 ? null : Shop.Customer?.item.id == id ? Shop.Customer.item : Shop.State.stock.FirstOrDefault(x => x.id == id);
        public bool Owned(int id) => Shop.State.stock.Any(x => x.id == id);
        public void Synchronize()
        {
            if (Find(State.carriedId) == null) State.carriedId = 0;
            if (Find(State.tableId) == null) State.tableId = 0;
            if (Find(State.ritualId) == null) { State.ritualId = 0; ResetRitual(); }
            var visitor = Shop.Customer?.item.id ?? 0;
            if (State.visitorId != visitor) { State.visitorId = visitor; State.doorOpen = false; State.itemBorrowed = false; }
        }
        public string Purchase(Supply supply)
        {
            if (!Enum.IsDefined(typeof(Supply), supply)) return "Unknown supply.";
            if (Shop.State.phase != NightPhase.Reception && Shop.State.phase != NightPhase.Results) return "The supplier is closed during settlement.";
            if (State.Count(supply) >= SupplyCatalog.Limit(supply)) return "You already have the maximum quantity.";
            var cost = SupplyCatalog.Price(supply);
            if (Shop.State.cash < cost) return "Not enough cash.";
            Shop.State.cash -= cost; Shop.State.report.supplies += cost;
            if (Shop.State.report.finalized) { Shop.State.report.profit -= cost; Shop.State.totalProfit -= cost; }
            State.Add(supply, 1); Shop.Log($"Purchased {SupplyCatalog.Name(supply)} for {cost}.");
            return "Delivered: " + SupplyCatalog.Name(supply) + ".";
        }
        public string Borrow()
        {
            if (Shop.Customer == null || !State.doorOpen) return "Open the door and speak to the visitor.";
            if (State.carriedId != 0) return "Put down the item you are carrying first.";
            if (State.itemBorrowed) return "The visitor's item is already inside.";
            State.itemBorrowed = true; State.carriedId = Shop.Customer.item.id;
            return "Borrowed for inspection. The visitor retains ownership until you buy it.";
        }
        public string PickUp(int id)
        {
            if (State.carriedId != 0) return "Your hands are full.";
            if (Find(id) == null || (!Owned(id) && !State.itemBorrowed)) return "Item unavailable.";
            if (Shop.State.phase != NightPhase.Reception && Shop.State.phase != NightPhase.Settlement) return "Open the next night before moving goods.";
            if (State.tableId == id) State.tableId = 0;
            if (State.ritualId == id) { State.ritualId = 0; ResetRitual(); }
            State.carriedId = id;
            return "Carrying " + Shop.Title(Find(id)) + ".";
        }
        public string PlaceOnTable()
        {
            if (State.carriedId == 0) return "Bring an item to the desk.";
            if (State.tableId != 0) return "The desk is occupied. Pick up its item first.";
            State.tableId = State.carriedId; State.carriedId = 0;
            return "Item placed. Interact again to examine it.";
        }
        public string PlaceOnRug()
        {
            if (State.carriedId == 0 || !Owned(State.carriedId)) return "Rituals require an item you own.";
            if (State.ritualId != 0) return "The ritual rug is occupied.";
            if (Shop.State.phase != NightPhase.Reception) return "Rituals are available before closing.";
            State.ritualId = State.carriedId; State.carriedId = 0; ResetRitual();
            return "Item placed inside the ritual area. Salt, milk and vodka are required.";
        }
        public string PlaceOnShelf(int slot)
        {
            if (!Owned(State.carriedId)) return "Only purchased goods can be stored on your shelves.";
            if (Shop.State.phase != NightPhase.Reception) return "Place goods before closing.";
            var item = Find(State.carriedId);
            var result = Shop.Move(item.id, slot);
            if (item.slot != slot) return result;
            State.carriedId = 0;
            return "Placed on shelf. Set a sale price on its tag.";
        }
        public string CanInspect(InspectionTool tool)
        {
            var item = Find(State.tableId);
            if (item == null || Shop.State.phase != NightPhase.Reception) return "Place an item on the desk during reception.";
            if (tool == InspectionTool.Hands) return "Rotate the object with the right mouse button.";
            var supply = tool == InspectionTool.Loupe ? Supply.Loupe : tool == InspectionTool.Ultraviolet ? Supply.UVFlashlight : Supply.GoldReagent;
            if (State.Count(supply) == 0) return "Buy " + SupplyCatalog.Name(supply) + " from the supplier first.";
            if ((tool == InspectionTool.Loupe && item.inspected) || (tool == InspectionTool.Ultraviolet && item.tested) || (tool == InspectionTool.GoldReagent && item.goldTested)) return "This result has already been verified.";
            if (Shop.State.actions < 1) return "No actions remain tonight.";
            return null;
        }
        public string CompleteInspection(InspectionTrace trace, int expectedItem)
        {
            if (trace == null || !trace.Complete || State.tableId != expectedItem) return "Complete the surface examination first.";
            var blocked = CanInspect(trace.Tool); if (blocked != null) return blocked;
            var item = Find(State.tableId);
            string result;
            if (trace.Tool == InspectionTool.Loupe)
            {
                item.inspected = true;
                result = $"Verified condition {item.condition}%. Appraisal {ShopSimulation.Value(item)}.";
            }
            else if (trace.Tool == InspectionTool.Ultraviolet)
            {
                item.tested = true; item.identified = true;
                if (item.curse != Curse.None && !Shop.State.discoveries.Contains(item.curse)) Shop.State.discoveries.Add(item.curse);
                result = "UV scan: " + ShopCatalog.CurseName(item.curse) + ". " + ShopCatalog.Rule(item.curse);
            }
            else
            {
                if (!State.Consume(Supply.GoldReagent)) return "No reagent left.";
                item.goldTested = true;
                item.containsGold = (item.template == 1 || item.template == 5) && unchecked((uint)(Shop.State.seed ^ item.id * 7919)) % 4 != 0;
                result = item.containsGold ? "Gold test: stable golden trace. Gold-bearing metal confirmed." : "Gold test: green reaction. Gold excluded at the tested surface.";
            }
            Shop.State.actions--; Shop.Log(Shop.Title(item) + " — " + result);
            return result;
        }
        public string TraceSalt(int sector)
        {
            if (Shop.State.phase != NightPhase.Reception || !Owned(State.ritualId) || sector < 0 || sector > 7) return "Place an owned item on the rug first.";
            if (State.saltPlaced) return "The circle is complete.";
            if (State.Count(Supply.Salt) == 0) return "You need a salt packet.";
            State.saltMask |= 1 << sector;
            if (State.saltMask != 255) return "Keep tracing the circle.";
            State.Consume(Supply.Salt); State.saltPlaced = true;
            Shop.Log("Completed a salt circle. Used one salt packet.");
            return "Circle complete. Pour milk into the offering bowl.";
        }
        public string PourMilk(bool traced)
        {
            if (!traced || !State.saltPlaced || State.milkPoured || !Owned(State.ritualId) || Shop.State.phase != NightPhase.Reception) return "Complete the circle, then hold over the bowl to pour.";
            if (!State.Consume(Supply.Milk)) return "You need a milk carton.";
            State.milkPoured = true; Shop.Log("Poured a milk offering. Used one carton.");
            return "Offering made. Wipe three regions of the item with vodka.";
        }
        public string Cleanse(InspectionTrace trace)
        {
            if (trace == null || !trace.Complete || !State.milkPoured || !Owned(State.ritualId) || Shop.State.phase != NightPhase.Reception) return "Complete the ritual before cleansing.";
            if (Shop.State.actions < 1) return "Cleansing requires one action.";
            if (!State.Consume(Supply.Vodka)) return "You need a small vodka bottle.";
            var item = Find(State.ritualId); var curse = item.curse;
            if (curse != Curse.None && !Shop.State.discoveries.Contains(curse)) Shop.State.discoveries.Add(curse);
            item.curse = Curse.None; item.identified = true; item.tested = true;
            Shop.State.actions--; Shop.Log($"Cleansed {Shop.Title(item)} on the ritual rug. Removed {ShopCatalog.CurseName(curse)}. Used one vodka bottle.");
            ResetRitual(); return "Ritual complete. The item is now free of supernatural effects.";
        }
        public void ResetRitual() { State.saltMask = 0; State.saltPlaced = false; State.milkPoured = false; }
        public bool IsOnShelf(ItemState item) => item.id != State.carriedId && item.id != State.tableId && item.id != State.ritualId;
        public string CloseNight()
        {
            if (Shop.State.stock.Any(x => !IsOnShelf(x))) return "Place every purchased item on a shelf before closing.";
            var result = Shop.CloseNight(); Synchronize(); return result;
        }
    }
}
