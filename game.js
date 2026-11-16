(() => {
  "use strict";

  const SAVE_KEY = "memeopoly-save-v1";
  const eras = ["Bedroom Meme Era", "Vine House Era", "YouTube Shorts Era", "TikTok Empire Era", "Meme HQ Era"];
  const board = [
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
  const coords = [[1,1],[1,2],[1,3],[1,4],[1,5],[2,5],[3,5],[4,5],[5,5],[5,4],[5,3],[5,2],[5,1],[4,1],[3,1],[2,1]];
  const landmarkNames = ["Bedroom Setup","Cheap Mic","Gaming PC","Ring Light","Meme Studio"];
  const landmarkIcons = ["🏠","🎤","💻","💡","🏢"];
  const memeCards = ["Doge","Pepe","NPC Streamer","Side Eye Cat","Sigma Baby","Ohio Boss","Viral Rat","Distracted Boyfriend"];
  const cardIcons = ["🐕","🐸","🕺","🐈","🗿","👑","🐀","💑"];
  const freshState = () => ({
    position:0, coins:5000, followers:0, posts:12, hype:0, turn:0,
    landmarks:landmarkNames.map(name=>({name,level:0})),
    cards:[], completed:false, log:[]
  });
  let state = freshState();
  let rolling = false;

  const $ = id => document.getElementById(id);
  const number = n => Math.floor(n).toLocaleString();
  const totalLevels = () => state.landmarks.reduce((sum,item)=>sum+item.level,0);
  const currentEra = () => Math.min(eras.length-1,Math.floor(totalLevels()/5));
  const costFor = level => 500 + level*750 + currentEra()*350;
  const addLog = message => {
    const entry = {message, time:new Date().toLocaleTimeString([], {hour:"2-digit",minute:"2-digit"})};
    state.log.unshift(entry);
    state.log = state.log.slice(0,40);
    renderLog();
    save();
  };
  const save = () => {
    try {
      localStorage.setItem(SAVE_KEY,JSON.stringify(state));
      $("saveStatus").textContent = "✓ Progress saved";
    } catch (error) {
      $("saveStatus").textContent = "Save unavailable in this browser";
    }
  };
  const load = () => {
    try {
      const raw = localStorage.getItem(SAVE_KEY);
      if (!raw) return false;
      const saved = JSON.parse(raw);
      if (!saved || !Array.isArray(saved.landmarks) || !Array.isArray(saved.cards)) return false;
      state = {...freshState(),...saved};
      state.landmarks = landmarkNames.map(name => {
        const old = saved.landmarks.find(item=>item && item.name===name);
        return {name,level:Math.max(0,Math.min(5,Number(old?.level)||0))};
      });
      state.cards = memeCards.filter(name=>saved.cards.includes(name));
      state.log = Array.isArray(saved.log) ? saved.log.slice(0,40) : [];
      state.position = Math.max(0,Math.min(15,Number(state.position)||0));
      state.coins = Math.max(0,Number(state.coins)||0);
      state.followers = Math.max(0,Number(state.followers)||0);
      state.posts = Math.max(0,Number(state.posts)||0);
      state.hype = Math.max(0,Math.min(99,Number(state.hype)||0));
      state.turn = Math.max(0,Number(state.turn)||0);
      state.completed = totalLevels()>=25 || Boolean(saved.completed);
      return true;
    } catch (error) { return false; }
  };

  function renderBoard() {
    const boardEl = $("board");
    boardEl.querySelectorAll(".tile").forEach(tile=>tile.remove());
    board.forEach((item,index)=>{
      const tile = document.createElement("article");
      const [row,col] = coords[index];
      tile.className = "tile " + item.type.toLowerCase() + (state.position===index ? " player" : "");
      tile.style.gridRow = String(row);
      tile.style.gridColumn = String(col);
      const icon = document.createElement("span"); icon.className="tile-icon"; icon.textContent=item.icon;
      const name = document.createElement("strong"); name.textContent=item.name;
      const detail = document.createElement("small"); detail.textContent=item.type==="LANDMARK" ? "Lv "+state.landmarks.find(l=>l.name===item.name).level+"/5" : item.description;
      tile.append(icon,name,detail);
      if(state.position===index){const token=document.createElement("span");token.className="token";token.textContent="😂";token.setAttribute("aria-label","Your token");tile.append(token);}
      boardEl.append(tile);
    });
  }

  function renderLandmarks() {
    const box=$("landmarks"); box.replaceChildren();
    state.landmarks.forEach((landmark,index)=>{
      const card=document.createElement("article"); card.className="landmark-card";
      const top=document.createElement("div"); top.className="landmark-top";
      const icon=document.createElement("span"); icon.className="landmark-icon"; icon.textContent=landmarkIcons[index];
      const title=document.createElement("div"); const name=document.createElement("strong"); name.textContent=landmark.name;
      const level=document.createElement("small"); level.textContent="LEVEL "+landmark.level+" / 5";
      title.append(name,level); top.append(icon,title);
      const bar=document.createElement("div"); bar.className="small-bar";
      const fill=document.createElement("div"); fill.className="small-fill"; fill.style.width=(landmark.level*20)+"%"; bar.append(fill);
      const bottom=document.createElement("div"); bottom.className="landmark-bottom";
      const cost=document.createElement("span"); cost.textContent=landmark.level===5 ? "MAX LEVEL" : "🪙 "+number(costFor(landmark.level));
      const button=document.createElement("button"); button.type="button"; button.textContent=landmark.level===0?"BUILD":landmark.level===5?"MAXED":"UPGRADE";
      button.disabled=landmark.level===5 || state.coins<costFor(landmark.level) || state.completed;
      button.addEventListener("click",()=>upgradeLandmark(index));
      bottom.append(cost,button); card.append(top,bar,bottom); box.append(card);
    });
    $("empireLevel").textContent=totalLevels();
    $("milestoneText").textContent=state.completed ? "You built the ultimate Memeopoly empire!" : (5-totalLevels()%5)+" more upgrade(s) until the next era milestone.";
  }

  function renderCollection() {
    const box=$("collection"); box.replaceChildren();
    memeCards.forEach((name,index)=>{
      const owned=state.cards.includes(name);
      const card=document.createElement("article"); card.className="collect-card"+(owned?" owned":"");
      const icon=document.createElement("span"); icon.className="collect-icon"; icon.textContent=owned?cardIcons[index]:"❔";
      const title=document.createElement("strong"); title.textContent=owned?name:"Unknown meme";
      const status=document.createElement("small"); status.textContent=owned?"COLLECTED":"NOT DISCOVERED";
      card.append(icon,title,status); box.append(card);
    });
    $("collectionCount").textContent=state.cards.length+"/8";
    $("cardCount").textContent=state.cards.length;
    $("centerCards").textContent=state.cards.length;
  }

  function renderLog() {
    const box=$("log"); box.replaceChildren();
    state.log.forEach(item=>{
      const line=document.createElement("div"); line.className="log-entry";
      const time=document.createElement("time"); time.textContent=item.time||"";
      const message=document.createElement("span"); message.textContent=item.message||"";
      line.append(time,message); box.append(line);
    });
  }

  function renderUI() {
    $("coins").textContent=number(state.coins);
    $("followers").textContent=number(state.followers);
    $("posts").textContent=number(state.posts);
    $("hype").textContent=state.hype;
    $("turnCount").textContent=state.turn;
    $("eraName").textContent=state.completed?"Memeopoly Champion":eras[currentEra()];
    $("progressText").textContent=totalLevels()+" / 25 upgrades";
    $("progressFill").style.width=(totalLevels()/25*100)+"%";
    $("eraProgress").textContent=Math.round(totalLevels()/25*100)+"%";
    $("postBtn").disabled=rolling||state.posts<=0||state.completed;
    $("postBtn").textContent=state.completed?"EMPIRE COMPLETE":rolling?"ROLLING…":"ROLL & POST";
    $("rollHint").textContent=state.completed?"You have conquered the board.":state.posts+" posts available · 1 post per turn";
    $("centerMessage").textContent=state.completed?"🏆 You are the Memeopoly champion!":"Your next viral moment starts here.";
    $("upgradeBtn").disabled=state.completed||!state.landmarks.some(l=>l.level<5);
    renderBoard(); renderLandmarks(); renderCollection(); renderLog();
  }

  function awardHype(amount) {
    state.hype=Math.min(100,state.hype+amount);
    if(state.hype>=100){
      state.hype=0; state.followers+=15000; state.coins+=5000;
      addLog("🌟 HYPE BURST! +15,000 followers and +5,000 coins.");
    }
  }

  function upgradeLandmark(index) {
    const landmark=state.landmarks[index];
    if(!landmark||landmark.level>=5||state.completed) return;
    const cost=costFor(landmark.level);
    if(state.coins<cost){addLog("Not enough coins. You need "+number(cost)+" coins.");return;}
    state.coins-=cost; landmark.level++;
    const fans=landmark.level*500;
    state.followers+=fans;
    addLog("🏗️ "+landmark.name+" upgraded to level "+landmark.level+". +"+number(fans)+" followers.");
    checkCompletion(); renderUI(); save();
  }

  function upgradeCheapest() {
    const candidates=state.landmarks.map((l,i)=>({l,i})).filter(x=>x.l.level<5).sort((a,b)=>costFor(a.l.level)-costFor(b.l.level));
    if(!candidates.length){checkCompletion();return;}
    const choice=candidates.find(x=>state.coins>=costFor(x.l.level));
    if(!choice){addLog("You need more coins for your next upgrade. Earn coins by rolling the board.");return;}
    upgradeLandmark(choice.i);
  }

  function checkCompletion() {
    const levels=totalLevels();
    if(levels>0&&levels%5===0&&levels<25){
      const unlocked=eras[Math.min(eras.length-1,levels/5)];
      state.coins+=5000; state.followers+=10000; state.posts+=5;
      addLog("🌍 NEW ERA: "+unlocked+" unlocked! Milestone bonus: 5,000 coins, 10,000 followers and 5 posts.");
    }
    if(levels>=25&&!state.completed){
      state.completed=true; state.coins+=50000; state.followers+=100000;
      addLog("🏆 MEMEOPOLY COMPLETED! All 25 upgrades built. You are the ultimate meme mogul!");
    }
  }

  function collectCard() {
    const missing=memeCards.filter(name=>!state.cards.includes(name));
    if(!missing.length){state.coins+=2500;addLog("🃏 Your pack was a duplicate haul. +2,500 coins.");return;}
    const card=missing[Math.floor(Math.random()*missing.length)];
    state.cards.push(card); state.followers+=3000;
    addLog("🃏 RARE DROP: "+card+" collected! +3,000 followers.");
    if(state.cards.length===memeCards.length){state.coins+=10000;addLog("🌟 FULL COLLECTION! Bonus +10,000 coins.");}
  }

  function raid() {
    const reward=2000+Math.floor(Math.random()*5001)+currentEra()*500;
    state.coins+=reward; state.followers+=Math.floor(reward/2);
    addLog("🏦 Algorithm raid success! +"+number(reward)+" coins and +"+number(Math.floor(reward/2))+" followers.");
  }

  function rivalAttack() {
    const loss=Math.min(state.coins,500+Math.floor(Math.random()*1501));
    const gain=500+Math.floor(Math.random()*1501);
    state.coins-=loss; state.hype=Math.max(0,state.hype-10);
    addLog("⚔️ Rival drama! You lost "+number(loss)+" coins, but picked up "+number(gain)+" followers.");
    state.followers+=gain;
  }

  function viralEvent() {
    const events=[
      ()=>{state.coins+=2500;addLog("🔥 Your post went viral! +2,500 coins.");},
      ()=>{state.followers+=5000;addLog("🚀 Explore page takeover! +5,000 followers.");},
      ()=>{state.posts+=5;addLog("🎁 Creator care package! +5 posts.");},
      ()=>{awardHype(40);addLog("⚡ The crowd is buzzing! +40 hype.");},
      ()=>{state.coins+=1000;state.followers+=1500;addLog("🤝 A brand deal landed! +1,000 coins and +1,500 followers.");}
    ];
    events[Math.floor(Math.random()*events.length)]();
  }

  function handleTile(tile) {
    switch(tile.type){
      case "GO": state.coins+=1500; state.followers+=500; addLog("🚀 GO VIRAL! +1,500 coins and +500 followers."); break;
      case "COINS": {const reward=1000+currentEra()*500;state.coins+=reward;addLog("🪙 "+tile.name+"! +"+number(reward)+" coins.");break;}
      case "LANDMARK": {const lm=state.landmarks.find(l=>l.name===tile.name);addLog("🏙️ You landed on "+tile.name+". "+(lm.level===0?"Build it in the Empire tab.":"Upgrade it in the Empire tab."));break;}
      case "CARD": collectCard(); break;
      case "RAID": raid(); break;
      case "ATTACK": rivalAttack(); break;
      case "EVENT": viralEvent(); break;
      case "HYPE": awardHype(30); addLog("⚡ Hype boost! +30 hype."); break;
    }
  }

  const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  async function rollPost() {
    if(rolling||state.posts<=0||state.completed)return;
    rolling=true; state.posts--; state.turn++;
    $("postBtn").disabled=true; $("diceIcon").classList.add("rolling");
    const dice=Math.floor(Math.random()*6)+1;
    $("diceText").textContent="Rolling…";
    for(let i=0;i<dice;i++){
      await wait(115);
      state.position=(state.position+1)%board.length;
      if(state.position===0){state.coins+=1500;addLog("🏁 Board lap complete! +1,500 coins.");}
      renderBoard();
    }
    $("diceIcon").classList.remove("rolling");
    $("diceText").textContent="You rolled a "+dice+"!";
    handleTile(board[state.position]);
    rolling=false; renderUI(); save();
  }

  function switchScreen(screenId) {
    document.querySelectorAll(".screen").forEach(screen=>screen.classList.toggle("hidden",screen.id!==screenId));
    document.querySelectorAll(".tab").forEach(tab=>tab.classList.toggle("activeTab",tab.dataset.screen===screenId));
  }

  $("postBtn").addEventListener("click",rollPost);
  $("upgradeBtn").addEventListener("click",upgradeCheapest);
  document.querySelectorAll(".tab").forEach(tab=>tab.addEventListener("click",()=>switchScreen(tab.dataset.screen)));
  $("clearLogBtn").addEventListener("click",()=>{state.log=[];renderLog();save();});
  $("newGameBtn").addEventListener("click",()=>{
    if(!window.confirm("Start a new Memeopoly game? Your saved game will be replaced."))return;
    state=freshState(); switchScreen("boardScreen"); renderUI();
    addLog("🎲 A fresh game begins. Build your meme empire!");
    save();
  });

  const resumed=load();
  renderUI();
  if(resumed)addLog("👋 Welcome back! Your saved empire has been restored.");
  else {addLog("🎲 Welcome to Memeopoly! Roll the board, collect cards and build your empire.");addLog("💡 Tip: upgrade landmarks in the Empire tab to unlock new eras.");}
  save();
})();