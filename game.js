(() => {
  "use strict";
  const SAVE_KEY="memeopoly-save-v1";
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
    {type:"COINS",icon:"🤝",name:"Sponsor Deal",description:"Earn a sponsor payout"},
    {type:"CARD",icon:"🎁",name:"Mystery Drop",description:"Open a mystery pack"},
    {type:"EVENT",icon:"🌈",name:"Community Event",description:"See what happens"}
  ];
  const coords=[[1,1],[1,2],[1,3],[1,4],[1,5],[2,5],[3,5],[4,5],[5,5],[5,4],[5,3],[5,2],[5,1],[4,1],[3,1],[2,1]];
  const landmarkNames=["Bedroom Setup","Cheap Mic","Gaming PC","Ring Light","Meme Studio"];
  const landmarkIcons=["🏠","🎤","💻","💡","🏢"];
  const memeCards=["Doge","Pepe","NPC Streamer","Side Eye Cat","Sigma Baby","Ohio Boss","Viral Rat","Distracted Boyfriend"];
  const cardIcons=["🐕","🐸","🕺","🐈","🗿","👑","🐀","💑"];
  const achievements=[
    {id:"first-roll",name:"First Post",icon:"📱",description:"Take your first turn.",reward:500,done:s=>s.turn>=1},
    {id:"first-build",name:"Under Construction",icon:"🏗️",description:"Build your first landmark.",reward:1000,done:s=>levelCount(s)>=1},
    {id:"first-card",name:"Collector",icon:"🃏",description:"Find your first meme card.",reward:1000,done:s=>s.cards.length>=1},
    {id:"ten-turns",name:"Algorithm Regular",icon:"🔥",description:"Take 10 turns.",reward:2500,done:s=>s.turn>=10},
    {id:"five-levels",name:"Big Creator Energy",icon:"⭐",description:"Reach five landmark upgrades.",reward:5000,done:s=>levelCount(s)>=5},
    {id:"full-set",name:"Meme Museum",icon:"🏆",description:"Collect all eight meme cards.",reward:10000,done:s=>s.cards.length===8}
  ];
  const missions=[
    {id:"rolls",name:"Feed the algorithm",icon:"🎲",description:"Take 3 turns today.",goal:3,reward:1200,progress:s=>s.daily.rolls},
    {id:"builds",name:"Upgrade your setup",icon:"🛠️",description:"Build or upgrade 1 landmark today.",goal:1,reward:1800,progress:s=>s.daily.upgrades},
    {id:"cards",name:"Meme hunter",icon:"🃏",description:"Find 1 meme card today.",goal:1,reward:2200,progress:s=>s.daily.cards}
  ];
  const today=()=>{const d=new Date();return d.getFullYear()+"-"+String(d.getMonth()+1).padStart(2,"0")+"-"+String(d.getDate()).padStart(2,"0");};
  const freshDaily=()=>({date:today(),rolls:0,upgrades:0,cards:0,claims:[]});
  const freshState=()=>({position:0,coins:5000,followers:0,posts:12,hype:0,turn:0,landmarks:landmarkNames.map(name=>({name,level:0})),cards:[],completed:false,log:[],daily:freshDaily(),dailyBonusDate:"",achievements:[]});
  let state=freshState(),rolling=false,installPrompt=null;
  const $=id=>document.getElementById(id);
  const number=n=>Math.floor(n).toLocaleString();
  const levelCount=s=>s.landmarks.reduce((sum,item)=>sum+item.level,0);
  const totalLevels=()=>levelCount(state);
  const currentEra=()=>Math.min(eras.length-1,Math.floor(totalLevels()/5));
  const costFor=level=>500+level*750+currentEra()*350;
  const save=()=>{try{localStorage.setItem(SAVE_KEY,JSON.stringify(state));$("saveStatus").textContent="✓ Progress saved";}catch(e){$("saveStatus").textContent="Save unavailable in this browser";}};
  const addLog=message=>{state.log.unshift({message,time:new Date().toLocaleTimeString([],{hour:"2-digit",minute:"2-digit"})});state.log=state.log.slice(0,40);renderLog();save();};
  function normalizeDaily(){
    if(!state.daily||state.daily.date!==today())state.daily=freshDaily();
    if(!Array.isArray(state.daily.claims))state.daily.claims=[];
  }
  function load(){
    try{
      const raw=localStorage.getItem(SAVE_KEY);if(!raw)return false;
      const saved=JSON.parse(raw);if(!saved||!Array.isArray(saved.landmarks)||!Array.isArray(saved.cards))return false;
      state={...freshState(),...saved};
      state.landmarks=landmarkNames.map(name=>{const old=saved.landmarks.find(x=>x&&x.name===name);return{name,level:Math.max(0,Math.min(5,Number(old?.level)||0))};});
      state.cards=memeCards.filter(name=>saved.cards.includes(name));
      state.log=Array.isArray(saved.log)?saved.log.slice(0,40):[];
      state.achievements=Array.isArray(saved.achievements)?saved.achievements.filter(id=>achievements.some(a=>a.id===id)):[];
      state.position=Math.max(0,Math.min(15,Number(state.position)||0));
      for(const key of ["coins","followers","posts","turn"])state[key]=Math.max(0,Number(state[key])||0);
      state.hype=Math.max(0,Math.min(99,Number(state.hype)||0));
      state.completed=totalLevels()>=25||Boolean(saved.completed);
      normalizeDaily();return true;
    }catch(e){return false;}
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
    $("coins").textContent=number(state.coins);$("followers").textContent=number(state.followers);$("posts").textContent=number(state.posts);$("hype").textContent=state.hype;$("turnCount").textContent=state.turn;
    $("eraName").textContent=state.completed?"Memeopoly Champion":eras[currentEra()];$("progressText").textContent=totalLevels()+" / 25 upgrades";$("progressFill").style.width=totalLevels()/25*100+"%";$("eraProgress").textContent=Math.round(totalLevels()/25*100)+"%";
    $("postBtn").disabled=rolling||state.posts<=0||state.completed;$("postBtn").textContent=state.completed?"EMPIRE COMPLETE":rolling?"ROLLING…":"ROLL & POST";$("rollHint").textContent=state.completed?"You have conquered the board.":state.posts+" posts available · 1 post per turn";$("centerMessage").textContent=state.completed?"🏆 You are the Memeopoly champion!":"Your next viral moment starts here.";
    $("upgradeBtn").disabled=state.completed||!state.landmarks.some(l=>l.level<5);
    renderBoard();renderLandmarks();renderCollection();renderLog();renderDaily();renderAchievements();
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
    if(levels>=25&&!state.completed){state.completed=true;state.coins+=50000;state.followers+=100000;addLog("🏆 MEMEOPOLY COMPLETED! All 25 upgrades built. You are the ultimate meme mogul!");}
  }
  function collectCard(){
    const missing=memeCards.filter(n=>!state.cards.includes(n));
    if(!missing.length){state.coins+=2500;addLog("🃏 Duplicate pack! +2,500 coins.");return;}
    const card=missing[Math.floor(Math.random()*missing.length)];state.cards.push(card);state.followers+=3000;state.daily.cards++;
    addLog("🃏 RARE DROP: "+card+" collected! +3,000 followers.");
    if(state.cards.length===8){state.coins+=10000;addLog("🌟 FULL COLLECTION! Bonus +10,000 coins.");}
    checkAchievements();
  }
  function raid(){const reward=2000+Math.floor(Math.random()*5001)+currentEra()*500;state.coins+=reward;state.followers+=Math.floor(reward/2);addLog("🏦 Algorithm raid success! +"+number(reward)+" coins and +"+number(Math.floor(reward/2))+" followers.");}
  function rivalAttack(){const loss=Math.min(state.coins,500+Math.floor(Math.random()*1501)),gain=500+Math.floor(Math.random()*1501);state.coins-=loss;state.hype=Math.max(0,state.hype-10);state.followers+=gain;addLog("⚔️ Rival drama! -"+number(loss)+" coins, but +"+number(gain)+" followers.");}
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
      case"GO":state.coins+=1500;state.followers+=500;addLog("🚀 GO VIRAL! +1,500 coins and +500 followers.");break;
      case"COINS":{const reward=1000+currentEra()*500;state.coins+=reward;addLog("🪙 "+tile.name+"! +"+number(reward)+" coins.");break;}
      case"LANDMARK":{const lm=state.landmarks.find(l=>l.name===tile.name);addLog("🏙️ You landed on "+tile.name+". "+(lm.level===0?"Build it in the Empire tab.":"Upgrade it in the Empire tab."));break;}
      case"CARD":collectCard();break;case"RAID":raid();break;case"ATTACK":rivalAttack();break;case"EVENT":viralEvent();break;
      case"HYPE":awardHype(30);addLog("⚡ Hype boost! +30 hype.");break;
    }
  }
  const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  async function rollPost(){
    if(rolling||state.posts<=0||state.completed)return;
    rolling=true;state.posts--;state.turn++;state.daily.rolls++;$("postBtn").disabled=true;$("diceIcon").classList.add("rolling");
    const dice=Math.floor(Math.random()*6)+1;$("diceText").textContent="Rolling…";
    for(let i=0;i<dice;i++){await wait(115);state.position=(state.position+1)%board.length;if(state.position===0){state.coins+=1500;addLog("🏁 Board lap complete! +1,500 coins.");}renderBoard();}
    $("diceIcon").classList.remove("rolling");$("diceText").textContent="You rolled a "+dice+"!";handleTile(board[state.position]);
    rolling=false;checkAchievements();renderUI();save();
  }
  function claimDailyBonus(){
    normalizeDaily();if(state.dailyBonusDate===today()||state.completed)return;
    state.dailyBonusDate=today();state.coins+=2000;state.followers+=2000;
    addLog("🎁 DAILY CREATOR BONUS! +2,000 coins and +2,000 followers.");renderUI();save();
  }
  function claimMission(id){
    normalizeDaily();const m=missions.find(x=>x.id===id);if(!m||state.daily.claims.includes(id)||m.progress(state)<m.goal)return;
    state.daily.claims.push(id);state.coins+=m.reward;addLog("✅ DAILY MISSION COMPLETE: "+m.name+". +"+number(m.reward)+" coins.");renderUI();save();
  }
  function switchScreen(id){document.querySelectorAll(".screen").forEach(s=>s.classList.toggle("hidden",s.id!==id));document.querySelectorAll(".tab").forEach(t=>t.classList.toggle("activeTab",t.dataset.screen===id));}
  $("postBtn").addEventListener("click",rollPost);$("upgradeBtn").addEventListener("click",upgradeCheapest);$("dailyBonusBtn").addEventListener("click",claimDailyBonus);
  document.querySelectorAll(".tab").forEach(t=>t.addEventListener("click",()=>switchScreen(t.dataset.screen)));
  $("clearLogBtn").addEventListener("click",()=>{state.log=[];renderLog();save();});
  $("newGameBtn").addEventListener("click",()=>{if(rolling)return;if(!window.confirm("Start a new Memeopoly game? Your saved game will be replaced."))return;state=freshState();switchScreen("boardScreen");renderUI();addLog("🎲 A fresh game begins. Build your meme empire!");save();});
  window.addEventListener("beforeinstallprompt",event=>{event.preventDefault();installPrompt=event;$("installBtn").classList.remove("hidden");});
  $("installBtn").addEventListener("click",async()=>{if(!installPrompt)return;installPrompt.prompt();await installPrompt.userChoice;installPrompt=null;$("installBtn").classList.add("hidden");});
  if("serviceWorker"in navigator)window.addEventListener("load",()=>navigator.serviceWorker.register("./sw.js").catch(()=>{$("saveStatus").textContent="Offline mode unavailable";}));
  const resumed=load();normalizeDaily();renderUI();
  if(resumed)addLog("👋 Welcome back! Your saved empire has been restored.");
  else{addLog("🎲 Welcome to Memeopoly! Roll the board, collect cards and build your empire.");addLog("💡 Tip: complete daily missions for extra coins.");}
  save();
})();