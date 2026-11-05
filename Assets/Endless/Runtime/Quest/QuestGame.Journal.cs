using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace Pawnshop.Endless
{
    public sealed partial class QuestGame
    {
        readonly System.Collections.Generic.List<(Button button, Action action)> dialogueAnswers = new System.Collections.Generic.List<(Button, Action)>();
        Button Answer(VisualElement parent, string value, Action action, bool enabled=true)
        {
            var button=Button(parent, $"{dialogueAnswers.Count+1}. {value}", action, enabled);
            button.style.unityTextAlign=TextAnchor.MiddleLeft;
            dialogueAnswers.Add((button,action)); return button;
        }
        Label printingLabel;
        string printingText;
        float printStarted;
        void DialogueLine(VisualElement parent, string line)
        {
            printingLabel = Text(parent, "", 19, "#efddbe"); printingText = line; printStarted = Time.unscaledTime;
            printingLabel.RegisterCallback<ClickEvent>(_ => { printStarted = -10000; UpdateDialogue(); });
        }
        void UpdateDialogue()
        {
            var keyboard=Keyboard.current;
            var focused=root?.focusController?.focusedElement as VisualElement;
            bool enteringPrice=focused is IntegerField || focused?.GetFirstAncestorOfType<IntegerField>()!=null;
            if(!menu && !journalOpen && keyboard!=null && !enteringPrice)
                for(int i=0;i<dialogueAnswers.Count && i<9;i++)
                    if(keyboard[(Key)((int)Key.Digit1+i)].wasPressedThisFrame && dialogueAnswers[i].button.enabledInHierarchy) { dialogueAnswers[i].action(); return; }
            if (printingLabel?.panel == null || printingText == null) return;
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) printStarted = -10000;
            int count = Mathf.Min(printingText.Length, Mathf.FloorToInt((Time.unscaledTime - printStarted) * 45));
            printingLabel.text = printingText.Substring(0,Mathf.Max(0,count));
        }
        void DrawSpeaker(VisualElement parent, string name)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; parent.Add(row);
            var key = Quest.State.policePending ? "Police" : Quest.Shop.Customer?.story == "boris" ? "Boris" : Quest.Shop.Customer?.story == "dealer" ? "Dealer" : "Anna";
            var portrait = new Image { image = Resources.Load<Texture2D>("ChapterPortraits/" + key), scaleMode = ScaleMode.ScaleToFit };
            portrait.style.width = 44; portrait.style.height = 58; portrait.style.marginRight = 16; row.Add(portrait); Text(row, name, 23, "#c9a365");
        }
        void DrawJournal()
        {
            var panel=Overlay(root); panel.style.left=20; panel.style.right=Length.Percent(51); panel.style.top=68; panel.style.bottom=110;
            panel.style.backgroundColor=new Color(.12f,.115f,.09f,.98f);
            var scroll=new ScrollView(); scroll.style.flexGrow=1; panel.Add(scroll);
            Text(scroll,"ЖУРНАЛ ПРИЁМЩИКА",23,"#e9c58b");
            Text(scroll,"Осмотрите вещь инструментами. Найденные факты появятся здесь автоматически.",14);
            var customer=Quest.Shop.Customer;
            if(customer!=null && Quest.Active(DayPhase.Noon))
            {
                Text(scroll,Names[customer.item.template],20);
                string[] titles={"СОСТОЯНИЕ","КЛЕЙМО","ПРОКЛЯТИЕ"};
                string[] rules={"Износ снижает цену исправной вещи.","Серия П / Б-17 — реквизит. Ю-041 — украденное кольцо.","Зелёные буквы и чужой голос — Шёпот."};
                for(int i=0;i<3;i++)
                {
                    int index=i;bool known=i==2?customer.item.tested:customer.item.inspected;
                    bool valid=i==0?customer.item.condition<70:i==1?customer.story=="boris"||customer.story=="dealer":customer.item.curse==Curse.Whisper;
                    bool used=(customer.evidenceMask&(1<<i))!=0;
                    Text(scroll,titles[i]+" · "+(known?Quest.Observation((Evidence)i):"ещё не проверено"),16);
                    if(known)Text(scroll,rules[i],13);
                    Button(scroll,!known?"Проверить на столе":used?"✓ Учтено в цене":valid?"Указать на несоответствие → снизить цену":"✓ Несоответствий нет",()=>
                    {
                        if(!known){journalOpen=false;world?.SetInspection(true);world?.ChooseTool(index==2?InspectionTool.Ultraviolet:InspectionTool.Loupe);}
                        else Act(()=>Quest.Compare(index,(Evidence)index));
                    },!used&&(!known||valid)&&!customer.accepted);
                }
            }
            else Text(scroll,"Выберите вещь у посетителя. Результаты проверок сохраняются в её карточке.",16);
            Text(scroll,"ЗАПИСИ И СВОДКИ",16);
            Text(scroll,Quest.Shop.State.night>=2?"Сводка: из ювелирного украдено кольцо Ю-041. Не принимайте вещи с этим номером.":"День 1. Серия Б-17 связана с реквизитом из квартиры 17. Проверяйте возраст и материал.",14);
            foreach(var note in Quest.Shop.State.journal) Text(scroll,note,13);
            Button(panel,"Закрыть журнал",()=>{journalOpen=false;Draw();});
        }

    }
}
