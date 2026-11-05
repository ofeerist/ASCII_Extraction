using System;
using System.Linq;

namespace Pawnshop.Endless
{
    public enum ChapterEnding { None, Interrogation, OpenLedger, Agreement }
    public enum Evidence { Wear, Mark, Voice }

    public sealed partial class QuestRules
    {
        public void StartChapter()
        {
            State.chapter = true;
            House.State.Add(Supply.Loupe, 1);
            House.State.Add(Supply.UVFlashlight, 1);
            House.State.Add(Supply.Crackers, 1);
            PrepareChapterDay();
            Say("Глава I. Чужие имена. В журнале — справочник клейм и сводки. Лупа и УФ-фонарь остались от прежнего хозяина.");
        }
        void PrepareChapterDay()
        {
            if (!State.chapter) return;
            Shop.State.customers.Clear(); Shop.State.customerIndex = 0;
            int day = Shop.State.night;
            AddVisit("boris", day == 1 ? 9 : day == 2 ? 5 : 4, CustomerKind.Desperate, day == 2 ? 240 : 120, 65, Curse.None);
            AddVisit("dealer", day == 1 ? 3 : day == 2 ? 1 : 9, CustomerKind.Dealer, 300, 42, Curse.None);
            AddVisit("anna", day == 1 ? 2 : day == 2 ? 6 : 7, CustomerKind.Collector, 180, 60, Curse.Whisper);
            if (day >= 3 && State.robberyDelivered) State.policePending = true;
            Shop.State.actions = Math.Max(Shop.State.actions, 12);
        }
        void AddVisit(string key, int template, CustomerKind kind, int price, int condition, Curse curse)
        {
            Shop.State.customers.Add(new CustomerState { story = key, kind = kind, ask = price, minimum = price * 3 / 4,
                item = new ItemState { id = Shop.State.nextId++, template = template, baseValue = ShopCatalog.Items[template].value,
                    condition = condition, curse = curse, suspicious = curse != Curse.None, stolen = key == "boris" && Shop.State.night == 2 } });
        }
        public string VisitorName => State.policePending ? "Следователь" : Shop.Customer?.story == "boris" ? "Борис / сборщик чужих вещей" : Shop.Customer?.story == "dealer" ? "Аркадий / перекупщик" : "Анна / квартира № 17";
        public string ChapterStory()
        {
            var key = Shop.Customer?.story; int d = Shop.State.night;
            if (key == "boris") return d == 1 ? "Борис: «Медаль нашёл в котельной. Сказали, довоенная. На обороте Б-17. Возьмёшь — завтра ещё принесу»." : d == 2 ? "Борис: «Кольцо. Ю-041. Вчера лежало на витрине, сегодня у меня. Не спрашивай, как. Двести сорок — и я исчезаю»." : "Борис: «Я завязал с ювелиркой. Вот компас с той же котельной. Б-17. Стрелка всё время показывает на ваш подвал».";
            if (key == "dealer") return d == 1 ? "Аркадий: «Серебро 925, ручная работа. Видите клеймо 925-П? Такие до революции делали»." : d == 2 ? "Аркадий: «Камея из той же коллекции. Номер П-17. Бумаги потерялись при переезде»." : "Аркадий: «Ладно. Все эти вещи из квартиры 17. Продайте мне свои записи за 100 рублей — и забудем».";
            return d == 1 ? "Анна: «Молитвенник отца. Каждую ночь кто-то читает из него вслух. Отцу уже год как нечем говорить»." : d == 2 ? "Анна: «Теперь шкатулка повторяет тот же голос. Я нашла накладную: вещи из квартиры увозил Аркадий. Сохраните её в журнале»." : "Анна: «Атлас дописал моё имя. Помогите закончить это. Голос просит оставить одну вещь неочищенной до рассвета. Я боюсь того, что он обещает».";
        }
        public static readonly string[] Records = {
            "РЕСТАВРАЦИЯ · Износ ниже 70%: трещины и замена деталей. Цена исправной вещи неприменима.",
            "КЛЕЙМА · 925-П, П-17 и Б-17 — серия реквизита из квартиры № 17. Гальваника, послевоенная отливка. Ю-041 — кольцо из сводки об ограблении (со второго дня).",
            "АКУСТИКА · Зелёные буквы под УФ и повторяющийся голос — Шёпот. Замкнуть пять линий соли. Для договора: оставить носитель голоса неочищенным до третьего рассвета."
        };
        public string Observation(Evidence evidence)
        {
            var c = Shop.Customer; if (c == null) return "Нет предмета на проверке.";
            if (evidence == Evidence.Wear) return c.item.inspected ? $"Лупа: состояние {c.item.condition}%, трещины, следы замены деталей." : "Сначала осмотрите поверхность лупой.";
            if (evidence == Evidence.Mark) return c.item.inspected ? "Клеймо: " + (c.story == "boris" ? Shop.State.night == 2 ? "Ю-041" : "Б-17" : c.story == "dealer" ? Shop.State.night == 1 ? "925-П" : "П-17" : "не найдено") : "Сначала прочитайте клеймо лупой.";
            return c.item.tested ? c.item.curse == Curse.Whisper ? "УФ: зелёные буквы. Голос повторяет имя Анны." : "УФ: поверхность чистая, букв нет." : "Сначала проверьте поверхность УФ-фонарём.";
        }
        public string Compare(int record, Evidence evidence)
        {
            var c = Shop.Customer;
            if (!Active(DayPhase.Noon) || !State.doorOpened || c == null || State.policePending || c.accepted) return Say("Сверка доступна до согласования цены.");
            if (record < 0 || record >= Records.Length || !Enum.IsDefined(typeof(Evidence), evidence)) return Say("Выберите запись и признак.");
            bool observed = evidence == Evidence.Voice ? c.item.tested : c.item.inspected;
            if (!observed) return Say("Сначала получите этот признак инструментом на столе.");
            if ((c.evidenceMask & (1 << record)) != 0) return Say("Этот аргумент уже учтён. Повторная скидка не положена.");
            bool match = record == (int)evidence && (evidence == Evidence.Wear ? c.item.condition < 70 : evidence == Evidence.Mark ? c.story == "dealer" || c.story == "boris" : c.item.curse == Curse.Whisper);
            if (!match)
            {
                c.attempts--; if (c.attempts <= 0) { Shop.Reject(); ResetVisitor(); }
                return Say("«Эта запись ничего не доказывает». Собеседник теряет терпение; ложные обвинения могут сорвать сделку.");
            }
            c.evidenceMask |= 1 << record;
            c.minimum = Math.Max(4, c.minimum / 3); c.ask = Math.Max(c.minimum, c.ask / 2);
            if (c.story == "dealer" && record == 1) State.forgeryExposed = true;
            if (c.story == "boris" && Shop.State.night == 2 && record == 1) State.ringIdentified = true;
            var note = $"День {Shop.State.night}. {VisitorName}: {Observation(evidence)}";
            if (!Shop.State.journal.Contains(note)) Shop.Log(note);
            return Say($"«Ладно, это серьёзно. Теперь прошу {c.ask} ₽». Вы нашли основание для гораздо более низкого встречного предложения.");
        }
        void ResetVisitor() { State.doorOpened = State.storyAsked = State.priceAsked = false; }
        void RecordPurchase(CustomerState customer)
        {
            if (!State.chapter || !Shop.State.stock.Contains(customer.item)) return;
            if (customer.story == "boris" && customer.item.stolen) { State.robberyDelivered = true; Shop.Log("Принято кольцо Ю-041 от Бориса. Следователь придёт на третий день."); }
            if (customer.story == "anna") State.annaHelped = true;
        }
        public string SellRecords()
        {
            if (!Active(DayPhase.Noon) || Shop.State.night != 3 || Shop.Customer?.story != "dealer" || State.recordsSold || !State.doorOpened) return Say("Предложение недоступно.");
            State.recordsSold = true; Shop.State.cash += 100; Shop.State.report.sales += 100;
            Shop.Reject(); ResetVisitor();
            return Say("Аркадий забирает накладную. +100 ₽. Вы сохранили деньги, но лишились доказательства подмены.");
        }
        public bool CanAgree => State.chapter && State.annaHelped && Shop.State.stock.Any(x => x.curse == Curse.Whisper);
        public string FinishChapter(bool agreement)
        {
            if (!State.chapter || State.ending != ChapterEnding.None || Shop.State.night < 3 || Shop.State.phase != NightPhase.Results) return Say("Финал открывается после расчётов третьего дня.");
            if (State.robberyDelivered && !State.policeResolved) return Say("Сначала предстоит допрос.");
            if (agreement && !CanAgree) return Say("Для договора нужен неочищенный носитель голоса Анны.");
            State.ending = agreement ? ChapterEnding.Agreement : ChapterEnding.OpenLedger;
            return Say(EndingText);
        }
        public string EndingText => State.ending == ChapterEnding.Interrogation ? "ФИНАЛ I · ПОКАЗАНИЯ\nСледователь выключает диктофон. За стеклом сидит Борис. Последний вопрос — уже о вас. Глава I окончена." : State.ending == ChapterEnding.Agreement ? "ФИНАЛ III · НОВЫЙ ЖИЛЕЦ\nВы называете голосу своё имя. Долги в книге исчезают. За запертой дверью подвала кто-то ставит второй стул. Глава I окончена." : State.forgeryExposed && State.annaHelped && !State.recordsSold ? "ФИНАЛ II · ОТКРЫТАЯ КНИГА\nНакладная Анны и клейма складываются в доказательство подмены. Аркадий больше не придёт. Анна оставляет ключ от квартиры 17: теперь можно узнать, кто говорил из вещей. Глава I окончена." : "ФИНАЛ II · ОТКРЫТАЯ КНИГА\nВы закрываете кассу и отказываетесь от голоса. В цепочке подмен не хватает доказательств. Под дверью — ключ от квартиры 17 и записка: «Проверьте сами». Глава I окончена.";
    }
}
