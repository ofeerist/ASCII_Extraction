using System;
using System.Collections.Generic;

namespace Pawnshop.Endless
{
    public enum NightPhase { Reception, Settlement, Results, Bankrupt }
    public enum Curse { None, Cold, Whisper, Hunger }
    public enum Category { Curios, Jewelry, Books, Instruments }
    public enum CustomerKind { Desperate, Dealer, Collector, Stranger }
    public enum Diagnostic { Loupe, Scales, Occult }
    public enum Upgrade { Shelves, Workbench, Insulation, Expert }

    [Serializable]
    public sealed class ItemState
    {
        public int id;
        public int template;
        public int condition;
        public int baseValue;
        public Curse curse;
        public bool suspicious;
        public bool inspected;
        public bool weighed;
        public bool tested;
        public bool identified;
        public bool protectedItem;
        public int acquiredNight;
        public int slot;
        public int cost;
        public int askingPrice;
        public bool listed;
        public bool stolen;
        public bool goldTested;
        public bool containsGold;
        public string observedMark;
        public bool cleansed;
        public bool barterPurchase;
    }

    [Serializable]
    public sealed class CustomerState
    {
        public CustomerKind kind;
        public ItemState item;
        public int ask;
        public int minimum;
        public int attempts = 3;
        public bool usedArgument;
        public int evidenceMask;
        public bool accepted;
        public int acceptedPrice;
        public string story;
        public string response;
        public bool conversationStarted;
        public List<int> conversationActions = new List<int>();
    }

    [Serializable]
    public sealed class NightReport
    {
        public int openingCash;
        public int purchases;
        public int tests;
        public int protection;
        public int supplies;
        public int upgrades;
        public int sales;
        public int liquidation;
        public int curseLoss;
        public int damage;
        public int expenses;
        public int soldCount;
        public int profit;
        public bool finalized;
    }

    [Serializable]
    public sealed class RunState
    {
        public int version = 1;
        public int balanceVersion = 1;
        public int seed;
        public uint randomState;
        public int nextId = 1;
        public int night = 1;
        public int completedNights;
        public int cash = 220;
        public int actions;
        public int due;
        public int customerIndex;
        public int shelves;
        public int workbench;
        public int insulation;
        public int expert;
        public int totalProfit;
        public int peakCapital = 220;
        public bool distortion;
        public string eventName;
        public string eventDescription;
        public Category favoredCategory;
        public List<CustomerState> customers = new List<CustomerState>();
        public List<ItemState> stock = new List<ItemState>();
        public List<string> journal = new List<string>();
        public List<Curse> discoveries = new List<Curse>();
        public NightReport report = new NightReport();
        public NightPhase phase;
        public HouseState house;
        public QuestState quest;
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public int version = 1;
        public int bestNights;
        public int bestProfit;
        public int bestCapital;
        public int recordSeed;
        public bool sound = true;
        public bool effects = true;
        public List<Curse> discoveries = new List<Curse>();
    }

    public sealed class ItemTemplate
    {
        public readonly string name;
        public readonly Category category;
        public readonly int value;
        public readonly Curse curse;
        public readonly string description;
        public ItemTemplate(string name, Category category, int value, Curse curse, string description)
        { this.name = name; this.category = category; this.value = value; this.curse = curse; this.description = description; }
    }

    public static class ShopCatalog
    {
        public static readonly ItemTemplate[] Items = {
            new ItemTemplate("Pocket watch", Category.Curios, 96, Curse.Cold, "The hands move even when you hold your breath."),
            new ItemTemplate("Ivory cameo", Category.Jewelry, 88, Curse.Cold, "A face cut in ivory. The eyes seem unfinished."),
            new ItemTemplate("Prayer book", Category.Books, 76, Curse.Whisper, "Someone has crossed out every name in the margins."),
            new ItemTemplate("Silver coin", Category.Jewelry, 84, Curse.Hunger, "The date has been polished away."),
            new ItemTemplate("Brass compass", Category.Instruments, 90, Curse.Cold, "North is a matter of opinion tonight."),
            new ItemTemplate("Wedding ring", Category.Jewelry, 110, Curse.Hunger, "An inscription has been filed off the inside."),
            new ItemTemplate("Music box", Category.Curios, 102, Curse.Whisper, "The last note never quite resolves."),
            new ItemTemplate("Botanical atlas", Category.Books, 82, Curse.Whisper, "Several plants are labelled with human names."),
            new ItemTemplate("Opera glasses", Category.Instruments, 94, Curse.Cold, "The lenses show a room that is slightly too dark."),
            new ItemTemplate("Bronze medal", Category.Curios, 74, Curse.Hunger, "Awarded for a service no one will describe.")
        };
        public static string CurseName(Curse curse) => curse == Curse.Cold ? "Cold touch" : curse == Curse.Whisper ? "Whispering" : curse == Curse.Hunger ? "Hunger" : "No curse";
        public static string Rule(Curse curse)
        {
            switch (curse)
            {
                case Curse.Cold: return "At closing, damages one orthogonally adjacent item. An empty neighboring space or the isolation slot blocks contact. Wrapping suppresses the curse.";
                case Curse.Whisper: return "If kept through closing, distorts decorative text during the next night. Wrapping suppresses it. The ledger and financial controls stay readable.";
                case Curse.Hunger: return "At closing, consumes up to 12 cash while stored. Isolation does not help. Sell, wrap or dispose of it before closing.";
                default: return "No supernatural effect. An ordinary defect can imitate a warning sign.";
            }
        }
        public static string Sign(ItemState item)
        {
            if (!item.suspicious) return "No unusual signs. Ordinary wear is visible.";
            switch (Items[item.template].curse)
            {
                case Curse.Cold: return "WARNING: the surface is unusually cold.";
                case Curse.Whisper: return "WARNING: a faint whisper seems to follow the object.";
                default: return "WARNING: it feels heavier than it should.";
            }
        }
    }
}
