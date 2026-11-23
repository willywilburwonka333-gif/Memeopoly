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
        private MemeopolyAudio audioEngine;
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
        private RectTransform popup;
        private Text popupTitle, popupDescription;
        private readonly Button[] popupButtons=new Button[4];
        private Button popupClose;
        private enum ScreenPanel { None, Hub, Missions, Album, Tokens, Heist, Rival }
        private ScreenPanel openPanel=ScreenPanel.None;
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
            MemeopolyProgression.SyncDaily(save,NowMs());
            Application.targetFrameRate=60;
            QualitySettings.antiAliasing=2;
            audioEngine=new MemeopolyAudio(transform,save.soundMuted);
            SetupCameraAndLight();
            city = new MemeopolyCity(transform);
            city.ApplyWorldTheme(save.worldIndex);
            city.SetToken(save.position);
            city.SelectToken(save.selectedToken);
            city.SetDice(save.lastDieA,save.lastDieB);
            for (int i=0;i<6;i++) city.RenderLandmark(i,save.buildings[i],false);
            BuildHUD();
            ApplySelection(0);
            FitWholeBoard();
            RefreshHUD();
            if(save.pendingHeist) ShowPanel(ScreenPanel.Heist);
            else if(save.pendingAttack) ShowPanel(ScreenPanel.Rival);
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
            city.ApplyWorldTheme(save.worldIndex);
            city.SelectToken(save.selectedToken);
            MemeopolyProgression.SyncDaily(save,NowMs());
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
            key.shadows=Application.platform==RuntimePlatform.WebGLPlayer ?
                LightShadows.None : LightShadows.Soft;
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
                        Vector3 point=new Vector3(x*8.65f,y*6.3f,z*8.65f);
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
                if(MemeopolyProgression.SyncDaily(save,NowMs())) { Persist(); RefreshHUD(); }
                int before=save.dice;
                MemeopolyRules.RegenerateDice(save,NowMs());
                if (before!=save.dice) { Persist(); RefreshHUD(); }
            }
            if(playing || openPanel!=ScreenPanel.None) return;
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
            worldText=Label("",header,new Vector2(.49f,.56f),
                new Vector2(.82f,.96f),12,Aqua,TextAnchor.MiddleRight,true);
            MakeButton("MENU",header,new Vector2(.84f,.57f),new Vector2(.975f,.96f),
                Navy,Gold,OpenHub);
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

            // Full-screen context overlay, created once, used for 3 real
            // interactive panels plus daily missions/album/token wardrobe.
            popup=Panel("Memeopoly overlay — swipe-free, tap-friendly",safeRegion,
                Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,
                new Color(.014f,.025f,.055f,.96f));
            RectTransform card=Panel("CENTRAL PANEL",popup,
                new Vector2(.035f,.16f),new Vector2(.965f,.86f),
                Vector2.zero,Vector2.zero,Navy);
            popupTitle=Label("",card,new Vector2(.06f,.86f),
                new Vector2(.81f,.97f),23,Gold,TextAnchor.MiddleLeft,true);
            popupClose=MakeButton("X",card,new Vector2(.84f,.88f),
                new Vector2(.96f,.97f),Deep,Pale,ClosePanel);
            popupDescription=Label("",card,new Vector2(.07f,.40f),
                new Vector2(.93f,.84f),17,Pale,TextAnchor.MiddleCenter,false);
            popupButtons[0]=MakeButton("A",card,new Vector2(.04f,.22f),
                new Vector2(.48f,.37f),Gold,Deep,()=>PanelAction(0));
            popupButtons[1]=MakeButton("B",card,new Vector2(.52f,.22f),
                new Vector2(.96f,.37f),Aqua,Deep,()=>PanelAction(1));
            popupButtons[2]=MakeButton("C",card,new Vector2(.04f,.045f),
                new Vector2(.48f,.195f),new Color(.47f,.30f,.88f),Pale,()=>PanelAction(2));
            popupButtons[3]=MakeButton("D",card,new Vector2(.52f,.045f),
                new Vector2(.96f,.195f),new Color(.24f,.35f,.54f),Pale,()=>PanelAction(3));
            popup.gameObject.SetActive(false);
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
            rollButton.interactable=!playing&&!complete&&save.dice>=mult&&
                !save.pendingHeist&&!save.pendingAttack;
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
            audioEngine.Play(MemeopolySfx.Menu);
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
            MemeopolyProgression.OnBuild(save,NowMs());
            audioEngine.Play(MemeopolySfx.Build);
            city.RenderLandmark(selected,save.buildings[selected],true);
            Notify(message);
            Persist(); RefreshHUD();
        }

        private void NextClicked()
        {
            if(playing||!MemeopolyRules.NextWorld(save))return;
            for(int i=0;i<6;i++)city.RenderLandmark(i,0,false);
            city.ApplyWorldTheme(save.worldIndex);
            city.SetToken(0);
            ApplySelection(0);
            audioEngine.Play(MemeopolySfx.Reward);
            Notify("A new meme world is unlocked. Build your next empire!");
            Persist(); RefreshHUD();
        }

        private void RollClicked()
        {
            if(playing) return;
            if(save.pendingHeist) {ShowPanel(ScreenPanel.Heist);return;}
            if(save.pendingAttack) {ShowPanel(ScreenPanel.Rival);return;}
            MemeopolyRules.RegenerateDice(save,NowMs());
            int mult=multipliers[multiplierIndex];
            if(save.dice<mult) { Notify("Not enough dice. One regenerates every five minutes.");RefreshHUD();return; }
            if(MemeopolyRules.Completed(save)) { Notify("Finish the world! Tap NEXT WORLD.");RefreshHUD();return; }
            audioEngine.Play(MemeopolySfx.Roll);
            StartCoroutine(RollSequence(mult));
        }

        private IEnumerator RollSequence(int multiplier)
        {
            playing=true;
            diceSettled=false;
            int start=save.position;
            int a=random.Next(1,7),b=random.Next(1,7);
            TurnOutcome result=MemeopolyRules.Roll(save,multiplier,a,b,random);
            MemeopolyProgression.OnRoll(save,NowMs());
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
                    int rolled=i==0?a:b;
                    Quaternion spin=Quaternion.Euler(875*.78f+40*i,1095*.78f+30*i,
                        675*.78f+60*i);
                    if(t<.78f)
                    {
                        float u=t/.78f;
                        die.localRotation=Quaternion.Euler(875*t+40*i,
                            1095*t+30*i,675*t+60*i);
                        die.localScale=Vector3.one*(1f+Mathf.Sin(u*3.14159f)*.035f);
                    }
                    else
                    {
                        float u=(t-.78f)/.22f;
                        die.localRotation=Quaternion.Slerp(spin,
                            MemeopolyCity.DiceTop(rolled),
                            Mathf.SmoothStep(0,1,u));
                        die.localScale=new Vector3(1f+.12f*Mathf.Sin(u*Mathf.PI),
                            1f-.12f*Mathf.Sin(u*Mathf.PI),
                            1f+.12f*Mathf.Sin(u*Mathf.PI));
                    }
                    Vector3 startPos=i==0?diceInitialA:diceInitialB;
                    float bounce=Mathf.Abs(Mathf.Sin(t*Mathf.PI*5))*(1f-t)*.46f;
                    die.localPosition=startPos+Vector3.up*
                        (Mathf.Sin(t*Mathf.PI)*1.0f+bounce);
                }
                yield return null;
            }
            city.Dice[0].localPosition=diceInitialA;
            city.Dice[1].localPosition=diceInitialB;
            city.Dice[0].localScale=Vector3.one;
            city.Dice[1].localScale=Vector3.one;
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
                if(step%2==0) audioEngine.Play(MemeopolySfx.Step);
            }
            city.SetToken(result.destination);
            Notify(result.message+"   [Roll "+a+" + "+b+" = "+result.steps+"]");
            playing=false;
            if(save.pendingHeist) ShowPanel(ScreenPanel.Heist);
            else if(save.pendingAttack) ShowPanel(ScreenPanel.Rival);
            else if(MemeopolyRules.TileKinds[save.position]=="CARD")
                audioEngine.Play(MemeopolySfx.Reward);
            RefreshHUD();
        }

        private void OpenHub()
        {
            if(playing)return;
            audioEngine.Play(MemeopolySfx.Menu);
            if(save.pendingHeist) ShowPanel(ScreenPanel.Heist);
            else if(save.pendingAttack) ShowPanel(ScreenPanel.Rival);
            else ShowPanel(ScreenPanel.Hub);
        }

        private void ClosePanel()
        {
            // Can't ignore a heist/rival outcome: saved pending choices must
            // be resolved before another dice roll.
            if(openPanel==ScreenPanel.Heist || openPanel==ScreenPanel.Rival)return;
            openPanel=ScreenPanel.None;
            popup.gameObject.SetActive(false);
            audioEngine.Play(MemeopolySfx.Menu);
            RefreshHUD();
        }

        private void ShowPanel(ScreenPanel mode)
        {
            openPanel=mode;
            popup.gameObject.SetActive(true);
            popupClose.interactable=mode!=ScreenPanel.Heist&&mode!=ScreenPanel.Rival;

            string headline="",body="";
            string[] choices={"","","",""};
            if(mode==ScreenPanel.Hub)
            {
                headline="YOUR MEME EMPIRE";
                body="WORLD "+(save.worldIndex+1)+"   •   "+MemeopolyRules.TotalUpgrades(save)+"/30 BUILDS"+
                     "\nHEISTS "+save.totalHeists+"   •   RIVALS "+save.totalAttacks+
                     "\n\nMake your creator empire famous. Select a section.";
                choices=new[]{"MISSIONS","STICKER ALBUM","CHARACTERS",save.soundMuted?"SOUND OFF":"SOUND ON"};
            }
            else if(mode==ScreenPanel.Missions)
            {
                MemeopolyProgression.SyncDaily(save,NowMs());
                headline="DAILY CHALLENGES";
                body="";
                for(int i=0;i<3;i++)
                    body+=MemeopolyProgression.DailyLabel(i)+"   "+
                        Mathf.Min(MemeopolyProgression.DailyProgress(save,i),
                            MemeopolyProgression.DailyGoal(i))+"/"+
                        MemeopolyProgression.DailyGoal(i)+
                        (((save.dailyRewardMask&(1<<i))!=0)?"  ✓ CLAIMED":"")+"\n\n";
                choices=new[]{"CLAIM ROLLS","CLAIM BUILDS","CLAIM HEIST","BACK"};
            }
            else if(mode==ScreenPanel.Album)
            {
                headline="MEME STICKER ALBUM";
                body="COLLECTED "+MemeopolyProgression.CardCount(save)+"/"+
                     MemeopolyProgression.Album.Length+"\n\n";
                foreach(string name in MemeopolyProgression.Album)
                    body+=(Array.IndexOf(save.cards,name)>=0?"✓ ":"○ ")+name+"\n";
                if(save.albumRewardPaid)body+="\nALBUM COMPLETED!";
                choices=new[]{"BACK","","",""};
            }
            else if(mode==ScreenPanel.Tokens)
            {
                headline="PICK YOUR 3D HERO";
                body="Change your character anytime.\n\nCURRENT: "+
                    MemeopolyProgression.Tokens[save.selectedToken]+
                    "\n\nYour chosen character will travel the board.";
                choices=new[]{"SHARKO","CROCWING","VIRAL CAT","BACK"};
            }
            else if(mode==ScreenPanel.Heist)
            {
                headline="ALGORITHM HEIST";
                body="You found three locked vaults!\n\n"+
                    "Choose a vault. Riskier options pay more, but security can catch you.\n"+
                    "Multiplier x"+save.pendingMultiplier;
                choices=new[]{"SAFE 90%","BOLD 70%","JACKPOT 45%",""};
            }
            else if(mode==ScreenPanel.Rival)
            {
                headline="CREATOR SHOWDOWN";
                body="A rival is trying to steal your reach!\n\n"+
                    "Choose a challenger. Harder opponents pay more when you win.\n"+
                    "Multiplier x"+save.pendingMultiplier;
                choices=new[]{"ALGORITHM 88%","COPYCAT 68%","TREND THIEF 48%",""};
            }
            popupTitle.text=headline;
            popupDescription.text=body;
            for(int i=0;i<4;i++)
            {
                popupButtons[i].gameObject.SetActive(choices[i].Length>0);
                if(choices[i].Length>0)
                    popupButtons[i].GetComponentInChildren<Text>().text=choices[i];
                popupButtons[i].interactable=true;
            }
        }

        private void PanelAction(int index)
        {
            if(openPanel==ScreenPanel.Hub)
            {
                if(index==0) ShowPanel(ScreenPanel.Missions);
                else if(index==1) ShowPanel(ScreenPanel.Album);
                else if(index==2) ShowPanel(ScreenPanel.Tokens);
                else if(index==3)
                {
                    save.soundMuted=!save.soundMuted;
                    audioEngine.SetMuted(save.soundMuted);
                    Persist();
                    ShowPanel(ScreenPanel.Hub);
                }
            }
            else if(openPanel==ScreenPanel.Missions)
            {
                if(index==3) {ShowPanel(ScreenPanel.Hub);return;}
                string message;
                bool claimed=MemeopolyProgression.ClaimDaily(save,index,NowMs(),out message);
                if(claimed)audioEngine.Play(MemeopolySfx.Reward);
                Notify(message);
                Persist();
                ShowPanel(ScreenPanel.Missions);
                RefreshHUD();
            }
            else if(openPanel==ScreenPanel.Album)
            {
                ShowPanel(ScreenPanel.Hub);
            }
            else if(openPanel==ScreenPanel.Tokens)
            {
                if(index==3) {ShowPanel(ScreenPanel.Hub);return;}
                save.selectedToken=Mathf.Clamp(index,0,2);
                city.SelectToken(save.selectedToken);
                audioEngine.Play(MemeopolySfx.Menu);
                Persist();
                ShowPanel(ScreenPanel.Tokens);
            }
            else if(openPanel==ScreenPanel.Heist && index<3)
            {
                string result=MemeopolyProgression.ResolveHeist(save,index,random);
                Notify(result);
                audioEngine.Play(result.StartsWith("HEIST BUSTED")?
                    MemeopolySfx.Failure:MemeopolySfx.Heist);
                Persist();
                CloseCompletedEvent();
            }
            else if(openPanel==ScreenPanel.Rival && index<3)
            {
                string result=MemeopolyProgression.ResolveRival(save,index,random);
                Notify(result);
                audioEngine.Play(result.Contains("defeated")?
                    MemeopolySfx.Reward:MemeopolySfx.Failure);
                Persist();
                CloseCompletedEvent();
            }
        }

        private void CloseCompletedEvent()
        {
            openPanel=ScreenPanel.None;
            popup.gameObject.SetActive(false);
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