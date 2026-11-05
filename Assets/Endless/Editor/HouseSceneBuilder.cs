using System;
using System.Linq;
using Pawnshop.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pawnshop.Endless.Editor
{
    public static class HouseSceneBuilder
    {
        public const string ScenePath="Assets/Endless/HouseShop.unity";
        [MenuItem("Tools/Pawnshop/Open 3D House")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)Create();
            EditorSceneManager.OpenScene(ScenePath);
        }
        public static string Create()
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)return "House scene already exists. Open it to adjust station transforms.";
            var source=SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if(!source.IsValid())source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Additive);
            if(!EditorSceneManager.SaveScene(source,ScenePath,true))throw new InvalidOperationException("Could not copy the house scene.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform Find(string path)
            {
                var item=all.FirstOrDefault(x=>Path(x)==path);
                if(item==null)throw new InvalidOperationException("Missing house object: "+path);
                return item;
            }
            foreach(var transform in all)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                var mesh=transform.GetComponent<MeshFilter>();
                if(mesh!=null&&mesh.sharedMesh!=null&&transform.GetComponent<Collider>()==null)
                {var collider=transform.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh.sharedMesh;}
                var director=transform.GetComponent<PlayableDirector>();if(director!=null)UnityEngine.Object.DestroyImmediate(director);
            }
            var host=new GameObject("Physical Pawnshop");
            Transform Anchor(string name,Vector3 position,Quaternion rotation)
            {var obj=new GameObject(name);obj.transform.SetParent(host.transform);obj.transform.SetPositionAndRotation(position,rotation);return obj.transform;}
            void Station(Transform target,HouseStation station,string title,int index=0)
            {var component=target.gameObject.AddComponent<HouseInteractable>();component.station=station;component.title=title;component.index=index;}
            var camera=Find("Main Camera").GetComponent<Camera>();
            var playerObject=new GameObject("Player");playerObject.transform.position=new Vector3(1.5f,.12f,2.1f);
            var controller=playerObject.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.23f;controller.center=new Vector3(0,.9f,0);controller.stepOffset=.2f;controller.skinWidth=.03f;
            camera.transform.SetParent(playerObject.transform,false);camera.transform.localPosition=new Vector3(0,1.6f,0);camera.nearClipPlane=.035f;camera.farClipPlane=80;camera.fieldOfView=70;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var ui=host.AddComponent<UIDocument>();ui.panelSettings=AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Endless/UI/EndlessPanel.asset");
            var game=host.AddComponent<PhysicalShop>();game.player=controller;game.view=camera;
            game.theme=AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Endless/UI/Endless.uss");game.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Endless/UI/Fonts/NotoSansMono-Regular.ttf");
            game.dialogues=EndlessDialogueBuilder.Create();
            game.door=Find("House/Walls/wall_with_big_door/big_door_left");
            GameObjectUtility.SetStaticEditorFlags(game.door.gameObject, 0);
            Station(game.door,HouseStation.Door,"Answer the front door");Station(Find("House/Walls/wall_with_big_door/big_door_right"),HouseStation.Door,"Answer the front door");
            game.tableAnchor=Anchor("Inspection point",new Vector3(4.05f,.94f,3.06f),Quaternion.identity);
            Station(Find("House/Assets/photo_frame_desk"),HouseStation.Desk,"Use the inspection desk");
            Find("House/Assets/photo_3").position=new Vector3(4.4f,.92f,2.73f);
            Station(Find("House/Assets/phone"),HouseStation.Supplier,"Call the night supplier");
            var rug=Find("House/Assets/carpet_2 (1)");Station(rug,HouseStation.RitualRug,"Use the ritual rug");
            game.rugAnchor=Anchor("Ritual point",new Vector3(1.47f,.11f,8f),Quaternion.identity);
            game.visitorSpawn=Anchor("Visitor arrival",new Vector3(2f,.05f,-3.3f),Quaternion.identity);
            game.visitorStop=Anchor("Doorstep",new Vector3(2f,.05f,-.48f),Quaternion.identity);
            var porch=GameObject.CreatePrimitive(PrimitiveType.Cube);porch.name="Front step";porch.transform.SetParent(host.transform);porch.transform.position=new Vector3(1.58f,-.05f,-1.8f);porch.transform.localScale=new Vector3(3.2f,.16f,3.9f);
            var porchLight=new GameObject("Doorstep lamp");porchLight.transform.SetParent(host.transform);porchLight.transform.position=new Vector3(2f,2.1f,-.8f);var light=porchLight.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.8f,.65f,.40f);light.intensity=1.5f;light.range=4;
            var deskLight=new GameObject("Inspection lamp");deskLight.transform.SetParent(host.transform);deskLight.transform.position=new Vector3(3.8f,1.7f,3.05f);var taskLight=deskLight.AddComponent<Light>();taskLight.type=LightType.Point;taskLight.color=new Color(1,.87f,.66f);taskLight.intensity=.65f;taskLight.range=1.6f;
            var shelf=Find("House/Assets/bookshelf").GetComponent<Renderer>().bounds;
            var second=Find("House/Assets/bookshelf (1)").GetComponent<Renderer>().bounds;
            foreach(var item in all.Where(x=>x.parent!=null&&Path(x.parent)=="House/Assets"))
            {
                if(item.name.StartsWith("bookshelf"))continue;
                var render=item.GetComponent<Renderer>();
                if(render!=null&&(shelf.Contains(render.bounds.center)||second.Contains(render.bounds.center)))item.gameObject.SetActive(false);
            }
            game.shelfAnchors=new Transform[16];
            var heights=new[]{.18f,.98f,1.37f,1.73f};
            for(var i=0;i<16;i++)
            {
                var pos=new Vector3(-.36f,heights[i/4],9.43f+i%4*.40f);
                game.shelfAnchors[i]=Anchor("Shelf position "+(i+1),pos,Quaternion.identity);
                var tag=new GameObject("Shelf tag "+(i+1));tag.transform.SetParent(host.transform);tag.transform.position=new Vector3(.015f,pos.y+.08f,pos.z);
                var collider=tag.AddComponent<BoxCollider>();collider.size=new Vector3(.07f,.21f,.34f);
                Station(tag.transform,HouseStation.Shelf,"Shelf position "+(i+1),i);
                Label(tag.transform,(i+1).ToString("00"),new Vector3(.05f,-.02f,0),Quaternion.Euler(0,-90,0),.022f);
            }
            game.isolationAnchor=Anchor("Isolation position",new Vector3(3.35f,.98f,12.15f),Quaternion.identity);
            var isolationTag=new GameObject("Isolation tag");isolationTag.transform.SetParent(host.transform);isolationTag.transform.position=new Vector3(3.35f,1.05f,11.9f);var isolationCollider=isolationTag.AddComponent<BoxCollider>();isolationCollider.size=new Vector3(.55f,.22f,.08f);Station(isolationTag.transform,HouseStation.Shelf,"Isolation shelf",ShopSimulation.IsolationSlot);
            Label(isolationTag.transform,"ISOLATION",new Vector3(-.23f,0,-.05f),Quaternion.identity,.018f);
            var ledger=GameObject.CreatePrimitive(PrimitiveType.Cube);ledger.name="Protected ledger";ledger.transform.SetParent(host.transform);ledger.transform.position=new Vector3(4.38f,.955f,3.13f);ledger.transform.localScale=new Vector3(.18f,.03f,.22f);Station(ledger.transform,HouseStation.Ledger,"Read the protected ledger");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(x=>x.path!=ScenePath)).ToArray();
            foreach(var other in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray())
                if(other!=scene&&!other.isDirty)EditorSceneManager.CloseScene(other,true);
            return ScenePath;
        }
        static void Label(Transform parent,string text,Vector3 offset,Quaternion rotation,float size)
        {
            var obj=new GameObject("Label");obj.transform.SetParent(parent,false);obj.transform.localPosition=offset;obj.transform.localRotation=rotation;
            var label=obj.AddComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=size;label.color=new Color(.9f,.8f,.55f);label.anchor=TextAnchor.MiddleCenter;
        }
        static string Path(Transform transform)=>transform.parent==null?transform.name:Path(transform.parent)+"/"+transform.name;
    }
}
