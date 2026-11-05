using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Pawnshop.Endless
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class QuestGame : MonoBehaviour
    {
        public QuestRules Quest { get; private set; }
        RunStorage storage;
        PlayerProfile profile;
        VisualElement root;
        Label hoverLabel;
        QuestWorld world;
        bool conversation;
        bool journalOpen;

        public bool IsMenu => menu;
        public bool AnimateWorld => effects;
        public int SelectedItem => selected;
        public void CloseConversation() { conversation = false; journalOpen=false; }
        public void RedrawWorld() => Draw();
        public void Converse(bool boris) { street = boris; conversation = true; Draw(); }
        public void SelectWorldItem(int id, bool sale)
        {
            if(Quest.State.phase==DayPhase.Night && Quest.State.saltPoints>0 && Quest.State.ritualId!=id)
            { Quest.Say("Сначала завершите текущий ритуал или нажмите «Убрать соль»."); Draw(); return; }
            selected = id; conversation = sale; Draw();
        }
        public void WorldAct(Func<string> action) { conversation = false; Act(action); }
        public bool BlocksWorldPointer(Vector2 screenPoint)
        {
            if(menu || Quest?.State.ending != ChapterEnding.None || root?.panel == null) return true;
            var point = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPoint.x, Screen.height-screenPoint.y));
            for(var element = root.panel.Pick(point); element != null; element = element.parent)
                if(element.ClassListContains("quest-overlay")) return true;
            return false;
        }
        bool menu = true;
        bool street;
        bool replace;
        bool effects = true;
        int selected;
        int offer;
        
        string saveError;
        static readonly string[] Phases = { "УТРО / ЛАВКА НИНЫ", "ОБЕД / СТУК В ДВЕРЬ", "ВЕЧЕР / ОБЪЯВЛЕНИЯ", "НОЧЬ / РИТУАЛЫ" };
        static readonly string[] Names = { "Карманные часы", "Камея", "Молитвенник", "Серебряная монета", "Компас", "Обручальное кольцо", "Музыкальная шкатулка", "Атлас растений", "Театральный бинокль", "Бронзовая медаль" };
        static readonly string[] Supplies = { "Лупа", "УФ-фонарь", "Реактив на золото", "Молоко", "Соль", "Чекушка", "Сухари" };
        static readonly string[] Visitors = { "Анна / последняя электричка", "Аркадий / перекупщик", "Лев / коллекционер", "Женщина в вуали" };
        public void Initialize(Font font, StyleSheet theme, string saveDirectory = null)
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (theme != null) root.styleSheets.Add(theme);
            if (font != null) root.style.unityFontDefinition = FontDefinition.FromFont(font);
            storage = new RunStorage(Path.Combine(string.IsNullOrEmpty(saveDirectory) ? Application.persistentDataPath : saveDirectory, "quest"));
            profile = storage.LoadProfile(); effects = profile.effects;
            var saved = storage.LoadRun(); if (saved != null) Quest = new QuestRules(new ShopSimulation(saved));
            saveError = storage.Notice;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var source = GetComponent<PhysicalShop>();
            if (source != null)
            {
                world = gameObject.AddComponent<QuestWorld>(); world.Initialize(this, source, font);
            }
            Draw();
        }
        public void StartRun(int seed)
        {
            Quest = new QuestRules(ShopSimulation.NewRun(seed)); Quest.StartChapter(); journalOpen = false; menu = false; street = false; replace = false; selected = 0;
            Save(); Draw();
        }
        void Save()
        {
            if (storage == null || Quest == null) return;
            try { storage.SaveRun(Quest.Shop.State); RunStorage.UpdateRecords(profile, Quest.Shop.State, Quest.Shop.Capital); profile.effects = effects; storage.SaveProfile(profile); saveError = null; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { saveError = "Не удалось сохранить: " + e.Message; }
        }
        void OnApplicationQuit() => Save();
        void OnDisable() => Save();
        void Act(Func<string> action)
        {
            var visitor = Quest?.Shop.Customer?.item.id ?? 0;
            action();
            if ((Quest?.Shop.Customer?.item.id ?? 0) != visitor) conversation = false;
            Save(); Draw();
        }
        static Label Text(VisualElement parent, string value, int size = 16, string color = "#d4cbb7")
        {
            var label = new Label(value); label.style.whiteSpace = WhiteSpace.Normal; label.style.fontSize = size; ColorUtility.TryParseHtmlString(color, out var c); label.style.color = c; label.style.marginBottom = 12; parent.Add(label); return label;
        }
        static Button Button(VisualElement parent, string value, Action action, bool enabled = true)
        {
            var button = new Button(action) { text = value }; button.SetEnabled(enabled); button.style.whiteSpace = WhiteSpace.Normal; button.style.minHeight = 38; button.style.marginBottom = 7; parent.Add(button); return button;
        }
        void Draw()
        {
            if (root == null) return;
            dialogueAnswers.Clear();
            root.Clear(); root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1; root.style.backgroundColor = Color.clear;
            root.style.paddingLeft = 0; root.style.paddingRight = 0; root.style.paddingTop = 0; root.style.paddingBottom = 0;
            if (Quest != null && (selected == 0 || !Quest.Shop.State.stock.Any(x => x.id == selected)))
                selected = Quest.State.ritualId != 0 ? Quest.State.ritualId : Quest.Shop.State.stock.FirstOrDefault()?.id ?? 0;
            world?.Refresh();
            if(menu)
            {
                var panel = Overlay(root); panel.style.left = Length.Percent(22); panel.style.right = Length.Percent(22); panel.style.top = Length.Percent(15); panel.style.bottom = Length.Percent(15);
                var content = new ScrollView(); panel.Add(content); DrawMenu(content); return;
            }
            if (Quest.State.ending != ChapterEnding.None)
            {
                var ending = Overlay(root); ending.style.left = Length.Percent(20); ending.style.right = Length.Percent(20); ending.style.top = Length.Percent(20);
                Text(ending, Quest.EndingText, 24); Button(ending, "В главное меню", () => { menu = true; Draw(); }); return;
            }
            world?.DrawLabels(root);
            var s = Quest.Shop.State;
            var header = new VisualElement(); header.pickingMode = PickingMode.Ignore; header.style.position = Position.Absolute;
            header.style.left = 20; header.style.top = 14; header.style.right = 20; header.style.flexDirection = FlexDirection.Row; root.Add(header);
            var status = Text(header, $"ДЕНЬ {s.night:00} · {Phases[(int)Quest.State.phase]} · {s.cash} ₽ · СИЛЫ {s.actions}", 14);
            status.pickingMode = PickingMode.Ignore; status.style.flexGrow = 1;
            var journalButton = Button(header,"Журнал / сверка",()=>{journalOpen=!journalOpen;Draw();}); journalButton.AddToClassList("quest-overlay");
            var menuButton = Button(header,"Меню",()=>{menu=true;Draw();}); menuButton.AddToClassList("quest-overlay");
            if(conversation || s.phase != NightPhase.Reception)
            {
                var panel = Overlay(root); panel.style.left = Length.Percent(s.phase==NightPhase.Reception && Quest.State.phase==DayPhase.Evening ? 68 : 51); panel.style.right = 20; panel.style.top = 68; panel.style.bottom = 110;
                var scroll = new ScrollView(); scroll.style.flexGrow = 1; scroll.style.minHeight = 0; panel.Add(scroll);
                if(s.phase != NightPhase.Reception) DrawResults(scroll);
                else if(Quest.State.phase == DayPhase.Morning) { if(street) DrawStreet(scroll); else DrawMorning(scroll); }
                else if(Quest.State.phase == DayPhase.Noon) DrawNoon(scroll);
                else if(Quest.State.phase == DayPhase.Evening) DrawEvening(scroll);
                if(s.phase == NightPhase.Reception) Button(panel,"Закончить разговор / убрать записи",()=>{conversation=false;Draw();});
            }
            if(journalOpen) DrawJournal();
            if(!conversation && !journalOpen && s.phase==NightPhase.Reception) DrawActivity();
            var footer = new VisualElement(); footer.pickingMode = PickingMode.Ignore; footer.style.position = Position.Absolute;
            footer.style.left = 20; footer.style.right = 20; footer.style.bottom = 12;
            footer.style.backgroundColor = new Color(.025f,.035f,.03f,.78f);footer.style.paddingLeft=14;footer.style.paddingTop=8;root.Add(footer);
            hoverLabel = Text(footer,"Наведите мышь на предмет или человека. ЛКМ — взаимодействовать.",14,"#91b5a9");hoverLabel.pickingMode=PickingMode.Ignore;
            var message = Text(footer,Quest.State.message,16,"#e9c58b");message.pickingMode=PickingMode.Ignore;
            if(!string.IsNullOrEmpty(saveError))Text(footer,saveError,13,"#f29b85").pickingMode=PickingMode.Ignore;
        }
        static VisualElement Overlay(VisualElement parent)
        {
            var panel = new VisualElement(); panel.AddToClassList("quest-overlay"); panel.style.position=Position.Absolute;
            panel.style.backgroundColor=new Color(.035f,.045f,.04f,.94f);panel.style.paddingLeft=18;panel.style.paddingRight=18;panel.style.paddingTop=18;panel.style.paddingBottom=14;parent.Add(panel);return panel;
        }
        void DrawMenu(VisualElement parent)
        {
            Text(parent, "ГЛАВА I · ЧУЖИЕ ИМЕНА", 34);
            Text(parent,"Три дня. Девять визитов. Три исхода. Сверяйте факты, торгуйтесь и решайте, какие вещи оставить до рассвета.");
            Text(parent, "Утром — знакомые за прилавком. Днём — чужие у двери.\nВечером — объявления. Ночью — то, что осталось внутри вещей.", 20);
            Text(parent, "Нажимайте на людей и предметы в 3D-сцене. Камера меняет ракурсы без WASD. Время движется только по вашему выбору. Сохранение после каждого действия.");
            if (Quest != null) Button(parent, "Продолжить день " + Quest.Shop.State.night, () => { menu = false; Draw(); });
            Button(parent, replace ? "Подтвердить новую игру — заменить сохранение" : "Новая история", () => { if (Quest != null && !replace) { replace = true; Draw(); } else StartRun(12345); });
            if (replace) Button(parent, "Отмена", () => { replace = false; Draw(); });
            var toggle = new Toggle("Анимация ASCII") { value = effects }; toggle.RegisterValueChangedCallback(e => { effects = e.newValue; Save(); }); parent.Add(toggle);
            if (!string.IsNullOrEmpty(saveError)) Text(parent, saveError);
        }
        void DrawMorning(VisualElement parent)
        {
            Text(parent,"Нина / продавщица",24);
            Text(parent,"«Всё на прилавке. Чужим словам не верь: сверяй клейма с журналом. Лупа и фонарь у тебя уже есть». ");
            Button(parent,"Как снимать проклятия?",()=>Act(()=>Quest.Say("Нина: «Шёпот замкни пятью линиями соли. Голод напои молоком внутри звезды. Холодную вещь прокипяти. Нажимай на молоко, плитку и сам предмет; соль веди рукой по полу».")));
            Button(parent,"Что у тебя за инструменты?",()=>Act(()=>Quest.Say("Нина: «Лупа покажет износ, фонарь — проклятие, реактив — золото. Лупа и фонарь навсегда, остальное расходуется. Выбирай прямо на прилавке».")));
        }
        void DrawStreet(VisualElement parent)
        {
            Text(parent, "Борис / у задней стены", 26);
            Text(parent, "Можно просто уйти: участие в его затее не обязательно.", 13);
            var q = Quest.State;
            if (q.streetQuestion < 3)
            {
                Text(parent, new[] { "«Как тебя звать-то? Хотя важнее: чего тебе от меня надо?»", "«Если человеку некуда идти, он уже никто?»", "«А слово своё держишь, хозяин?»" }[q.streetQuestion], 20);
                var answers = new[] { new[] { "Есть работа для таких, как ты.", "Просто поговорить. Я тебя слушаю." }, new[] { "Он всё ещё человек.", "Сам виноват." }, new[] { "Смотря сколько заплатят.", "Если обещал — держу." } }[q.streetQuestion];
                for (int i = 0; i < 2; i++) { int a = i; Button(parent, answers[i], () => Act(() => Quest.AnswerStreet(a))); }
            }
            else
            {
                Text(parent, q.trust >= 2 ? "Он вам доверяет. За чекушку предлагает принести украшение из ювелирки. Это краденое: публикация объявления приведёт к допросу." : "Борис не хочет продолжать разговор. Можно вернуться к обычной работе.");
                if (q.trust >= 2) Button(parent, q.robberyPromised ? "Договор заключён" : "Отдать чекушку и согласиться", () => Act(Quest.PromiseRobbery), !q.robberyPromised && Quest.House.State.Count(Supply.Vodka) > 0);
                if (q.robberyPromised) Text(parent, q.robberyDelivered ? "Кольцо передано в хранилище." : "Борис придёт завтра в обед. Оставьте место в хранилище.");
            }
        }
        void DrawNoon(VisualElement parent)
        {
            if (Quest.State.policePending)
            {
                DrawSpeaker(parent, "Следователь");
                Text(parent, new[] { "«Откуда у вас кольцо с витрины ограбленного магазина?»", "«Вы знали, что оно краденое?»", "«Зачем вы приняли вещь без документов?»" }[Quest.State.policeQuestion], 20);
                var truth = new[] { "Его принёс Борис. Сделка есть в моих записях.", "Да. Он сказал, откуда оно.", "Хотел заработать. Покажу записи." };
                var lie = new[] { "Нашёл у двери.", "Первый раз слышу.", "В моей книге такой сделки нет." };
                Answer(parent, truth[Quest.State.policeQuestion], () => Act(() => Quest.AnswerPolice(0)));
                Answer(parent, lie[Quest.State.policeQuestion], () => Act(() => Quest.AnswerPolice(1))); return;
            }
            var c = Quest.Shop.Customer;
            if (c == null) { Text(parent,"На сегодня посетителей больше нет. Нажмите на табличку «К вечеру» на столе.");return; }
            Text(parent, $"Посетитель {Quest.Shop.State.customerIndex + 1} из {Quest.Shop.State.customers.Count}", 14);
            if (!Quest.State.doorOpened) { Text(parent,"Сначала откройте дверь в сцене.");return; }
            DrawSpeaker(parent, Quest.State.chapter ? Quest.VisitorName : Visitors[(int)c.kind]); Text(parent, Names[c.item.template], 20);
            if(Quest.State.chapter) DialogueLine(parent, Quest.State.storyAsked ? Quest.State.message : "«Можно войти? У меня кое-что для вас».");
            Answer(parent, "Что это за вещь? Откуда она?", () => Act(Quest.AskStory));
            Answer(parent, "Сколько вы хотите?", () => Act(() => { Quest.State.priceAsked = true; return Quest.Say($"«За {c.ask} ₽ отдам». Попыток торга: {c.attempts}."); }));
            if (Quest.State.chapter && c.story == "dealer" && Quest.Shop.State.night == 3)
                Button(parent,"Продать Аркадию накладную за 100 ₽",()=>Act(Quest.SellRecords),!Quest.State.recordsSold);

            Text(parent, c.item.identified ? QuestRules.CurseTitle(c.item.curse) : c.item.suspicious ? "Неприятное ощущение от вещи. Стоит проверить." : "На первый взгляд — обычная вещь.", 14, "#b09ac7");
            Text(parent,"Лупа и УФ-фонарь лежат на столе. Пробел — показать реплику целиком.",13);
            if (c.item.inspected) Text(parent, $"Состояние {c.item.condition}% · оценка {ShopSimulation.Value(c.item)} ₽", 14);
            if (Quest.State.priceAsked)
            {
                var field = new IntegerField("Предложить ₽") { value = offer }; field.RegisterValueChangedCallback(e => offer = e.newValue); parent.Add(field);
                Answer(parent, "Предложить свою цену", () => Act(() => Quest.Deal(0, offer)), !c.accepted);
                Answer(parent, "Сверить с журналом — обосновать скидку", () => { journalOpen=true; Draw(); }, !c.accepted);
                Answer(parent, $"Купить за {(c.accepted ? c.acceptedPrice : c.ask)} ₽", () => Act(() => Quest.Deal(1)));
            }
            Answer(parent, "За такую вещь могу дать лишь пачку сухариков", () => Act(() => Quest.Barter(Supply.Crackers)), !c.accepted);
            Answer(parent, "Осмотреть вещь на столе", () => { world?.SetInspection(true); });
            Answer(parent, "Отказаться и закрыть дверь", () => Act(() => Quest.Deal(2)));
        }
        void DrawEvening(VisualElement parent)
        {
            Text(parent, "Доска объявлений", 26);
            Text(parent, "Продажи завершатся к утру. Высокая цена снижает спрос; непроданные вещи останутся у вас.");
            foreach (var item in Quest.Shop.State.stock.Where(x => x.id == selected).ToArray())
            {
                Text(parent, Names[item.template], 20);
                Text(parent, QuestRules.ItemDetails(item), 16);
                int price = item.askingPrice;
                var field = new IntegerField("Цена ₽") { value = price }; field.RegisterValueChangedCallback(e => price = e.newValue); parent.Add(field);
                Text(parent, item.listed ? $"Опубликовано за {item.askingPrice} ₽ · шанс продажи {Quest.Shop.SaleChance(item)}%" : "Не выставлено", 13);
                Button(parent, item.listed ? "Обновить объявление" : "Разместить объявление", () => Act(() => Quest.List(item.id, price, true)));
                if (item.listed) Button(parent, "Снять с продажи", () => Act(() => Quest.List(item.id, price, false)));
            }
            if (Quest.Shop.State.stock.Count == 0) Text(parent, "Пока нечего продавать.");
            Text(parent,"Все товары стоят на полках. Нажмите на вещь, чтобы открыть её карточку.",13);
        }
        void DrawResults(VisualElement parent)
        {
            var s = Quest.Shop.State; var r = s.report;
            Text(parent, "Рассвет / книга учёта", 26);
            Text(parent, $"Продано: {r.soldCount}\nВыручка: {r.sales} ₽\nПотеря от проклятий: {r.curseLoss} ₽\nПовреждение вещей: {r.damage} ₽\nОплаченные расходы: {r.expenses} ₽\nОстаток: {s.cash} ₽");
            if (s.phase == NightPhase.Results)
            {
                if (Quest.State.chapter && s.night >= 3)
                {
                    Text(parent,"Третий рассвет. Вы решаете, с чем войти в следующую главу.");
                    Button(parent,"Сохранить журнал и отказаться от голоса",()=>Act(()=>Quest.FinishChapter(false)));
                    Button(parent,"Назвать голосу своё имя — заключить договор",()=>Act(()=>Quest.FinishChapter(true)),Quest.CanAgree);
                    if(!Quest.CanAgree) Text(parent,"Договор требует сохранённой неочищенной вещи с Шёпотом и истории Анны.");
                }
                else Button(parent, "Следующее утро", () => Act(Quest.NextDay));
            }
            else if (s.phase == NightPhase.Settlement)
            {
                Text(parent, $"Для оплаты дня нужно {s.due} ₽. Можно срочно продать оставшиеся вещи.");
                foreach (var item in s.stock.ToArray()) Button(parent, $"Продать {Names[item.template]} за {ShopSimulation.LiquidationValue(item)} ₽", () => Act(() => { Quest.Shop.Liquidate(item.id); return Quest.Say("Предмет продан скупщику."); }));
                Button(parent, "Оплатить расходы", () => Act(() => { Quest.Shop.PayExpenses(); return Quest.Say("Расчёт по книге обновлён."); }), s.cash >= s.due);
                Button(parent, "Закрыть ломбард — завершить историю", () => { replace = true; DrawBankruptcy(parent); });
            }
            else { Text(parent, "Ломбард закрыт. История окончена."); Button(parent, "В главное меню", () => { menu = true; replace = false; Draw(); }); }
        }
        void DrawBankruptcy(VisualElement parent)
        {
            parent.Clear(); Text(parent, "Подтвердить закрытие ломбарда? Текущую историю нельзя будет продолжить.");
            Button(parent, "Да, завершить историю", () => Act(() => { replace = false; Quest.Shop.ConfirmBankruptcy(); return Quest.Say("Ключ остался под ковриком."); })); Button(parent, "Вернуться к расчётам", Draw);
        }
        void Update()
        {
            UpdateDialogue();
            if(activityProgress!=null && world!=null) activityProgress.text=world.ActivityProgress;
            if(!menu && hoverLabel!=null && world!=null)
                hoverLabel.text=string.IsNullOrEmpty(world.Hover)?"Наведите мышь на предмет или человека. ЛКМ — взаимодействовать.":world.Hover;
        }
    }
}
