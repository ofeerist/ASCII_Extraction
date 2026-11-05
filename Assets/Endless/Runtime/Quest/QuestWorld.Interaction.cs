using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Pawnshop.Endless
{
    public sealed partial class QuestWorld
    {
        public InspectionTool ActiveTool { get; private set; }
        InspectionGame inspection;
        int inspectedId;
        Transform inspectionModel;
        float inspectionHeight;
        LineRenderer scratchLine;
        readonly List<TextMesh> inspectionMarks=new List<TextMesh>();
        QuestWorldTarget hovered;
        readonly List<(QuestWorldTarget target,LineRenderer line)> outlines=new List<(QuestWorldTarget,LineRenderer)>();
        static Material guideMaterial;
        public string ActivityProgress => Inspecting ? inspection==null?"Выберите инструмент.":inspection.Complete?"✓ Проверка завершена. Результат записан в карточку.":ActiveTool==InspectionTool.GoldReagent?inspection.Scratch<1?$"Проведите царапину слева направо: {inspection.Scratch:P0}":$"Нанесите каплю в центр и удерживайте: {inspection.Reaction:P0}":$"Найдено участков: {inspection.coverage.Count(x=>x>=1)}/3 · {inspection.Progress:P0}" : game.Quest.State.phase==DayPhase.Night?$"Линии соли: {game.Quest.State.saltPoints}/5":"";
        Material GuideMaterial()
        {
            if(guideMaterial==null)guideMaterial=new Material(Resources.Load<Shader>("InteractionGuide"));
            return guideMaterial;
        }
        void Outline(QuestWorldTarget target,BoxCollider box)
        {
            var line=new GameObject("Interaction outline").AddComponent<LineRenderer>();line.transform.SetParent(target.transform,false);
            line.sharedMaterial=GuideMaterial();line.useWorldSpace=false;line.startWidth=line.endWidth=.004f;
            var c=box.center;var s=box.size*.5f;var p=new Vector3[8];
            for(int i=0;i<8;i++)p[i]=c+new Vector3((i&1)==0?-s.x:s.x,(i&2)==0?-s.y:s.y,(i&4)==0?-s.z:s.z);
            int[] order={0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};
            line.positionCount=order.Length;line.SetPositions(order.Select(i=>p[i]).ToArray());outlines.Add((target,line));
        }
        void UpdateOutlines()
        {
            outlines.RemoveAll(x=>x.target==null||x.line==null);
            foreach(var pair in outlines)
            {
                var t=pair.target;pair.line.enabled=t.gameObject.activeInHierarchy && (t.click!=null||t.saltLine>=0);
                var color=t.CanUse?new Color(.40f,.78f,.63f,.55f):new Color(.35f,.35f,.34f,.3f);
                if(t==hovered)color=t.CanUse?new Color(1,.78f,.25f):new Color(.75f,.36f,.25f);
                pair.line.startColor=pair.line.endColor=color;
                pair.line.startWidth=pair.line.endWidth=t==hovered?.008f:.003f;
            }
        }
        bool ToolDone(ItemState item,InspectionTool tool)=>tool==InspectionTool.Loupe?item.inspected:tool==InspectionTool.Ultraviolet?item.tested:item.goldTested;
        public void ChooseTool(InspectionTool tool)
        {
            if(!Inspecting || game.Quest.Shop.Customer==null)return;
            var q=game.Quest;var item=q.Shop.Customer.item;
            var supply=tool==InspectionTool.Loupe?Supply.Loupe:tool==InspectionTool.Ultraviolet?Supply.UVFlashlight:Supply.GoldReagent;
            if(q.House.State.Count(supply)<1){game.WorldAct(()=>q.Say("Нет инструмента или реактива. Купить можно утром у Нины."));return;}
            if(ToolDone(item,tool)){game.WorldAct(()=>q.Say("✓ Эта проверка уже записана в карточку вещи."));return;}
            if(q.Shop.State.actions<1){game.WorldAct(()=>q.Say("Для проверки нужна одна единица сил."));return;}
            ActiveTool=tool;inspection=new InspectionGame(tool);inspectedId=item.id;
            game.WorldAct(()=>q.Say(tool==InspectionTool.GoldReagent?"ЛКМ: проведите по полоске слева направо. Затем задержите каплю в её центре.":"Удерживайте ЛКМ и проведите инструментом по трём отметкам на вещи."));
        }
        void BuildInspection()
        {
            var q=game.Quest;var anchor=house.tableAnchor.position;
            if(inspectedId!=q.Shop.Customer.item.id){inspection=null;ActiveTool=InspectionTool.Hands;inspectedId=q.Shop.Customer.item.id;}
            CameraAt(anchor+new Vector3(-.30f,.95f,-1.15f),anchor+new Vector3(-.30f,.12f,0));
            inspectionModel=HouseVisuals.Item(q.Shop.Customer.item,props).transform;inspectionModel.position=anchor;inspectionModel.localScale=Vector3.one*1.6f;
            var visible=inspectionModel.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.activeInHierarchy && !(r is LineRenderer)).ToArray();
            var top=visible.Length>0?visible.Max(r=>r.bounds.max.y):anchor.y+.15f;
            inspectionHeight=inspectionModel.InverseTransformPoint(new Vector3(anchor.x,top,anchor.z)).y+.015f;
            Target(inspectionModel,"Выберите инструмент слева; ПКМ — повернуть вещь",()=>{},new Vector3(0,.12f,0),new Vector3(.4f,.28f,.4f));
            inspectionMarks.Clear();
            for(int i=0;i<3;i++)
            {
                var marker=Label(inspectionModel,ActiveTool==InspectionTool.GoldReagent?(i==1?"КАПЛЯ":"—"):"["+(i+1)+"]",new Vector3((i-1)*.095f,inspectionHeight,0),.006f,false);
                marker.color=new Color(.9f,.75f,.3f);
                if(inspection!=null && inspection.coverage[i]>=1) { marker.text="+"; marker.color=new Color(.25f,1,.65f); }
                inspectionMarks.Add(marker);
            }
            scratchLine=null;
            if(ActiveTool==InspectionTool.GoldReagent)
            {
                scratchLine=new GameObject("Reagent scratch").AddComponent<LineRenderer>();scratchLine.transform.SetParent(inspectionModel,false);
                scratchLine.sharedMaterial=GuideMaterial();scratchLine.useWorldSpace=false;scratchLine.positionCount=2;
                scratchLine.startWidth=scratchLine.endWidth=.008f;scratchLine.startColor=scratchLine.endColor=new Color(1,.7f,.2f);
                scratchLine.SetPositions(new[]{new Vector3(-.095f,inspectionHeight,0),new Vector3(-.095f,inspectionHeight,0)});
            }
            Sign(props,"К ДВЕРИ",anchor+new Vector3(.4f,.36f,.30f),()=>SetInspection(false));
        }
        bool UpdateInspection(Ray ray,Mouse mouse)
        {
            if(inspectionModel==null)return false;
            if(mouse.rightButton.isPressed)
            {
                inspectionModel.Rotate(Vector3.up,-mouse.delta.ReadValue().x*.5f,Space.World);return true;
            }
            if(inspection==null||inspection.Complete)return false;
            var plane=new Plane(Vector3.up,inspectionModel.TransformPoint(new Vector3(0,inspectionHeight,0)));
            if(!plane.Raycast(ray,out float distance))return false;
            var point=ray.GetPoint(distance);var local=inspectionModel.InverseTransformPoint(point);
            bool over=Mathf.Abs(local.x)<.23f&&Mathf.Abs(local.z)<.20f;
            if(!over)return false;
            var viewport=view.ScreenToViewportPoint(mouse.position.ReadValue());
            Shader.SetGlobalVector("_QuestLens",new Vector4(viewport.x,viewport.y,.13f,(float)ActiveTool));
            for(int i=0;i<inspectionMarks.Count;i++)
            {
                var mark=inspectionMarks[i];mark.transform.rotation=view.transform.rotation;
                bool near=Vector3.Distance(point,mark.transform.position)<.085f;
                if(ActiveTool==InspectionTool.Ultraviolet)mark.text=near?"{?}":"·";
                if(ActiveTool!=InspectionTool.GoldReagent)
                {
                    inspection.Scan(i,Time.deltaTime,near&&mouse.leftButton.isPressed);
                    mark.color=inspection.coverage[i]>=1?new Color(.25f,1,.65f):near?new Color(.8f,.5f,1):new Color(.55f,.50f,.32f);
                    if(inspection.coverage[i]>=1)mark.text="+";
                }
            }
            if(ActiveTool==InspectionTool.GoldReagent)inspection.ApplyReagent((local.x+.095f)/.19f,Mathf.Abs(local.z)<.045f,Time.deltaTime,mouse.leftButton.isPressed);
            if(scratchLine!=null)
            {
                scratchLine.SetPosition(1,new Vector3(Mathf.Lerp(-.095f,.095f,inspection.Scratch),inspectionHeight,0));
                if(inspection.Scratch>=1)
                {
                    bool gold=game.Quest.Shop.Customer.item.template==5;
                    inspectionMarks[1].text=inspection.Reaction>0?"●":"КАПЛЯ";
                    inspectionMarks[1].color=Color.Lerp(new Color(1,.7f,.2f),gold?new Color(1,.9f,.25f):new Color(.2f,.9f,.5f),inspection.Reaction);
                }
            }
            Hover=ActivityProgress;
            if(inspection.Complete)game.WorldAct(()=>game.Quest.Inspect(ActiveTool));
            return true;
        }
        void BuildShelfDisplay()
        {
            // Dedicated front-facing shelf view keeps every owned item visible, including isolation.
            var anchor=new Vector3(50,.25f,0);
            Part(props,"Shelf room floor",anchor+new Vector3(0,-.20f,0),new Vector3(8,.1f,8),new Color(.12f,.10f,.07f));
            Part(props,"Shelf room wall",anchor+new Vector3(0,1.6f,.5f),new Vector3(8,4,.1f),new Color(.08f,.10f,.09f));
            var lamp=Group("Shelf lamp",props,anchor+new Vector3(0,2,-1)).gameObject.AddComponent<Light>();
            lamp.type=LightType.Point;lamp.range=6;lamp.intensity=5;lamp.color=new Color(1,.84f,.62f);
            CameraAt(anchor+new Vector3(.70f,1.1f,-3.2f),anchor+new Vector3(.70f,.60f,0));
            int count=game.Quest.Shop.State.stock.Count;int rows=Math.Max(2,(count+4)/5);
            float height=rows*.44f;
            Part(props,"Shelf back",anchor+new Vector3(0,height*.5f-.05f,.22f),new Vector3(2.6f,height,.04f),new Color(.12f,.09f,.06f));
            foreach(float x in new[]{-1.28f,1.28f})Part(props,"Shelf support",anchor+new Vector3(x,height*.5f-.05f,0),new Vector3(.07f,height,.4f),new Color(.25f,.16f,.08f));
            for(int row=0;row<rows;row++)
                Part(props,"Display shelf "+row,anchor+new Vector3(0,row*.44f-.025f,0),new Vector3(2.5f,.045f,.40f),new Color(.25f,.16f,.08f));
            int index=0;
            foreach(var item in game.Quest.Shop.State.stock)
            {
                var pos=anchor+new Vector3(-.96f+(index%5)*.48f,(index/5)*.44f,0);
                var model=HouseVisuals.Item(item,props);model.transform.position=pos;
                int id=item.id;Target(model.transform,"Открыть карточку вещи и объявление",()=>game.SelectWorldItem(id,true),new Vector3(0,.12f,0),new Vector3(.36f,.30f,.32f));
                Label(model.transform,(item.listed?"В ПРОДАЖЕ":"НЕ ВЫСТАВЛЕНО")+"\n"+(item.listed?item.askingPrice+" ₽":"Покупка: "+item.cost+" ₽"),new Vector3(0,-.08f,-.12f),.014f);index++;
            }
            Sign(props,"ДО НОЧИ →",anchor+new Vector3(1.10f,1.1f,0),()=>game.WorldAct(game.Quest.Advance),.65f);
        }
    }
}
