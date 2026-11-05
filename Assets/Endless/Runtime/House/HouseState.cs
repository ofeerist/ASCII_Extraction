using System;
using System.Collections.Generic;
using System.Linq;

namespace Pawnshop.Endless
{
    public enum Supply { Loupe, UVFlashlight, GoldReagent, Milk, Salt, Vodka, Crackers }
    public enum InspectionTool { Hands, Loupe, Ultraviolet, GoldReagent }
    [Serializable] public sealed class SupplyStack { public Supply supply; public int count; }
    [Serializable] public sealed class HouseState
    {
        public List<SupplyStack> inventory = new List<SupplyStack>();
        public int carriedId;
        public int tableId;
        public int ritualId;
        public int saltMask;
        public bool saltPlaced;
        public bool milkPoured;
        public int visitorId;
        public bool doorOpen;
        public bool itemBorrowed;
        public bool hasPosition;
        public float playerX = 1.5f;
        public float playerY = 0.1f;
        public float playerZ = 2.1f;
        public float yaw = 180;
        public float pitch;
        public int Count(Supply supply) => inventory?.FirstOrDefault(x => x.supply == supply)?.count ?? 0;
        public void Add(Supply supply, int count)
        {
            inventory ??= new List<SupplyStack>();
            var stack = inventory.FirstOrDefault(x => x.supply == supply);
            if (stack == null) { stack = new SupplyStack { supply = supply }; inventory.Add(stack); }
            stack.count += count;
        }
        public bool TryExchange(IReadOnlyDictionary<Supply, int> required, IReadOnlyDictionary<Supply, int> received)
        {
            if (required == null || received == null || required.Any(x => !Enum.IsDefined(typeof(Supply), x.Key) || x.Value < 0 || Count(x.Key) < x.Value) || received.Any(x => !Enum.IsDefined(typeof(Supply), x.Key) || x.Value < 0)) return false;
            foreach (Supply supply in Enum.GetValues(typeof(Supply)))
            {
                var outgoing = required.TryGetValue(supply, out var a) ? a : 0;
                var incoming = received.TryGetValue(supply, out var b) ? b : 0;
                var total = (long)Count(supply) - outgoing + incoming;
                if (total > SupplyCatalog.Limit(supply)) return false;
            }
            foreach (var entry in required) Add(entry.Key, -entry.Value);
            foreach (var entry in received) Add(entry.Key, entry.Value);
            return true;
        }
        public bool Consume(Supply supply) => TryExchange(new Dictionary<Supply, int> { [supply] = 1 }, new Dictionary<Supply, int>());
    }
    public static class SupplyCatalog
    {
        public static string Name(Supply supply) => supply switch {
            Supply.Loupe => "Magnifying glass", Supply.UVFlashlight => "UV flashlight", Supply.GoldReagent => "Gold testing reagent",
            Supply.Milk => "Milk carton", Supply.Salt => "Salt packet", Supply.Vodka => "Small vodka bottle", _ => "Crackers"
        };
        public static string Description(Supply supply) => supply switch {
            Supply.Loupe => "Reusable. Examine three surface details at the desk.",
            Supply.UVFlashlight => "Reusable. Scan three surface regions for supernatural residue.",
            Supply.GoldReagent => "One application. Verify gold content at the marked test patch.",
            Supply.Salt => "One circle. Trace all eight sections around an item on the rug.",
            Supply.Milk => "One offering. Pour into the bowl inside a completed salt circle.",
            Supply.Vodka => "One cleansing. Wipe the item after making the milk offering.",
            _ => "Trade goods. Kept for future exchanges and quests; no quest use in endless."
        };
        public static int Price(Supply supply) => supply switch {
            Supply.Loupe => 32, Supply.UVFlashlight => 55, Supply.GoldReagent => 6,
            Supply.Milk => 8, Supply.Salt => 6, Supply.Vodka => 14, _ => 4
        };
        public static int Limit(Supply supply) => supply == Supply.Loupe || supply == Supply.UVFlashlight ? 1 : 99;
    }
    public sealed class InspectionTrace
    {
        readonly float[] dwell = new float[3];
        readonly float duration;
        public InspectionTool Tool { get; }
        public int Required => Tool == InspectionTool.GoldReagent ? 1 : 3;
        public bool Complete => Tool != InspectionTool.Hands && Enumerable.Range(0, Required).All(x => dwell[x] >= duration);
        public int Completed => Enumerable.Range(0, Required).Count(x => dwell[x] >= duration);
        public InspectionTrace(InspectionTool tool, int expertise = 0) { Tool = tool; duration = Math.Max(0.4f, 0.8f - Math.Clamp(expertise, 0, 2) * 0.15f); }
        public void Trace(int region, float seconds)
        {
            if (Tool == InspectionTool.Hands || region < 0 || region >= Required || float.IsNaN(seconds) || seconds <= 0) return;
            dwell[region] = Math.Min(duration, dwell[region] + Math.Min(seconds, 0.1f));
        }
        public float Progress(int region) => region >= 0 && region < 3 ? dwell[region] / duration : 0;
    }
}
