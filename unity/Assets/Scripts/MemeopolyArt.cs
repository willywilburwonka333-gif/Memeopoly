using UnityEngine;

namespace Memeopoly
{
    /// <summary>
    /// Original self-contained game art generated on device at startup.
    /// This avoids broken external sprite references and keeps the mobile
    /// player lightweight. Tile icons are bespoke signed-distance drawings.
    /// </summary>
    public static class MemeopolyArt
    {
        private const int N=128;
        private static readonly Color32[] Base =
        {
            new Color32(31,198,148,255),
            new Color32(57,156,241,255),
            new Color32(238,181,58,255),
            new Color32(151,98,242,255),
            new Color32(233,72,103,255),
            new Color32(57,211,209,255),
            new Color32(241,109,180,255),
            new Color32(127,155,232,255)
        };

        public static Texture2D MakeTile(int kind)
        {
            int k=Mathf.Clamp(kind,0,Base.Length-1);
            var pixels=new Color32[N*N];
            Color32 b=Base[k];
            for(int y=0;y<N;y++)
                for(int x=0;x<N;x++)
                {
                    float u=(x+0.5f)/N*2-1;
                    float v=(y+0.5f)/N*2-1;
                    float edge=Mathf.Max(Mathf.Abs(u),Mathf.Abs(v));
                    float radial=Mathf.Sqrt(u*u+v*v);
                    float shine=Mathf.Clamp01(1f-radial*.7f);
                    // Dark outer edge, glowing inner border and diagonal
                    // two-tone shading give every tile visual depth.
                    Color body=new Color(b.r/255f,b.g/255f,b.b/255f);
                    Color shade=Color.Lerp(body,new Color(.025f,.06f,.14f),.33f+.24f*(u+v)*.5f);
                    if(edge>.91f)shade=new Color(.055f,.078f,.16f);
                    else if(edge>.85f)shade=new Color(1f,.85f,.42f);
                    else if(edge>.80f)shade=Color.Lerp(body,Color.white,.30f);
                    else shade=Color.Lerp(shade,Color.white,.08f*shine);
                    bool icon=Icon(k,u,v);
                    if(icon) shade=new Color(.97f,.97f,.88f);
                    pixels[y*N+x]=(Color32)shade;
                }

            var texture=new Texture2D(N,N,TextureFormat.RGBA32,false);
            texture.name="MEMEOPOLY • original tile "+kind;
            texture.filterMode=FilterMode.Bilinear;
            texture.wrapMode=TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            return texture;
        }

        private static bool Box(float x,float y,float cx,float cy,float w,float h)
        {
            return Mathf.Abs(x-cx)<=w && Mathf.Abs(y-cy)<=h;
        }

        private static bool Ring(float u,float v,float radius,float thickness)
        {
            float d=Mathf.Sqrt(u*u+v*v);
            return Mathf.Abs(d-radius)<thickness;
        }

        private static bool Icon(int k,float u,float v)
        {
            // Simple bold symbols still read at 60 tiny spaces on mobile.
            if(k==0) // go viral: tall rocket + upward flight
                return Box(u,v,0,-.07f,.14f,.38f) ||
                    (v>.23f&&v<.56f&&Mathf.Abs(u)<(.55f-v)*.55f) ||
                    Box(u,v,-.25f,-.22f,.10f,.23f) ||
                    Box(u,v,.25f,-.22f,.10f,.23f);
            if(k==1) // coins: embossed circular medallion
                return Ring(u,v,.52f,.07f) ||
                    Ring(u,v,.28f,.065f) ||
                    Box(u,v,0,0,.06f,.27f);
            if(k==2) // property: three stepped city towers
                return Box(u,v,-.37f,-.15f,.17f,.38f) ||
                    Box(u,v,.02f,.07f,.19f,.57f) ||
                    Box(u,v,.41f,-.06f,.16f,.46f);
            if(k==3) // sticker pack: two stacked cards
                return Box(u,v,-.19f,.08f,.35f,.45f) ||
                    Box(u,v,.18f,-.17f,.35f,.40f);
            if(k==4) // strike: lightning cut
                return ((u>-.25f&&u<.13f&&v>0&&v<.65f) ||
                    (u>-.05f&&u<.27f&&v>-.66f&&v<0) ||
                    (v>-.1f&&v<.12f&&u>-.46f&&u<.43f));
            if(k==5) // hype: burst diamond ring
                return (Mathf.Abs(Mathf.Abs(u)+Mathf.Abs(v)-.62f)<.065f) ||
                    Ring(u,v,.17f,.06f);
            if(k==6) // event: star sparks
            {
                float angle=Mathf.Atan2(v,u);
                float r=Mathf.Sqrt(u*u+v*v);
                float star=.28f+.30f*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle*4)),10);
                return r<star || (Mathf.Abs(u)>.67f&&Mathf.Abs(v)<.05f);
            }
            // shield / heist: silhouette of safe + keyhole
            return (Box(u,v,0,0,.45f,.46f) &&
                (!Box(u,v,0,-.02f,.12f,.26f))) ||
                Box(u,v,0,-.51f,.20f,.09f);
        }
    }
}
