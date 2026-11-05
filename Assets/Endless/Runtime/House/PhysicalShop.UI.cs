using System;
using System.Linq;
using Pawnshop.Dialogue;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pawnshop.Endless
{
    public sealed partial class PhysicalShop
    {
        VisualElement Box(VisualElement parent,string classes)
        {
            var element=new VisualElement();foreach(var c in classes.Split(' '))element.AddToClassList(c);parent.Add(element);return element;
        }
        Label Text(VisualElement parent,string value,string classes="body")
        {
            var label=new Label(value);foreach(var c in classes.Split(' '))label.AddToClassList(c);parent.Add(label);return label;
        }
        Button Button(VisualElement parent,string value,Action action,bool enabled=true)
        {
            var button=new Button(action){text=value};button.SetEnabled(enabled);parent.Add(button);return button;
        }
        void Number(VisualElement parent,string label,int value,Action<int> changed)
        {
            var field=new IntegerField(label){value=value};field.RegisterValueChangedCallback(e=>changed(e.newValue));parent.Add(field);
        }
        void Confirm(string text,Action action){confirmText=text;confirmation=action;Draw();}
        void Draw()
        {
            if(root==null)return;
            root.Clear();root.AddToClassList("house-screen");root.pickingMode=PickingMode.Ignore;
            var hud=Box(root,"house-hud");hud.pickingMode=PickingMode.Ignore;
            statusLabel=Text(hud,Game==null?"PAWNSHOP OF THE DAMNED":$"NIGHT {Game.State.night:000}   CASH {Game.State.cash}   DUE {Game.State.due}   ACTIONS {Game.State.actions}","metric");
            statusLabel.pickingMode=PickingMode.Ignore;
            messageLabel=Text(hud,message,"muted");messageLabel.pickingMode=PickingMode.Ignore;
            if(mode=="Explore")
            {
                var cross=Text(root,"+","house-crosshair");cross.pickingMode=PickingMode.Ignore;
                var bottom=Box(root,"house-hint");bottom.pickingMode=PickingMode.Ignore;promptLabel=Text(bottom,"","lead");promptLabel.pickingMode=PickingMode.Ignore;
                var carried=Rules.Find(Rules.State.carriedId);
                Text(bottom,carried==null?"WASD move · E interact · Escape pause":$"CARRYING: {Game.Title(carried)} · E place on desk, shelf or rug","muted").pickingMode=PickingMode.Ignore;
            }
            else if(mode=="Inspect"||mode=="Ritual")DrawFocusControls();
            else
            {
                var shell=Box(root,mode=="Door"?"house-panel house-door-panel":"house-panel");
                var scroll=new ScrollView(ScrollViewMode.Vertical);shell.Add(scroll);
                if(mode=="Menu")DrawMenu(scroll);
                else if(mode=="Pause")DrawPause(scroll);
                else if(mode=="Supplier")DrawSupplier(scroll);
                else if(mode=="Door")DrawDoor(scroll);
                else if(mode=="Shelf")DrawShelf(scroll);
                else if(mode=="Ledger")DrawLedger(scroll);
                if(mode!="Menu")Button(shell,"Back to the House [Esc]",()=>Open("Explore"));
            }
            if(confirmation!=null)
            {
                var shade=Box(root,"modal-shade");var panel=Box(shade,"modal card");Text(panel,"CONFIRM","section-title");Text(panel,confirmText);
                Button(panel,"Confirm",()=>{var action=confirmation;confirmation=null;confirmText=null;action();});
                Button(panel,"Cancel",()=>{confirmation=null;confirmText=null;Draw();});
            }
        }
        void DrawMenu(VisualElement parent)
        {
            Text(parent,"THE HOUSE AT THE END OF THE STREET","title");
            Text(parent,"Visitors knock at the front door. Their possessions are yours to judge.");
            if(Game!=null)Button(parent,$"Continue — Night {Game.State.night}",()=>{RestorePlayer();mode="Explore";RefreshWorld();Draw();SetCursor(true);});
            Number(parent,"Run seed",seed,value=>seed=value);
            Button(parent,"Start New Run",()=>{if(Game!=null&&Game.State.phase!=NightPhase.Bankrupt)Confirm("Replace the current run? Records and discoveries will remain.",()=>StartRun(seed));else StartRun(seed);});
            Text(parent,"YOUR FIRST NIGHT","section-title");
            Text(parent,"1. Answer the knock with E. Borrow the item before buying.\n2. Use the phone on the desk to order a magnifying glass, UV flashlight and supplies.\n3. Carry the item to the desk. E places it; E again begins examination.\n4. Rotate with right drag; hold left mouse on the marked regions with the selected tool.\n5. Return to the door to bargain, buy or refuse.\n6. Put purchased goods on a shelf and set their sale tags.\n7. Use the rear rug for salt, milk and vodka rituals.\n8. Close the night at the ledger beside the phone.");
            Text(parent,$"Records: {profile.bestNights} nights · capital {profile.bestCapital} · cash profit {profile.bestProfit}","muted");
            Button(parent,"Quit",()=>{Save();Application.Quit();});
        }
        void DrawPause(VisualElement parent)
        {
            Text(parent,"PAUSED","title");
            Text(parent,"There is no time limit. The game saves after every completed action.");
            Text(parent,"SUPPLIES","section-title");DrawInventory(parent);
            var toggle=new Toggle("Desk sounds"){value=profile.sound};toggle.RegisterValueChangedCallback(e=>{profile.sound=e.newValue;Save();});parent.Add(toggle);
            Button(parent,"Save & Main Menu",()=>{Save();Open("Menu");});
            Button(parent,"Save & Quit",()=>{Save();Application.Quit();});
        }
        void DrawInventory(VisualElement parent)
        {
            foreach(Supply supply in Enum.GetValues(typeof(Supply)))Text(parent,$"{SupplyCatalog.Name(supply)}: {Rules.State.Count(supply)}","muted");
        }
        void DrawSupplier(VisualElement parent)
        {
            Text(parent,"NIGHT SUPPLIER","title");
            Text(parent,$"Cash {Game.State.cash}. Keep {Game.State.due} for tonight's expenses. Delivery is immediate.");
            Text(parent,"TOOLS & TRADE GOODS","section-title");
            foreach(Supply supply in Enum.GetValues(typeof(Supply)))
            {
                var card=Box(parent,"card");Text(card,SupplyCatalog.Name(supply).ToUpperInvariant(),"section-title");Text(card,SupplyCatalog.Description(supply));
                Button(card,$"Buy · {SupplyCatalog.Price(supply)}   |   Owned {Rules.State.Count(supply)}",()=>Act(()=>Rules.Purchase(supply)),Rules.State.Count(supply)<SupplyCatalog.Limit(supply)&&Game.State.cash>=SupplyCatalog.Price(supply));
            }
            Text(parent,"HOUSE IMPROVEMENTS","section-title");
            foreach(Upgrade upgrade in Enum.GetValues(typeof(Upgrade)))
            {
                var description=upgrade==Upgrade.Shelves?"Unlock four physical shelf positions.":upgrade==Upgrade.Workbench?"Gain two actions per night.":upgrade==Upgrade.Insulation?"Reduce protective wrapping cost.":"Steadier hands: surface checks take less time.";
                Text(parent,upgrade+" — "+description);
                Button(parent,$"{upgrade} {Game.UpgradeLevel(upgrade)}/2 · {Game.UpgradeCost(upgrade)}",()=>Act(()=>Game.BuyUpgrade(upgrade)),Game.UpgradeLevel(upgrade)<2&&Game.State.cash>=Game.UpgradeCost(upgrade));
            }
        }
        void DrawDoor(VisualElement parent)
        {
            var customer=Game.Customer;
            if(customer==null){Text(parent,"The visitor has left.");return;}
            Text(parent,customer.kind.ToString().ToUpperInvariant(),"title");Text(parent,customer.story,"quote");
            Text(parent,Game.Title(customer.item),"section-title");Text(parent,ShopCatalog.Sign(customer.item),"warning");
            if(!Rules.State.itemBorrowed)Button(parent,"Take Item for Inspection",()=>Act(Rules.Borrow),Rules.State.carriedId==0);
            else Text(parent,"The item is inside. It remains the visitor's property until purchased.","muted");
            if(dialogues!=null&&(int)customer.kind<dialogues.Length&&dialogues[(int)customer.kind]!=null)
            {
                var asset=dialogues[(int)customer.kind];
                if(!customer.conversationStarted)Button(parent,"Ask About the Item",()=>Act(()=>{customer.conversationStarted=true;return "The visitor begins their story.";}));
                else
                {
                    var session=VisitorConversation.Restore(asset,customer);
                    if(!session.Finished&&session.Current?.kind==StepKind.Line){Text(parent,session.Current.GetText(),"quote");Button(parent,"Continue Conversation",()=>Act(()=>VisitorConversation.Advance(asset,customer,-1)));}
                    else if(!session.Finished&&session.Current?.kind==StepKind.Choice)
                        for(var i=0;i<session.Current.answers.Length;i++){var answer=i;Button(parent,session.Current.GetAnswer(i),()=>Act(()=>VisitorConversation.Advance(asset,customer,answer)));}
                }
            }
            Text(parent,customer.response);
            Text(parent,$"Asking {customer.ask} · Offers left {customer.attempts}");Number(parent,"Your offer",offer,value=>offer=value);
            Button(parent,"Make Offer",()=>Act(()=>Game.Offer(offer)),!customer.accepted);
            Button(parent,"Use Verified Evidence",()=>Act(Game.UseArgument),!customer.usedArgument&&!customer.accepted&&(customer.item.inspected||customer.item.identified||customer.item.goldTested));
            var price=customer.accepted?customer.acceptedPrice:customer.ask;
            Button(parent,$"Buy for {price}",()=>Confirm($"Buy {Game.Title(customer.item)} for {price}?\nCash after purchase: {Game.State.cash-price}. Expenses due: {Game.State.due}.",()=>Act(BuyVisitorItem)),Game.State.cash>=price);
            Button(parent,"Refuse & Send Away",()=>Confirm("Return the visitor's item and refuse this deal? No money changes hands.",()=>Act(Game.Reject)));
        }
        string BuyVisitorItem()
        {
            var item=Game.Customer?.item;if(item==null)return "The visitor has left.";
            if(!Rules.State.itemBorrowed&&Rules.State.carriedId!=0)return "Put down the carried item before buying another.";
            var result=Game.Buy();
            if(Rules.Owned(item.id)&&item.id!=Rules.State.tableId&&item.id!=Rules.State.carriedId)Rules.State.carriedId=item.id;
            return result;
        }
        void DrawShelf(VisualElement parent)
        {
            var item=Rules.Find(selected);if(item==null){Text(parent,"This shelf is empty.");return;}
            Text(parent,Game.Title(item).ToUpperInvariant(),"title");Text(parent,ShopCatalog.Sign(item),"warning");
            Text(parent,item.inspected?$"Appraisal {ShopSimulation.Value(item)} · Condition {item.condition}%":"Appraisal unverified. Carry it to the desk for examination.");
            if(item.identified)Text(parent,ShopCatalog.CurseName(item.curse)+" — "+ShopCatalog.Rule(item.curse));
            if(item.goldTested)Text(parent,item.containsGold?"Gold-bearing metal confirmed.":"Gold excluded at the tested surface.");
            Text(parent,$"Paid {item.cost} · Liquidation {ShopSimulation.LiquidationValue(item)} · {Game.Demand(item)}");
            Number(parent,"Sale price",salePrice,value=>salePrice=value);
            Button(parent,"List / Update Sale Tag",()=>Act(()=>Game.ListItem(item.id,salePrice,true)),Game.State.phase==NightPhase.Reception);
            Button(parent,"Remove Sale Tag",()=>Act(()=>Game.ListItem(item.id,item.askingPrice,false)),item.listed&&Game.State.phase==NightPhase.Reception);
            Button(parent,"Pick Up",()=>{Open("Explore");Act(()=>Rules.PickUp(item.id));},Rules.State.carriedId==0);
            Button(parent,$"Protective Wrapping · {Game.WrapCost}",()=>Act(()=>Game.Wrap(item.id)),!item.protectedItem&&Game.State.phase==NightPhase.Reception);
            Button(parent,$"Liquidate · +{ShopSimulation.LiquidationValue(item)}",()=>Confirm("Sell this item immediately at the displayed liquidation price?",()=>Act(()=>Game.Liquidate(item.id))));
            Button(parent,"Dispose",()=>Confirm("Dispose of this item permanently with no refund?",()=>Act(()=>Game.Dispose(item.id))));
        }
        void DrawLedger(VisualElement parent)
        {
            Text(parent,"PROTECTED LEDGER","title");var s=Game.State;var r=s.report;
            Text(parent,$"{s.eventName}: {s.eventDescription}");
            Text(parent,$"Opening cash {r.openingCash}\nPurchases -{r.purchases}\nDiagnostics -{r.tests}\nSupplies -{r.supplies}\nProtection -{r.protection}\nImprovements -{r.upgrades}\nSales +{r.sales}\nLiquidation +{r.liquidation}\nCurse cash loss -{r.curseLoss}\nExpenses paid -{r.expenses}\nCash {s.cash}\nAppraisal damage {r.damage}\nLiquidation capital {Game.Capital}");
            if(r.finalized)Text(parent,$"Nights survived {s.completedNights} · Cash change {r.profit:+#;-#;0}");
            if(s.phase==NightPhase.Reception)
            {
                Text(parent,Game.Forecast());
                Text(parent,"Place purchased goods on shelves before closing. Unlisted goods cannot sell. Remaining visitors leave safely.","warning");
                Button(parent,"Close the House for Tonight",()=>Confirm(Game.Forecast()+"\nClose now?",()=>Act(Rules.CloseNight)));
            }
            else if(s.phase==NightPhase.Settlement)
            {
                Text(parent,$"Shortfall {Math.Max(0,s.due-s.cash)}. Liquidate goods from shelf tags before declaring bankruptcy.","warning");
                Button(parent,"Pay Expenses",()=>Act(Game.PayExpenses),s.cash>=s.due);
                Button(parent,"Declare Bankruptcy",()=>Confirm("End this run? You can still liquidate goods from their shelf tags.",()=>Act(Game.ConfirmBankruptcy)),s.cash<s.due);
            }
            else if(s.phase==NightPhase.Results)Button(parent,$"Open Night {s.night+1}",()=>Act(Game.NextNight));
            else Button(parent,"Return to Main Menu",()=>Open("Menu"));
            Text(parent,"VERIFIED HISTORY","section-title");foreach(var line in s.journal.AsEnumerable().Reverse())Text(parent,line,"ledger-line");
        }
        void DrawFocusControls()
        {
            var controls=Box(root,"house-focus-controls");
            if(mode=="Inspect")
            {
                var row=Box(controls,"row");
                Button(row,"1 · Rotate",()=>SelectTool(InspectionTool.Hands));
                Button(row,"2 · Magnify",()=>SelectTool(InspectionTool.Loupe));
                Button(row,"3 · UV",()=>SelectTool(InspectionTool.Ultraviolet));
                Button(row,"4 · Gold Test",()=>SelectTool(InspectionTool.GoldReagent));
            }
            else Text(controls,ritualFinished?"Cleansing complete. Pick up the item and return it to a shelf.":!Rules.State.saltPlaced?"SALT: hold left mouse and trace the eight sections of the ring.":!Rules.State.milkPoured?"MILK: hold left mouse over the bowl to pour an offering.":"VODKA: rotate the item and wipe all three marked regions with left mouse.","muted");
            var actions=Box(controls,"row");Button(actions,"Pick Up Item",TakeFocusedItem);Button(actions,"Leave in Place [Esc]",()=>Open("Explore"));
        }
    }
}
