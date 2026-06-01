const eras = [
  "Bedroom Meme Era",
  "Vine House Era",
  "YouTube Shorts Era",
  "TikTok Empire Era",
  "Meme HQ Era"
];

const board = [
  { type:"GO", icon:"🚀", name:"Go Viral" },
  { type:"COINS", icon:"💵", name:"Ad Revenue" },
  { type:"RAID", icon:"💰", name:"Algorithm Raid" },
  { type:"CARD", icon:"🃏", name:"Meme Pack" },

  { type:"LANDMARK", icon:"🏠", name:"Bedroom Setup" },

  { type:"ATTACK", icon:"⚔️", name:"Cancel Strike" },

  { type:"LANDMARK", icon:"🎤", name:"Cheap Mic" },

  { type:"HYPE", icon:"⚡", name:"Hype Boost" },

  { type:"LANDMARK", icon:"💻", name:"Gaming PC" },

  { type:"EVENT", icon:"🔥", name:"Viral Event" },

  { type:"LANDMARK", icon:"💡", name:"Ring Light" },

  { type:"RAID", icon:"🏦", name:"Follower Heist" },

  { type:"LANDMARK", icon:"🏢", name:"Meme Studio" },

  { type:"ATTACK", icon:"💀", name:"Shadowban" },

  { type:"COINS", icon:"🤝", name:"Sponsor Deal" },

  { type:"EVENT", icon:"🎁", name:"Bonus Event" }
];

let landmarks = [
  { name:"Bedroom Setup", level:0 },
  { name:"Cheap Mic", level:0 },
  { name:"Gaming PC", level:0 },
  { name:"Ring Light", level:0 },
  { name:"Meme Studio", level:0 }
];

const memeCards = [
  "Doge",
  "Pepe",
  "NPC Streamer",
  "Side Eye Cat",
  "Sigma Baby",
  "Ohio Boss",
  "Viral Rat",
  "Distracted Boyfriend"
];

let position = 0;
let coins = 5000;
let followers = 0;
let posts = 50;
let hype = 0;
let era = 0;
let cards = [];
let rolling = false;

function log(text){
  const box = document.getElementById("log");
  box.innerHTML += `<div>${text}</div>`;
  box.scrollTop = box.scrollHeight;
}

function updateUI(){
  document.getElementById("coins").innerText = Math.floor(coins).toLocaleString();
  document.getElementById("followers").innerText = Math.floor(followers).toLocaleString();
  document.getElementById("posts").innerText = posts;
  document.getElementById("hype").innerText = hype;

  const total = landmarks.reduce((s,l)=>s+l.level,0);
  const max = 25;

  document.getElementById("eraName").innerText =
    eras[Math.min(era, eras.length-1)];

  document.getElementById("progressText").innerText =
    `${total}/${max} upgrades`;

  document.getElementById("progressFill").style.width =
    `${(total/max)*100}%`;
}

function renderBoard(){

  const order = [
    0,1,2,3,
    11,null,null,4,
    10,null,null,5,
    9,8,7,6
  ];

  const boardDiv = document.getElementById("board");
  boardDiv.innerHTML = "";

  order.forEach(index=>{

    const div = document.createElement("div");

    if(index === null){
      div.style.background = "transparent";
      div.style.border = "none";
      boardDiv.appendChild(div);
      return;
    }

    const tile = board[index];

    let cls = "tile";

    if(tile.type === "LANDMARK") cls += " landmark";
    if(tile.type === "COINS" || tile.type === "GO" || tile.type === "HYPE") cls += " good";
    if(tile.type === "ATTACK") cls += " bad";
    if(tile.type === "RAID" || tile.type === "CARD" || tile.type === "EVENT") cls += " special";

    if(index === position) cls += " player";

    div.className = cls;

    let levelText = "";

    if(tile.type === "LANDMARK"){
      const lm = landmarks.find(l=>l.name===tile.name);
      levelText = `\nLv ${lm.level}/5`;
    }

    div.innerHTML = `
      ${tile.icon}<br>
      ${tile.name}
      ${levelText}
      ${index===position ? '<div class="token">😂</div>' : ''}
    `;

    boardDiv.appendChild(div);

  });

}

function renderLandmarks(){

  const box = document.getElementById("landmarks");
  box.innerHTML = "";

  landmarks.forEach(l=>{

    const div = document.createElement("div");
    div.className = "landmarkCard";

    div.innerHTML = `
      <b>${l.name}</b>
      <div>Level ${l.level}/5</div>
      <div class="smallBar">
        <div class="smallFill" style="width:${l.level*20}%"></div>
      </div>
    `;

    box.appendChild(div);

  });

}

function upgradeCost(level){
  return 500 + (level * 1000) + (era * 1500);
}

function upgradeCheapest(){

  const available =
    landmarks.filter(l=>l.level<5);

  if(!available.length){
    checkEraComplete();
    return;
  }

  available.sort(
    (a,b)=>upgradeCost(a.level)-upgradeCost(b.level)
  );

  const lm = available[0];

  const cost = upgradeCost(lm.level);

  if(coins < cost){
    log(`❌ Need ${cost.toLocaleString()} coins.`);
    return;
  }

  coins -= cost;
  lm.level++;

  followers += lm.level * 500;

  log(`🏗️ Upgraded ${lm.name} to Level ${lm.level}`);

  checkEraComplete();

}

function checkEraComplete(){

  const done =
    landmarks.every(l=>l.level>=5);

  if(!done) return;

  era++;

  coins += 10000;
  followers += 25000;
  posts += 25;

  landmarks.forEach(l=>l.level=0);

  if(era >= eras.length){

    log("🏆 MEMEOPOLY COMPLETED!");

  } else {

    log(`🌍 New Era Unlocked: ${eras[era]}`);

  }

}

function collectCard(){

  const card =
    memeCards[Math.floor(Math.random()*memeCards.length)];

  if(cards.includes(card)){

    coins += 2000;
    log(`🃏 Duplicate ${card} → +2000 coins`);

  }else{

    cards.push(card);

    followers += 3000;

    log(`🃏 New Card: ${card}`);

  }

}

function raid(){

  const reward =
    2000 + Math.floor(Math.random()*5000);

  coins += reward;

  followers += Math.floor(reward/2);

  log(`💰 Raid Success +${reward.toLocaleString()} coins`);

}

function attack(){

  const reward =
    1000 + Math.floor(Math.random()*3000);

  coins += reward;

  hype += 20;

  log(`⚔️ Rival hit +${reward.toLocaleString()} coins`);

}

function viralEvent(){

  const roll =
    Math.floor(Math.random()*4);

  if(roll===0){
    coins += 2500;
    log("🔥 Viral Post");
  }

  if(roll===1){
    followers += 5000;
    log("🚀 Explore Page");
  }

  if(roll===2){
    posts += 10;
    log("🎁 Free Posts");
  }

  if(roll===3){
    hype += 40;
    log("⚡ Huge Hype");
  }

}

function handleTile(tile){

  if(tile.type==="GO"){
    coins += 1500;
  }

  if(tile.type==="COINS"){
    coins += 1000 + era*500;
  }

  if(tile.type==="LANDMARK"){
    upgradeCheapest();
  }

  if(tile.type==="CARD"){
    collectCard();
  }

  if(tile.type==="RAID"){
    raid();
  }

  if(tile.type==="ATTACK"){
    attack();
  }

  if(tile.type==="EVENT"){
    viralEvent();
  }

  if(tile.type==="HYPE"){
    hype += 30;
  }

  if(hype >= 100){

    hype = 0;

    followers += 15000;
    coins += 5000;

    log("🌟 HYPE BURST!");

  }

}

async function animateMove(steps){

  for(let i=0;i<steps;i++){

    await new Promise(r=>setTimeout(r,120));

    position++;

    if(position>=board.length){
      position=0;
      coins += 1500;
      log("🏁 Completed Board Lap");
    }

    renderBoard();

  }

}

async function rollPost(){

  if(rolling) return;

  if(posts<=0){
    log("❌ No Posts Left");
    return;
  }

  rolling = true;

  posts--;

  const diceIcon =
    document.getElementById("diceIcon");

  diceIcon.classList.add("rolling");

  let dice =
    Math.floor(Math.random()*6)+1;

  document.getElementById("diceText")
    .innerText = `Rolled ${dice}`;

  await animateMove(dice);

  handleTile(board[position]);

  diceIcon.classList.remove("rolling");

  updateUI();
  renderBoard();
  renderLandmarks();

  rolling = false;

}

document.getElementById("postBtn")
.addEventListener("click",rollPost);

document.getElementById("upgradeBtn")
.addEventListener("click",()=>{

  upgradeCheapest();

  updateUI();
  renderLandmarks();
  renderBoard();

});

document.getElementById("boardTab")
.onclick=()=>{

  document.getElementById("boardScreen")
  .classList.remove("hidden");

  document.getElementById("empireScreen")
  .classList.add("hidden");

};

document.getElementById("empireTab")
.onclick=()=>{

  document.getElementById("empireScreen")
  .classList.remove("hidden");

  document.getElementById("boardScreen")
  .classList.add("hidden");

};

renderBoard();
renderLandmarks();
updateUI();

log("🎲 Welcome to Memeopoly V4");
log("😂 Token now moves around the board");
log("🏗️ Build landmarks in Empire tab");