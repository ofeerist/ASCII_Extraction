using System;
using System.Linq;

namespace Pawnshop.Endless
{
    public enum DayPhase { Morning, Noon, Evening, Night }
    [Serializable] public sealed class QuestState
    {
        public DayPhase phase;
        public bool chapter, forgeryExposed, ringIdentified, annaHelped, recordsSold;
        public ChapterEnding ending;
        public int trust;
        public int streetQuestion;
        public bool robberyPromised;
        public bool robberyDelivered;
        public int robberyDay;
        public bool policePending;
        public bool policeResolved;
        public int policeQuestion;
        public int contradictions;
        public int ritualId;
        public int saltPoints;
        public bool milk;
        public bool boiled;
        public bool shopTalked;
        public bool doorOpened;
        public bool storyAsked;
        public bool priceAsked;
        public string message = "Утро пахнет хлебом и сырым подвалом. За прилавком — Нина.";
    }

    // Narrative phase guards live here, independently of the presentation and old endless mode.
    public sealed partial class QuestRules
    {
        public ShopSimulation Shop { get; }
        public QuestState State => Shop.State.quest;
        public HouseRules House { get; }
        public QuestRules(ShopSimulation shop)
        {
            Shop = shop; shop.State.quest ??= new QuestState(); House = new HouseRules(shop);
        }
        public bool Active(DayPhase phase) => State.ending == ChapterEnding.None && State.phase == phase && Shop.State.phase == NightPhase.Reception;
        public string Say(string text) { State.message = text; return text; }
        public string Purchase(Supply supply)
        {
            if (!Enum.IsDefined(typeof(Supply), supply)) return Say("Такого товара нет.");
            if (!Active(DayPhase.Morning)) return Say("Нина уже закрыла магазин.");
            if (House.State.Count(supply) >= SupplyCatalog.Limit(supply) || Shop.State.cash < SupplyCatalog.Price(supply)) return Say("Не хватает денег или в сумке уже достаточно.");
            House.Purchase(supply); return Say("Нина заворачивает покупку в газету. «Береги себя».");
        }
        public string Advance()
        {
            if (Shop.State.phase != NightPhase.Reception) return Say("Сначала разберитесь с итогами дня.");
            if (State.phase == DayPhase.Noon && (Shop.Customer != null || State.policePending)) return Say("Сначала закончите разговоры у двери.");
            if (State.phase == DayPhase.Night) return Say("Сначала завершите ночь.");
            State.phase++;
            if (State.phase == DayPhase.Noon && !State.chapter) DeliverRobbery();
            return Say(State.phase == DayPhase.Noon ? "Тук. Тук. Тук. За дверью первый посетитель." : State.phase == DayPhase.Evening ? "Дом затих. Пора сфотографировать вещи и разместить объявления." : "Телефон погас. На полу проступают чужие следы. Пора заняться проклятиями.");
        }
        public string AnswerStreet(int answer)
        {
            if (!Active(DayPhase.Morning) || answer < 0 || answer > 1 || State.streetQuestion >= 3) return Say("Разговор уже закончен.");
            var correct = new[] { 1, 0, 1 };
            if (answer == correct[State.streetQuestion]) State.trust++;
            State.streetQuestion++;
            return Say(State.streetQuestion == 3 ? (State.trust >= 2 ? "Борис: «Ты нормальный. Принесёшь чекушку — принесу тебе кое-что из ювелирки. Только потом сам отвечай»." : "Борис отворачивается: «С тобой дел не будет». Ограбление недоступно, но ломбард работает дальше.") : "Борис присматривается к вам и задаёт следующий вопрос.");
        }
        public string PromiseRobbery()
        {
            if (!Active(DayPhase.Morning) || State.streetQuestion < 3 || State.trust < 2 || State.robberyPromised) return Say("Борис не согласен на сделку.");
            if (!House.State.Consume(Supply.Vodka)) return Say("Нужна одна чекушка. Её продаёт Нина.");
            State.robberyPromised = true; State.robberyDay = Shop.State.night;
            return Say("Борис прячет чекушку. «Завтра занесу». Вы понимаете: украшение будет краденым.");
        }
        void DeliverRobbery()
        {
            if (!State.robberyPromised || State.robberyDelivered || Shop.State.night <= State.robberyDay) return;
            var slot = FreeSlot(); if (slot < 0) { Say("Борис вернётся завтра: в хранилище нет места."); return; }
            Shop.State.stock.Add(new ItemState { id = Shop.State.nextId++, template = 5, baseValue = 110, condition = 100, slot = slot, askingPrice = 110, acquiredNight = Shop.State.night, stolen = true, containsGold = true });
            State.robberyDelivered = true; Shop.Log("Борис принёс краденое кольцо. Объявление привлечёт полицию.");
        }
        int FreeSlot() => Enumerable.Range(0, Shop.Capacity).Concat(new[] { ShopSimulation.IsolationSlot }).Where(x => Shop.State.stock.All(i => i.slot != x)).DefaultIfEmpty(-1).First();
        public string AskStory()
        {
            if (!Active(DayPhase.Noon) || !State.doorOpened || Shop.Customer == null || State.policePending) return Say("Сейчас некому отвечать.");
            State.storyAsked = true;
            if (State.chapter) { if (Shop.Customer.story == "anna" && Shop.State.night >= 2) { State.annaHelped = true; Shop.Log("Накладная Анны: квартиру 17 освобождал Аркадий. Серия П-17."); } return Say(ChapterStory()); }
            return Say(new[] { "Анна: «Это осталось от отца. Мне нужно уехать, пока я ещё могу спать по ночам».", "Аркадий: «С распродажи старой квартиры. Документов нет, зато вещь редкая. Я цену знаю».", "Лев: «Я собирал такие годами. Но этой вещи не место рядом с остальными».", "Женщина в вуали: «Она сама выбрала ваш дом. Я только донесла»." }[(int)Shop.Customer.kind]);
        }
        public string Inspect(InspectionTool tool)
        {
            if (!Active(DayPhase.Noon) || !State.doorOpened || Shop.Customer == null || State.policePending) return Say("Сначала откройте дверь посетителю.");
            if (!Enum.IsDefined(typeof(InspectionTool), tool)) return Say("Неизвестный инструмент.");
            var item = Shop.Customer.item;
            if (tool == InspectionTool.Hands) return Say(item.suspicious ? "На поверхности что-то не так. Нужна проверка инструментом." : "Обычные следы износа. На ощупь ничего странного.");
            var supply = tool == InspectionTool.Loupe ? Supply.Loupe : tool == InspectionTool.Ultraviolet ? Supply.UVFlashlight : Supply.GoldReagent;
            if (House.State.Count(supply) == 0) return Say("Этот инструмент нужно купить утром.");
            if ((tool == InspectionTool.Loupe && item.inspected) || (tool == InspectionTool.Ultraviolet && item.tested) || (tool == InspectionTool.GoldReagent && item.goldTested)) return Say("Проверка уже проведена; результат сохранён.");
            if (Shop.State.actions < 1) return Say("Сил на проверку больше нет.");
            Shop.State.actions--;
            if (tool == InspectionTool.Loupe) { item.inspected = true; item.observedMark = State.chapter ? Observation(Evidence.Mark) : "Клеймо не установлено"; return Say($"Лупа: состояние {item.condition}%. Оценка — {ShopSimulation.Value(item)} ₽. Теперь можно аргументировать скидку."); }
            if (tool == InspectionTool.GoldReagent) { House.State.Consume(supply); item.goldTested = true; item.containsGold = item.template == 5; return Say(item.containsGold ? "Реактив подтверждает золото." : "Проба не подтвердила золото."); }
            item.tested = item.identified = true;
            if (item.curse != Curse.None && !Shop.State.discoveries.Contains(item.curse)) Shop.State.discoveries.Add(item.curse);
            return Say("УФ-проверка: " + CurseTitle(item.curse) + ". " + Recipe(item.curse));
        }
        public string Deal(int action, int amount = 0)
        {
            if (!Active(DayPhase.Noon) || !State.doorOpened || Shop.Customer == null || State.policePending) return Say("Сделка сейчас недоступна.");
            if (action < 0 || action > 3) return Say("Неизвестное действие.");
            var old = Shop.Customer;
            string result;
            if (action == 0) { result = Shop.Offer(amount); result = old.accepted ? $"«Согласен на {old.acceptedPrice} ₽». Подтвердите покупку." : Shop.Customer != old ? "Посетитель ушёл после трёх отказов." : $"Предложение не принято. Осталось попыток: {old.attempts}. Проверьте сумму и свободное место."; }
            else if (action == 1) { Shop.Buy(); result = Shop.Customer != old ? "Сделка завершена. Предмет в хранилище." : "Покупка невозможна: проверьте деньги и свободное место."; }
            else if (action == 2) { Shop.Reject(); result = "Посетитель уходит. Денег вы не потратили."; }
            else { if (State.chapter) return Say("Откройте журнал: выберите признак вещи и запись справочника, затем сопоставьте их."); Shop.UseArgument(); result = old.usedArgument ? $"«Ладно. Теперь прошу {old.ask} ₽»." : "Сначала найдите дефект инструментом. Согласованную цену изменить нельзя."; }
            if (Shop.Customer != old) { RecordPurchase(old); State.doorOpened = State.storyAsked = State.priceAsked = false; }
            return Say(result);
        }
        public string Barter(Supply supply)
        {
            var customer = Shop.Customer;
            if (!Active(DayPhase.Noon) || !State.doorOpened || customer == null || customer.accepted || State.policePending) return Say("Бартер сейчас недоступен.");
            if (supply != Supply.Crackers) return Say("Предложите деньги или символическую пачку сухариков.");
            if (State.chapter && (customer.evidenceMask == 0 || customer.minimum > 20)) return Say("«За сухари?! Сначала докажи, что вещь ничего не стоит». Нужны несколько убедительных сверок.");
            if (!State.chapter) return Say("Предложение пачки сухариков доступно в новой главе.");
            if (House.State.Count(supply) < 1 || FreeSlot() < 0) return Say("Нужна пачка сухариков и свободное место.");
            var item = customer.item; item.slot = FreeSlot(); item.cost = SupplyCatalog.Price(supply); item.acquiredNight = Shop.State.night; item.askingPrice = ShopSimulation.Value(item);
            item.barterPurchase = true; House.State.Add(supply, -1); Shop.State.stock.Add(item); RecordPurchase(customer); Shop.State.customerIndex++;
            ResetVisitor();
            return Say("«Вот до чего доторговались… Забирай». За вещь отдана одна пачка сухариков: символическая цена, а не просьба накормить.");
        }

        public string List(int id, int price, bool listed)
        {
            if (!Active(DayPhase.Evening)) return Say("Объявления размещаются вечером.");
            var item = Shop.State.stock.FirstOrDefault(x => x.id == id);
            if (item == null || price < 1 || price > 100000) return Say("Цена должна быть от 1 до 100000 ₽.");
            Shop.ListItem(id, price, listed);
            if (listed && item.stolen && !State.policeResolved) State.policePending = true;
            return Say(listed ? (item.stolen ? "Объявление опубликовано. На фото различим номер украшения. Теперь снятие объявления не отменит интерес полиции." : "Объявление опубликовано. Покупатели ответят к утру.") : "Объявление снято.");
        }
        public string AnswerPolice(int answer)
        {
            if (!Active(DayPhase.Noon) || !State.policePending || answer < 0 || answer > 1) return Say("Допрос сейчас не идёт.");
            if (answer == 1) State.contradictions++;
            State.policeQuestion++;
            if (State.policeQuestion < 3) return Say("Следователь сверяет ваши слова с распечаткой объявления.");
            Shop.State.stock.RemoveAll(x => x.stolen);
            var fine = Math.Min(Shop.State.cash, State.contradictions == 0 ? 20 : 65);
            Shop.State.cash -= fine; Shop.State.report.supplies += fine;
            State.policePending = false; State.policeResolved = true;
            if (State.chapter) { State.ending = ChapterEnding.Interrogation; return Say(EndingText); }
            return Say(State.contradictions == 0 ? $"Показания записаны. Украшение изъято, расходы по делу: {fine} ₽. Вы остались свидетелем." : $"В показаниях нашли противоречия. Украшение изъято, взыскано {fine} ₽. Дело закрыто, но вам больше не доверяют.");
        }
        public string Ritual(int id, int step)
        {
            var item = Shop.State.stock.FirstOrDefault(x => x.id == id);
            if (!Active(DayPhase.Night) || item == null) return Say("Ритуалы проводятся ночью над своими вещами.");
            if (State.ritualId != 0 && !Shop.State.stock.Any(x => x.id == State.ritualId))
            { State.ritualId = State.saltPoints = 0; State.milk = State.boiled = false; }
            if (State.ritualId != id)
            {
                if (State.saltPoints > 0 || State.milk) return Say("Закончите текущий ритуал или уберите круг (расходники не возвращаются).");
                State.ritualId = id;
            }
            if (step == 0) { State.saltPoints = 0; State.milk = State.boiled = false; State.ritualId = 0; return Say("Стол очищен. Использованные расходники потеряны."); }
            if (item.curse == Curse.None) return Say("Этот предмет не нуждается в очищении.");
            if (step == 1)
            {
                if (State.saltPoints >= 5) return Say("Пентаграмма замкнута.");
                if (State.saltPoints == 0 && !House.State.Consume(Supply.Salt)) return Say("Нет соли. Можно отложить ритуал до следующего дня.");
                State.saltPoints++; return Say($"Соль ложится по линии звезды. Узлов замкнуто: {State.saltPoints}/5.");
            }
            if (step == 2)
            {
                if (State.saltPoints < 5 || State.milk) return Say("Нужна завершённая звезда и пустая чаша.");
                if (!House.State.Consume(Supply.Milk)) return Say("Нет молока.");
                State.milk = true; return Say("Предмет погружается в молоко. На поверхности появляются буквы.");
            }
            if (step == 3)
            {
                if (!State.milk) return Say("Сначала налейте молоко внутрь замкнутой звезды.");
                State.boiled = true; return Say("Молоко закипает. Чужой голос захлёбывается в пузырьках.");
            }
            if (step != 4 || State.saltPoints < 5 || (item.curse == Curse.Cold && !State.boiled) || (item.curse == Curse.Hunger && !State.milk)) return Say("Ритуал ещё не готов. Проверьте рецепт.");
            if (Shop.State.actions < 1) return Say("Не осталось сил. Продолжите завтра.");
            Shop.State.actions--; item.cleansed = true; item.curse = Curse.None; item.identified = item.tested = true;
            State.saltPoints = 0; State.milk = State.boiled = false; State.ritualId = 0;
            return Say("Тишина. Символы распадаются в пепел. Проклятие снято.");
        }
        public string Close()
        {
            if (!Active(DayPhase.Night)) return Say("День ещё не закончен.");
            Shop.CloseNight();
            if (!Shop.State.stock.Any(x => x.id == State.ritualId)) { State.ritualId = State.saltPoints = 0; State.milk = State.boiled = false; }
            return Say("Рассвет. Продажи, влияние оставшихся проклятий и расходы записаны в книгу.");
        }
        public string NextDay()
        {
            if (State.ending != ChapterEnding.None || (State.chapter && Shop.State.night >= 3)) return Say("Три дня завершены. Выберите исход главы.");
            if (Shop.State.phase != NightPhase.Results) return Say("Сначала оплатите расходы.");
            Shop.NextNight(); State.phase = DayPhase.Morning; State.shopTalked = State.doorOpened = State.storyAsked = State.priceAsked = false;
            PrepareChapterDay();
            return Say("Новое утро. Нина уже открыла магазин.");
        }
        public static string CurseTitle(Curse curse) => new[] { "Нет проклятия", "Мёртвый холод", "Шёпот", "Голод" }[(int)curse];
        public static string Recipe(Curse curse) => curse == Curse.Cold ? "Пять узлов соли → молоко → кипячение → завершить." : curse == Curse.Hunger ? "Пять узлов соли → молоко → завершить." : curse == Curse.Whisper ? "Пять узлов соли → завершить в тишине." : "Ритуал не нужен.";
    }
}
