using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Pawnshop.Endless;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
public static class HouseSmoke
{
    public static object Main()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<PhysicalShop>();
        if(game==null)throw new Exception("PhysicalShop is not running");
        var host=new GameObject("House smoke driver");var driver=host.AddComponent<HouseSmokeDriver>();driver.game=game;
        return "House UI smoke test running; Temp/house-smoke-result.json";
    }
}
public sealed class HouseSmokeDriver:MonoBehaviour
{
    public PhysicalShop game;
    Mouse testMouse;
    Mouse originalMouse;
    readonly List<string> steps=new List<string>();
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string field)=>typeof(PhysicalShop).GetField(field,Flags).GetValue(game);
    void Set(string field,object value)=>typeof(PhysicalShop).GetField(field,Flags).SetValue(game,value);
    object Call(string method,params object[] args)=>typeof(PhysicalShop).GetMethod(method,Flags).Invoke(game,args);
    void Check(bool okay,string text){if(!okay)throw new Exception(text);steps.Add(text);}
    void Click(string starts)
    {
        var root=(VisualElement)Get("root");var button=root.Query<Button>().ToList().FirstOrDefault(x=>x.text.StartsWith(starts,StringComparison.Ordinal));
        if(button==null||!button.enabledInHierarchy)throw new Exception("Button unavailable: "+starts);
        typeof(Clickable).GetMethod("SimulateSingleClick",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(button.clickable,new object[]{null,0});
    }
    HouseInteractable Station(HouseStation kind)=>UnityEngine.Object.FindObjectsByType<HouseInteractable>(FindObjectsSortMode.None).First(x=>x.station==kind);
    void Pointer(Vector2 position,bool down)
    {
        testMouse.MakeCurrent();
        InputState.Change(testMouse,new MouseState{position=position,buttons=(ushort)(down?1:0)},InputUpdateType.Dynamic);
        InputState.Change(testMouse,new MouseState{position=position,buttons=(ushort)(down?1:0)},InputUpdateType.Editor);
        Call("UpdateFocus");

    }
    IEnumerator Hold(Vector2 point,float duration)
    {
        var until=Time.realtimeSinceStartup+duration;
        while(Time.realtimeSinceStartup<until){Pointer(point,true);yield return null;}
        Pointer(point,false);
        var state=(InspectionTrace)Get("trace");
        steps.Add("Hold completed: " + string.Join(",",Enumerable.Range(0,3).Select(x=>state.Progress(x).ToString("F3"))));
        yield return null;
    }
    IEnumerator Start()
    {
        var run=Run();
        while(true)
        {
            object next=null;bool more=false;Exception error=null;
            try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){error=e;}
            if(error!=null){Finish(false,error.ToString());yield break;}
            if(!more){Finish(true,null);yield break;}
            yield return next;
        }
    }
    void Finish(bool passed,string error)
    {
        if(testMouse!=null)InputSystem.RemoveDevice(testMouse);if(originalMouse!=null)originalMouse.MakeCurrent();
        File.WriteAllText("Temp/house-smoke-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,steps,mode=game.Mode,night=game.Game.State.night,cash=game.Game.State.cash},Newtonsoft.Json.Formatting.Indented));
        Destroy(gameObject);
    }
    IEnumerator Examine(InspectionTool tool)
    {
        game.SelectTool(tool);yield return null;
        var model=(Transform)Get("focusedModel");
        var required=tool==InspectionTool.GoldReagent?1:3;
        for(var index=0;index<required;index++)
        {
            var normal=index==0?Vector3.back:index==1?Vector3.right:Vector3.forward;
            normal=model.GetComponentsInChildren<InspectionRegion>(true).First(x=>x.index==index).transform.localPosition.normalized;
            model.rotation=Quaternion.FromToRotation(normal,(game.view.transform.position-model.position).normalized);
            yield return null;Physics.SyncTransforms();
            var marker=model.GetComponentsInChildren<InspectionRegion>(true).First(x=>x.index==index);
            var point=game.view.WorldToScreenPoint(marker.transform.position);
            Check(Physics.Raycast(game.view.ScreenPointToRay(point),out var hit,4)&&hit.collider.GetComponent<InspectionRegion>()==marker,"Physical ray reaches "+tool+" region "+index);
            var hold=Hold(point,2);while(hold.MoveNext())yield return hold.Current;
        }
    }
    IEnumerator Run()
    {
        Set("storage",new RunStorage(Path.Combine(Application.dataPath,"../Temp/HouseUISave")));Set("profile",new PlayerProfile());
        game.StartRun(12345);game.Game.Customer.item.curse=Curse.Cold;game.Game.Customer.item.suspicious=true;
        yield return new WaitForSeconds(5);
        Check((bool)Get("arrived"),"Visitor walked to the doorstep");
        Physics.SyncTransforms();
        Check(Physics.Raycast(game.view.transform.position,game.view.transform.forward,out var doorway,2.5f)&&doorway.collider.GetComponentInParent<HouseInteractable>()?.station==HouseStation.Door,"Front door reachable from spawn");
        game.Interact(Station(HouseStation.Door));Click("Take Item for Inspection");Check(game.Rules.State.carriedId!=0,"Door dialogue lends the item without buying it");
        game.Interact(Station(HouseStation.Supplier));
        foreach(var supply in new[]{Supply.Loupe,Supply.UVFlashlight,Supply.GoldReagent,Supply.Milk,Supply.Salt,Supply.Vodka})
        {
            var card=((VisualElement)Get("root")).Query<VisualElement>(className:"card").ToList().First(x=>x.Query<Label>().ToList().Any(l=>l.text==SupplyCatalog.Name(supply).ToUpperInvariant()));
            var button=card.Query<Button>().First();typeof(Clickable).GetMethod("SimulateSingleClick",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(button.clickable,new object[]{null,0});
        }
        Check(game.Rules.State.Count(Supply.Loupe)==1&&game.Rules.State.Count(Supply.UVFlashlight)==1,"Supplier UI delivers reusable tools");
        Call("Open","Explore");game.Interact(Station(HouseStation.Desk));yield return null;
        game.Interact(Station(HouseStation.Desk));yield return null;
        Check(game.Mode=="Inspect","Desk enters physical examination mode");
        originalMouse=Mouse.current;testMouse=InputSystem.AddDevice<Mouse>("HouseSmokeMouse");testMouse.MakeCurrent();
        var loupe=Examine(InspectionTool.Loupe);while(loupe.MoveNext())yield return loupe.Current;
        Check(game.Game.Customer.item.inspected,"Actual mouse holds completed magnification");
        var uv=Examine(InspectionTool.Ultraviolet);while(uv.MoveNext())yield return uv.Current;
        Check(game.Game.Customer.item.identified,"Actual mouse holds completed UV diagnosis");
        var gold=Examine(InspectionTool.GoldReagent);while(gold.MoveNext())yield return gold.Current;
        Check(game.Game.Customer.item.goldTested,"Actual mouse hold applied gold reagent");
        Click("Leave in Place");game.Interact(Station(HouseStation.Door));Click("Use Verified Evidence");
        ((VisualElement)Get("root")).Query<IntegerField>().First().value=40;Click("Make Offer");Click("Buy for");Click("Confirm");
        Check(game.Game.State.stock.Count==1,"Inspected item purchased exactly once");
        game.Interact(Station(HouseStation.Desk));Click("Pick Up Item");game.Interact(Station(HouseStation.RitualRug));yield return null;
        game.Interact(Station(HouseStation.RitualRug));yield return null;
        Check(game.Mode=="Ritual","Rug enters ritual mode");
        for(var sector=0;sector<8;sector++)
        {
            var angle=(sector+.5f)*Mathf.PI/4;var p=game.rugAnchor.position+new Vector3(Mathf.Cos(angle)*.62f,0,Mathf.Sin(angle)*.62f);
            var saltHold=Hold(game.view.WorldToScreenPoint(p),.2f);while(saltHold.MoveNext())yield return saltHold.Current;
        }
        Check(game.Rules.State.saltPlaced,"Mouse traced all eight salt sections");
        var milk=game.rugAnchor.position+new Vector3(-.35f,0,-.17f);var milkHold=Hold(game.view.WorldToScreenPoint(milk),2);while(milkHold.MoveNext())yield return milkHold.Current;
        Check(game.Rules.State.milkPoured,"Mouse hold poured the milk offering");
        var ritualModel=(Transform)Get("focusedModel");
        for(var i=0;i<3;i++)
        {
            var normal=i==0?Vector3.back:i==1?Vector3.right:Vector3.forward;normal=ritualModel.GetComponentsInChildren<InspectionRegion>(true).First(x=>x.index==i).transform.localPosition.normalized;ritualModel.rotation=Quaternion.FromToRotation(normal,(game.view.transform.position-ritualModel.position).normalized);yield return null;Physics.SyncTransforms();
            var marker=ritualModel.GetComponentsInChildren<InspectionRegion>(true).First(x=>x.index==i);var screen=game.view.WorldToScreenPoint(marker.transform.position);
            var wipeHold=Hold(screen,2);while(wipeHold.MoveNext())yield return wipeHold.Current;
        }
        Check(game.Game.State.stock[0].curse==Curse.None,"Physical ritual removed the curse");
        Check(game.Rules.State.Count(Supply.Milk)==0&&game.Rules.State.Count(Supply.Vodka)==0,"Ritual consumed milk and vodka once");
        Click("Pick Up Item");var shelf=UnityEngine.Object.FindObjectsByType<HouseInteractable>(FindObjectsSortMode.None).First(x=>x.station==HouseStation.Shelf&&x.index==0);game.Interact(shelf);game.Interact(shelf);
        ((VisualElement)Get("root")).Query<IntegerField>().First().value=40;Click("List / Update Sale Tag");
        game.Interact(Station(HouseStation.Ledger));Click("Close the House for Tonight");Click("Confirm");
        Check(game.Game.State.phase==NightPhase.Results,"Shelved item sold and expenses settled");Click("Open Night 2");
        var saved=new RunStorage(Path.Combine(Application.dataPath,"../Temp/HouseUISave")).LoadRun();
        Check(saved!=null&&saved.night==2&&saved.cash==game.Game.State.cash,"3D run autosave matches night two");
        Call("Open","Explore");
    }
}
