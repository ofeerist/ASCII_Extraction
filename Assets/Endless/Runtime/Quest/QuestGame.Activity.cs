using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace Pawnshop.Endless
{
    public sealed partial class QuestRules
    {
        public static string ItemDetails(ItemState item) =>
            (item.barterPurchase?"Цена сделки: 1 пачка сухариков (4 ₽)":$"Цена сделки: {item.cost} ₽")+
            (item.inspected?$"\nСостояние: {item.condition}% · износ {100-item.condition}%":"\nИзнос: не проверен")+
            "\n"+(string.IsNullOrEmpty(item.observedMark)?"Клеймо: не прочитано":item.observedMark)+
            "\nПроклятие: "+(item.cleansed?"снято ритуалом":item.identified?CurseTitle(item.curse):"не проверено")+
            ((item.observedMark??"").Contains("Ю-041")?"\nПроисхождение: номер из сводки о краже":"")+
            "\nПроба: "+(!item.goldTested?"не проведена":item.containsGold?"золото подтверждено":"золото не подтверждено");
        public string RitualHint(int id)
        {
            var item=Shop.State.stock.FirstOrDefault(x=>x.id==id);
            if(item==null)return "Выберите вещь рядом с кругом.";
            if(item.curse==Curse.None)return "Эта вещь чистая. Выберите другую или завершите ночь.";
            if(State.ritualId!=0 && State.ritualId!=id && State.saltPoints>0)return "Завершите начатый ритуал или уберите соль.";
            if(State.saltPoints<5)return State.saltPoints==0&&House.State.Count(Supply.Salt)==0?"Нет соли. Купите её утром или отложите ритуал.":$"Шаг 1 · Соль: ведите ЛКМ от яркой точки по светящейся линии. {State.saltPoints}/5 линий.";
            if(item.curse!=Curse.Whisper&&!State.milk)return House.State.Count(Supply.Milk)==0?"Нет молока. Ритуал можно продолжить завтра.":"Шаг 2 · Нажмите на бутылку молока слева от круга.";
            if(item.curse==Curse.Cold&&!State.boiled)return "Шаг 3 · Нажмите на плитку справа — прогрейте чашу.";
            return Shop.State.actions<1?"Не осталось сил. Продолжите завтра.":"Последний шаг · Нажмите на вещь в центре. Завершите очищение.";
        }
        public bool RitualReady(int id)
        {
            var item=Shop.State.stock.FirstOrDefault(x=>x.id==id);
            return item!=null&&item.curse!=Curse.None&&State.saltPoints==5&&Shop.State.actions>0&&
                (item.curse==Curse.Whisper||State.milk)&&(item.curse!=Curse.Cold||State.boiled);
        }
    }
    public sealed partial class QuestGame
    {
        Label activityProgress;
        void DrawActivity()
        {
            activityProgress=null;
            if(world==null)return;
            if(world.Inspecting)
            {
                var panel=Overlay(root);panel.style.left=20;panel.style.top=68;panel.style.width=310;
                Text(panel,"ПРОВЕРКА ПРЕДМЕТА",20);
                Text(panel,"Выберите инструмент. Ведите его по отмеченным участкам, удерживая ЛКМ. ПКМ — повернуть вещь.",14);
                foreach(var tool in new[]{InspectionTool.Loupe,InspectionTool.Ultraviolet,InspectionTool.GoldReagent})
                {
                    var t=tool;string title=t==InspectionTool.Loupe?"Лупа":t==InspectionTool.Ultraviolet?"УФ-фонарь":"Проба золота";
                    var current=Quest.Shop.Customer?.item;
                    bool done=current!=null&&(t==InspectionTool.Loupe?current.inspected:t==InspectionTool.Ultraviolet?current.tested:current.goldTested);
                    Button(panel,(done?"✓ ":world.ActiveTool==t?"► ":"")+title,()=>world.ChooseTool(t),!done);
                }
                activityProgress=Text(panel,world.ActivityProgress,16,"#e9c58b");
                Button(panel,"Результаты / журнал",()=>{journalOpen=true;Draw();});
                Button(panel,"Вернуть вещь посетителю",()=>world.SetInspection(false));
            }
            else if(Quest.State.phase==DayPhase.Night)
            {
                var panel=Overlay(root);panel.style.left=20;panel.style.top=68;panel.style.width=300;
                var item=Quest.Shop.State.stock.FirstOrDefault(x=>x.id==selected);
                Text(panel,"ОЧИЩЕНИЕ",21);Text(panel,item==null?"Нет выбранной вещи":Names[item.template],17);
                Text(panel,Quest.RitualHint(selected),18,"#e9c58b");
                Text(panel,"Яркая обводка — следующий шаг. Серая — пока недоступно. Один пакет соли на весь рисунок.",13);
                if(item!=null)Text(panel,"Рецепт: "+QuestRules.Recipe(item.curse),13);
                activityProgress=Text(panel,world.ActivityProgress,14);
                if(Quest.Shop.State.stock.Count>1)
                {
                    Button(panel,"Выбрать следующую вещь",()=>
                    {
                        var stock=Quest.Shop.State.stock;int index=stock.FindIndex(x=>x.id==selected);
                        SelectWorldItem(stock[(index+1)%stock.Count].id,false);
                    },Quest.State.saltPoints==0);
                }
            }
            else if(Quest.State.phase==DayPhase.Evening)
            {
                var panel=Overlay(root);panel.style.left=20;panel.style.top=68;panel.style.width=280;
                Text(panel,"ВИТРИНА",22);Text(panel,"Нажмите на любой товар на полках. В карточке — цена покупки, результаты проверок и объявление.",15);
                Text(panel,$"На полках: {Quest.Shop.State.stock.Count} · В продаже: {Quest.Shop.State.stock.Count(x=>x.listed)}",14);
            }
        }
    }
}
