using UnityEngine;

namespace Memeopoly
{
    /// <summary>
    /// Shared vector-like rounded button/card background, with a true
    /// nine-slice border so its corners aren't stretched on tall phones.
    /// Runtime-generated once; no external textures, fonts or assets.
    /// </summary>
    public static class MemeopolyUIArt
    {
        private static Sprite rounded;
        public static Sprite Rounded
        {
            get
            {
                if(rounded!=null)return rounded;
                const int size=64;
                Color32[] pixels=new Color32[size*size];
                for(int y=0;y<size;y++)
                    for(int x=0;x<size;x++)
                    {
                        float qx=Mathf.Abs(x-31.5f)-19.5f;
                        float qy=Mathf.Abs(y-31.5f)-19.5f;
                        float distance=Mathf.Sqrt(
                            Mathf.Max(0,qx)*Mathf.Max(0,qx)+
                            Mathf.Max(0,qy)*Mathf.Max(0,qy))+
                            Mathf.Min(Mathf.Max(qx,qy),0)-12f;
                        byte alpha=(byte)Mathf.RoundToInt(
                            Mathf.Clamp01(.5f-distance)*255f);
                        pixels[y*size+x]=new Color32(255,255,255,alpha);
                    }
                var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                texture.name="Memeopoly rounded UI nine-slice";
                texture.wrapMode=TextureWrapMode.Clamp;
                texture.filterMode=FilterMode.Bilinear;
                texture.SetPixels32(pixels);
                texture.Apply(false,true);
                rounded=Sprite.Create(texture,new Rect(0,0,size,size),
                    new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,
                    new Vector4(17,17,17,17));
                rounded.name="Memeopoly responsive rounded UI";
                return rounded;
            }
        }
    }
}
