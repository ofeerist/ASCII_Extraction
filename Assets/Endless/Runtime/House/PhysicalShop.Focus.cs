using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pawnshop.Endless
{
    public sealed partial class PhysicalShop
    {
        InspectionTrace trace;
        int focusItem;
        float previousFov;
        bool previousPostProcessing;
        float pourProgress;
        Transform focusedModel;
        GameObject ritualDrawing;
        GameObject toolModel;
        GameObject bowl;
        bool ritualFinished;
        public void BeginFocus(bool ritual)
        {
            var id=ritual?Rules.State.ritualId:Rules.State.tableId;
            if(!items.TryGetValue(id,out var model))return;
            if(Game.State.phase!=NightPhase.Reception){message="Examinations and rituals are available before closing.";return;}
            ritualFinished=false;focusItem=id;focusedModel=model.transform;previousFov=view.fieldOfView;previousPostProcessing=cameraData.renderPostProcessing;
            cameraData.renderPostProcessing=false;
            var anchor=ritual?rugAnchor:tableAnchor;
            view.transform.position=anchor.position+(ritual?new Vector3(0,1.65f,-1.15f):new Vector3(-.72f,.38f,0));
            view.transform.LookAt(anchor.position+Vector3.up*.07f);view.fieldOfView=ritual?58:42;
            mode=ritual?"Ritual":"Inspect";trace=new InspectionTrace(ritual?InspectionTool.Loupe:InspectionTool.Hands);pourProgress=0;
            message=ritual?"Trace the salt circle while holding the left mouse button. Each completed stage uses one component.":"Rotate with right drag. Choose a tool with 1–4. Hold left mouse over a surface marker to examine it.";
            if(ritual)BuildRitualDrawing();
            UpdateRegions();SetCursor(false);Draw();
        }
        void EndFocus()
        {
            cameraData.renderPostProcessing=previousPostProcessing;view.fieldOfView=previousFov;
            view.transform.localPosition=new Vector3(0,1.6f,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            if(focusedModel!=null)foreach(var region in focusedModel.GetComponentsInChildren<InspectionRegion>(true))region.gameObject.SetActive(false);
            if(toolModel!=null)Destroy(toolModel);
            focusedModel=null;focusItem=0;
        }
        public void SelectTool(InspectionTool tool)
        {
            if(mode!="Inspect")return;
            var blocked=Rules.CanInspect(tool);
            if(blocked!=null&&tool!=InspectionTool.Hands){message=blocked;return;}
            trace=new InspectionTrace(tool,Game.State.expert);message=tool==InspectionTool.Hands?"Rotate with right drag. Wheel adjusts the camera zoom.":"Hold left mouse over each marker. Rotate the object to expose hidden regions.";
            if(toolModel!=null)Destroy(toolModel);
            toolModel=new GameObject("Active inspection tool");toolModel.transform.SetParent(transform,false);
            if(tool==InspectionTool.Loupe)
            {
                for(var i=0;i<24;i++){var a=i*Mathf.PI*2/24;HouseVisuals.Part(toolModel.transform,"Loupe rim",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.055f,0,Mathf.Sin(a)*.055f),Vector3.one*.009f,new Color(.65f,.52f,.28f));}
                HouseVisuals.Part(toolModel.transform,"Handle",PrimitiveType.Cube,new Vector3(0,0,-.10f),new Vector3(.02f,.015f,.10f),Color.black);
                view.fieldOfView=32;
            }
            else if(tool==InspectionTool.Ultraviolet)
            {
                HouseVisuals.Part(toolModel.transform,"UV flashlight",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.035f,.07f,.035f),new Color(.15f,.13f,.2f));
                var light=toolModel.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.45f,.05f,1);light.intensity=1.4f;light.range=.8f;
                view.fieldOfView=42;
            }
            else if(tool==InspectionTool.GoldReagent) HouseVisuals.Part(toolModel.transform,"Reagent dropper",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.025f,.055f,.025f),new Color(.52f,.27f,.09f));
            else toolModel.SetActive(false);
            UpdateRegions();Draw();
        }
        void UpdateRegions()
        {
            if(focusedModel==null)return;
            var show=mode=="Inspect"?trace.Tool!=InspectionTool.Hands:Rules.State.milkPoured;
            foreach(var region in focusedModel.GetComponentsInChildren<InspectionRegion>(true))
            {
                region.gameObject.SetActive(show&&region.index<trace.Required);
                if(region.gameObject.activeSelf)region.GetComponent<Renderer>().sharedMaterial=HouseVisuals.Material(trace.Progress(region.index)>=1?Color.green:trace.Tool==InspectionTool.Ultraviolet?new Color(.65f,.12f,1):new Color(1,.65f,.12f));
            }
        }
        void BuildRitualDrawing()
        {
            if(ritualDrawing!=null)Destroy(ritualDrawing);
            ritualDrawing=new GameObject("Ritual marks");ritualDrawing.transform.position=rugAnchor.position;
            for(var i=0;i<8;i++)
            {
                var segment=new GameObject("Salt section "+i);segment.transform.SetParent(ritualDrawing.transform,false);
                var line=segment.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=9;line.startWidth=.012f;line.endWidth=.012f;
                line.sharedMaterial=HouseVisuals.Material((Rules.State.saltMask&(1<<i))!=0?Color.white:new Color(.24f,.20f,.13f));
                for(var p=0;p<=8;p++){var angle=(i+p/8f)*Mathf.PI/4;line.SetPosition(p,new Vector3(Mathf.Cos(angle)*.62f,.012f,Mathf.Sin(angle)*.62f));}
            }
            bowl=HouseVisuals.Part(ritualDrawing.transform,"Offering bowl",PrimitiveType.Cylinder,new Vector3(-.35f,.045f,-.17f),new Vector3(.19f,.035f,.19f),Rules.State.milkPoured?Color.white:new Color(.15f,.09f,.06f));
        }
        void RedrawRitual()
        {
            if(ritualDrawing!=null)Destroy(ritualDrawing);BuildRitualDrawing();UpdateRegions();Draw();
        }
        void UpdateFocus()
        {
            var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null||focusedModel==null)return;
            if(mode=="Ritual"&&ritualFinished)return;
            if(mode=="Inspect"&&keyboard!=null)
            {
                if(keyboard.digit1Key.wasPressedThisFrame)SelectTool(InspectionTool.Hands);
                if(keyboard.digit2Key.wasPressedThisFrame)SelectTool(InspectionTool.Loupe);
                if(keyboard.digit3Key.wasPressedThisFrame)SelectTool(InspectionTool.Ultraviolet);
                if(keyboard.digit4Key.wasPressedThisFrame)SelectTool(InspectionTool.GoldReagent);
            }
            var cursor=mouse.position.ReadValue();
            var scale=Screen.height/720f;
            if(cursor.y<135*scale||cursor.y>Screen.height-85*scale)return;
            if(mouse.rightButton.isPressed)
            {
                var delta=mouse.delta.ReadValue();focusedModel.Rotate(Vector3.up,-delta.x*.45f,Space.World);focusedModel.Rotate(view.transform.right,delta.y*.45f,Space.World);
            }
            if(mode=="Inspect")view.fieldOfView=Mathf.Clamp(view.fieldOfView-mouse.scroll.ReadValue().y*.02f,25,52);
            var ray=view.ScreenPointToRay(cursor);
            if(toolModel!=null)
            {
                var plane=new Plane(-view.transform.forward,focusedModel.position);
                if(plane.Raycast(ray,out var distance)){toolModel.transform.position=ray.GetPoint(distance)-view.transform.forward*.14f;toolModel.transform.up=-view.transform.forward;}
            }
            if(!mouse.leftButton.isPressed)return;
            if(mode=="Ritual"&&!Rules.State.milkPoured)
            {
                var plane=new Plane(Vector3.up,rugAnchor.position);
                if(!plane.Raycast(ray,out var distance))return;
                var local=ray.GetPoint(distance)-rugAnchor.position;
                if(!Rules.State.saltPlaced)
                {
                    var radius=new Vector2(local.x,local.z).magnitude;
                    if(Mathf.Abs(radius-.62f)>.10f)return;
                    var angle=Mathf.Atan2(local.z,local.x);if(angle<0)angle+=Mathf.PI*2;
                    var sector=Mathf.Clamp((int)(angle/(Mathf.PI/4)),0,7);
                    if((Rules.State.saltMask&(1<<sector))!=0)return;
                    var before=Rules.State.saltMask;message=Rules.TraceSalt(sector);
                    if(before!=Rules.State.saltMask){Save();RedrawRitual();}
                }
                else if(new Vector2(local.x+.35f,local.z+.17f).magnitude<.16f)
                {
                    pourProgress+=Mathf.Min(Time.deltaTime,.1f);message=$"Pouring milk... {Mathf.Clamp01(pourProgress/1.5f):P0}";
                    if(pourProgress>=1.5f){message=Rules.PourMilk(true);pourProgress=0;Save();RedrawRitual();}
                }
                return;
            }
            if(trace.Tool==InspectionTool.Hands || trace.Complete)return;
            if(!Physics.Raycast(ray,out var hit,4,~0,QueryTriggerInteraction.Collide))return;
            var region=hit.collider.GetComponent<InspectionRegion>();if(region==null||!region.transform.IsChildOf(focusedModel))return;
            trace.Trace(region.index,Time.deltaTime);UpdateRegions();
            message=$"{(mode=="Ritual"?"Cleansing":trace.Tool.ToString())}: region {region.index+1} {trace.Progress(region.index):P0} — {trace.Completed}/{trace.Required} complete";
            if(!trace.Complete)return;
            message=mode=="Ritual"?Rules.Cleanse(trace):Rules.CompleteInspection(trace,focusItem);
            if(mode=="Ritual"&&!Rules.State.milkPoured)
            {
                ritualFinished=true;
                if(ritualDrawing!=null)Destroy(ritualDrawing);
                UpdateRegions();
            }
            Save();Draw();
        }
        void TakeFocusedItem()
        {
            var id=focusItem;EndFocus();mode="Explore";Act(()=>Rules.PickUp(id));
        }
    }
}
