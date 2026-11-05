using System;
using System.Reflection;
using Pawnshop.Endless;
using UnityEngine;
using UnityEngine.UIElements;
public static class ChapterPreview
{
    public static string Main()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<QuestGame>();
        if(game==null)throw new Exception("Open HouseShop in Play mode.");
        typeof(QuestGame).GetField("storage",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,new RunStorage(System.IO.Path.Combine(Application.dataPath,"../Temp/ChapterSmoke")));
        game.StartRun(12345);game.WorldAct(game.Quest.Advance);game.Quest.State.doorOpened=true;game.Quest.AskStory();game.Converse(false);
        if(game.Quest.Shop.State.customers.Count!=3)throw new Exception("Expected three visits.");
        var house=UnityEngine.Object.FindFirstObjectByType<PhysicalShop>();
        if(house.door.GetComponent<Renderer>().isPartOfStaticBatch)throw new Exception("Interactive door cannot be statically batched.");
        var art=Resources.Load<ChapterArt>("ChapterArt");
        if(art==null||art.props[5]==null)throw new Exception("Missing prop bindings.");
        foreach(var key in new[]{"Boris","Anna","Dealer","Police","Nina"})if(FlatVisitor.Portrait(key)==null)throw new Exception("Missing portrait: "+key);
        return "Chapter preview ready; art bindings and five portraits verified; isolated save in Temp/ChapterSmoke.";
    }
}
