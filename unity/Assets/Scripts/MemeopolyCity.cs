using System;
using System.Collections;
using System.Collections.Generic;
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

    // Subtle ambient animation without per-frame material allocations or physics.
    public sealed class OrbitProp : MonoBehaviour
    {
        public float degreesPerSecond=18f;
        private void Update()
        {
            transform.Rotate(0,degreesPerSecond*Time.deltaTime,0,Space.Self);
        }
    }

    public sealed class FloatProp : MonoBehaviour
    {
        public float amplitude=.10f;
        public float speed=2.1f;
        private Vector3 start;
        private void Start() { start=transform.localPosition; }
        private void Update()
        {
            transform.localPosition=start+Vector3.up*(Mathf.Sin(Time.time*speed)*amplitude);
            transform.Rotate(0,Time.deltaTime*8f,0,Space.Self);
        }
    }

    // One continuous 60-space tabletop, not a 60-card UI grid.
    // 3D geometry and stages are built natively so iOS/Android/WebGL see the same world.
    public sealed class MemeopolyCity
    {
        public readonly Vector3[] Path = new Vector3[MemeopolyRules.BoardSpaces];
        public readonly Transform[] Buildings = new Transform[6];
        private readonly Transform[] selectionFrames = new Transform[6];
        public readonly Transform[] Dice = new Transform[2];
        public Transform Token { get; private set; }
        private readonly Transform[] tokenModels = new Transform[3];
        private static readonly Dictionary<int,Material> materialCache = new Dictionary<int,Material>();
        public readonly Transform Root;

        private readonly Material[] tileMaterials = new Material[8];
        private readonly Material[] trackSurfaceMaterials = new Material[8];
        private readonly Material ocean, pavement, grass, edge, gold, ink, cream, light;
        private static readonly Color[] WorldOcean =
        {
            new Color(.055f,.15f,.28f),new Color(.09f,.16f,.32f),
            new Color(.10f,.10f,.31f),new Color(.14f,.07f,.29f),
            new Color(.065f,.15f,.26f),new Color(.11f,.11f,.21f),
            new Color(.14f,.06f,.22f),new Color(.10f,.06f,.27f)
        };
        private static readonly Color[] WorldGrass =
        {
            new Color(.07f,.31f,.31f),new Color(.09f,.29f,.28f),
            new Color(.08f,.28f,.34f),new Color(.24f,.11f,.29f),
            new Color(.07f,.29f,.30f),new Color(.17f,.22f,.28f),
            new Color(.20f,.13f,.27f),new Color(.19f,.17f,.29f)
        };
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
            // Eight original procedural tile icon materials. The 3D character
            // paint is kept untextured; only the board's upper surfaces use icons.
            for(int k=0;k<trackSurfaceMaterials.Length;k++)
            {
                trackSurfaceMaterials[k]=new Material(tileMaterials[k]);
                trackSurfaceMaterials[k].name="Memeopoly tile "+k;
                trackSurfaceMaterials[k].color=Color.white;
                Texture2D icon=MemeopolyArt.MakeTile(k);
                if(trackSurfaceMaterials[k].HasProperty("_MainTex"))
                    trackSurfaceMaterials[k].SetTexture("_MainTex",icon);
                if(trackSurfaceMaterials[k].HasProperty("_BaseMap"))
                    trackSurfaceMaterials[k].SetTexture("_BaseMap",icon);
            }
            CreateEnvironment();
            CreateTrack();
            CreatePlots();
            CreateToken();
            CreateDice(0, new Vector3(-1.10f,.95f,-5.6f));
            CreateDice(1, new Vector3(1.10f,.95f,-5.6f));
        }

        private static Material Mat(Color c, float glow=0)
        {
            int key = (Mathf.RoundToInt(Mathf.Clamp01(c.r)*255)<<24) |
                (Mathf.RoundToInt(Mathf.Clamp01(c.g)*255)<<16) |
                (Mathf.RoundToInt(Mathf.Clamp01(c.b)*255)<<8) |
                Mathf.RoundToInt(Mathf.Clamp01(glow)*255);
            Material cached;
            if (materialCache.TryGetValue(key,out cached)) return cached;
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
            materialCache[key] = mat;
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

        private GameObject Roof(string name,Transform parent,Vector3 local,
            float width,float height,float depth,Material material)
        {
            // Real six-vertex custom prism mesh, rather than a scaled cube.
            // Roof line changes across building silhouettes and upgrade levels.
            float w=width*.5f,d=depth*.5f;
            Vector3[] points={
                new Vector3(-w,0,-d),new Vector3(w,0,-d),new Vector3(0,height,-d),
                new Vector3(-w,0,d),new Vector3(w,0,d),new Vector3(0,height,d)
            };
            int[] triangles={
                0,2,1, 3,4,5,        // triangular gables
                0,1,4, 0,4,3,        // underside
                0,3,5, 0,5,2,        // roof slope A
                1,2,5, 1,5,4         // roof slope B
            };
            var mesh=new Mesh();
            mesh.name="Original sloped roof";
            mesh.vertices=points;
            mesh.triangles=triangles;
            mesh.RecalculateNormals();
            var obj=new GameObject(name);
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=local;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial=material;
            return obj;
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
            // A recognisable centrepiece, visible from the locked mobile camera.
            // Empty world 1 should resemble a living game board, not a blank slab.
            var plaza=new GameObject("CENTRAL VIRAL PLAZA").transform;
            plaza.SetParent(Root,false);
            Cylinder("golden plaza ring",plaza,new Vector3(0,.09f,0),
                new Vector3(2.6f,.10f,2.6f),gold);
            Cylinder("plaza interior",plaza,new Vector3(0,.17f,0),
                new Vector3(2.35f,.08f,2.35f),ink);
            Cylinder("meme monument base",plaza,new Vector3(0,.36f,0),
                new Vector3(.95f,.26f,.95f),pavement);
            Cylinder("meme monument pillar",plaza,new Vector3(0,1.05f,0),
                new Vector3(.34f,.7f,.34f),gold);
            Sphere("meme beacon",plaza,new Vector3(0,1.85f,0),
                new Vector3(.85f,.84f,.85f),light);
            var satellites=new GameObject("ORBITING VIRAL SATELLITES").transform;
            satellites.SetParent(plaza,false);
            satellites.localPosition=new Vector3(0,1.6f,0);
            satellites.gameObject.AddComponent<OrbitProp>().degreesPerSecond=19f;
            for(int j=0;j<4;j++)
            {
                float a=j*Mathf.PI*.5f;
                Sphere("floating meme orb "+j,satellites,
                    new Vector3(Mathf.Cos(a)*1.62f,(j%2)*.36f,Mathf.Sin(a)*1.62f),
                    new Vector3(.26f,.30f,.26f),tileMaterials[(j*2)%8]);
            }
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6f;
                Vector3 p=new Vector3(Mathf.Cos(a)*2.0f,.26f,Mathf.Sin(a)*2.0f);
                Cube("circular LED",plaza,p,new Vector3(.27f,.10f,.27f),tileMaterials[i%8]);
            }
            for(int i=0;i<4;i++)
            {
                float x=i<2?-2.75f:2.75f;
                float z=i%2==0?-1.65f:1.65f;
                Cylinder("decorative city tree trunk",Root,new Vector3(x,.34f,z),
                    new Vector3(.11f,.42f,.11f),gold);
                Sphere("decorative city tree canopy",Root,new Vector3(x,.92f,z),
                    new Vector3(.68f,.83f,.68f),tileMaterials[(i+1)%8]);
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
                Material tile = trackSurfaceMaterials[KindIndex(MemeopolyRules.TileKinds[i])];
                Cube("gold border",tileRoot,new Vector3(0,.11f,0),new Vector3(.97f,.24f,.97f),gold);
                Cube("space",tileRoot,new Vector3(0,.25f,0),new Vector3(.85f,.16f,.85f),tile);
                // Keep the centre clear so the original textured tile icon stays visible.
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

                // A visible glowing rectangle identifies the currently tapped
                // plot without requiring users to decipher the bottom HUD.
                Transform frame=new GameObject("SELECTION • lit construction border").transform;
                frame.SetParent(root,false);
                Cube("top light",frame,new Vector3(0,.36f,2.05f),
                    new Vector3(3.85f,.10f,.12f),light);
                Cube("bottom light",frame,new Vector3(0,.36f,-2.05f),
                    new Vector3(3.85f,.10f,.12f),light);
                Cube("left light",frame,new Vector3(-1.91f,.36f,0),
                    new Vector3(.12f,.10f,4.08f),light);
                Cube("right light",frame,new Vector3(1.91f,.36f,0),
                    new Vector3(.12f,.10f,4.08f),light);
                selectionFrames[i]=frame;
                frame.gameObject.SetActive(false);
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
                // Before purchase, show a proper construction-ready foundation:
                // players can see what will transform when they press BUILD.
                Cube("foundation blueprints",stage,new Vector3(0,.47f,0),
                    new Vector3(2.48f,.035f,2.45f),Mat(Color.Lerp(buildingColours[index],ink.color,.40f)));
                for(int edgeIndex=0;edgeIndex<4;edgeIndex++)
                {
                    float x=edgeIndex%2==0?-1.12f:1.12f;
                    Cube("blueprint scaffolding pole",stage,
                        new Vector3(x,.82f,edgeIndex<2?-1.07f:1.07f),
                        new Vector3(.07f,.85f,.07f),secondary);
                }
                Cube("future landmark billboard",stage,new Vector3(0,1.15f,0),
                    new Vector3(1.45f,.42f,.08f),primary);
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
                // Real staged construction: each level adds a gold structural
                // beam and a second illuminated floor marker to the model.
                for(int floor=0;floor<level;floor++)
                {
                    float beamY=.73f+floor*.46f;
                    Cube("stage floor edge",stage,new Vector3(0,beamY,1.26f),
                        new Vector3(2.82f,.09f,.10f),gold);
                    for(int s=-1;s<=1;s+=2)
                        Cube("stage edge LED",stage,
                            new Vector3(s*1.33f,beamY+.09f,0),
                            new Vector3(.065f,.13f,2.56f),light);
                }
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
                // Distinct roofline geometry per land plot, increasingly
                // pronounced from stage 2 to 5 rather than a single taller box.
                if(index==1)
                    Roof("Crocwing hangar A-frame",stage,
                        new Vector3(0,height+.60f,.44f),2.25f,.62f,1.55f,secondary);
                else if(index==2)
                {
                    Roof("Mocha café pitched roof",stage,
                        new Vector3(0,height+.64f,.40f),2.3f,.50f,1.4f,primary);
                    Cylinder("mocha rooftop bean",stage,
                        new Vector3(.7f,height+1.31f,.10f),
                        new Vector3(.14f,.30f,.14f),gold);
                }
                else if(index==4)
                {
                    Roof("Meme factory sawtooth",stage,
                        new Vector3(-.58f,height+.61f,0),1.35f,.66f,2.2f,ink);
                    Roof("Meme factory sawtooth two",stage,
                        new Vector3(.58f,height+.61f,0),1.35f,.66f,2.2f,secondary);
                }
                else if(index==5)
                    Roof("Algorithm Palace royal gable",stage,
                        new Vector3(0,height+.67f,0),2.8f,.63f,2.70f,secondary);

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

        public void ApplyWorldTheme(int worldIndex)
        {
            int world=Mathf.Abs(worldIndex)%WorldOcean.Length;
            ocean.color=WorldOcean[world];
            grass.color=WorldGrass[world];
        }

        public void HighlightSelection(int chosen)
        {
            for(int i=0;i<selectionFrames.Length;i++)
            {
                if(selectionFrames[i]!=null)
                    selectionFrames[i].gameObject.SetActive(i==chosen);
            }
        }

        // Three original selectable 3D characters. Every avatar is constructed
        // from Unity meshes so the Unity Cloud build needs NO external asset store.
        private void CreateToken()
        {
            Token=new GameObject("PLAYER AVATAR • saved skin").transform;
            Token.SetParent(Root,false);
            Token.localPosition=Path[0]+Vector3.up*.11f;
            Sphere("shadow under hero",Token,new Vector3(0,-.07f,0),
                new Vector3(.78f,.05f,.73f),ink);

            Transform shark=new GameObject("SHARKO • mascot").transform;
            shark.SetParent(Token,false);
            tokenModels[0]=shark;
            shark.gameObject.AddComponent<FloatProp>().amplitude=.075f;
            Material blue=tileMaterials[1];
            Sphere("sharko body",shark,new Vector3(0,.43f,0),
                new Vector3(.72f,.76f,.57f),blue);
            Sphere("sharko belly",shark,new Vector3(0,.31f,-.30f),
                new Vector3(.49f,.47f,.15f),cream);
            Sphere("friendly shark head",shark,new Vector3(0,1.02f,-.06f),
                new Vector3(.78f,.69f,.67f),blue);
            Sphere("shark snout",shark,new Vector3(0,.87f,-.42f),
                new Vector3(.52f,.26f,.35f),cream);
            for(int side=-1;side<=1;side+=2)
            {
                Sphere("eye socket",shark,new Vector3(side*.25f,1.09f,-.36f),
                    Vector3.one*.21f,cream);
                Sphere("eye pupil",shark,new Vector3(side*.25f,1.09f,-.47f),
                    Vector3.one*.105f,ink);
                var sidefin=Sphere("hero side fin",shark,new Vector3(side*.48f,.58f,.06f),
                    new Vector3(.45f,.16f,.30f),blue);
                sidefin.transform.localRotation=Quaternion.Euler(0,0,side*30);
            }
            var fin=Cylinder("hero dorsal fin",shark,new Vector3(0,1.49f,.14f),
                new Vector3(.24f,.31f,.38f),blue);
            fin.transform.localRotation=Quaternion.Euler(0,0,18);
            Cube("sharko golden collar",shark,new Vector3(0,.66f,-.33f),
                new Vector3(.47f,.10f,.11f),gold);

            Transform croc=new GameObject("CROCWING • flying crocodile").transform;
            croc.SetParent(Token,false);
            tokenModels[1]=croc;
            croc.gameObject.AddComponent<FloatProp>().amplitude=.14f;
            Material green=tileMaterials[0];
            Sphere("croc belly",croc,new Vector3(0,.49f,0),
                new Vector3(.77f,.69f,.60f),green);
            Sphere("croc grinning head",croc,new Vector3(0,1.01f,-.16f),
                new Vector3(.82f,.47f,.73f),green);
            Sphere("long croc snout",croc,new Vector3(0,.81f,-.56f),
                new Vector3(.62f,.21f,.53f),cream);
            for(int side=-1;side<=1;side+=2)
            {
                Sphere("eye crest",croc,new Vector3(side*.23f,1.22f,-.37f),
                    new Vector3(.23f,.27f,.23f),green);
                Sphere("eye",croc,new Vector3(side*.23f,1.22f,-.50f),
                    Vector3.one*.13f,ink);
                var wing=Cube("emerald flyer wing",croc,
                    new Vector3(side*.75f,.60f,.09f),new Vector3(1.0f,.10f,.51f),green);
                wing.transform.localRotation=Quaternion.Euler(0,0,side*20f);
            }
            var tail=Sphere("swishing tail",croc,new Vector3(0,.36f,.55f),
                new Vector3(.29f,.25f,.86f),green);
            tail.transform.localRotation=Quaternion.Euler(16,0,0);
            Cube("croc golden pilot goggles",croc,new Vector3(0,1.34f,-.20f),
                new Vector3(.68f,.08f,.25f),gold);

            Transform cat=new GameObject("VIRAL CAT • streamer").transform;
            cat.SetParent(Token,false);
            tokenModels[2]=cat;
            cat.gameObject.AddComponent<FloatProp>().amplitude=.065f;
            Material pink=tileMaterials[6];
            Sphere("cat body",cat,new Vector3(0,.45f,0),
                new Vector3(.68f,.74f,.52f),pink);
            Sphere("cat face",cat,new Vector3(0,1.02f,-.13f),
                new Vector3(.79f,.68f,.65f),pink);
            for(int side=-1;side<=1;side+=2)
            {
                var ear=Cube("pointy cat ear",cat,
                    new Vector3(side*.36f,1.51f,-.07f),
                    new Vector3(.27f,.40f,.22f),pink);
                ear.transform.localRotation=Quaternion.Euler(0,0,side*-20);
                Sphere("cat eye",cat,new Vector3(side*.22f,1.03f,-.47f),
                    new Vector3(.13f,.18f,.10f),ink);
                Cube("cat whisker",cat,new Vector3(side*.44f,.81f,-.49f),
                    new Vector3(.38f,.025f,.025f),cream);
            }
            Sphere("cat nose",cat,new Vector3(0,.89f,-.52f),
                new Vector3(.12f,.10f,.10f),gold);
            var tail2=Cylinder("cat tail",cat,new Vector3(.41f,.53f,.49f),
                new Vector3(.12f,.50f,.12f),pink);
            tail2.transform.localRotation=Quaternion.Euler(32,0,-30);
            Cube("streamer gold headset",cat,new Vector3(0,1.50f,-.06f),
                new Vector3(.65f,.09f,.24f),gold);

            SelectToken(0);
        }

        public void SelectToken(int index)
        {
            int chosen=Mathf.Clamp(index,0,tokenModels.Length-1);
            for(int i=0;i<tokenModels.Length;i++)
                if(tokenModels[i]!=null)tokenModels[i].gameObject.SetActive(i==chosen);
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