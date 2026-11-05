using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pawnshop.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Pawnshop.Endless
{
    public sealed partial class PhysicalShop : MonoBehaviour
    {
        public bool questMode = true;
        public CharacterController player;
        public Camera view;
        public Transform door;
        public Transform tableAnchor;
        public Transform rugAnchor;
        public Transform visitorSpawn;
        public Transform visitorStop;
        public Transform[] shelfAnchors;
        public Transform isolationAnchor;
        public StyleSheet theme;
        public Font font;
        public DialogueAsset[] dialogues;
        public string SaveDirectoryOverride { get; set; }
        public ShopSimulation Game { get; private set; }
        public HouseRules Rules { get; private set; }
        public string Mode => mode;
        RunStorage storage;
        PlayerProfile profile;
        VisualElement root;
        Label statusLabel;
        Label promptLabel;
        Label messageLabel;
        string mode="Menu";
        string message="WASD to move. Mouse to look. E to interact. Escape to pause.";
        string confirmText;
        Action confirmation;
        int seed=12345;
        int offer;
        int selected;
        int salePrice;
        float yaw;
        float pitch;
        float verticalSpeed;
        float saveTimer;
        Quaternion closedDoor;
        GameObject visitor;
        int visibleVisitor;
        bool arrived;
        AudioSource sound;
        AudioClip knock;
        readonly Dictionary<int,GameObject> items=new Dictionary<int,GameObject>();
        GameObject heldVisual;
        UniversalAdditionalCameraData cameraData;
        HouseInteractable aimed;
        void OnEnable()
        {
            if (questMode)
            {
                enabled = false;
                if (player != null) player.enabled = false;
                var quest = GetComponent<QuestGame>();
                if (quest == null) quest = gameObject.AddComponent<QuestGame>();
                quest.Initialize(font, theme, SaveDirectoryOverride);
                return;
            }
            storage=new RunStorage(string.IsNullOrEmpty(SaveDirectoryOverride)?Application.persistentDataPath:SaveDirectoryOverride);
            profile=storage.LoadProfile();
            var saved=storage.LoadRun();
            if(saved!=null) { Game=new ShopSimulation(saved);Rules=new HouseRules(Game); }
            if(!string.IsNullOrEmpty(storage.Notice)) message=storage.Notice;
            root=GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(theme);root.style.unityFontDefinition=FontDefinition.FromFont(font);
            root.pickingMode=PickingMode.Ignore;
            closedDoor=door.localRotation;cameraData=view.GetUniversalAdditionalCameraData();
            sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;
            var samples=new float[22050];
            for(var i=0;i<samples.Length;i++) { var t=i%7350;samples[i]=(float)(Math.Sin(t*.06)*Math.Exp(-t/350.0)*.14); }
            knock=AudioClip.Create("Three knocks",samples.Length,1,44100,false);knock.SetData(samples,0);
            Draw();SetCursor(false);
        }
        void OnDisable() { Save();SetCursor(false);if(knock!=null)Destroy(knock); }
        void OnApplicationQuit() => Save();
        void OnApplicationFocus(bool focused) { if(!focused && mode=="Explore") Open("Pause"); }
        void SetCursor(bool locked) { Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked; }
        void Save()
        {
            if(storage==null||profile==null)return;
            try
            {
                if(Game!=null)
                {
                    if(player!=null && (mode=="Explore" || mode=="Pause"))
                    {
                        var h=Rules.State;var p=player.transform.position;h.playerX=p.x;h.playerY=p.y;h.playerZ=p.z;h.yaw=yaw;h.pitch=pitch;h.hasPosition=true;
                    }
                    storage.SaveRun(Game.State);RunStorage.UpdateRecords(profile,Game.State,Game.Capital);
                }
                storage.SaveProfile(profile);
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException) { message="SAVE FAILED: "+e.Message; }
        }
        public void StartRun(int runSeed)
        {
            if(mode=="Inspect"||mode=="Ritual")EndFocus();
            if(visitor!=null)Destroy(visitor);visitor=null;visibleVisitor=-1;arrived=false;
            Game=ShopSimulation.NewRun(runSeed);Rules=new HouseRules(Game);seed=runSeed;
            RestorePlayer();mode="Explore";message="Someone is approaching the front door. Supplies can be ordered from the phone on the desk.";
            RefreshWorld();Save();Draw();SetCursor(true);
        }
        void RestorePlayer()
        {
            var h=Rules.State;player.enabled=false;
            player.transform.position=h.hasPosition?new Vector3(h.playerX,h.playerY,h.playerZ):new Vector3(1.5f,.12f,2.1f);
            yaw=h.yaw;pitch=h.pitch;player.transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localPosition=new Vector3(0,1.6f,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);player.enabled=true;
        }
        void Open(string panel)
        {
            if(mode=="Inspect"||mode=="Ritual") EndFocus();
            mode=panel;confirmText=null;confirmation=null;SetCursor(panel=="Explore");Draw();
        }
        void Act(Func<string> action)
        {
            var prior=Game.Customer?.item.id??0;
            message=action();Rules.Synchronize();
            if((Game.Customer?.item.id??0)!=prior) { offer=Game.Customer?.ask??0;if(mode=="Door")mode="Explore"; }
            RefreshWorld();Save();Draw();SetCursor(mode=="Explore");
        }
        void Update()
        {
            if(Game==null)return;
            UpdateVisitor();
            door.localRotation=Quaternion.Slerp(door.localRotation,closedDoor*Quaternion.Euler(0,Rules.State.doorOpen?-100:0,0),Time.deltaTime*5);
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {
                if(confirmation!=null){confirmation=null;confirmText=null;Draw();}
                else Open(mode=="Explore"?"Pause":"Explore");
                return;
            }
            if(mode=="Explore")
            {
                MovePlayer();
                aimed=null;
                if(Physics.Raycast(view.transform.position,view.transform.forward,out var hit,2.5f,~0,QueryTriggerInteraction.Collide)) aimed=hit.collider.GetComponentInParent<HouseInteractable>();
                if(promptLabel!=null)promptLabel.text=aimed==null?"": "[E] "+aimed.title;
                if(Keyboard.current?.eKey.wasPressedThisFrame==true&&aimed!=null) Interact(aimed);
                saveTimer+=Time.deltaTime;if(saveTimer>5){saveTimer=0;Save();}
            }
            else if(mode=="Inspect"||mode=="Ritual") UpdateFocus();
            if(statusLabel!=null)statusLabel.text=$"NIGHT {Game.State.night:000}   CASH {Game.State.cash}   DUE {Game.State.due}   ACTIONS {Game.State.actions}";
            if(messageLabel!=null)messageLabel.text=message;
        }
        void MovePlayer()
        {
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            var keyboard=Keyboard.current;var mouse=Mouse.current;if(keyboard==null||mouse==null)return;
            var delta=mouse.delta.ReadValue();yaw+=delta.x*.11f;pitch=Mathf.Clamp(pitch-delta.y*.11f,-75,75);
            player.transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var input=new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
            if(input.sqrMagnitude>1)input.Normalize();
            if(player.isGrounded&&verticalSpeed<0)verticalSpeed=-2;else verticalSpeed-=18*Time.deltaTime;
            player.Move((player.transform.right*input.x*2.1f+player.transform.forward*input.y*2.1f+Vector3.up*verticalSpeed)*Time.deltaTime);
            if(player.transform.position.y < -3) { player.enabled=false;player.transform.position=new Vector3(1.5f,.12f,2.1f);player.enabled=true;verticalSpeed=0; }
        }
        public void Interact(HouseInteractable target)
        {
            if(Game==null||target==null)return;
            switch(target.station)
            {
                case HouseStation.Door:
                    if(Game.Customer==null){message="No more visitors tonight. Close the night at the ledger.";break;}
                    if(!arrived){message="The visitor is still approaching.";break;}
                    Rules.State.doorOpen=true;offer=Game.Customer.accepted?Game.Customer.acceptedPrice:Game.Customer.ask;Save();Open("Door");break;
                case HouseStation.Supplier:Open("Supplier");break;
                case HouseStation.Ledger:Open("Ledger");break;
                case HouseStation.Desk:
                    if(Rules.State.carriedId!=0)Act(Rules.PlaceOnTable);
                    else if(Rules.State.tableId!=0)BeginFocus(false);
                    else message="Bring the visitor's item to this desk.";
                    break;
                case HouseStation.RitualRug:
                    if(Rules.State.carriedId!=0)Act(Rules.PlaceOnRug);
                    else if(Rules.State.ritualId!=0)BeginFocus(true);
                    else message="Place a purchased item on the rug to perform a ritual.";
                    break;
                case HouseStation.Shelf:
                    if(target.index!=ShopSimulation.IsolationSlot&&target.index>=Game.Capacity){message="Buy a shelf improvement from the supplier to use this space.";break;}
                    if(Rules.State.carriedId!=0)Act(()=>Rules.PlaceOnShelf(target.index));
                    else
                    {
                        var item=Game.State.stock.FirstOrDefault(x=>x.slot==target.index&&Rules.IsOnShelf(x));
                        if(item==null){message="Empty shelf position.";break;}
                        selected=item.id;salePrice=item.askingPrice;Open("Shelf");
                    }
                    break;
                case HouseStation.Item:
                    if(target.index==Rules.State.tableId)BeginFocus(false);
                    else if(target.index==Rules.State.ritualId)BeginFocus(true);
                    else {var item=Rules.Find(target.index);if(item!=null){selected=item.id;salePrice=item.askingPrice;Open("Shelf");}}
                    break;
            }
        }
        public void RefreshWorld()
        {
            if(Rules==null)return;
            Rules.Synchronize();
            foreach(var item in items.Values)if(item!=null)Destroy(item);items.Clear();
            if(heldVisual!=null)Destroy(heldVisual);
            var all=Game.State.stock.ToList();if(Game.Customer!=null&&Rules.State.itemBorrowed)all.Add(Game.Customer.item);
            foreach(var item in all)
            {
                if(item.id==Rules.State.carriedId)
                {
                    heldVisual=HouseVisuals.Item(item,view.transform);heldVisual.transform.localPosition=new Vector3(.22f,-.27f,.47f);heldVisual.transform.localScale=Vector3.one*.65f;
                    foreach(var collider in heldVisual.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                    continue;
                }
                Transform anchor=null;
                if(item.id==Rules.State.tableId)anchor=tableAnchor;
                else if(item.id==Rules.State.ritualId)anchor=rugAnchor;
                else if(Rules.Owned(item.id))anchor=item.slot==ShopSimulation.IsolationSlot?isolationAnchor:shelfAnchors[item.slot];
                if(anchor==null)continue;
                var model=HouseVisuals.Item(item,anchor);items[item.id]=model;
                if(anchor==tableAnchor || anchor==rugAnchor)model.transform.localRotation=Quaternion.identity;
            }
            if(ritualDrawing!=null)Destroy(ritualDrawing);
            if(Rules.State.ritualId!=0 && (Rules.State.saltMask!=0 || Rules.State.milkPoured))BuildRitualDrawing();
            var id=Game.Customer?.item.id??0;
            if(id!=visibleVisitor)
            {
                if(visitor!=null)Destroy(visitor);
                visibleVisitor=id;arrived=false;
                if(id!=0){visitor=HouseVisuals.Visitor(Game.Customer.kind,transform);visitor.transform.position=visitorSpawn.position;visitor.transform.rotation=visitorSpawn.rotation;}
            }
        }
        void UpdateVisitor()
        {
            if(visitor==null||arrived)return;
            visitor.transform.position=Vector3.MoveTowards(visitor.transform.position,visitorStop.position,Time.deltaTime*.65f);
            if(Vector3.Distance(visitor.transform.position,visitorStop.position)<.02f){arrived=true;if(profile.sound)sound.PlayOneShot(knock);message="Three knocks at the front door.";}
        }
    }
}
