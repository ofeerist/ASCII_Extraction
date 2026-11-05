using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Pawnshop.Endless;
using UnityEngine;
public static class HouseWalk
{
    public static object Main()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<PhysicalShop>();
        typeof(PhysicalShop).GetMethod("Open",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{"Pause"});
        var results=new List<object>();
        foreach(var destination in new[]{new Vector3(2.8f,0,2.1f),new Vector3(3.2f,0,3.1f),new Vector3(2.2f,0,3.1f),new Vector3(1.5f,0,5),new Vector3(1.5f,0,7),new Vector3(.7f,0,9.5f),new Vector3(2,0,10.8f),new Vector3(3.2f,0,11.2f)})
        {
            for(var i=0;i<250;i++)
            {
                var delta=destination-game.player.transform.position;delta.y=0;
                if(delta.magnitude<.06f)break;
                game.player.Move(Vector3.ClampMagnitude(delta,.035f)+Vector3.down*.02f);
                Physics.SyncTransforms();
            }
            var actual=game.player.transform.position;var error=Vector2.Distance(new Vector2(actual.x,actual.z),new Vector2(destination.x,destination.z));
            results.Add(new{destination=destination.ToString(),actual=actual.ToString(),passed=error<.15f});
        }
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("Temp/house-walk-result.json",json);return json;
    }
}
