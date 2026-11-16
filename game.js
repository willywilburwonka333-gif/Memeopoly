(() => {
  "use strict";
  const SAVE_KEY="memeopoly-save-v2",LEGACY_SAVE_KEY="memeopoly-save-v1";
  const MAX_DICE=50,DICE_REGEN_MS=5*60*1000;
  const eras=["Bedroom Meme Era","Vine House Era","YouTube Shorts Era","TikTok Empire Era","Meme HQ Era"];
  const board=[
    {type:"GO",icon:"🚀",name:"Go Viral",description:"Collect your creator bonus"},
    {type:"COINS",icon:"🪙",name:"Ad Revenue",description:"Earn coins"},
    {type:"CARD",icon:"🃏",name:"Meme Pack",description:"Find a collectible"},
    {type:"LANDMARK",icon:"🏠",name:"Bedroom Setup",description:"Upgrade your setup"},
    {type:"ATTACK",icon:"⚔️",name:"Cancel Strike",description:"A rival makes a move"},
    {type:"HYPE",icon:"⚡",name:"Hype Boost",description:"Build your hype"},
    {type:"LANDMARK",icon:"🎤",name:"Cheap Mic",description:"Upgrade your audio"},
    {type:"EVENT",icon:"🔥",name:"Viral Event",description:"A random event"},
    {type:"LANDMARK",icon:"💻",name:"Gaming PC",description:"Upgrade your workstation"},
    {type:"RAID",icon:"🏦",name:"Follower Heist",description:"Raid the algorithm"},
    {type:"LANDMARK",icon:"💡",name:"Ring Light",description:"Upgrade your lighting"},
    {type:"ATTACK",icon:"💀",name:"Shadowban",description:"Survive a platform setback"},
    {type:"LANDMARK",icon:"🏢",name:"Meme Studio",description:"Upgrade your studio"},
    {type:"SHIELD",icon:"🛡️",name:"Shield Drop",description:"Protect your studio"},
    {type:"CARD",icon:"🎁",name:"Mystery Drop",description:"Open a mystery pack"},
    {type:"EVENT",icon:"🌈",name:"Community Event",description:"See what happens"}
  ];
  const coords=[[1,1],[1,2],[1,3],[1,4],[1,5],[2,5],[3,5],[4,5],[5,5],[5,4],[5,3],[5,2],[5,1],[4,1],[3,1],[2,1]];
  const landmarkNames=["Bedroom Setup","Cheap Mic","Gaming PC","Ring Light","Meme Studio"];
  const landmarkIcons=["🏠","🎤","💻","💡","🏢"];
  const memeCards=["Doge","Pepe","NPC Streamer","Side Eye Cat","Sigma Baby","Ohio Boss","Viral Rat","Distracted Boyfriend"];
  const cardIcons=["🐕","🐸","🕺","🐈","🗿","👑","🐀","💑"];
  const districts=[
    {id:"meme-lane",name:"Meme Lane",icon:"😂",theme:"The birthplace of viral culture",unlock:0,reward:2500,followers:1500,landmark:"Bedroom Setup"},
    {id:"streamer-strip",name:"Streamer Strip",icon:"🎮",theme:"Where creators become celebrities",unlock:5,reward:5000,followers:3000,landmark:"Cheap Mic"},
    {id:"viral-heights",name:"Viral Heights",icon:"🚀",theme:"The home of overnight sensations",unlock:10,reward:9000,followers:6000,landmark:"Gaming PC"},
    {id:"creator-city",name:"Creator City",icon:"🏙️",theme:"Brand deals, studios and big money",unlock:15,reward:15000,followers:10000,landmark:"Ring Light"},
    {id:"algorithm-central",name:"Algorithm Central",icon:"🌐",theme:"Own the feed. Rule the internet.",unlock:20,reward:25000,followers:18000,landmark:"Meme Studio"}
  ];
  const rivalBlueprints=[
    {id:"pixel-pirate",name:"Pixel Pirate",handle:"@pixelpirate",icon:"🏴‍☠️",district:"Meme Lane",power:1,coins:12000,shields:1},
    {id:"queen-scroll",name:"Queen Scroll",handle:"@queenscroll",icon:"👑",district:"Streamer Strip",power:2,coins:18000,shields:2},
    {id:"drip-lord",name:"Drip Lord",handle:"@driplord",icon:"🕶️",district:"Viral Heights",power:3,coins:26000,shields:2},
    {id:"botfather",name:"The Botfather",handle:"@botfather",icon:"🤖",district:"Creator City",power:4,coins:38000,shields:3},
    {id:"algo-king",name:"Algo King",handle:"@algoking",icon:"🧠",district:"Algorithm Central",power:5,coins:55000,shields:3}
  ];
  const albumBlueprints=[
    {id:"classic-chaos",name:"Classic Chaos",icon:"🤣",stickers:[["Doge Energy","🐕"],["Pepe Mood","🐸"],["Side Eye","👀"],["Big Brain","🧠"],["Main Character","✨"]]},
    {id:"creator-life",name:"Creator Life",icon:"🎬",stickers:[["Ring Light Royalty","💡"],["Stream Goblin","🎮"],["Mic Check","🎤"],["Sponsored","💸"],["Viral Overnight","🚀"]]},
    {id:"internet-legends",name:"Internet Legends",icon:"🏆",stickers:[["Keyboard Warrior","⌨️"],["Comment Section","💬"],["Algorithm Whisperer","🌐"],["Meme Mogul","👑"],["Certified Icon","⭐"]]}
  ];
  const allStickers=albumBlueprints.flatMap(a=>a.stickers.map(s=>({id:a.id+":"+s[0],album:a.id,name:s[0],icon:s[1]})));
  const achievements=[
    {id:"first-roll",name:"First Post",icon:"📱",description:"Take your first turn.",reward:500,done:s=>s.turn>=1},
    {id:"first-build",name:"Under Construction",icon:"🏗️",description:"Build your first landmark.",reward:1000,done:s=>levelCount(s)>=1},
    {id:"first-card",name:"Collector",icon:"🃏",description:"Find your first meme card.",reward:1000,done:s=>s.cards.length>=1},
    {id:"ten-turns",name:"Algorithm Regular",icon:"🔥",description:"Take 10 turns.",reward:2500,done:s=>s.turn>=10},
    {id:"five-levels",name:"Big Creator Energy",icon:"⭐",description:"Reach five landmark upgrades.",reward:5000,done:s=>levelCount(s)>=5},
    {id:"full-set",name:"Meme Museum",icon:"🏆",description:"Collect all eight meme cards.",reward:10000,done:s=>s.cards.length===8},
    {id:"first-publish",name:"Content Machine",icon:"📣",description:"Publish your first post.",reward:1000,done:s=>s.totalPostsPublished>=1},
    {id:"first-district",name:"Neighbourhood Famous",icon:"🌆",description:"Claim your first creator district.",reward:2000,done:s=>s.districtClaims.length>=1},
    {id:"first-raid",name:"Studio Breaker",icon:"⚔️",description:"Successfully raid a rival studio.",reward:2500,done:s=>s.rivals.some(r=>r.raids>=1)},
    {id:"first-album",name:"Album Artist",icon:"⭐",description:"Complete your first sticker album.",reward:5000,done:s=>s.completedAlbums.length>=1}
  ];
  const missions=[
    {id:"rolls",name:"Feed the algorithm",icon:"🎲",description:"Take 3 turns today.",goal:3,reward:1200,progress:s=>s.daily.rolls},
    {id:"builds",name:"Upgrade your setup",icon:"🛠️",description:"Build or upgrade 1 landmark today.",goal:1,reward:1800,progress:s=>s.daily.upgrades},
    {id:"cards",name:"Meme hunter",icon:"🃏",description:"Find 1 meme card today.",goal:1,reward:2200,progress:s=>s.daily.cards},
    {id:"posts",name:"Keep the feed alive",icon:"📣",description:"Publish 2 posts today.",goal:2,reward:1500,progress:s=>s.daily.posts},
    {id:"raids",name:"Rival trouble",icon:"⚔️",description:"Successfully raid 1 rival today.",goal:1,reward:2500,progress:s=>s.daily.raids}
  ];
  const today=()=>{const d=new Date();return d.getFullYear()+"-"+String(d.getMonth()+1).padStart(2,"0")+"-"+String(d.getDate()).padStart(2,"0");};
  const freshDaily=()=>({date:today(),rolls:0,upgrades:0,cards:0,posts:0,raids:0,claims:[]});
  const freshState=()=>({position:0,coins:5000,followers:0,posts:12,dice:30,diceUpdatedAt:Date.now(),shields:1,hype:0,turn:0,landmarks:landmarkNames.map(name=>({name,level:0})),cards:[],completed:false,completionRewarded:false,tutorialSeen:false,totalPostsPublished:0,log:[],daily:freshDaily(),dailyBonusDate:"",achievements:[],districtClaims:[],rivals:rivalBlueprints.map(r=>({...r,cooldownTurn:0,raids:0})),stickers:[],stickerStars:0,completedAlbums:[]});
  let state=freshState(),rolling=false,installPrompt=null;
  const $=id=>document.getElementById(id);
  const number=n=>Math.floor(n).toLocaleString();
  const levelCount=s=>s.landmarks.reduce((sum,item)=>sum+item.level,0);
  const totalLevels=()=>levelCount(state);
  const currentEra=()=>Math.min(eras.length-1,Math.floor(totalLevels()/5));
  const costFor=level=>500+level*750+currentEra()*350;
  const save=()=>{try{localStorage.setItem(SAVE_KEY,JSON.stringify(state));$("saveStatus").textContent="✓ Progress saved";}catch(e){$("saveStatus").textContent="Save unavailable in this browser";}};
  const addLog=message=>{state.log.unshift({message,time:new Date().toLocaleTimeString([],{hour:"2-digit",minute:"2-digit"})});state.log=state.log.slice(0,40);renderLog();save();};
  function regenerateDice(){
    const now=Date.now(),last=Number(state.diceUpdatedAt)||now;
    if(state.dice<MAX_DICE){const gained=Math.floor((now-last)/DICE_REGEN_MS);if(gained>0){state.dice=Math.min(MAX_DICE,state.dice+gained);state.diceUpdatedAt=last+gained*DICE_REGEN_MS;}}
    else state.diceUpdatedAt=now;
  }
  function normalizeDaily(){
    if(!state.daily||state.daily.date!==today())state.daily=freshDaily();
    for(const key of ["rolls","upgrades","cards","posts","raids"])state.daily[key]=Math.max(0,Math.min(100000,Math.floor(Number(state.daily[key])||0)));
    state.daily.claims=Array.isArray(state.daily.claims)?state.daily.claims.filter(id=>missions.some(m=>m.id===id)):[];
  }
  function load(){
    try{
      const primary=localStorage.getItem(SAVE_KEY),legacy=localStorage.getItem(LEGACY_SAVE_KEY);
      let saved=null;
      try{saved=primary?JSON.parse(primary):null;}catch(e){saved=null;}
      if(!saved&&legacy){try{saved=JSON.parse(legacy);}catch(e){saved=null;}}
      if(!saved||!Array.isArray(saved.landmarks)||!Array.isArray(saved.cards))return false;
      state={...freshState(),...saved};
      state.landmarks=landmarkNames.map(name=>{const old=saved.landmarks.find(x=>x&&x.name===name);return{name,level:Math.max(0,Math.min(5,Number(old?.level)||0))};});
      state.cards=memeCards.filter(name=>saved.cards.includes(name));
      state.districtClaims=Array.isArray(saved.districtClaims)?saved.districtClaims.filter(id=>districts.some(d=>d.id===id)):[];
      state.rivals=rivalBlueprints.map(base=>{
        const old=Array.isArray(saved.rivals)?saved.rivals.find(r=>r&&r.id===base.id)||{}:{};
        return {...base,coins:Math.max(0,Math.min(100000000,Number(old.coins??base.coins)||0)),shields:Math.max(0,Math.min(3,Math.floor(Number(old.shields??base.shields)||0))),raids:Math.max(0,Math.min(100000,Math.floor(Number(old.raids)||0))),cooldownTurn:Math.max(0,Math.min(100000000,Math.floor(Number(old.cooldownTurn)||0)))};
      });
      state.stickers=Array.isArray(saved.stickers)?allStickers.filter(s=>saved.stickers.includes(s.id)).map(s=>s.id):[];
      state.stickerStars=Math.max(0,Math.min(1000000,Math.floor(Number(saved.stickerStars)||0)));
      state.completedAlbums=Array.isArray(saved.completedAlbums)?saved.completedAlbums.filter(id=>albumBlueprints.some(a=>a.id===id)):[];
      state.log=Array.isArray(saved.log)?saved.log.filter(x=>x&&typeof x.message==="string").slice(0,40).map(x=>({message:x.message.slice(0,300),time:typeof x.time==="string"?x.time.slice(0,20):""})):[];
      state.achievements=Array.isArray(saved.achievements)?saved.achievements.filter(id=>achievements.some(a=>a.id===id)):[];
      state.position=Math.max(0,Math.min(15,Number(state.position)||0));
      for(const key of ["coins","followers","posts","turn","dice","shields","totalPostsPublished"])state[key]=Math.max(0,Math.min(1000000000,Math.floor(Number(state[key])||0)));
      state.shields=Math.min(3,state.shields);state.dailyBonusDate=typeof state.dailyBonusDate==="string"?state.dailyBonusDate:"";
      state.tutorialSeen=Boolean(state.tutorialSeen);state.completionRewarded=Boolean(saved.completionRewarded||saved.completed);
      state.dice=Math.min(MAX_DICE,state.dice);state.diceUpdatedAt=Math.min(Date.now(),Math.max(0,Number(state.diceUpdatedAt)||Date.now()));
      state.hype=Math.max(0,Math.min(99,Number(state.hype)||0));
      // Completion is derived from the actual empire, never trusted from an old save flag.
      state.completed=totalLevels()>=25;
      normalizeDaily();
      if(state.completed&&!state.completionRewarded)checkCompletion();
      checkAlbumRewards();
      checkAchievements();
      return true;
    }catch(e){return false;}
  }
  function setDiceFace(value){
    const icon=$("diceIcon");
    const face=document.createElement("span");
    face.className="die-face";
    face.setAttribute("role","img");
    face.setAttribute("aria-label","Dice result "+value);
    const patterns={
      1:[4],2:[0,8],3:[0,4,8],4:[0,2,6,8],
      5:[0,2,4,6,8],6:[0,2,3,5,6,8]
    };
    for(let i=0;i<9;i++){
      const pip=document.createElement("i");
      pip.className="die-pip"+(patterns[value].includes(i)?" visible":"");
      face.append(pip);
    }
    icon.replaceChildren(face);
    icon.setAttribute("aria-label","Dice showing "+value);
  }
  function renderBoard(){
    const el=$("board");el.querySelectorAll(".tile").forEach(t=>t.remove());
    board.forEach((item,i)=>{
      const tile=document.createElement("article"),[row,col]=coords[i];
      tile.className="tile "+item.type.toLowerCase()+(state.position===i?" player":"");
      tile.style.gridRow=String(row);tile.style.gridColumn=String(col);
      const icon=document.createElement("span");icon.className="tile-icon";icon.textContent=item.icon;
      const name=document.createElement("strong");name.textContent=item.name;
      const detail=document.createElement("small");detail.textContent=item.type==="LANDMARK"?"Lv "+state.landmarks.find(l=>l.name===item.name).level+"/5":item.description;
      tile.append(icon,name,detail);
      if(state.position===i){const token=document.createElement("span");token.className="token";token.textContent="😂";token.setAttribute("aria-label","Your token");tile.append(token);}
      el.append(tile);
    });
  }
  function renderLandmarks(){
    const box=$("landmarks");box.replaceChildren();
    state.landmarks.forEach((lm,i)=>{
      const card=document.createElement("article");card.className="landmark-card";
      const top=document.createElement("div");top.className="landmark-top";
      const icon=document.createElement("span");icon.className="landmark-icon";icon.textContent=landmarkIcons[i];
      const title=document.createElement("div"),name=document.createElement("strong"),level=document.createElement("small");
      name.textContent=lm.name;level.textContent="LEVEL "+lm.level+" / 5";title.append(name,level);top.append(icon,title);
      const bar=document.createElement("div");bar.className="small-bar";const fill=document.createElement("div");fill.className="small-fill";fill.style.width=lm.level*20+"%";bar.append(fill);
      const bottom=document.createElement("div");bottom.className="landmark-bottom";
      const cost=document.createElement("span");cost.textContent=lm.level===5?"MAX LEVEL":"🪙 "+number(costFor(lm.level));
      const btn=document.createElement("button");btn.type="button";btn.textContent=lm.level===0?"BUILD":lm.level===5?"MAXED":"UPGRADE";btn.disabled=lm.level===5||state.coins<costFor(lm.level)||state.completed;btn.addEventListener("click",()=>upgradeLandmark(i));
      bottom.append(cost,btn);card.append(top,bar,bottom);box.append(card);
    });
    $("empireLevel").textContent=totalLevels();
    $("milestoneText").textContent=state.completed?"You built the ultimate Memeopoly empire!":(5-totalLevels()%5)+" more upgrade(s) until the next era milestone.";
  }
  function renderCollection(){
    const box=$("collection");box.replaceChildren();
    memeCards.forEach((name,i)=>{const owned=state.cards.includes(name),card=document.createElement("article");card.className="collect-card"+(owned?" owned":"");const icon=document.createElement("span");icon.className="collect-icon";icon.textContent=owned?cardIcons[i]:"❔";const title=document.createElement("strong");title.textContent=owned?name:"Unknown meme";const status=document.createElement("small");status.textContent=owned?"COLLECTED":"NOT DISCOVERED";card.append(icon,title,status);box.append(card);});
    $("collectionCount").textContent=state.cards.length+"/8";$("cardCount").textContent=state.cards.length;$("centerCards").textContent=state.cards.length;
  }
  function renderLog(){const box=$("log");box.replaceChildren();state.log.forEach(item=>{const line=document.createElement("div");line.className="log-entry";const time=document.createElement("time");time.textContent=item.time||"";const msg=document.createElement("span");msg.textContent=item.message||"";line.append(time,msg);box.append(line);});}
  function renderDaily(){
    normalizeDaily();
    const bonusClaimed=state.dailyBonusDate===today(),bonus=$("dailyBonusBtn");
    bonus.disabled=bonusClaimed||state.completed;bonus.textContent=bonusClaimed?"✓ BONUS CLAIMED":"🎁 CLAIM DAILY BONUS";
    $("dailyBonusNote").textContent=bonusClaimed?"Today's creator bonus has been claimed. Come back tomorrow!":"Claim 2,000 coins and 2,000 followers once each day.";
    const box=$("missions");box.replaceChildren();
    missions.forEach(m=>{
      const progress=Math.min(m.goal,m.progress(state)),claimed=state.daily.claims.includes(m.id),card=document.createElement("article");card.className="mission-card"+(claimed?" mission-done":"");
      const top=document.createElement("div");top.className="mission-top";const icon=document.createElement("span");icon.textContent=m.icon;const title=document.createElement("strong");title.textContent=m.name;top.append(icon,title);
      const desc=document.createElement("p");desc.textContent=m.description;
      const meter=document.createElement("div");meter.className="small-bar";const fill=document.createElement("div");fill.className="small-fill";fill.style.width=(progress/m.goal*100)+"%";meter.append(fill);
      const foot=document.createElement("div");foot.className="mission-foot";const reward=document.createElement("span");reward.textContent="🪙 "+number(m.reward);const btn=document.createElement("button");btn.type="button";btn.textContent=claimed?"CLAIMED":progress>=m.goal?"CLAIM":"IN PROGRESS";btn.disabled=claimed||progress<m.goal;btn.addEventListener("click",()=>claimMission(m.id));foot.append(reward,btn);card.append(top,desc,meter,foot);box.append(card);
    });
  }
  function renderAchievements(){
    const box=$("achievements");box.replaceChildren();
    achievements.forEach(a=>{const earned=state.achievements.includes(a.id),card=document.createElement("article");card.className="achievement-card"+(earned?" earned":"");const icon=document.createElement("span");icon.textContent=earned?a.icon:"🔒";const info=document.createElement("div"),title=document.createElement("strong"),desc=document.createElement("small");title.textContent=a.name;desc.textContent=earned?"Unlocked · reward paid":a.description;info.append(title,desc);const reward=document.createElement("b");reward.textContent=earned?"✓":"+"+number(a.reward);card.append(icon,info,reward);box.append(card);});
    $("achievementCount").textContent=state.achievements.length+" / "+achievements.length;
  }
  function renderUI(){
    regenerateDice();$("dice").textContent=state.dice;$("shields").textContent=state.shields;
    $("coins").textContent=number(state.coins);$("followers").textContent=number(state.followers);$("posts").textContent=number(state.posts);$("hype").textContent=state.hype;$("turnCount").textContent=state.turn;
    $("eraName").textContent=state.completed?"Memeopoly Champion":eras[currentEra()];$("progressText").textContent=totalLevels()+" / 25 upgrades";$("progressFill").style.width=totalLevels()/25*100+"%";$("eraProgress").textContent=Math.round(totalLevels()/25*100)+"%";
    const multiplier=Number($("rollMultiplier").value)||1;$("postBtn").disabled=rolling||state.dice<multiplier||state.completed;$("postBtn").textContent=state.completed?"EMPIRE COMPLETE":rolling?"ROLLING…":"ROLL DICE";$("rollHint").textContent=state.completed?"You have conquered the board.":state.dice+" dice ready · "+(state.dice<MAX_DICE?"regenerates 1 every 5 min":"energy full");$("centerMessage").textContent=state.completed?"🏆 You are the Memeopoly champion!":"Your next viral moment starts here.";
    $("upgradeBtn").disabled=state.completed||!state.landmarks.some(l=>l.level<5);
    $("publishBtn").disabled=state.completed||state.posts<1;
    $("publishBtn").textContent=state.posts>0?"📣 PUBLISH POST · "+state.posts+" LEFT":"NO POSTS LEFT";
    renderBoard();renderLandmarks();renderCollection();renderLog();renderDaily();renderAchievements();renderDistricts();renderRivals();renderStickerAlbums();
  }
  function renderDistricts(){
    const render=boxId=>{const box=$(boxId);if(!box)return;box.replaceChildren();districts.forEach(d=>{
      const unlocked=totalLevels()>=d.unlock,claimed=state.districtClaims.includes(d.id),card=document.createElement("article");
      card.className="district-card"+(unlocked?" district-open":" district-locked");
      const icon=document.createElement("span");icon.className="district-icon";icon.textContent=unlocked?d.icon:"🔒";
      const title=document.createElement("strong");title.textContent=unlocked?d.name:"Undiscovered district";
      const desc=document.createElement("p");desc.textContent=unlocked?d.theme:"Unlock at "+d.unlock+" total landmark upgrades";
      const progress=document.createElement("small");progress.textContent=unlocked?"Connected to "+d.landmark:"Empire progress: "+totalLevels()+"/"+d.unlock;
      const btn=document.createElement("button");btn.type="button";btn.textContent=claimed?"REWARD CLAIMED":unlocked?"CLAIM TAKEOVER BONUS":"LOCKED";btn.disabled=!unlocked||claimed;btn.addEventListener("click",()=>claimDistrict(d.id));
      card.append(icon,title,desc,progress,btn);box.append(card);
    });};
    render("districts");render("districtsFull");
    const count=districts.filter(d=>totalLevels()>=d.unlock).length;if($("districtCount"))$("districtCount").textContent=count+"/5";
  }
  function claimDistrict(id){
    const d=districts.find(x=>x.id===id);if(!d||totalLevels()<d.unlock||state.districtClaims.includes(id))return;
    state.districtClaims.push(id);state.coins+=d.reward;state.followers+=d.followers;checkAchievements();
    addLog("🌆 DISTRICT TAKEOVER: "+d.name+" is yours! +"+number(d.reward)+" coins and +"+number(d.followers)+" followers.");
    renderUI();save();
  }
  function renderRivals(){
    const box=$("rivals");if(!box)return;box.replaceChildren();
    state.rivals.forEach(r=>{
      const card=document.createElement("article");card.className="rival-card";
      const head=document.createElement("div");head.className="rival-head";
      const icon=document.createElement("span");icon.textContent=r.icon;
      const info=document.createElement("div"),name=document.createElement("strong"),handle=document.createElement("small");
      name.textContent=r.name;handle.textContent=r.handle+" · "+r.district;info.append(name,handle);head.append(icon,info);
      const requirement=districts.find(d=>d.name===r.district),unlocked=totalLevels()>=(requirement?.unlock??0);
      const stats=document.createElement("div");stats.className="rival-stats";
      stats.textContent=unlocked?"🛡️ "+r.shields+" shields   ·   💰 "+number(r.coins)+" vault   ·   ⚔️ "+r.raids+" raids":"🔒 Unlock "+r.district+" at "+(requirement?.unlock??0)+" upgrades";
      const btn=document.createElement("button");btn.type="button";const cooling=state.turn<r.cooldownTurn;
      btn.textContent=!unlocked?"DISTRICT LOCKED":cooling?"RECOVERING · "+(r.cooldownTurn-state.turn)+" TURNS":r.shields>0?"BREAK SHIELD & RAID":"RAID STUDIO";
      btn.disabled=!unlocked||cooling||state.completed;btn.addEventListener("click",()=>attackRival(r.id));card.append(head,stats,btn);box.append(card);
    });
  }
  function attackRival(id){
    normalizeDaily();const r=state.rivals.find(x=>x.id===id),requirement=r&&districts.find(d=>d.name===r.district);
    if(!r||state.completed||totalLevels()<(requirement?.unlock??0)||state.turn<r.cooldownTurn)return;
    if(r.shields>0){r.shields--;r.cooldownTurn=state.turn+2;addLog("🛡️ "+r.name+" blocked your raid! One rival shield destroyed.");}
    else{
      const rate=5+Math.floor(Math.random()*8),loot=Math.min(r.coins,Math.max(500,Math.floor(r.coins*rate/100)));
      r.coins-=loot;const fans=750+r.power*350+Math.floor(Math.random()*500);
      state.coins+=loot;state.followers+=fans;r.raids++;state.daily.raids++;r.cooldownTurn=state.turn+3;
      addLog("⚔️ STUDIO RAID SUCCESS! You hit "+r.name+" for "+number(loot)+" coins and gained "+number(fans)+" followers.");
    }
    checkAchievements();renderUI();save();
  }
  function renderStickerAlbums(){
    const box=$("stickerAlbums");if(!box)return;box.replaceChildren();$("stickerStats").textContent=state.stickerStars;
    const missing=allStickers.filter(s=>!state.stickers.includes(s.id)).length;
    $("tradeStarsBtn").disabled=state.stickerStars<3||missing===0;
    $("stickerTradeHint").textContent=missing===0?"Every sticker collected. Your albums are complete!":state.stickerStars+" / 3 duplicate stars · "+missing+" stickers still missing";
    albumBlueprints.forEach(a=>{
      const items=a.stickers.map(x=>({id:a.id+":"+x[0],name:x[0],icon:x[1]}));
      const count=items.filter(x=>state.stickers.includes(x.id)).length,card=document.createElement("article");card.className="album-card";
      const head=document.createElement("div");head.className="album-head";const title=document.createElement("strong");title.textContent=a.icon+" "+a.name;
      const countEl=document.createElement("span");countEl.textContent=count+"/5";head.append(title,countEl);
      const grid=document.createElement("div");grid.className="sticker-grid";
      items.forEach(s=>{const owned=state.stickers.includes(s.id),tile=document.createElement("div");tile.className="sticker"+(owned?" sticker-owned":"");
        const icon=document.createElement("span");icon.textContent=owned?s.icon:"❔";const label=document.createElement("small");label.textContent=owned?s.name:"Unknown";tile.append(icon,label);grid.append(tile);});
      const reward=document.createElement("p");reward.textContent=state.completedAlbums.includes(a.id)?"🏆 Album completed · reward collected":"Complete all five stickers for 8,000 coins + 5,000 followers";
      card.append(head,grid,reward);box.append(card);
    });
  }
  function checkAlbumRewards(){
    albumBlueprints.forEach(a=>{const ids=a.stickers.map(s=>a.id+":"+s[0]);if(ids.every(id=>state.stickers.includes(id))&&!state.completedAlbums.includes(a.id)){
      state.completedAlbums.push(a.id);state.coins+=8000;state.followers+=5000;addLog("⭐ ALBUM COMPLETE: "+a.name+"! +8,000 coins and +5,000 followers.");checkAchievements();
    }});
  }
  function collectSticker(){
    const s=allStickers[Math.floor(Math.random()*allStickers.length)];
    if(state.stickers.includes(s.id)){state.stickerStars++;addLog("⭐ DUPLICATE STICKER: "+s.name+" converted to 1 sticker star.");return;}
    state.stickers.push(s.id);addLog("⭐ STICKER DROP: "+s.name+" added to your album.");checkAlbumRewards();
  }
  function tradeStickerStars(){
    const missing=allStickers.filter(s=>!state.stickers.includes(s.id));if(state.stickerStars<3||!missing.length)return;
    state.stickerStars-=3;const s=missing[Math.floor(Math.random()*missing.length)];state.stickers.push(s.id);
    addLog("🔁 STICKER TRADE: "+s.name+" added to your album.");checkAlbumRewards();renderUI();save();
  }
  function checkAchievements(){
    achievements.forEach(a=>{if(!state.achievements.includes(a.id)&&a.done(state)){state.achievements.push(a.id);state.coins+=a.reward;addLog("🏅 ACHIEVEMENT: "+a.name+" unlocked! Bonus +"+number(a.reward)+" coins.");}});
  }
  function awardHype(amount){
    state.hype+=amount;
    if(state.hype>=100){state.hype-=100;state.followers+=15000;state.coins+=5000;addLog("🌟 HYPE BURST! +15,000 followers and +5,000 coins.");}
  }
  function upgradeLandmark(index){
    const lm=state.landmarks[index];if(!lm||lm.level>=5||state.completed)return;
    const cost=costFor(lm.level);if(state.coins<cost){addLog("Not enough coins. You need "+number(cost)+" coins.");return;}
    state.coins-=cost;lm.level++;state.followers+=lm.level*500;state.daily.upgrades++;
    addLog("🏗️ "+lm.name+" upgraded to level "+lm.level+". +"+number(lm.level*500)+" followers.");
    checkCompletion();checkAchievements();renderUI();save();
  }
  function upgradeCheapest(){
    const choices=state.landmarks.map((l,i)=>({l,i})).filter(x=>x.l.level<5).sort((a,b)=>costFor(a.l.level)-costFor(b.l.level));
    const pick=choices.find(x=>state.coins>=costFor(x.l.level));
    if(!pick){addLog("You need more coins for your next upgrade. Earn coins by rolling the board.");return;}upgradeLandmark(pick.i);
  }
  function checkCompletion(){
    const levels=totalLevels();
    if(levels>0&&levels%5===0&&levels<25){state.coins+=5000;state.followers+=10000;state.posts+=5;addLog("🌍 NEW ERA: "+eras[levels/5]+" unlocked! +5,000 coins, +10,000 followers and +5 posts.");}
    if(levels>=25&&!state.completionRewarded){state.completed=true;state.completionRewarded=true;state.coins+=50000;state.followers+=100000;addLog("🏆 MEMEOPOLY COMPLETED! All 25 upgrades built. You are the ultimate meme mogul!");}
    if(levels>=25)state.completed=true;
  }
  function collectCard(){
    const missing=memeCards.filter(n=>!state.cards.includes(n));
    if(!missing.length){state.coins+=2500;addLog("🃏 Duplicate pack! +2,500 coins.");collectSticker();return;}
    const card=missing[Math.floor(Math.random()*missing.length)];state.cards.push(card);state.followers+=3000;state.daily.cards++;
    addLog("🃏 RARE DROP: "+card+" collected! +3,000 followers.");
    if(state.cards.length===8){state.coins+=10000;addLog("🌟 FULL COLLECTION! Bonus +10,000 coins.");}
    collectSticker();checkAchievements();
  }
  function raid(){const ready=state.rivals.filter(r=>state.turn>=r.cooldownTurn&&totalLevels()>=(districts.find(d=>d.name===r.district)?.unlock??0));if(ready.length){const target=ready[Math.floor(Math.random()*ready.length)];attackRival(target.id);}else{const reward=1500+Math.floor(Math.random()*2000);state.coins+=reward;addLog("🏦 Rival studios are on alert. You salvage +"+number(reward)+" coins from the algorithm.");}}
  function rivalAttack(){if(state.shields>0){state.shields--;addLog("🛡️ SHUTDOWN BLOCKED! Your shield protected your creator studio. "+state.shields+" shield(s) remain.");return;}const loss=Math.min(state.coins,500+Math.floor(Math.random()*1501));state.coins-=loss;state.hype=Math.max(0,state.hype-10);addLog("💥 RIVAL SHUTDOWN! Your studio was hit. -"+number(loss)+" coins. Earn a shield to block the next attack.");}
  function shieldDrop(){if(state.shields<3){state.shields++;addLog("🛡️ SHIELD COLLECTED! Your studio has "+state.shields+" shield(s).");}else{state.coins+=1500;addLog("🛡️ Shield stash full. Converted to +1,500 coins.");}}
  function viralEvent(){
    const events=[
      ()=>{state.coins+=2500;addLog("🔥 Your post went viral! +2,500 coins.");},
      ()=>{state.followers+=5000;addLog("🚀 Explore page takeover! +5,000 followers.");},
      ()=>{state.posts+=5;addLog("🎁 Creator care package! +5 posts.");},
      ()=>{awardHype(40);addLog("⚡ The crowd is buzzing! +40 hype.");},
      ()=>{state.coins+=1000;state.followers+=1500;addLog("🤝 A brand deal landed! +1,000 coins and +1,500 followers.");}
    ];events[Math.floor(Math.random()*events.length)]();
  }
  function handleTile(tile){
    switch(tile.type){
      // The board-lap reward is paid when crossing Go; landing on Go must not pay it twice.
      case"GO":addLog("🚀 GO VIRAL! Board-lap creator bonus collected.");break;
      case"COINS":{const reward=1000+currentEra()*500;state.coins+=reward;addLog("🪙 "+tile.name+"! +"+number(reward)+" coins.");break;}
      case"LANDMARK":{const lm=state.landmarks.find(l=>l.name===tile.name);addLog("🏙️ You landed on "+tile.name+". "+(lm.level===0?"Build it in the Empire tab.":"Upgrade it in the Empire tab."));break;}
      case"CARD":collectCard();break;case"RAID":raid();break;case"ATTACK":rivalAttack();break;case"EVENT":viralEvent();break;
      case"HYPE":awardHype(30);addLog("⚡ Hype boost! +30 hype.");break;
      case"SHIELD":shieldDrop();break;
    }
  }
  const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  async function rollPost(){
    regenerateDice();normalizeDaily();
    const multiplier=Number($("rollMultiplier").value)||1;
    if(rolling||state.dice<multiplier||state.completed)return;
    rolling=true;state.dice-=multiplier;state.turn++;state.daily.rolls++;
    $("postBtn").disabled=true;$("diceIcon").classList.add("rolling");
    const roll=Math.floor(Math.random()*6)+1,steps=roll*multiplier;
    $("diceText").textContent="Rolling…";
    for(let i=0;i<steps;i++){
      await wait(multiplier>1?45:100);
      setDiceFace(Math.floor(Math.random()*6)+1);
      state.position=(state.position+1)%board.length;
      if(state.position===0){
        const lap=1500*multiplier;
        state.coins+=lap;state.followers+=250*multiplier;
        addLog("🏁 BOARD LAP! +"+number(lap)+" coins and "+number(250*multiplier)+" followers.");
      }
      renderBoard();
    }
    $("diceIcon").classList.remove("rolling");
    setDiceFace(roll);
    $("diceText").textContent=roll+" × "+multiplier+" = "+steps+" spaces";
    handleTile(board[state.position]);
    rolling=false;checkAchievements();renderUI();save();
  }
  function publishPost(){
    if(state.completed||state.posts<1)return;
    state.posts--;state.totalPostsPublished++;state.daily.posts++;
    state.coins+=250;state.followers+=500;awardHype(10);
    addLog("📣 POST PUBLISHED! +250 coins, +500 followers and +10 hype.");
    checkAchievements();renderUI();save();
  }
  function claimDailyBonus(){
    normalizeDaily();if(state.dailyBonusDate===today()||state.completed)return;
    state.dailyBonusDate=today();state.coins+=2000;state.followers+=2000;state.dice=Math.min(MAX_DICE,state.dice+5);
    addLog("🎁 DAILY CREATOR BONUS! +2,000 coins, +2,000 followers and +5 dice.");renderUI();save();
  }
  function claimMission(id){
    normalizeDaily();const m=missions.find(x=>x.id===id);if(!m||state.daily.claims.includes(id)||m.progress(state)<m.goal)return;
    state.daily.claims.push(id);state.coins+=m.reward;addLog("✅ DAILY MISSION COMPLETE: "+m.name+". +"+number(m.reward)+" coins.");renderUI();save();
  }
  function exportSave(){
    try{
      const blob=new Blob([JSON.stringify(state,null,2)],{type:"application/json"});
      const url=URL.createObjectURL(blob),a=document.createElement("a");
      a.href=url;a.download="memeopoly-save-"+today()+".json";a.click();URL.revokeObjectURL(url);
      $("saveStatus").textContent="Backup downloaded";
    }catch(e){$("saveStatus").textContent="Backup could not be created";}
  }
  function importSave(file){
    if(!file)return;
    const reader=new FileReader();
    reader.onload=()=>{
      try{
        const candidate=JSON.parse(String(reader.result||""));
        if(!candidate||!Array.isArray(candidate.landmarks)||!Array.isArray(candidate.cards))throw new Error("Invalid save");
        const previous=localStorage.getItem(SAVE_KEY);
        try{
          localStorage.setItem(SAVE_KEY,JSON.stringify(candidate));
          state=freshState();
          if(!load())throw new Error("Save could not be restored");
          checkAchievements();renderUI();save();addLog("📦 Backup restored successfully.");
          $("saveStatus").textContent="Backup restored";
        }catch(error){
          if(previous===null)localStorage.removeItem(SAVE_KEY);else localStorage.setItem(SAVE_KEY,previous);
          state=freshState();load();renderUI();
          throw error;
        }
      }catch(e){$("saveStatus").textContent="That backup file is invalid or damaged";}
      $("importSaveFile").value="";
    };
    reader.onerror=()=>{$("saveStatus").textContent="Could not read backup file";$("importSaveFile").value="";};
    reader.readAsText(file);
  }
  function switchScreen(id){document.querySelectorAll(".screen").forEach(s=>s.classList.toggle("hidden",s.id!==id));document.querySelectorAll(".tab").forEach(t=>t.classList.toggle("activeTab",t.dataset.screen===id));}
  $("postBtn").addEventListener("click",rollPost);$("publishBtn").addEventListener("click",publishPost);$("rollMultiplier").addEventListener("change",()=>renderUI());$("upgradeBtn").addEventListener("click",upgradeCheapest);$("dailyBonusBtn").addEventListener("click",claimDailyBonus);
  document.querySelectorAll(".tab").forEach(t=>t.addEventListener("click",()=>switchScreen(t.dataset.screen)));
  $("clearLogBtn").addEventListener("click",()=>{state.log=[];renderLog();save();});$("tradeStarsBtn").addEventListener("click",tradeStickerStars);
  $("tutorialDismissBtn").addEventListener("click",()=>{state.tutorialSeen=true;$("tutorialPanel").classList.add("hidden");save();});
  $("exportSaveBtn").addEventListener("click",exportSave);
  $("importSaveBtn").addEventListener("click",()=>$("importSaveFile").click());
  $("importSaveFile").addEventListener("change",event=>importSave(event.target.files?.[0]));
  $("newGameBtn").addEventListener("click",()=>{if(rolling)return;if(!window.confirm("Start a new Memeopoly game? Your saved game will be replaced."))return;state=freshState();switchScreen("boardScreen");renderUI();addLog("🎲 A fresh game begins. Build your meme empire!");save();});
  window.addEventListener("beforeinstallprompt",event=>{event.preventDefault();installPrompt=event;$("installBtn").classList.remove("hidden");});
  $("installBtn").addEventListener("click",async()=>{if(!installPrompt)return;installPrompt.prompt();await installPrompt.userChoice;installPrompt=null;$("installBtn").classList.add("hidden");});
  if("serviceWorker"in navigator)window.addEventListener("load",()=>navigator.serviceWorker.register("./sw.js").catch(()=>{$("saveStatus").textContent="Offline mode unavailable";}));
  const resumed=load();normalizeDaily();renderUI();
  if(state.tutorialSeen)$("tutorialPanel").classList.add("hidden");
  window.setInterval(()=>{const before=state.dice;regenerateDice();if(state.dice!==before){renderUI();save();}},30000);
  if(resumed)addLog("👋 Welcome back! Your saved empire has been restored.");
  else{addLog("🎲 Welcome to Memeopoly! Roll the board, collect cards and build your empire.");addLog("💡 Tip: complete daily missions for extra coins.");}
  save();
})();