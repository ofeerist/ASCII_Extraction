using UnityEngine;
namespace Pawnshop.Endless
{
    public sealed class ChapterArt : ScriptableObject
    {
        public GameObject[] props = new GameObject[10];
    }
    public sealed class FlatVisitor : MonoBehaviour
    {
        static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites = new System.Collections.Generic.Dictionary<string, Sprite>();
        public static Sprite Portrait(string key)
        {
            if(sprites.TryGetValue(key,out var sprite) && sprite!=null)return sprite;
            var texture=Resources.Load<Texture2D>("ChapterPortraits/"+key);
            if(texture==null)return null;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0),texture.height/1.85f);
            sprites[key]=sprite;return sprite;
        }
        public static GameObject Create(Transform parent,string key)
        {
            var root=new GameObject("Flat visitor / "+key);root.transform.SetParent(parent,false);
            var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=Portrait(key);
            root.AddComponent<FlatVisitor>();return root;
        }
        void LateUpdate()
        {
            var camera=Camera.main;if(camera==null)return;
            var forward=transform.position-camera.transform.position;forward.y=0;
            if(forward.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(forward);
        }
    }
}
