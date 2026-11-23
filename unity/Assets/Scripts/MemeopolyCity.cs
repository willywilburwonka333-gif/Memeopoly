using System;
using System.Collections;
using UnityEngine;

namespace Memeopoly
{
    public sealed class LandmarkHit : MonoBehaviour
    {
        public int index;
    }

    public sealed class SpinProp : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 45, 0);
        private void Update() { transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self); }
    }

    // One continuous 60-space tabletop, not a 60-card UI grid.
    // 3D geometry and stages are built natively so iOS/Android/WebGL see the same world.
    public sealed class MemeopolyCity
    {
        public readonly Vector3[] Path = new Vector3[MemeopolyRules.BoardSpaces];
        public readonly Transform[] Buildings = new Transform[6];
        public readonly Transform[] Dice = new Transform[2];
        public Transform Token { get; private set; }
        public readonly Transform Root;

        private readonly Material[] tileMaterials = new Material[8];
        private readonly Material ocean, pavement, grass, edge, gold, ink, cream, light;
        private readonly Color[] buildingColours =
        {
            new Color(.08f,.73f,.89f), new Color(.20f,.79f,.46f),
            new Color(.98f,.45f,.69f), new Color(.82f,.34f,.96f),
            new Color(1f,.54f,.18f), new Color(.56f,.39f,1f)
        };
        private static readonly Vector3[] Plots =
        {
            new Vector3(-4.85f,0,-3.35f), new Vector3(0,0,-3.35f), new Vector3(4.85f,0,-3.35f),
            new Vector3(-4.85f,0,3.35f), new Vector3(0,0,3.35f), new Vector3(4.85f,0,3.35f)
        };

        public MemeopolyCity(Transform parent)
        {
            Root = new GameObject("THE WHOLE MEMEOPOLY BOARD").transform;
            Root.SetParent(parent, false);
            ocean = Mat(new Color(.055f,.15f,.28f));
            pavement = Mat(new Color(.12f,.16f,.27f));
            grass = Mat(new Color(.07f,.31f,.31f));
            edge = Mat(new Color(.038f,.055f,.11f));
            gold = Mat(new Color(1f,.72f,.26f), .22f);
            ink = Mat(new Color(.045f,.065f,.14f));
            cream = Mat(new Color(1f,.93f,.76f));
            light = Mat(new Color(.4f,.9f,1f),.55f);
            tileMaterials[0] = Mat(new Color(.14f,.91f,.69f),.25f);
            tileMaterials[1] = Mat(new Color(.2f,.63f,.95f),.15f);
            tileMaterials[2] = Mat(new Color(1f,.77f,.27f),.15f);
            tileMaterials[3] = Mat(new Color(.67f,.38f,.98f),.2f);
            tileMaterials[4] = Mat(new Color(.98f,.34f,.42f),.18f);
            tileMaterials[5] = Mat(new Color(.26f,.85f,.85f),.17f);
            tileMaterials[6] = Mat(new Color(.99f,.48f,.7f),.18f);
            tileMaterials[7] = Mat(new Color(.55f,.7f,1f),.18f);
            CreateEnvironment();
            CreateTrack();
            CreatePlots();
            CreateToken();
            CreateDice(0, new Vector3(-1.10f,.95f,-5.6f));
            CreateDice(1, new Vector3(1.10f,.95f,-5.6f));
        }

        private static Material Mat(Color c, float glow=0)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = c;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (glow > 0 && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * glow);
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",.22f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",.36f);
            return mat;
        }

        private static GameObject Shape(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = material;
            // Use a single collider on each landmark base for reliable touch interaction.
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return go;
        }

        private GameObject Cube(string name, Transform parent, Vector3 local, Vector3 size, Material material)
        {
            return Shape(PrimitiveType.Cube,name,parent,local,size,material);
        }
        private GameObject Sphere(string name, Transform parent, Vector3 local, Vector3 size, Material material)
        {
            return Shape(PrimitiveType.Sphere,name,parent,local,size,material);
        }
        private GameObject Cylinder(string name, Transform parent, Vector3 local, Vector3 size, Material material)
        {
            return Shape(PrimitiveType.Cylinder,name,parent,local,size,material);
        }

        private void CreateEnvironment()
        {
            Cube("midnight floating table",Root,new Vector3(0,-.62f,0),new Vector3(18,.72f,18),edge);
            Cube("gold rim",Root,new Vector3(0,-.22f,0),new Vector3(17.2f,.09f,17.2f),gold);
            Cube("ocean gradient base",Root,new Vector3(0,-.15f,0),new Vector3(16.9f,.14f,16.9f),ocean);
            Cube("central park",Root,new Vector3(0,-.065f,0),new Vector3(13.65f,.2f,13.65f),grass);
            // Double lane around the central district keeps the space track visibly continuous.
            for (int i=0;i<4;i++)
            {
                float x = i<2 ? -6.75f : 6.75f;
                if (i<2) Cube("avenue",Root,new Vector3(0,.035f,i==0?-6.75f:6.75f),new Vector3(13.6f,.06f,.50f),pavement);
                else Cube("avenue",Root,new Vector3(x,.035f,0),new Vector3(.50f,.06f,13.6f),pavement);
            }
            for (int i=0;i<24;i++)
            {
                float angle = i * Mathf.PI * 2 / 24;
                float radius = 6.5f;
                var lamp = Sphere("district lantern",Root,new Vector3(Mathf.Cos(angle)*radius,.21f,Mathf.Sin(angle)*radius),
                    Vector3.one*.16f,light);
            }
            // A little height and architectural mass between six landmarks.
            for (int i=0;i<12;i++)
            {
                float x = -6f + (i%6)*2.4f;
                float z = i<6 ? -.17f : .17f;
                Cube("city boulevard line",Root,new Vector3(x,.08f,z),new Vector3(.22f,.04f,.12f),gold);
            }
        }

        private static int KindIndex(string kind)
        {
            switch (kind)
            {
                case "GO": return 0;
                case "COINS": return 1;
                case "LANDMARK": return 2;
                case "CARD": return 3;
                case "ATTACK": return 4;
                case "HYPE": return 5;
                case "EVENT": return 6;
                default: return 7;
            }
        }

        private void CreateTrack()
        {
            for (int i=0;i<60;i++)
            {
                Vector3 p;
                if(i<16) p=new Vector3(-7.5f+i,0,-7.5f);
                else if(i<31) p=new Vector3(7.5f,0,-7.5f+i-15);
                else if(i<46) p=new Vector3(7.5f-(i-30),0,7.5f);
                else p=new Vector3(-7.5f,0,7.5f-(i-45));
                Path[i] = p + new Vector3(0,.34f,0);
                var tileRoot = new GameObject(i.ToString("00")+" "+MemeopolyRules.TileKinds[i]).transform;
                tileRoot.SetParent(Root,false);
                tileRoot.localPosition=p;
                Material tile = tileMaterials[KindIndex(MemeopolyRules.TileKinds[i])];
                Cube("gold border",tileRoot,new Vector3(0,.11f,0),new Vector3(.97f,.24f,.97f),gold);
                Cube("space",tileRoot,new Vector3(0,.25f,0),new Vector3(.85f,.16f,.85f),tile);
                Cube("inset",tileRoot,new Vector3(0,.34f,0),new Vector3(.46f,.03f,.46f),ink);
                // Distinct icon sculptures remain legible even at full-board phone size.
                string k = MemeopolyRules.TileKinds[i];
                if (k=="COINS") Cylinder("coin",tileRoot,new Vector3(0,.48f,0),new Vector3(.22f,.06f,.22f),gold);
                if (k=="GO") Sphere("viral orb",tileRoot,new Vector3(0,.51f,0),Vector3.one*.30f,light);
                if (k=="ATTACK") Cube("rival strike",tileRoot,new Vector3(0,.51f,0),new Vector3(.09f,.24f,.34f),tileMaterials[4]);
                if (k=="CARD") Cube("meme card",tileRoot,new Vector3(0,.50f,0),new Vector3(.31f,.05f,.22f),cream);
                if (k=="SHIELD") Sphere("shield",tileRoot,new Vector3(0,.50f,0),new Vector3(.32f,.19f,.27f),light);
                if (k=="RAID") Cube("loot chest",tileRoot,new Vector3(0,.5f,0),new Vector3(.24f,.16f,.24f),gold);
                if (k=="HYPE") Cylinder("hype booster",tileRoot,new Vector3(0,.49f,0),new Vector3(.09f,.16f,.09f),light);
            }
        }

        private void CreatePlots()
        {
            for(int i=0;i<6;i++)
            {
                var root = new GameObject(MemeopolyRules.Landmarks[i]).transform;
                root.SetParent(Root,false);
                root.localPosition=Plots[i];
                Buildings[i]=root;
                Cube("plot foundation",root,new Vector3(0,.08f,0),new Vector3(3.8f,.22f,4.15f),gold);
                Cube("city block",root,new Vector3(0,.19f,0),new Vector3(3.59f,.20f,3.95f),Mat(Color.Lerp(buildingColours[i],ink.color,.68f)));
                // Touch anywhere on the plot even when no building has been constructed.
                GameObject colliderGo = new GameObject("TAP TO BUILD");
                colliderGo.transform.SetParent(root,false);
                colliderGo.transform.localPosition=new Vector3(0,2.6f,0);
                BoxCollider bc=colliderGo.AddComponent<BoxCollider>();
                bc.size=new Vector3(3.7f,6.0f,3.95f);
                colliderGo.AddComponent<LandmarkHit>().index=i;
                for (int corner=0;corner<4;corner++)
                {
                    float x=corner%2==0?-1.67f:1.67f, z=corner<2?-1.84f:1.84f;
                    Cylinder("neon post",root,new Vector3(x,.48f,z),new Vector3(.07f,.32f,.07f),light);
                }
                RenderLandmark(i,0,false);
            }
        }

        public void RenderLandmark(int index, int level, bool animate)
        {
            if(index<0||index>=6) return;
            Transform baseRoot = Buildings[index];
            Transform previous=baseRoot.Find("VISIBLE BUILDING STAGE");
            if(previous!=null)
            {
                previous.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(previous.gameObject);
            }
            var stage=new GameObject("VISIBLE BUILDING STAGE").transform;
            stage.SetParent(baseRoot,false);
            Material primary = Mat(buildingColours[index],.28f);
            Material secondary = Mat(Color.Lerp(buildingColours[index],cream.color,.48f),.09f);
            if(level==0)
            {
                Cube("empty build site - visibly unbuilt",stage,new Vector3(0,.37f,0),
                    new Vector3(2.72f,.12f,2.8f),pavement);
                for(int i=0;i<4;i++)
                {
                    float z=i<2?-1.16f:1.16f, x=i%2==0?-1.1f:1.1f;
                    Cube("future floor-plan marker",stage,new Vector3(x,.47f,z),new Vector3(.23f,.07f,.23f),secondary);
                }
            }
            else
            {
                float height = .68f + level * .46f;
                Cube("main animated floor structure",stage,new Vector3(0,.43f+height/2,0),
                    new Vector3(2.65f,height,2.45f),primary);
                Cube("white facade",stage,new Vector3(0,.44f+height*.62f,-1.25f),
                    new Vector3(2.20f,height*.49f,.12f),secondary);
                Cube("gold roof trim",stage,new Vector3(0,.48f+height,0),
                    new Vector3(2.93f,.19f,2.78f),gold);
                // Every upgrade visibly adds another floor and architectural details.
                for (int floor=0;floor<level;floor++)
                {
                    float y=.68f+floor*.45f;
                    for (int window=0;window<3;window++)
                    {
                        float x=-.73f+window*.73f;
                        Cube("lit window",stage,new Vector3(x,y,-1.33f),
                            new Vector3(.36f,.22f,.04f),light);
                        Cube("rear window",stage,new Vector3(x,y,1.26f),
                            new Vector3(.36f,.19f,.045f),secondary);
                    }
                }
                // Six entirely different silhouettes, not six recoloured identical buildings.
                if (index==0) // Sharko Surf Shack: cresting wave and a shark fin.
                {
                    Sphere("surf wave",stage,new Vector3(-.7f,.58f+height,.2f),
                        new Vector3(1.55f,.56f,.9f),light);
                    var fin=Cylinder("shark fin",stage,new Vector3(.64f,.87f+height,.12f),
                        new Vector3(.38f,.47f,.25f),primary);
                    fin.transform.localRotation=Quaternion.Euler(0,0,24);
                }
                if (index==1) // Crocwing Academy: runway tower with propeller.
                {
                    Cylinder("flight control tower",stage,new Vector3(0,.71f+height,0),
                        new Vector3(.55f,.36f,.55f),secondary);
                    Cube("aircraft wing",stage,new Vector3(0,.94f+height,0),
                        new Vector3(3.1f,.15f,.55f),gold);
                    var prop=Cylinder("spinning propeller",stage,new Vector3(0,1.12f+height,-.42f),
                        new Vector3(.12f,.70f,.12f),light);
                    prop.transform.localRotation=Quaternion.Euler(90,0,0);
                    prop.AddComponent<SpinProp>().degreesPerSecond=new Vector3(0,0,140);
                }
                if (index==2) // Prima Mocha Café: rooftop cup + steam.
                {
                    Cylinder("giant coffee cup",stage,new Vector3(0,.84f+height,0),
                        new Vector3(.66f,.45f,.66f),cream);
                    Cylinder("coffee rim",stage,new Vector3(0,1.30f+height,0),
                        new Vector3(.75f,.09f,.75f),gold);
                    for(int i=0;i<3;i++) Sphere("coffee steam",stage,
                        new Vector3(-.34f+i*.34f,1.72f+height+(i%2)*.22f,0),
                        Vector3.one*.15f,light);
                }
                if (index==3) // Viral Video Tower: giant broadcast panels.
                {
                    Cube("broadcast screen",stage,new Vector3(0,.6f+height,-.45f),
                        new Vector3(2.3f,.95f,.12f),ink);
                    Cube("screen light",stage,new Vector3(0,.6f+height,-.53f),
                        new Vector3(1.84f,.63f,.06f),light);
                    Cylinder("antenna",stage,new Vector3(0,1.26f+height,0),
                        new Vector3(.08f,.75f,.08f),gold);
                    Sphere("viral beacon",stage,new Vector3(0,2.1f+height,0),
                        Vector3.one*.24f,tileMaterials[6]);
                }
                if (index==4) // Meme Factory: twin chimneys and conveyor roof.
                {
                    for(int chimney=0;chimney<2;chimney++)
                    {
                        Cylinder("industrial stack",stage,new Vector3(chimney==0?-.82f:.82f,1.04f+height,0),
                            new Vector3(.30f,.70f,.30f),secondary);
                        Sphere("glowing steam",stage,new Vector3(chimney==0?-.82f:.82f,1.87f+height,0),
                            Vector3.one*.25f,light);
                    }
                    Cube("production conveyor",stage,new Vector3(0,.49f,-1.46f),
                        new Vector3(1.6f,.25f,.45f),gold);
                }
                if (index==5) // Algorithm Palace: turrets, crown and star.
                {
                    for(int x=-1;x<=1;x+=2)
                        for(int z=-1;z<=1;z+=2)
                        {
                            Cylinder("royal turret",stage,new Vector3(x*1.06f,.71f+height,z*.98f),
                                new Vector3(.32f,.55f,.32f),secondary);
                            Sphere("turret cap",stage,new Vector3(x*1.06f,1.34f+height,z*.98f),
                                new Vector3(.64f,.34f,.64f),gold);
                        }
                    Cylinder("central crown",stage,new Vector3(0,.84f+height,0),
                        new Vector3(.66f,.53f,.66f),gold);
                    Sphere("palace gem",stage,new Vector3(0,1.42f+height,0),Vector3.one*.38f,light);
                }
                if(level>=3)
                {
                    Sphere("upgrade halo",stage,new Vector3(0,height+1.2f,0),
                        new Vector3(1.35f,.13f,1.35f),gold);
                }
                if(level==5)
                {
                    Cylinder("max stage monument",stage,new Vector3(0,height+1.66f,0),
                        new Vector3(.12f,.54f,.12f),gold);
                    Sphere("max stage award",stage,new Vector3(0,height+2.18f,0),Vector3.one*.42f,light);
                    Sphere("max stage glow",stage,new Vector3(0,height+2.53f,0),Vector3.one*.18f,gold);
                }
            }
            if (animate && level>0)
                Root.GetComponentInParent<MemeopolyGame>().StartCoroutine(Construct(stage,index));
        }

        private IEnumerator Construct(Transform stage,int index)
        {
            Transform plot=Buildings[index];
            var crane=new GameObject("TEMPORARY CRANE - visibly builds the landmark").transform;
            crane.SetParent(plot,false);
            Material cranePaint=Mat(new Color(1f,.82f,.22f),.16f);
            Cube("tower",crane,new Vector3(1.9f,2.9f,1.6f),
                new Vector3(.16f,5.5f,.16f),cranePaint);
            Transform arm=new GameObject("animated crane arm").transform;
            arm.SetParent(crane,false);
            arm.localPosition=new Vector3(1.9f,5.5f,1.6f);
            Cube("overhead boom",arm,new Vector3(-1.45f,0,0),
                new Vector3(3.4f,.17f,.18f),cranePaint);
            Cube("hanging winch",arm,new Vector3(-2.5f,-.73f,0),
                new Vector3(.06f,1.5f,.06f),ink);
            Sphere("shiny construction hook",arm,new Vector3(-2.5f,-1.5f,0),
                Vector3.one*.28f,gold);
            var particles=new GameObject("gold construction sparks").AddComponent<ParticleSystem>();
            particles.transform.SetParent(plot,false);
            particles.transform.localPosition=new Vector3(0,1.0f,0);
            var main=particles.main;
            main.duration=1.3f;
            main.startLifetime=.78f;
            main.startSpeed=4.0f;
            main.startSize=.14f;
            main.gravityModifier=.55f;
            main.startColor=new ParticleSystem.MinMaxGradient(
                new Color(1f,.9f,.42f), new Color(.35f,1f,.94f));
            main.maxParticles=120;
            var emission=particles.emission;
            emission.rateOverTime=0;
            var shape=particles.shape;
            shape.shapeType=ParticleSystemShapeType.Box;
            shape.scale=new Vector3(2f,.3f,2f);
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

            float duration=1.15f, elapsed=0;
            Vector3 initial=stage.localScale;
            stage.localScale=new Vector3(.8f,.02f,.8f);
            bool exploded=false;
            while(elapsed<duration && stage!=null)
            {
                elapsed+=Time.deltaTime;
                float t=Mathf.Clamp01(elapsed/duration);
                float rising=Mathf.SmoothStep(.02f,1f,Mathf.Min(1,t/.82f));
                float width = t<.82f?Mathf.Lerp(.8f,1.04f,t/.82f):
                    Mathf.Lerp(1.04f,1f,(t-.82f)/.18f);
                stage.localScale=Vector3.Scale(initial,new Vector3(width,rising,width));
                arm.localRotation=Quaternion.Euler(0,Mathf.Sin(t*Mathf.PI*2)*25f,0);
                if(!exploded&&t>.52f)
                {
                    exploded=true;
                    particles.Emit(70);
                }
                yield return null;
            }
            if(stage!=null) stage.localScale=initial;
            UnityEngine.Object.Destroy(crane.gameObject);
            UnityEngine.Object.Destroy(particles.gameObject,1.5f);
        }

        private void CreateToken()
        {
            var token = new GameObject("Original 3D SHARKO CREATOR PLAYER").transform;
            token.SetParent(Root,false);
            Token=token;
            Sphere("soft ground shadow",token,new Vector3(0,-.03f,0),
                new Vector3(.67f,.05f,.62f),ink);
            Sphere("sharko body",token,new Vector3(0,.45f,0),
                new Vector3(.55f,.69f,.45f),tileMaterials[1]);
            Sphere("sharko belly",token,new Vector3(0,.34f,-.20f),
                new Vector3(.36f,.37f,.13f),cream);
            Sphere("head",token,new Vector3(0,.93f,-.04f),
                new Vector3(.57f,.51f,.52f),tileMaterials[1]);
            for (int side=-1;side<=1;side+=2)
            {
                Sphere("eye",token,new Vector3(side*.17f,1.01f,-.29f),
                    Vector3.one*.14f,cream);
                Sphere("pupil",token,new Vector3(side*.17f,1.01f,-.36f),
                    Vector3.one*.07f,ink);
            }
            var fin=Cylinder("signature blue shark fin",token,new Vector3(0,1.36f,.05f),
                new Vector3(.18f,.29f,.26f),tileMaterials[1]);
            fin.transform.localRotation=Quaternion.Euler(0,0,22);
            Token.localPosition=Path[0]+new Vector3(0,.12f,0);
        }

        private void CreateDice(int index, Vector3 position)
        {
            var root=new GameObject("TRUE DICE "+(index+1)).transform;
            root.SetParent(Root,false);
            root.localPosition=position;
            Dice[index]=root;
            Cube("ivory dice",root,Vector3.zero,Vector3.one*.90f,cream);
            // Opposite face pairs 1/6, 2/5, 3/4. The final top face
            // is a rotation of this ACTUAL pip layout, not a fake '1' sprite.
            Face(root,1,Vector3.up,Quaternion.Euler(90,0,0));
            Face(root,6,Vector3.down,Quaternion.Euler(-90,0,0));
            Face(root,2,Vector3.forward,Quaternion.identity);
            Face(root,5,Vector3.back,Quaternion.Euler(0,180,0));
            Face(root,3,Vector3.right,Quaternion.Euler(0,90,0));
            Face(root,4,Vector3.left,Quaternion.Euler(0,-90,0));
        }

        private void Face(Transform root,int value,Vector3 normal,Quaternion facing)
        {
            // Face-local x/y coordinates are mapped onto cube normals.
            Vector3[] pips = PipPositions(value);
            for(int i=0;i<pips.Length;i++)
            {
                Vector3 v=facing*new Vector3(pips[i].x,pips[i].y,0);
                Sphere("pip "+value+"."+i,root,normal*.472f+v,
                    Vector3.one*.105f,ink);
            }
        }

        private static Vector3[] PipPositions(int value)
        {
            Vector3 a=new Vector3(-.23f,.23f,0),b=new Vector3(.23f,-.23f,0),
                    c=new Vector3(-.23f,-.23f,0),d=new Vector3(.23f,.23f,0),
                    e=new Vector3(0,0,0),f=new Vector3(-.23f,0,0),g=new Vector3(.23f,0,0);
            if (value==1) return new[]{e};
            if (value==2) return new[]{a,b};
            if (value==3) return new[]{a,e,b};
            if (value==4) return new[]{a,b,c,d};
            if (value==5) return new[]{a,b,c,d,e};
            return new[]{a,b,c,d,f,g};
        }

        public static Quaternion DiceTop(int value)
        {
            switch(value)
            {
                case 1:return Quaternion.identity;
                case 6:return Quaternion.Euler(180,0,0);
                case 2:return Quaternion.Euler(-90,0,0);
                case 5:return Quaternion.Euler(90,0,0);
                case 3:return Quaternion.Euler(0,0,90);
                default:return Quaternion.Euler(0,0,-90);
            }
        }

        public void SetDice(int a,int b)
        {
            Dice[0].rotation=DiceTop(a);
            Dice[1].rotation=DiceTop(b);
        }

        public void SetToken(int index) { Token.localPosition=Path[index]+new Vector3(0,.11f,0); }
    }
}