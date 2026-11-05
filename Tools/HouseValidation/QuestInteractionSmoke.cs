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
public static class QuestInteractionSmoke
{
 public static string Main(){new GameObject("Quest interaction validation").AddComponent<QuestInteractionDriver>();return "Running pointer validation; Temp/quest-interaction-smoke.json";}
}
public sealed class QuestInteractionDriver:MonoBehaviour
{
 QuestGame game;QuestWorld world;Camera view;Mouse original,test;readonly List<string> checks=new List<string>();
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 void Check(bool condition,string text){if(!condition)throw new Exception(text);checks.Add(text);}
 Transform Model => (Transform)typeof(QuestWorld).GetField("inspectionModel",Flags).GetValue(world);
 List<TextMesh> Marks => (List<TextMesh>)typeof(QuestWorld).GetField("inspectionMarks",Flags).GetValue(world);
 void Pointer(Vector3 point,bool held)
 {
  test.MakeCurrent();var state=new MouseState{position=view.WorldToScreenPoint(point),buttons=(ushort)(held?1:0)};
  InputState.Change(test,state,InputUpdateType.Dynamic);InputState.Change(test,state,InputUpdateType.Editor);
  typeof(QuestWorld).GetMethod("Update",Flags).Invoke(world,null);
 }
 IEnumerator Hold(Vector3 point,float duration)
 {
  float end=Time.realtimeSinceStartup+duration;while(Time.realtimeSinceStartup<end){Pointer(point,true);yield return null;}Pointer(point,false);
 }
 IEnumerator Run()
 {
  game=UnityEngine.Object.FindFirstObjectByType<QuestGame>();world=UnityEngine.Object.FindFirstObjectByType<QuestWorld>();view=Camera.main;
  typeof(QuestGame).GetField("storage",Flags).SetValue(game,new RunStorage(Path.Combine(Application.dataPath,"../Temp/QuestInteractionSave")));
  game.StartRun(124);game.Quest.House.State.Add(Supply.GoldReagent,2);game.WorldAct(game.Quest.Advance);game.Quest.State.doorOpened=true;world.SetInspection(true);
  original=Mouse.current;test=InputSystem.AddDevice<Mouse>("QuestValidationMouse");test.MakeCurrent();
  yield return new WaitForSeconds(2);
  foreach(var tool in new[]{InspectionTool.Loupe,InspectionTool.Ultraviolet})
  {
   world.ChooseTool(tool);yield return null;
   foreach(var marker in Marks.ToArray())
   {
    var point=marker.transform.position;Check(!game.BlocksWorldPointer(view.WorldToScreenPoint(point)),"Marker is outside UI: "+tool);
    var hold=Hold(point,1.2f);while(hold.MoveNext())yield return hold.Current;
   }
   Check(tool==InspectionTool.Loupe?game.Quest.Shop.Customer.item.inspected:game.Quest.Shop.Customer.item.tested,"Pointer completed "+tool);
  }
  world.ChooseTool(InspectionTool.GoldReagent);yield return null;
  var start=Marks[0].transform.position;var endPoint=Marks[2].transform.position;
  for(int i=0;i<=80;i++){Pointer(Vector3.Lerp(start,endPoint,i/80f),true);yield return null;}
  var drop=Hold((start+endPoint)*.5f,1.4f);while(drop.MoveNext())yield return drop.Current;
  Check(game.Quest.Shop.Customer.item.goldTested,"Scratch and drop completed gold test");
  Check(game.Quest.House.State.Count(Supply.GoldReagent)==1,"Only one reagent consumed");
  int actions=game.Quest.Shop.State.actions;world.ChooseTool(InspectionTool.GoldReagent);Check(game.Quest.Shop.State.actions==actions,"Repeat inspection does not consume actions");
  world.SetInspection(false);
 }
 IEnumerator Start()
 {
  var run=Run();while(true){bool more=false;object next=null;Exception error=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){error=e;}
   if(error!=null||!more){if(test!=null)InputSystem.RemoveDevice(test);if(original!=null)original.MakeCurrent();File.WriteAllText("Temp/quest-interaction-smoke.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=error==null,error=error?.ToString(),checks}));Destroy(gameObject);yield break;}yield return next;}
 }
}
