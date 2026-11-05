using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Pawnshop.Endless;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class QuestRitualSmoke
{
 public static string Main(){new GameObject("Ritual pointer validation").AddComponent<RitualPointerDriver>();return "Ritual pointer test started";}
}
public sealed class RitualPointerDriver:MonoBehaviour
{
 QuestGame game;QuestWorld world;Mouse original,test;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 void Pointer(Vector3 point,bool held)
 {
  test.MakeCurrent();var state=new MouseState{position=Camera.main.WorldToScreenPoint(point),buttons=(ushort)(held?1:0)};
  InputState.Change(test,state,InputUpdateType.Dynamic);InputState.Change(test,state,InputUpdateType.Editor);
  typeof(QuestWorld).GetMethod("Update",Flags).Invoke(world,null);
 }
 IEnumerator Run()
 {
  game=UnityEngine.Object.FindFirstObjectByType<QuestGame>();world=UnityEngine.Object.FindFirstObjectByType<QuestWorld>();
  typeof(QuestGame).GetField("storage",Flags).SetValue(game,new RunStorage(Path.Combine(Application.dataPath,"../Temp/RitualPointerSave")));
  game.StartRun(125);var q=game.Quest;var item=new ItemState{id=q.Shop.State.nextId++,template=2,condition=60,baseValue=90,curse=Curse.Cold,identified=true,slot=0};q.Shop.State.stock.Add(item);
  q.House.State.Add(Supply.Salt,2);q.House.State.Add(Supply.Milk,2);q.State.phase=DayPhase.Night;game.SelectWorldItem(item.id,false);
  original=Mouse.current;test=InputSystem.AddDevice<Mouse>("RitualTestMouse");yield return new WaitForSeconds(1.5f);
  var method=typeof(QuestWorld).GetMethod("StarPoint",Flags);
  for(int line=0;line<5;line++)
  {
   var a=(Vector3)method.Invoke(world,new object[]{line});var b=(Vector3)method.Invoke(world,new object[]{(line+1)%5});
   for(int step=0;step<=80;step++){Pointer(Vector3.Lerp(a,b,step/80f),true);yield return null;}
   Pointer(b,false);yield return null;if(q.State.saltPoints!=line+1)throw new Exception("Salt line failed: "+line+" "+q.State.message);
  }
  foreach(string name in new[]{"Milk for ritual","Ritual hotplate","Trade item "+item.id})
  {
   var target=UnityEngine.Object.FindObjectsByType<QuestWorldTarget>(FindObjectsSortMode.None).First(t=>t.name==name);
   if(!target.CanUse)throw new Exception("Blocked required step: "+name);
   var center=target.GetComponent<BoxCollider>().bounds.center;
   Pointer(center,false);yield return null;Pointer(center,true);yield return null;Pointer(center,false);yield return null;
  }
  if(item.curse!=Curse.None)throw new Exception("Ritual did not complete: "+q.State.message);
  if(q.House.State.Count(Supply.Salt)!=1||q.House.State.Count(Supply.Milk)!=1)throw new Exception("Wrong consumption");
 }
 IEnumerator Start()
 {
  var run=Run();while(true){bool more=false;object next=null;Exception error=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){error=e;}
   if(error!=null||!more){if(test!=null)InputSystem.RemoveDevice(test);if(original!=null)original.MakeCurrent();File.WriteAllText("Temp/ritual-pointer-smoke.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=error==null,error=error?.ToString()}));Destroy(gameObject);yield break;}yield return next;}
 }
}
