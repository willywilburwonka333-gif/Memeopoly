using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Memeopoly
{
    // Drop this component into any scene and press Play: it constructs
    // the entire playable city, camera, lighting and touch-friendly HUD.
    public sealed class MemeopolyGame : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void MemeopolyImportBrowserSave(string gameObjectName);
#endif
        private GameSave save;
        private MemeopolyCity city;
        private Camera boardCamera;
        private readonly System.Random random = new System.Random();
        private bool playing;
        private bool diceSettled=true;
        private int selected;
        private int multiplierIndex;
        private readonly int[] multipliers = {1,2,5,10};
        private Font font;
        private Canvas canvas;
        private CanvasScaler hudScaler;
        private Text title, resources, status, selection, upgradeInfo, diceInfo, worldText;
        private Button rollButton, buildButton, nextButton, multiplierButton;
        private float lastRegenCheck;
        private int previousScreenWidth, previousScreenHeight;
        private RectTransform safeRegion;
        private string toast = "Tap a landmark to build your empire.";

        private static readonly Color Deep = new Color(.035f,.05f,.11f,.96f);
        private static readonly Color Navy = new Color(.07f,.105f,.205f,.94f);
        private static readonly Color Gold = new Color(1f,.75f,.30f,1);
        private static readonly Color Aqua = new Color(.28f,1f,.82f,1);
        private static readonly Color Pale = new Color(.94f,.96f,1f,1);

        private void Awake()
        {
            gameObject.name="Memeopoly";
            save = GamePersistence.Load();
            SetupCameraAndLight();
            city = new MemeopolyCity(transform);
            city.SetToken(save.position);
            city.SetDice(save.lastDieA,save.lastDieB);
            for (int i=0;i<6;i++) city.RenderLandmark(i,save.buildings[i],false);
            BuildHUD();
            ApplySelection(0);
            FitWholeBoard();
            RefreshHUD();
        }

        private void Start()
        {
            // Safe, one-time migration on WebGL: only if Unity has no save yet.
            // The original browser save is never deleted or overwritten.
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!GamePersistence.HasUnitySave()) MemeopolyImportBrowserSave(gameObject.name);
#endif
        }

        public void ReceiveLegacySave(string json)
        {
            if (GamePersistence.HasUnitySave()) return;
            GameSave imported;
            if (!MemeopolyRules.TryImportLegacy(json,NowMs(),out imported)) return;
            save = MemeopolyRules.Sanitise(imported,NowMs());
            for(int i=0;i<6;i++) city.RenderLandmark(i,save.buildings[i],false);
            city.SetToken(save.position);
            city.SetDice(save.lastDieA,save.lastDieB);
            Persist();
            Notify("Existing Memeopoly browser save imported safely.");
            RefreshHUD();
        }

        private static long NowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        private void SetupCameraAndLight()
        {
            Camera existing=Camera.main;
            if(existing!=null) existing.gameObject.SetActive(false);
            GameObject cam = new GameObject("Entire board · locked isometric camera");
            cam.transform.SetParent(transform,false);
            boardCamera=cam.AddComponent<Camera>();
            boardCamera.tag="MainCamera";
            boardCamera.orthographic=true;
            boardCamera.clearFlags=CameraClearFlags.SolidColor;
            boardCamera.backgroundColor=new Color(.025f,.035f,.085f,1);
            boardCamera.nearClipPlane=.10f;
            boardCamera.farClipPlane=120f;
            cam.transform.position=new Vector3(25,33,-25);
            cam.transform.LookAt(new Vector3(0,.35f,0));

            GameObject sun=new GameObject("Premium softbox sun");
            sun.transform.SetParent(transform,false);
            Light key=sun.AddComponent<Light>();
            key.type=LightType.Directional;
            key.intensity=1.65f;
            key.color=new Color(.98f,.89f,.76f);
            sun.transform.rotation=Quaternion.Euler(48,-36,0);
            key.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.42f,.49f,.65f);
            RenderSettings.fog=false;
        }

        // Projection-based auto-fit: no zoom, panning, tile reflow, or board flashing.
        private void FitWholeBoard()
        {
            if (boardCamera == null) return;

            // HUD is separate from the board camera. No game geometry may be
            // hidden beneath the top status strip or the bottom roll/build dock.
            const float bottom = .202f;
            const float top = .840f;
            boardCamera.rect = new Rect(0, bottom, 1, top - bottom);

            // On an embedded landscape web player, scaling UI by screen WIDTH
            // produced enormous clipped buttons. Scale by HEIGHT instead.
            if (hudScaler != null)
                hudScaler.matchWidthOrHeight =
                    Screen.width > Screen.height ? 1f : 0f;

            float aspect = (float)Math.Max(1,Screen.width) /
                (Math.Max(1,Screen.height)*(top-bottom));
            boardCamera.aspect = Mathf.Max(.45f,aspect);

            // Reset the camera before calculating its bounds; this is called
            // again when Safari rotates/resizes. It must not drift every time.
            boardCamera.transform.position=new Vector3(26,35,-26);
            boardCamera.transform.LookAt(new Vector3(0,1.1f,0));

            // Project the full elevated city into camera space, not merely
            // its flat ground plane. Both dimensions must fit inside the viewport.
            Vector2 min = new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            for (int x=-1; x<=1; x+=2)
                for (int z=-1; z<=1; z+=2)
                    for (int y=0; y<=1; y++)
                    {
                        Vector3 point=new Vector3(x*8.65f,y*5.3f,z*8.65f);
                        Vector3 projected=boardCamera.transform.InverseTransformPoint(point);
                        min=Vector2.Min(min,new Vector2(projected.x,projected.y));
                        max=Vector2.Max(max,new Vector2(projected.x,projected.y));
                    }
            Vector2 centre=(min+max)*.5f;
            boardCamera.transform.position +=
                boardCamera.transform.right*centre.x +
                boardCamera.transform.up*centre.y;

            float halfWidth=(max.x-min.x)*.5f;
            float halfHeight=(max.y-min.y)*.5f;
            boardCamera.orthographicSize =
                Mathf.Max(halfHeight,halfWidth/boardCamera.aspect)*1.045f;

            previousScreenWidth=Screen.width;
            previousScreenHeight=Screen.height;
            UpdateSafeArea();
        }

        private void Update()
        {
            if (Screen.width!=previousScreenWidth || Screen.height!=previousScreenHeight)
                FitWholeBoard();
            if (Time.unscaledTime-lastRegenCheck>3)
            {
                lastRegenCheck=Time.unscaledTime;
                int before=save.dice;
                MemeopolyRules.RegenerateDice(save,NowMs());
                if (before!=save.dice) { Persist(); RefreshHUD(); }
            }
            if(playing) return;
            if(Input.touchCount>0)
            {
                Touch touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began &&
                   !(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                    TouchWorld(touch.position);
            }
            else if(Input.GetMouseButtonDown(0) &&
                !(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject()))
                    TouchWorld(Input.mousePosition);
        }

        private void TouchWorld(Vector2 screenPosition)
        {
            RaycastHit hit;
            if(!Physics.Raycast(boardCamera.ScreenPointToRay(screenPosition),out hit,100f))return;
            LandmarkHit marker=hit.collider.GetComponent<LandmarkHit>();
            if(marker!=null)
            {
                ApplySelection(marker.index);
                RefreshHUD();
            }
        }

        private void ApplySelection(int index)
        {
            selected=Mathf.Clamp(index,0,5);
            if(selection!=null) selection.text=MemeopolyRules.Landmarks[selected];
            if(city!=null)city.HighlightSelection(selected);
        }

        private static Font LoadFont()
        {
            Font result=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return result!=null?result:Font.CreateDynamicFontFromOSFont("Arial",24);
        }

        private void BuildHUD()
        {
            font=LoadFont();
            if(EventSystem.current==null)
            {
                GameObject ev=new GameObject("Memeopoly EventSystem");
                ev.transform.SetParent(transform,false);
                ev.AddComponent<EventSystem>();
                ev.AddComponent<StandaloneInputModule>();
            }
            GameObject ui=new GameObject("MEMEOPOLY • responsive compact phone HUD",typeof(RectTransform));
            ui.transform.SetParent(transform,false);
            canvas=ui.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=50;
            hudScaler=ui.AddComponent<CanvasScaler>();
            hudScaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution=new Vector2(430,900);
            hudScaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            hudScaler.matchWidthOrHeight=Screen.width>Screen.height?1f:0f;
            ui.AddComponent<GraphicRaycaster>();
            safeRegion=Rect("SAFE AREA",ui.transform,
                Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            UpdateSafeArea();

            // The header and toast occupy fixed *non-overlapping* compact
            // strips at the top of the screen, never board-relative anchors.
            RectTransform header=Panel("TOP • economy and world",safeRegion,
                new Vector2(0,1),new Vector2(1,1),
                new Vector2(0,-92),Vector2.zero,Deep);
            title=Label("MEMEOPOLY",header,new Vector2(.045f,.56f),
                new Vector2(.57f,.96f),23,Gold,TextAnchor.MiddleLeft,true);
            worldText=Label("",header,new Vector2(.56f,.56f),
                new Vector2(.965f,.96f),13,Aqua,TextAnchor.MiddleRight,true);
            resources=Label("",header,new Vector2(.045f,.07f),
                new Vector2(.965f,.58f),13,Pale,TextAnchor.MiddleLeft,true);

            RectTransform info=Panel("TOAST • below top bar",safeRegion,
                new Vector2(0,1),new Vector2(1,1),
                new Vector2(10,-129),new Vector2(-10,-96),Navy);
            status=Label("",info,new Vector2(.03f,.04f),
                new Vector2(.97f,.96f),12,Pale,TextAnchor.MiddleCenter,true);

            // All controls share a compact 162-unit dock. Previous prototype
            // had an enormous 202-unit dock plus a free-floating message
            // that visibly covered the city on small iPhone previews.
            RectTransform dock=Panel("BOTTOM • game controls",safeRegion,
                new Vector2(0,0),new Vector2(1,0),
                Vector2.zero,new Vector2(0,162),Deep);
            diceInfo=Label("",dock,new Vector2(.03f,.79f),
                new Vector2(.97f,.99f),16,Gold,TextAnchor.MiddleCenter,true);

            rollButton=MakeButton("ROLL",dock,new Vector2(.035f,.41f),
                new Vector2(.59f,.77f),Gold,Deep,RollClicked);
            multiplierButton=MakeButton("x1",dock,new Vector2(.61f,.41f),
                new Vector2(.77f,.77f),Navy,Aqua,CycleMultiplier);
            nextButton=MakeButton("NEXT",dock,new Vector2(.79f,.41f),
                new Vector2(.965f,.77f),new Color(.32f,.18f,.58f),Pale,NextClicked);

            selection=Label("",dock,new Vector2(.04f,.18f),
                new Vector2(.70f,.38f),13,Pale,TextAnchor.MiddleLeft,true);
            upgradeInfo=Label("",dock,new Vector2(.04f,.035f),
                new Vector2(.70f,.19f),12,Aqua,TextAnchor.MiddleLeft,true);
            buildButton=MakeButton("BUILD",dock,new Vector2(.73f,.035f),
                new Vector2(.965f,.36f),Aqua,Deep,BuildClicked);
        }

        private void UpdateSafeArea()
        {
            if(safeRegion==null||Screen.width==0||Screen.height==0)return;
            Rect s=Screen.safeArea;
            safeRegion.anchorMin=new Vector2(s.xMin/Screen.width,s.yMin/Screen.height);
            safeRegion.anchorMax=new Vector2(s.xMax/Screen.width,s.yMax/Screen.height);
            safeRegion.offsetMin=Vector2.zero;
            safeRegion.offsetMax=Vector2.zero;
        }

        private static RectTransform Rect(string n,Transform parent,
            Vector2 min,Vector2 max,Vector2 oMin,Vector2 oMax)
        {
            var go=new GameObject(n,typeof(RectTransform));
            go.transform.SetParent(parent,false);
            RectTransform rect=go.GetComponent<RectTransform>();
            rect.anchorMin=min; rect.anchorMax=max;
            rect.offsetMin=oMin; rect.offsetMax=oMax;
            return rect;
        }

        private RectTransform Panel(string n,Transform parent,Vector2 min,Vector2 max,
            Vector2 offMin,Vector2 offMax,Color bg)
        {
            var panel=Rect(n,parent,min,max,offMin,offMax);
            Image image=panel.gameObject.AddComponent<Image>();
            image.color=bg;
            return panel;
        }

        private Text Label(string n,Transform parent,Vector2 min,Vector2 max,
            int size,Color colour,TextAnchor anchor,bool bold)
        {
            var rect=Rect(n,parent,min,max,Vector2.zero,Vector2.zero);
            Text text=rect.gameObject.AddComponent<Text>();
            text.font=font;
            text.fontSize=size;
            text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            text.color=colour;
            text.alignment=anchor;
            text.resizeTextForBestFit=true;
            text.resizeTextMinSize=10;
            text.resizeTextMaxSize=size;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }

        private Button MakeButton(string caption,Transform parent,Vector2 min,Vector2 max,
            Color bg,Color fg,UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect=Rect(caption,parent,min,max,Vector2.zero,Vector2.zero);
            Image img=rect.gameObject.AddComponent<Image>();
            img.color=bg;
            Button button=rect.gameObject.AddComponent<Button>();
            ColorBlock block=button.colors;
            block.normalColor=bg;
            block.highlightedColor=Color.Lerp(bg,Color.white,.2f);
            block.pressedColor=Color.Lerp(bg,Color.black,.2f);
            block.disabledColor=Color.Lerp(bg,new Color(.15f,.15f,.20f),.65f);
            block.colorMultiplier=1;
            button.colors=block;
            button.targetGraphic=img;
            button.onClick.AddListener(onClick);
            Label(caption,rect,Vector2.zero,Vector2.one,16,fg,TextAnchor.MiddleCenter,true);
            return button;
        }

        private void Notify(string message)
        {
            toast=message;
            if(status!=null)status.text=message;
        }

        private void RefreshHUD()
        {
            if(resources==null)return;
            int upgrades=MemeopolyRules.TotalUpgrades(save);
            string w=MemeopolyRules.Worlds[save.worldIndex%MemeopolyRules.Worlds.Length];
            worldText.text="WORLD "+(save.worldIndex+1)+"  •  "+upgrades+"/30 BUILT";
            resources.text="COINS "+save.coins.ToString("N0")+"     DICE "+save.dice+"/50"+"     SHIELDS "+save.shields+"/3"+"\n"+
                "FANS "+save.followers.ToString("N0")+"     HYPE "+save.hype+"/100";
            selection.text=MemeopolyRules.Landmarks[selected]+"  ["+save.buildings[selected]+"/5]";
            int cost=MemeopolyRules.UpgradeCost(save,selected);
            upgradeInfo.text=cost<0?"MAX STAGE · Monument complete":"NEXT BUILD · "+cost.ToString("N0")+" coins";
            int mult=multipliers[multiplierIndex];
            multiplierButton.GetComponentInChildren<Text>().text="x"+mult;
            diceInfo.text=diceSettled ? "DICE "+save.lastDieA+" + "+save.lastDieB+" = "+(save.lastDieA+save.lastDieB)+"    •    TURN "+save.turn : "ROLLING THE DICE...";
            if(status!=null) status.text=toast;
            bool complete=MemeopolyRules.Completed(save);
            rollButton.interactable=!playing&&!complete&&save.dice>=mult;
            buildButton.interactable=!playing&&MemeopolyRules.CanUpgrade(save,selected);
            nextButton.interactable=!playing&&complete;
            buildButton.GetComponentInChildren<Text>().text=
                save.buildings[selected]>=5?"MAXED":"BUILD ↑";
            rollButton.GetComponentInChildren<Text>().text=
                complete?"WORLD CLEARED":"ROLL";
        }

        private void CycleMultiplier()
        {
            if(playing) return;
            multiplierIndex=(multiplierIndex+1)%multipliers.Length;
            RefreshHUD();
        }

        private void BuildClicked()
        {
            if(playing) return;
            if(!MemeopolyRules.CanUpgrade(save,selected))
            {
                Notify("Earn more coins to continue construction."); RefreshHUD(); return;
            }
            string message=MemeopolyRules.Upgrade(save,selected);
            city.RenderLandmark(selected,save.buildings[selected],true);
            Notify(message);
            Persist(); RefreshHUD();
        }

        private void NextClicked()
        {
            if(playing||!MemeopolyRules.NextWorld(save))return;
            for(int i=0;i<6;i++)city.RenderLandmark(i,0,false);
            city.SetToken(0);
            ApplySelection(0);
            Notify("A new meme world is unlocked. Build your next empire!");
            Persist(); RefreshHUD();
        }

        private void RollClicked()
        {
            if(playing) return;
            MemeopolyRules.RegenerateDice(save,NowMs());
            int mult=multipliers[multiplierIndex];
            if(save.dice<mult) { Notify("Not enough dice. One regenerates every five minutes.");RefreshHUD();return; }
            if(MemeopolyRules.Completed(save)) { Notify("Finish the world! Tap NEXT WORLD.");RefreshHUD();return; }
            StartCoroutine(RollSequence(mult));
        }

        private IEnumerator RollSequence(int multiplier)
        {
            playing=true;
            diceSettled=false;
            int start=save.position;
            int a=random.Next(1,7),b=random.Next(1,7);
            TurnOutcome result=MemeopolyRules.Roll(save,multiplier,a,b,random);
            Persist();
            Notify("Rolling..."); RefreshHUD();
            float spinDuration=1.05f;
            float time=0f;
            var diceInitialA=city.Dice[0].localPosition;
            var diceInitialB=city.Dice[1].localPosition;
            while(time<spinDuration)
            {
                time+=Time.deltaTime;
                float t=Mathf.Clamp01(time/spinDuration);
                for(int i=0;i<2;i++)
                {
                    Transform die=city.Dice[i];
                    die.localRotation=Quaternion.Euler(875*t+40*i,1095*t+30*i,675*t+60*i);
                    Vector3 startPos=i==0?diceInitialA:diceInitialB;
                    die.localPosition=startPos+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.2f);
                }
                yield return null;
            }
            city.Dice[0].localPosition=diceInitialA;
            city.Dice[1].localPosition=diceInitialB;
            city.SetDice(a,b);
            diceSettled=true;
            Notify("ROLLED "+a+" + "+b+" = "+result.steps+"!"); RefreshHUD();
            yield return new WaitForSeconds(.19f);

            for(int step=1;step<=result.steps;step++)
            {
                Vector3 from=city.Path[(start+step-1)%60]+Vector3.up*.11f;
                Vector3 to=city.Path[(start+step)%60]+Vector3.up*.11f;
                float timer=0, travel=.105f;
                while(timer<travel)
                {
                    timer+=Time.deltaTime;
                    float t=Mathf.Clamp01(timer/travel);
                    city.Token.localPosition=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t))+
                        Vector3.up*(Mathf.Sin(t*Mathf.PI)*.42f);
                    yield return null;
                }
                city.Token.localPosition=to;
            }
            city.SetToken(result.destination);
            Notify(result.message+"   [Roll "+a+" + "+b+" = "+result.steps+"]");
            playing=false;
            RefreshHUD();
        }

        private void Persist() { GamePersistence.Save(save); }

        private void OnApplicationPause(bool paused)
        {
            if(paused&&save!=null) Persist();
        }

        private void OnApplicationQuit()
        {
            if(save!=null) Persist();
        }
    }
}