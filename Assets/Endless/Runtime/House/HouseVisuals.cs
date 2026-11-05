using System.Collections.Generic;
using UnityEngine;

namespace Pawnshop.Endless
{
    public static class HouseVisuals
    {
        static readonly Dictionary<Material, Material> importedMaterials = new Dictionary<Material, Material>();
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public static Material Material(Color color)
        {
            if (!materials.TryGetValue(color, out var material) || material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                materials[color] = material;
            }
            return material;
        }
        public static GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color, bool collider = false)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider) Object.Destroy(part.GetComponent<Collider>());
            return part;
        }
        public static GameObject Item(ItemState item, Transform parent)
        {
            var root = new GameObject("Trade item " + item.id); root.transform.SetParent(parent, false);
            var gold = new Color(.67f, .47f, .15f); var ivory = new Color(.75f, .71f, .53f); var wood = new Color(.19f, .08f, .045f);
            var art = Resources.Load<ChapterArt>("ChapterArt");
            if (art != null && item.template < art.props.Length && art.props[item.template] != null)
            {
                var model = Object.Instantiate(art.props[item.template], root.transform);
                foreach(var collider in model.GetComponentsInChildren<Collider>()) { collider.enabled = false; Object.Destroy(collider); }
                var renderers=model.GetComponentsInChildren<Renderer>();
                foreach(var renderer in renderers)
                {
                    var mapped=renderer.sharedMaterials;
                    for(int m=0;m<mapped.Length;m++)
                    {
                        var source=mapped[m];
                        if(source==null || source.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                        if(!importedMaterials.TryGetValue(source,out var converted) || converted==null)
                        {
                            converted=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            if(source.HasProperty("_MainTex"))converted.mainTexture=source.GetTexture("_MainTex");
                            if(source.HasProperty("_Color"))converted.color=source.GetColor("_Color");
                            importedMaterials[source]=converted;
                        }
                        mapped[m]=converted;
                    }
                    renderer.sharedMaterials=mapped;
                }
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float size=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                    if(size>.0001f) model.transform.localScale *= .30f/size;
                    bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    model.transform.position += root.transform.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                }
            }
            else if (item.template == 2 || item.template == 7)
            {
                Part(root.transform, "Pages", PrimitiveType.Cube, new Vector3(0,.08f,0), new Vector3(.24f,.1f,.30f), ivory);
                foreach (var y in new[] {.018f,.14f}) Part(root.transform,"Leather cover",PrimitiveType.Cube,new Vector3(0,y,0),new Vector3(.26f,.018f,.32f),wood);
            }
            else if (item.template == 5 || item.template == 1)
            {
                for (var i=0;i<20;i++)
                {
                    var a=i*Mathf.PI*2/20; Part(root.transform,"Ring",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.11f,.07f,Mathf.Sin(a)*.11f),Vector3.one*.035f,gold);
                }
                if(item.template==1) Part(root.transform,"Cameo",PrimitiveType.Sphere,new Vector3(0,.10f,0),new Vector3(.15f,.08f,.20f),ivory);
            }
            else if (item.template == 6)
            {
                Part(root.transform,"Music box",PrimitiveType.Cube,new Vector3(0,.07f,0),new Vector3(.28f,.14f,.24f),wood);
                Part(root.transform,"Brass lid",PrimitiveType.Cube,new Vector3(0,.145f,0),new Vector3(.29f,.018f,.25f),gold);
            }
            else if (item.template == 8)
            {
                foreach(var x in new[]{-.08f,.08f}) { var tube=Part(root.transform,"Lens barrel",PrimitiveType.Cylinder,new Vector3(x,.07f,0),new Vector3(.10f,.12f,.10f),gold);tube.transform.localRotation=Quaternion.Euler(90,0,0); }
            }
            else
            {
                Part(root.transform,"Case",PrimitiveType.Cylinder,new Vector3(0,.04f,0),new Vector3(.28f,.025f,.28f),gold);
                Part(root.transform,"Face",PrimitiveType.Cylinder,new Vector3(0,.068f,0),new Vector3(.24f,.003f,.24f),ivory);
                if(item.template==0 || item.template==4) Part(root.transform,"Pointer",PrimitiveType.Cube,new Vector3(.025f,.075f,0),new Vector3(.10f,.008f,.013f),Color.black);
            }
            var hit = root.AddComponent<BoxCollider>(); hit.center=new Vector3(0,.09f,0);hit.size=new Vector3(.36f,.18f,.36f);
            var interactive=root.AddComponent<HouseInteractable>();interactive.station=HouseStation.Item;interactive.index=item.id;interactive.title=ShopCatalog.Items[item.template].name;
            var locations=new[]{new Vector3(0,.10f,-.20f),new Vector3(.20f,.10f,0),new Vector3(0,.10f,.20f)};
            for(var i=0;i<3;i++)
            {
                var marker=Part(root.transform,"Examination region "+(i+1),PrimitiveType.Sphere,locations[i],Vector3.one*.032f,Color.yellow,true);
                marker.AddComponent<InspectionRegion>().index=i;marker.SetActive(false);
            }
            return root;
        }
        public static GameObject Visitor(CustomerKind kind, Transform parent)
        {
            var root=new GameObject("Waiting visitor");root.transform.SetParent(parent,false);
            Object.Destroy(root);
            return FlatVisitor.Create(parent, kind == CustomerKind.Desperate ? "Boris" : kind == CustomerKind.Dealer ? "Dealer" : kind == CustomerKind.Collector ? "Anna" : "Nina");
        }
    }
}
