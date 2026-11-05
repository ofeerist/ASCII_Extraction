using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Pawnshop.Endless
{
    public sealed class RunStorage
    {
        [Serializable] sealed class Envelope { public int version = 1; public string payload; public string checksum; }
        readonly string directory;
        public string Notice { get; private set; }
        public RunStorage(string directory) { this.directory = directory; }
        public bool HasRun => File.Exists(Path.Combine(directory, "endless-run.json")) || File.Exists(Path.Combine(directory, "endless-run.json.bak"));
        static string Hash(string value)
        {
            using var algorithm = SHA256.Create();
            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
        string Decode(string path)
        {
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if (envelope == null || envelope.version != 1 || string.IsNullOrEmpty(envelope.payload) || envelope.checksum != Hash(envelope.payload))
                throw new InvalidDataException("Save checksum or version is invalid.");
            return envelope.payload;
        }
        T Load<T>(string name, Func<T, bool> validate) where T : class
        {
            Notice = null;
            var path = Path.Combine(directory, name);
            foreach (var candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var value = JsonUtility.FromJson<T>(Decode(candidate));
                    if (!validate(value)) throw new InvalidDataException("Save contains invalid state.");
                    if (candidate.EndsWith(".bak", StringComparison.Ordinal)) Notice = "The main save could not be read. Recovered the previous backup.";
                    return value;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
                { Notice = "A saved file could not be read. " + e.Message; }
            }
            return null;
        }
        void Save<T>(string name, T value)
        {
            Directory.CreateDirectory(directory);
            var payload = JsonUtility.ToJson(value);
            var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(payload) }));
            var path = Path.Combine(directory, name); var temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(data, 0, data.Length); stream.Flush(true); }
            if (File.Exists(path))
            {
                bool valid;
                try { Decode(path); valid = true; } catch { valid = false; }
                File.Replace(temporary, path, valid ? path + ".bak" : null);
            }
            else File.Move(temporary, path);
        }
        public RunState LoadRun() => Load<RunState>("endless-run.json", Validate);
        public PlayerProfile LoadProfile() => Load<PlayerProfile>("endless-profile.json", x => x != null && x.version == 1 && x.discoveries != null) ?? new PlayerProfile();
        public void SaveRun(RunState state)
        {
            if (!Validate(state)) throw new InvalidDataException("Refusing to save an invalid run.");
            Save("endless-run.json", state);
        }
        public void SaveProfile(PlayerProfile profile) => Save("endless-profile.json", profile);
        public static bool Validate(RunState state)
        {
            if (state == null || state.version != 1 || state.balanceVersion != 1 || state.randomState == 0 || state.night < 1 ||
                state.cash < 0 || state.cash > 1000000000 || state.actions < 0 || state.actions > 20 || state.due < 0 || state.due > 1000 ||
                state.shelves < 0 || state.shelves > 2 || state.workbench < 0 || state.workbench > 2 || state.insulation < 0 || state.insulation > 2 || state.expert < 0 || state.expert > 2 ||
                !Enum.IsDefined(typeof(NightPhase), state.phase) || state.stock == null || state.customers == null || state.journal == null || state.discoveries == null || state.report == null ||
                state.customers.Count < 1 || state.customers.Count > 6 || state.customerIndex < 0 || state.customerIndex > state.customers.Count || state.stock.Count > 17) return false;
            if (state.house != null && !ValidateHouse(state.house)) return false;
            if (state.quest != null)
            {
                var q = state.quest;
                if (!Enum.IsDefined(typeof(ChapterEnding), q.ending) || !Enum.IsDefined(typeof(DayPhase), q.phase) || q.streetQuestion < 0 || q.streetQuestion > 3 ||
                    q.trust < 0 || q.trust > q.streetQuestion || q.policeQuestion < 0 || q.policeQuestion > 3 ||
                    q.contradictions < 0 || q.contradictions > q.policeQuestion || q.saltPoints < 0 || q.saltPoints > 5 ||
                    q.ritualId < 0 || (q.milk && q.saltPoints != 5) || (q.boiled && !q.milk) ||
                    (q.policeResolved && q.policePending)) return false;
            }
            bool ItemValid(ItemState item) => item != null && item.id > 0 && item.template >= 0 && item.template < ShopCatalog.Items.Length && item.condition >= 10 && item.condition <= 100 &&
                item.baseValue >= 50 && item.baseValue <= 150 && Enum.IsDefined(typeof(Curse), item.curse) && item.cost >= 0 && item.askingPrice >= 0 && item.askingPrice <= 100000;
            if (state.stock.Any(x => !ItemValid(x) || (x.slot != ShopSimulation.IsolationSlot && (x.slot < 0 || x.slot >= 8 + state.shelves * 4)))) return false;
            if (state.stock.Select(x => x.slot).Distinct().Count() != state.stock.Count || state.stock.Select(x => x.id).Distinct().Count() != state.stock.Count) return false;
            if (state.customers.Any(x => x == null || !ItemValid(x.item) || !Enum.IsDefined(typeof(CustomerKind), x.kind) || x.evidenceMask < 0 || x.evidenceMask > 7 || x.minimum < 1 || x.ask < x.minimum || x.attempts < 0 || x.attempts > 3 || x.acceptedPrice < 0)) return false;
            var activeIds = state.stock.Select(x => x.id).Concat(state.customers.Skip(state.customerIndex).Select(x => x.item.id)).ToArray();
            return activeIds.Distinct().Count() == activeIds.Length && state.nextId > state.customers.Max(x => x.item.id);
        }
        static bool ValidateHouse(HouseState house)
        {
            if (house.inventory == null || house.inventory.Count > Enum.GetValues(typeof(Supply)).Length || house.inventory.Any(x => x == null || !Enum.IsDefined(typeof(Supply), x.supply) || x.count < 0 || x.count > SupplyCatalog.Limit(x.supply))) return false;
            if (house.inventory.Select(x => x.supply).Distinct().Count() != house.inventory.Count) return false;
            if (house.carriedId < 0 || house.tableId < 0 || house.ritualId < 0 || house.saltMask < 0 || house.saltMask > 255 || (house.saltPlaced && house.saltMask != 255) || (house.milkPoured && !house.saltPlaced)) return false;
            var positions = new[] { house.carriedId, house.tableId, house.ritualId }.Where(x => x > 0).ToArray();
            if (positions.Distinct().Count() != positions.Length) return false;
            return new[] { house.playerX, house.playerY, house.playerZ, house.yaw, house.pitch }.All(x => !float.IsNaN(x) && !float.IsInfinity(x));
        }
        public static void UpdateRecords(PlayerProfile profile, RunState run, int capital)
        {
            if (run.completedNights > profile.bestNights) { profile.bestNights = run.completedNights; profile.recordSeed = run.seed; }
            profile.bestProfit = Math.Max(profile.bestProfit, run.totalProfit);
            profile.bestCapital = Math.Max(profile.bestCapital, Math.Max(run.peakCapital, capital));
            foreach (var discovery in run.discoveries) if (!profile.discoveries.Contains(discovery)) profile.discoveries.Add(discovery);
        }
    }
}
