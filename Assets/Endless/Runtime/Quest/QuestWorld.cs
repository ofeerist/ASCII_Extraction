using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Pawnshop.Endless
{
    public sealed class QuestWorldTarget : MonoBehaviour
    {
        public string title;
        public Action click;
        public int saltLine = -1;
        public Func<bool> available;
        public string lockedReason;
        public bool CanUse => available?.Invoke() ?? (click!=null || saltLine>=0);
    }

    // The scene owns all physical interactions. UI only supplies dialogue and sale-price entry.
    public sealed partial class QuestWorld : MonoBehaviour
    {
        QuestGame game;
        PhysicalShop house;
        Camera view;
        Transform scenery;
        Transform props;
        Transform store;
        Font font;
        readonly List<TextMesh> worldLabels = new List<TextMesh>();
        readonly List<(TextMesh source, Label overlay)> overlayLabels = new List<(TextMesh, Label)>();
        readonly List<QuestWorldTarget> doorTargets = new List<QuestWorldTarget>();
        readonly List<TextMesh> curseGlyphs = new List<TextMesh>();
        Quaternion closedDoor;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        bool firstCamera = true;
        int visitorId = -1;
        DayPhase? previousPhase;
        AudioSource audioSource;
        AudioClip knock;
        float traceProgress;
        int traceLine = -1;
        int saltGestureLine = -1;
        LineRenderer saltPreview;
        public bool Inspecting { get; private set; }
        public string Hover { get; private set; }
        static readonly Color Brass = new Color(.65f,.48f,.24f);
        static readonly Color Ink = new Color(.10f,.15f,.14f);
        static readonly string[] SupplyNames = { "ЛУПА", "УФ", "ПРОБА", "МОЛОКО", "СОЛЬ", "ЧЕКУШКА", "СУХАРИ" };

        public void Initialize(QuestGame owner, PhysicalShop source, Font typeface)
        {
            game = owner; house = source; font = typeface; view = source.view;
            closedDoor = house.door.localRotation;
            // Use the visible leaves themselves: a separate volume behind them is occluded.
            foreach (var station in house.door.parent.GetComponentsInChildren<HouseInteractable>())
                if (station.station == HouseStation.Door)
                {
                    var target=station.gameObject.AddComponent<QuestWorldTarget>();doorTargets.Add(target);
                    var box=station.gameObject.AddComponent<BoxCollider>();
                    var bounds=station.GetComponent<MeshFilter>().sharedMesh.bounds;box.center=bounds.center;box.size=bounds.size;
                    Outline(target,box);
                }
            scenery = new GameObject("Quest world scenery").transform; scenery.SetParent(transform, false);
            BuildStore();
            audioSource = gameObject.AddComponent<AudioSource>();
            var samples = new float[22050];
            for (int i=0;i<samples.Length;i++) { int t=i%7350; samples[i]=(float)(Math.Sin(t*.06)*Math.Exp(-t/350.0)*.13); }
            knock=AudioClip.Create("Quest door knocks",samples.Length,1,44100,false); knock.SetData(samples,0);
        }
        Transform Group(string name, Transform parent, Vector3 position)
        {
            var obj=new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.localPosition=position; return obj.transform;
        }
        GameObject Part(Transform parent,string name,Vector3 position,Vector3 scale,Color color,PrimitiveType shape=PrimitiveType.Cube)
            => HouseVisuals.Part(parent,name,shape,position,scale,color);
        TextMesh Label(Transform parent,string text,Vector3 position,float size=.035f, bool overlay=true)
        {
            var obj=Group(text,parent,position); var label=obj.gameObject.AddComponent<TextMesh>();
            label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
            label.color=new Color(.92f,.85f,.66f);
            if(font!=null) { label.font=font; label.GetComponent<MeshRenderer>().sharedMaterial=font.material; }
            if (overlay)
            {
                label.GetComponent<MeshRenderer>().enabled=false;
                worldLabels.Add(label);
            }
            return label;
        }
        QuestWorldTarget Target(Transform obj,string title,Action action,Vector3 center,Vector3 size)
        {
            var collider=obj.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;
            var target=obj.gameObject.AddComponent<QuestWorldTarget>();target.title=title;target.click=action;
            if(action!=null) Outline(target,collider); return target;
        }
        void Sign(Transform parent,string title,Vector3 position,Action action,float width=.48f)
        {
            var sign=Group(title,parent,position);
            Part(sign,"Wooden sign",Vector3.zero,new Vector3(width,.16f,.025f),Ink);
            var label=Label(sign,title,new Vector3(0,0,-.018f),.018f);label.transform.localRotation=Quaternion.identity;
            Target(sign,title,action,Vector3.zero,new Vector3(width,.17f,.06f));
        }
        void BuildStore()
        {
            store=Group("Nina's corner store",scenery,new Vector3(30,0,0));
            Part(store,"Floor",new Vector3(0,-.07f,0),new Vector3(7,.14f,8),new Color(.19f,.17f,.12f));
            Part(store,"Back wall",new Vector3(0,1.6f,2),new Vector3(7,3.2f,.15f),new Color(.22f,.28f,.24f));
            Part(store,"Side wall",new Vector3(-3.5f,1.6f,0),new Vector3(.15f,3.2f,4),new Color(.18f,.23f,.20f));
            Part(store,"Counter",new Vector3(0,.55f,.2f),new Vector3(3.3f,1.1f,.8f),new Color(.19f,.095f,.045f));
            Part(store,"Counter top",new Vector3(0,1.12f,.2f),new Vector3(3.45f,.08f,.9f),new Color(.40f,.30f,.17f));
            for(int y=0;y<3;y++)
            {
                Part(store,"Shelf",new Vector3(0,.7f+y*.65f,1.7f),new Vector3(3.5f,.06f,.4f),Brass);
                for(int x=0;x<7;x++) Part(store,"Preserves",new Vector3(-1.3f+x*.43f,.87f+y*.65f,1.7f),new Vector3(.15f,.24f,.15f),new Color(.3f+x*.02f,.27f,.15f),PrimitiveType.Cylinder);
            }
            var nina=FlatVisitor.Create(store,"Nina");nina.name="Nina / shopkeeper";nina.transform.localPosition=new Vector3(0,.04f,1.1f);nina.transform.localRotation=Quaternion.Euler(0,180,0);

            Label(store,"ЛАВКА НИНЫ",new Vector3(0,2.7f,1.89f),.065f);
            var lamp=Group("Warm shop light",store,new Vector3(0,2.8f,-.5f)).gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=8;lamp.intensity=3;lamp.color=new Color(1,.79f,.50f);

        }
        public void SetInspection(bool active) { Inspecting=active; if(!active){inspection=null;ActiveTool=InspectionTool.Hands;}game.CloseConversation();game.RedrawWorld(); }
        public void Refresh()
        {
            if(view==null||game.Quest==null)return;
            Shader.SetGlobalVector("_QuestLens",Vector4.zero);
            if(props!=null) { props.gameObject.SetActive(false);Destroy(props.gameObject); }
            props=Group("Quest interactive props",scenery,Vector3.zero);curseGlyphs.Clear();saltPreview=null;
            foreach(var target in doorTargets) target.click=null;
            var q=game.Quest; var state=q.State;
            if(previousPhase != state.phase) { firstCamera=true; previousPhase=state.phase; }
            if(game.IsMenu) firstCamera=true;
            store.gameObject.SetActive(state.phase==DayPhase.Morning);
            house.door.localRotation=closedDoor*Quaternion.Euler(0,state.phase==DayPhase.Noon&&state.doorOpened?-100:0,0);
            if(state.phase!=DayPhase.Noon)Inspecting=false;
            if(q.Shop.State.phase!=NightPhase.Reception)
            {
                CameraAt(house.tableAnchor.position+new Vector3(-1.2f,.85f,-.4f),house.tableAnchor.position);
                return;
            }
            switch(state.phase)
            {
                case DayPhase.Morning: Morning();break;
                case DayPhase.Noon: Noon();break;
                case DayPhase.Evening: Evening();break;
                case DayPhase.Night: Night();break;
            }
            // Labels are actual scene geometry: face the chosen fixed camera, never the UI canvas.
            foreach(var label in props.GetComponentsInChildren<TextMesh>()) label.transform.rotation=cameraRotation;
            foreach(var label in store.GetComponentsInChildren<TextMesh>()) label.transform.rotation=cameraRotation;
            Physics.SyncTransforms();
        }
        void CameraAt(Vector3 position,Vector3 target)
        {
            cameraPosition=position;cameraRotation=Quaternion.LookRotation(target-position);view.fieldOfView=55;
            if(firstCamera) { view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);firstCamera=false; }
        }
        void Morning()
        {
            CameraAt(store.position+new Vector3(0,1.85f,-2.9f),store.position+new Vector3(.3f,1.1f,.65f));
            var nina=Group("Talk to Nina",props,store.position+new Vector3(0,0,1.1f));
            Target(nina,"Поговорить с Ниной",()=>game.Converse(false),new Vector3(0,1.55f,0),new Vector3(.6f,.65f,.5f));
            for(int i=0;i<7;i++)
            {
                var supply=(Supply)i;
                var spot=Group("Supply "+supply,props,store.position+new Vector3(-1.35f+i*.44f,1.18f,-.02f));
                SupplyModel(spot,supply);
                Label(spot,$"{SupplyNames[i]}\n{SupplyCatalog.Price(supply)} ₽",new Vector3(0,.38f,0),.018f);
                Target(spot,$"Купить: {SupplyNames[i]} · {SupplyCatalog.Price(supply)} ₽ (есть {game.Quest.House.State.Count(supply)})",()=>game.WorldAct(()=>game.Quest.Purchase(supply)),new Vector3(0,.1f,0),new Vector3(.3f,.3f,.3f));
            }
            Sign(props,"ДОМОЙ →",store.position+new Vector3(1.9f,1.25f,-.1f),()=>game.WorldAct(game.Quest.Advance),.7f);
        }
        void SupplyModel(Transform parent,Supply supply)
        {
            if(supply==Supply.Loupe)
            {
                for(int i=0;i<20;i++){float a=i*Mathf.PI/10;Part(parent,"Loupe rim",new Vector3(Mathf.Cos(a)*.09f,.07f,Mathf.Sin(a)*.09f),Vector3.one*.018f,Brass,PrimitiveType.Sphere);}
                Part(parent,"Loupe handle",new Vector3(0,.07f,-.15f),new Vector3(.035f,.035f,.15f),Ink);
            }
            else if(supply==Supply.Milk||supply==Supply.Salt||supply==Supply.Crackers)
            {
                Part(parent,"Packet",new Vector3(0,.13f,0),new Vector3(.16f,.26f,.12f),supply==Supply.Milk?new Color(.83f,.85f,.72f):supply==Supply.Salt?new Color(.63f,.67f,.61f):Brass);
            }
            else
            {
                Part(parent,"Bottle",new Vector3(0,.11f,0),new Vector3(.10f,.11f,.10f),supply==Supply.UVFlashlight?Ink:new Color(.28f,.40f,.25f),PrimitiveType.Cylinder);
                Part(parent,"Cap",new Vector3(0,.24f,0),new Vector3(.06f,.025f,.06f),Brass,PrimitiveType.Cylinder);
            }
        }
        void Noon()
        {
            var q=game.Quest;
            if(q.Shop.Customer==null&&!q.State.policePending)
            {
                CameraAt(house.tableAnchor.position+new Vector3(-1.15f,.8f,-.65f),house.tableAnchor.position);
                Sign(props,"К ВЕЧЕРУ →",house.tableAnchor.position+Vector3.up*.3f,()=>game.WorldAct(q.Advance),.65f);return;
            }
            if(Inspecting&&!q.State.policePending&&q.Shop.Customer!=null)
            {
                BuildInspection(); return;
            }
            var stop=house.visitorStop.position;
            CameraAt(new Vector3(stop.x-.65f,1.55f,stop.z+2.5f),stop+new Vector3(-.65f,1.05f,0));
            if(!q.State.doorOpened&&!q.State.policePending)
            {
                foreach(var target in doorTargets)
                {
                    target.title="Открыть дверь";
                    target.click=()=>game.WorldAct(()=>{q.State.doorOpened=true;return q.Say("Посетитель протягивает вам вещь. Нажмите на человека, чтобы поговорить, или на предмет для проверки.");});
                }
                int id=q.Shop.Customer?.item.id??0;
                if(visitorId!=id){visitorId=id;audioSource.PlayOneShot(knock);}
                return;
            }
            house.door.localRotation=closedDoor*Quaternion.Euler(0,-100,0);
            var person=FlatVisitor.Create(props,q.State.policePending?"Police":q.Shop.Customer.story=="boris"?"Boris":q.Shop.Customer.story=="dealer"?"Dealer":"Anna");person.transform.position=stop;
            Target(person.transform,q.State.policePending?"Ответить следователю":"Поговорить с посетителем",()=>game.Converse(false),new Vector3(0,1.2f,0),new Vector3(.65f,1.3f,.5f));
            if(!q.State.policePending)
            {
                var item=HouseVisuals.Item(q.Shop.Customer.item,props);item.transform.position=stop+new Vector3(0,1.05f,.38f);
                Target(item.transform,"Взять предмет на осмотр",()=>SetInspection(true),new Vector3(0,.09f,0),new Vector3(.4f,.22f,.4f));
            }
        }
        void Evening()
        {
            BuildShelfDisplay();
        }

        void DisplayStock(Vector3 anchor,bool ritual)
        {
            int index=0;
            foreach(var item in game.Quest.Shop.State.stock)
            {
                if(ritual && item.id==game.SelectedItem) continue;
                var pos=ritual?anchor+new Vector3(-1+(index%5)*.45f,.05f,.85f+(index/5)*.38f):anchor+new Vector3(-.42f+(index%4)*.28f,.04f,-.3f+(index/4)*.27f);
                var model=HouseVisuals.Item(item,props);model.transform.position=pos;model.transform.localScale=Vector3.one*(ritual?.75f:.65f);
                int id=item.id;Target(model.transform,ritual?"Положить предмет в пентаграмму":"Выставить предмет на продажу",()=>game.SelectWorldItem(id,!ritual),new Vector3(0,.1f,0),new Vector3(.4f,.25f,.4f));index++;
            }
        }
        Vector3 StarPoint(int index)
        {
            float angle=Mathf.PI*.5f+index*Mathf.PI*4/5;
            return house.rugAnchor.position+new Vector3(Mathf.Cos(angle)*.66f,.025f,Mathf.Sin(angle)*.66f);
        }
        void Night()
        {
            var anchor=house.rugAnchor.position;var q=game.Quest;
            CameraAt(anchor+new Vector3(0,2.5f,-1.9f),anchor+new Vector3(0,0,.2f));
            Part(props,"Dark ritual mat",anchor+Vector3.up*.012f,new Vector3(1.65f,.006f,1.65f),new Color(.018f,.022f,.018f),PrimitiveType.Cylinder);
            DisplayStock(anchor,true);
            var chosen=q.Shop.State.stock.FirstOrDefault(x=>x.id==game.SelectedItem);
            if(chosen!=null)
            {
                var model=HouseVisuals.Item(chosen,props);model.transform.position=anchor+Vector3.up*.05f;
                var finish=Target(model.transform,"Завершить очищение",()=>game.WorldAct(()=>q.Ritual(chosen.id,4)),new Vector3(0,.1f,0),new Vector3(.4f,.25f,.4f)); finish.available=()=>q.RitualReady(chosen.id);finish.lockedReason=q.RitualHint(chosen.id);
                ShowCurse(chosen,anchor);
            }
            for(int i=0;i<5;i++)
            {
                int lineIndex=i;var start=StarPoint(i);var end=StarPoint((i+1)%5);
                var obj=Group("Salt line "+i,props,Vector3.zero);var line=obj.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;
                line.SetPositions(new[]{start,end});line.startWidth=line.endWidth=i==q.State.saltPoints?.035f:.014f;
                line.sharedMaterial=GuideMaterial();line.startColor=line.endColor=i<q.State.saltPoints?new Color(.6f,1,.8f):i==q.State.saltPoints?new Color(1,.85f,.3f):new Color(.17f,.20f,.18f);
                var hit=Group("Trace segment "+i,props,(start+end)*.5f);hit.rotation=Quaternion.LookRotation(end-start);
                var target=Target(hit,"Ведите соль от НАЧАЛА к КОНЦУ",null,Vector3.zero,new Vector3(.10f,.06f,Vector3.Distance(start,end)));target.saltLine=lineIndex; target.available=()=>lineIndex==q.State.saltPoints && chosen!=null && chosen.curse!=Curse.None;
            }
            if(q.State.saltPoints<5)
            {
                saltPreview=Group("Salt falling from hand",props,Vector3.zero).gameObject.AddComponent<LineRenderer>();
                saltPreview.useWorldSpace=true;saltPreview.positionCount=2;saltPreview.startWidth=saltPreview.endWidth=.018f;
                saltPreview.sharedMaterial=GuideMaterial();saltPreview.startColor=saltPreview.endColor=new Color(1,.96f,.75f);
                saltPreview.SetPositions(new[]{StarPoint(q.State.saltPoints),StarPoint(q.State.saltPoints)});
            }
            var milk=Group("Milk for ritual",props,anchor+new Vector3(-.85f,.04f,-.5f));SupplyModel(milk,Supply.Milk);
            Label(milk,"МОЛОКО ×"+q.House.State.Count(Supply.Milk),new Vector3(0,.37f,0),.018f);
            var pour=Target(milk,"Налить молоко в чашу",()=>game.WorldAct(()=>q.Ritual(game.SelectedItem,2)),new Vector3(0,.13f,0),new Vector3(.25f,.3f,.25f));pour.available=()=>chosen!=null&&chosen.curse!=Curse.None&&chosen.curse!=Curse.Whisper&&q.State.saltPoints==5&&!q.State.milk&&q.House.State.Count(Supply.Milk)>0;pour.lockedReason="Молоко нужно после соли, для Голода и Холода.";
            var salt=Group("Salt for ritual",props,anchor+new Vector3(.85f,.04f,-.5f));SupplyModel(salt,Supply.Salt);
            Label(salt,"СОЛЬ ×"+q.House.State.Count(Supply.Salt),new Vector3(0,.37f,0),.018f);
            Target(salt,"Соль: проведите мышью по пяти линиям звезды",()=>game.WorldAct(()=>q.Say("Удерживайте левую кнопку мыши и ведите по пунктирным линиям от подсвеченного узла. Один пакет на пентаграмму.")),new Vector3(0,.13f,0),new Vector3(.25f,.3f,.25f));
            if(chosen!=null)
            {
                Part(props,"Ritual stove under bowl",anchor+new Vector3(0,.008f,0),new Vector3(.48f,.025f,.48f),Ink);
                for(int i=0;i<28;i++)
                {
                    float angle=i*Mathf.PI/14;
                    Part(props,"Basin rim",anchor+new Vector3(Mathf.Cos(angle)*.25f,.07f,Mathf.Sin(angle)*.25f),new Vector3(.035f,.08f,.035f),Brass,PrimitiveType.Cylinder);
                }
            }
            if(q.State.milk)
            {
                Part(props,"Milk in basin",anchor+new Vector3(0,.055f,0),new Vector3(.48f,.015f,.48f),new Color(.85f,.85f,.72f),PrimitiveType.Cylinder);
                for(int i=0;i<12;i++)if(q.State.boiled){var glyph=Label(props,"o",anchor+new Vector3(Mathf.Sin(i)*.18f,.2f+i*.02f,Mathf.Cos(i)*.18f),.04f);curseGlyphs.Add(glyph);}
            }
            var stove=Group("Ritual hotplate",props,anchor+new Vector3(.85f,.04f,.1f));
            Part(stove,"Hotplate",Vector3.zero,new Vector3(.35f,.10f,.35f),Ink);
            Part(stove,"Burner",Vector3.up*.06f,new Vector3(.25f,.015f,.25f),q.State.boiled?new Color(.8f,.24f,.04f):Brass,PrimitiveType.Cylinder);
            Label(stove,"НАГРЕВ",new Vector3(0,.2f,0),.02f);
            var heat=Target(stove,"Прогреть чашу с молоком",()=>game.WorldAct(()=>q.Ritual(game.SelectedItem,3)),Vector3.zero,new Vector3(.38f,.15f,.38f));heat.available=()=>chosen!=null&&chosen.curse==Curse.Cold&&q.State.milk&&!q.State.boiled;heat.lockedReason="Нагрев нужен только для Холода, после молока.";
            Sign(props,"УБРАТЬ СОЛЬ",anchor+new Vector3(-1.1f,.2f,.1f),()=>game.WorldAct(()=>q.Ritual(q.State.ritualId==0?game.SelectedItem:q.State.ritualId,0)),.6f);
            Sign(props,"ДО РАССВЕТА →",anchor+new Vector3(0,.18f,-1),()=>game.WorldAct(q.Close),.9f);
            if(q.State.saltPoints<5)
            {
                Label(props,"НАЧАЛО →",StarPoint(q.State.saltPoints)+Vector3.up*.12f,.025f);
                Label(props,"КОНЕЦ",StarPoint((q.State.saltPoints+1)%5)+Vector3.up*.12f,.022f);
            }
        }
        void ShowCurse(ItemState item,Vector3 anchor)
        {
            if(item.curse==Curse.None || (!item.identified && game.Quest.State.phase!=DayPhase.Night))return;
            var letters=item.curse==Curse.Cold?"*+x":item.curse==Curse.Whisper?"НЕСПИ":"O:0";
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var text=Label(props,letters[i%letters.Length].ToString(),anchor+new Vector3(Mathf.Cos(a)*.28f,.16f+(i%4)*.065f,Mathf.Sin(a)*.28f),.012f,false);
                text.color=item.curse==Curse.Cold?new Color(.55f,.8f,1):item.curse==Curse.Hunger?new Color(.82f,.36f,.20f):new Color(.69f,.46f,.82f);curseGlyphs.Add(text);
            }
        }
        public void DrawLabels(VisualElement root)
        {
            overlayLabels.Clear();
            worldLabels.RemoveAll(label=>label==null);
            var layer=new VisualElement { pickingMode=PickingMode.Ignore };
            layer.style.position=Position.Absolute;
            layer.style.left=0;layer.style.right=0;layer.style.top=0;layer.style.bottom=0;
            root.Add(layer);
            foreach(var source in worldLabels)
            {
                if(!source.gameObject.activeInHierarchy)continue;
                var label=new Label(source.text) { pickingMode=PickingMode.Ignore };
                label.style.position=Position.Absolute;
                label.style.translate=new Translate(Length.Percent(-50),Length.Percent(-50));
                label.style.fontSize=source.characterSize>=.06f?24:14;
                label.style.unityTextAlign=TextAnchor.MiddleCenter;
                label.style.whiteSpace=WhiteSpace.Pre;
                label.style.color=source.color;
                label.style.backgroundColor=new Color(.025f,.035f,.03f,.9f);
                label.style.paddingLeft=5;label.style.paddingRight=5;
                label.style.paddingTop=2;label.style.paddingBottom=2;
                layer.Add(label);overlayLabels.Add((source,label));
            }
        }
        void LateUpdate()
        {
            foreach(var entry in overlayLabels)
            {
                var source=entry.source;var label=entry.overlay;
                if(source==null || label.panel==null)continue;
                var screen=view.WorldToScreenPoint(source.transform.position);
                bool visible=!game.IsMenu && source.gameObject.activeInHierarchy && screen.z>0 &&
                    screen.x>=0 && screen.x<=Screen.width && screen.y>=0 && screen.y<=Screen.height;
                label.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;
                if(!visible)continue;
                var point=RuntimePanelUtils.ScreenToPanel(label.panel,new Vector2(screen.x,Screen.height-screen.y));
                label.style.left=point.x;label.style.top=point.y;
            }
        }
        void Update()
        {
            if(game==null||view==null||game.IsMenu){Shader.SetGlobalVector("_QuestLens",Vector4.zero);return;}
            view.transform.position=Vector3.Lerp(view.transform.position,cameraPosition,1-Mathf.Exp(-Time.deltaTime*8));
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,cameraRotation,1-Mathf.Exp(-Time.deltaTime*8));
            if(game.AnimateWorld)for(int i=0;i<curseGlyphs.Count;i++)
            {
                if(curseGlyphs[i]==null)continue;
                var color=curseGlyphs[i].color;color.a=.5f+.5f*Mathf.Sin(Time.time*2+i);curseGlyphs[i].color=color;
                curseGlyphs[i].transform.rotation=view.transform.rotation;
            }
            UpdateOutlines(); hovered=null;
            Shader.SetGlobalVector("_QuestLens",Vector4.zero);
            Hover="";if(Vector3.Distance(view.transform.position,cameraPosition)>.12f)return;var mouse=Mouse.current;if(mouse==null)return;
            if(game.BlocksWorldPointer(mouse.position.ReadValue())){traceProgress=0;return;}
            if(!mouse.leftButton.isPressed)saltGestureLine=-1;
            if(game.Quest.State.phase==DayPhase.Night && mouse.leftButton.isPressed && saltGestureLine>=0 && saltGestureLine!=game.Quest.State.saltPoints)return;
            var ray=view.ScreenPointToRay(mouse.position.ReadValue());
            if(Inspecting && UpdateInspection(ray,mouse))return;
            if(game.Quest.State.phase==DayPhase.Night && mouse.leftButton.isPressed && game.Quest.State.saltPoints<5)
            {
                int next=game.Quest.State.saltPoints;
                var a=StarPoint(next);var b=StarPoint((next+1)%5);
                var plane=new Plane(Vector3.up,a);
                if(plane.Raycast(ray,out float distance))
                {
                    var point=ray.GetPoint(distance);
                    float t=Vector3.Dot(point-a,b-a)/(b-a).sqrMagnitude;
                    if(t>=0 && t<=1 && Vector3.Distance(point,Vector3.Lerp(a,b,t))<.065f)
                    {saltGestureLine=next;TraceSalt(next,point,true);return;}
                }
            }
            if(mouse.leftButton.isPressed && saltGestureLine>=0)return;
            if(!Physics.Raycast(ray,out var hit,15,~0,QueryTriggerInteraction.Collide)){traceProgress=0;return;}
            var target=hit.collider.GetComponentInParent<QuestWorldTarget>();if(target==null || (target.click==null && target.saltLine<0)){traceProgress=0;return;}
            hovered=target;
            Hover=target.CanUse?target.title:target.lockedReason??"Сейчас недоступно";
            if(!target.CanUse)return;
            if(target.saltLine>=0)
            {
                TraceSalt(target.saltLine,hit.point,mouse.leftButton.isPressed);return;
            }
            if(mouse.leftButton.wasPressedThisFrame)target.click?.Invoke();
            if(Inspecting&&mouse.rightButton.isPressed&&target.GetComponent<HouseInteractable>()!=null)
                target.transform.Rotate(view.transform.up,-mouse.delta.ReadValue().x*.4f,Space.World);
        }
        void TraceSalt(int line,Vector3 point,bool held)
        {
            var q=game.Quest;
            if(!held||line!=q.State.saltPoints||game.SelectedItem==0||q.Shop.State.stock.FirstOrDefault(x=>x.id==game.SelectedItem)?.curse==Curse.None){traceProgress=0;traceLine=-1;return;}
            var a=StarPoint(line);var b=StarPoint((line+1)%5);
            float t=Mathf.Clamp01(Vector3.Dot(point-a,b-a)/(b-a).sqrMagnitude);
            // Start at the glowing vertex and follow the whole physical line; a held click is not enough.
            if(traceLine!=line){if(t>.15f)return;traceLine=line;traceProgress=0;}
            if(t>traceProgress+.16f)return;
            traceProgress=Mathf.Max(traceProgress,t);
            if(saltPreview!=null) saltPreview.SetPositions(new[]{a+Vector3.up*.003f,Vector3.Lerp(a,b,traceProgress)+Vector3.up*.003f});
            Hover=$"Соль: линия {line+1}/5 · {traceProgress:P0}";
            if(traceProgress>.9f){traceLine=-1;traceProgress=0;game.WorldAct(()=>q.Ritual(game.SelectedItem,1));}
        }
        void OnDestroy(){Shader.SetGlobalVector("_QuestLens",Vector4.zero);if(knock!=null)Destroy(knock);if(scenery!=null)Destroy(scenery.gameObject);}
    }
}
