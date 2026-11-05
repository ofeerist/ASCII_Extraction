using System.Reflection;
using Pawnshop.Endless;
using UnityEngine;
public static class QuestShelfPreview
{
 public static string Main()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<QuestGame>();
  typeof(QuestGame).GetField("storage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,new RunStorage(System.IO.Path.Combine(Application.dataPath,"../Temp/ShelfPreview")));
  g.StartRun(124);
  for(int i=0;i<9;i++)g.Quest.Shop.State.stock.Add(new ItemState{id=g.Quest.Shop.State.nextId++,template=i,baseValue=90,condition=65,cost=12+i,askingPrice=65,slot=i==8?ShopSimulation.IsolationSlot:i,inspected=true,identified=true,goldTested=true,observedMark="Клеймо: П-17",curse=i==2?Curse.Whisper:Curse.None,listed=i%2==0});
  g.Quest.State.phase=DayPhase.Evening;g.CloseConversation();g.RedrawWorld();return "Nine item shelf preview";
 }
}
