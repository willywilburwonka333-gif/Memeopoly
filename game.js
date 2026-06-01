const board = [
  { type: "GO", icon: "🚀", name: "Go Viral" },
  { type: "COINS", icon: "💵", name: "Ad Money" },
  { type: "ATTACK", icon: "⚔️", name: "Cancel Strike" },
  { type: "LANDMARK", icon: "🏠", name: "Bedroom Setup" },
  { type: "CARD", icon: "🃏", name: "Meme Pack" },
  { type: "RAID", icon: "💰", name: "Algorithm Raid" },
  { type: "LANDMARK", icon: "🎤", name: "Cheap Mic" },
  { type: "HYPE", icon: "⚡", name: "Hype Boost" },
  { type: "ATTACK", icon: "💀", name: "Shadowban" },
  { type: "LANDMARK", icon: "💻", name: "Gaming PC" },
  { type: "COINS", icon: "🤝", name: "Sponsor Deal" },
  { type: "RAID", icon: "🏦", name: "Follower Heist" },
  { type: "LANDMARK", icon: "💡", name: "Ring Light" },
  { type: "EVENT", icon: "🔥", name: "Viral Event" },
  { type: "ATTACK", icon: "📉", name: "Rival Strike" },
  { type: "LANDMARK", icon: "🏢", name: "Meme Studio" }
];

const eras = [
  "Bedroom Meme Era",
  "Vine House Era",
  "YouTube Shorts Era",
  "TikTok Empire Era",
  "Meme HQ Era"
];

let landmarks = [
  { name: "Bedroom Setup", icon: "🏠", level: 0 },
  { name: "Cheap Mic", icon: "🎤", level: 0 },
  { name: "Gaming PC", icon: "💻", level: 0 },
  { name: "Ring Light", icon: "💡", level: 0 },
  { name: "Meme Studio", icon: "🏢", level: 0 }
];

const memeCards = [
  "Doge", "Pepe", "Side Eye Cat", "NPC Streamer",
  "Crying Laughing Guy", "Sigma Baby", "Ohio Boss", "Viral Rat"
];

let position = 0;
let coins = 5000;
let followers = 0;
let posts = 50;
let hype = 0;
let era = 0;
let cards = [];
let rolling = false;

function money(n) {
  return Math.floor(n).toLocaleString();
}

function log(text) {
  const box = document.getElementById("log");
  box.innerHTML += `<div>${text}</div>`;
  box.scrollTop = box.scrollHeight;
}

function updateUI() {
  document.getElementById("coins").innerText = money(coins);
  document.getElementById("followers").innerText = money(followers);
  document.getElementById("posts").innerText = posts;
  document.getElementById("hype").innerText = hype;

  const totalLevels = landmarks.reduce((sum, l) => sum + l.level, 0);
  const maxLevels = landmarks.length * 5;
  const percent = (totalLevels / maxLevels) * 100;

  document.getElementById("eraName").innerText = eras[era] || "Ultimate Meme Empire";
  document.getElementById("progressText").innerText = `${totalLevels}/${maxLevels} upgrades`;
  document.getElementById("progressFill").style.width = `${percent}%`;
}

function renderBoard() {
  const boardDiv = document.getElementById("board");
  boardDiv.innerHTML = "";

  board.forEach((tile, index) => {
    const div = document.createElement("div");

    let cls = "tile";
    if (index === position) cls += " player";
    if (tile.type === "LANDMARK") cls += " landmarkTile";
    if (["GO", "COINS", "HYPE"].includes(tile.type)) cls += " goodTile";
    if (["ATTACK"].includes(tile.type)) cls += " badTile";
    if (["RAID", "CARD", "EVENT"].includes(tile.type)) cls += " specialTile";

    div.className = cls;

    if (tile.type === "LANDMARK") {
      const lm = landmarks.find(l => l.name === tile.name);
      div.innerText = `${tile.icon}\n${tile.name}\nLv ${lm.level}/5`;
    } else {
      div.innerText = `${tile.icon}\n${tile.name}`;
    }

    boardDiv.appendChild(div);
  });
}

function renderLandmarks() {
  const box = document.getElementById("landmarks");
  box.innerHTML = "";

  landmarks.forEach(lm => {
    const cost = getUpgradeCost(lm.level);
    const div = document.createElement("div");
    div.className = "landmark";

    div.innerHTML = `
      <div class="landmarkTop">
        <span>${lm.icon} ${lm.name}</span>
        <span>Lv ${lm.level}/5</span>
      </div>
      <div class="miniBar">
        <div class="miniFill" style="width:${(lm.level / 5) * 100}%"></div>
      </div>
      <div style="margin-top:6px;color:#cbd5e1;">
        ${lm.level >= 5 ? "Complete" : "Next upgrade: " + money(cost) + " coins"}
      </div>
    `;

    box.appendChild(div);
  });
}

function getUpgradeCost(level) {
  return 600 + level * 900 + era * 1500;
}

function setDiceText(text) {
  document.getElementById("diceText").innerText = text;
}

function upgradeLandmark(name) {
  const lm = landmarks.find(l => l.name === name);
  if (!lm) return;

  if (lm.level >= 5) {
    log(`✅ ${name} is already complete.`);
    return;
  }

  const cost = getUpgradeCost(lm.level);

  if (coins < cost) {
    log(`❌ Need ${money(cost)} coins to upgrade ${name}.`);
    return;
  }

  coins -= cost;
  lm.level += 1;
  followers += lm.level * 400;
  hype += 8;

  log(`🏗️ Built ${name} to Level ${lm.level}.`);
  checkEraComplete();
}

function upgradeCheapestLandmark() {
  const available = landmarks.filter(l => l.level < 5);

  if (available.length === 0) {
    checkEraComplete();
    return;
  }

  available.sort((a, b) => getUpgradeCost(a.level) - getUpgradeCost(b.level));
  upgradeLandmark(available[0].name);
}

function checkEraComplete() {
  const complete = landmarks.every(l => l.level >= 5);

  if (!complete) return;

  era += 1;
  coins += 8000;
  followers += 20000;
  posts += 25;
  hype = 0;

  if (era >= eras.length) {
    setDiceText("🏆 Ultimate Meme Empire Complete!");
    log("🏆 You completed every era. Memeopoly conquered.");
    return;
  }

  landmarks = landmarks.map(l => ({ ...l, level: 0 }));

  log(`🌍 Era complete! Unlocked ${eras[era]}. +8,000 coins, +20,000 fans, +25 posts.`);
}

function collectMemeCard() {
  const card = memeCards[Math.floor(Math.random() * memeCards.length)];

  if (!cards.includes(card)) {
    cards.push(card);
    followers += 2500;
    log(`🃏 New meme card: ${card}. +2,500 fans.`);
  } else {
    coins += 1500;
    log(`🃏 Duplicate ${card}. Converted to +1,500 coins.`);
  }

  if (cards.length === memeCards.length) {
    coins += 15000;
    followers += 40000;
    cards = [];
    log("📚 Meme album complete! +15,000 coins +40,000 fans.");
  }
}

function raid() {
  const targets = ["NPC Influencer", "Fake Guru", "Reaction Channel", "Crypto Streamer", "Drama Page"];
  const target = targets[Math.floor(Math.random() * targets.length)];
  const reward = 2000 + Math.floor(Math.random() * 6000) + era * 1000;

  coins += reward;
  followers += Math.floor(reward / 2);

  log(`💰 Algorithm Raid: hit ${target} and gained ${money(reward)} coins.`);
}

function attack() {
  const targets = ["Rival Meme Lord", "Copycat Page", "Spam Bot Army", "Clout Chaser", "Comment Warrior"];
  const target = targets[Math.floor(Math.random() * targets.length)];
  const reward = 1200 + Math.floor(Math.random() * 4000) + era * 700;

  coins += reward;
  hype += 20;

  log(`⚔️ Shadowban attack on ${target}. +${money(reward)} coins +20 hype.`);
}

function viralEvent() {
  const events = [
    () => {
      coins += 2500 + era * 1000;
      log("🔥 Viral repost chain. Big ad money gained.");
    },
    () => {
      followers += 6000 + era * 2000;
      log("🚀 Meme hit the explore page. Fans exploded.");
    },
    () => {
      posts += 10;
      log("🎁 Free post pack. +10 posts.");
    },
    () => {
      hype += 40;
      log("⚡ Internet surge. +40 hype.");
    },
    () => {
      coins = Math.max(0, coins - 1200);
      log("💀 Bad take backlash. Lost 1,200 coins.");
    }
  ];

  events[Math.floor(Math.random() * events.length)]();
}

function handleTile(tile) {
  if (tile.type === "GO") {
    coins += 1500;
    followers += 1000;
    log("🚀 GO VIRAL bonus. +1,500 coins +1,000 fans.");
  }

  if (tile.type === "COINS") {
    const reward = 1000 + era * 700;
    coins += reward;
    log(`💵 ${tile.name}. +${money(reward)} coins.`);
  }

  if (tile.type === "LANDMARK") {
    upgradeLandmark(tile.name);
  }

  if (tile.type === "CARD") {
    collectMemeCard();
  }

  if (tile.type === "RAID") {
    raid();
  }

  if (tile.type === "ATTACK") {
    attack();
  }

  if (tile.type === "EVENT") {
    viralEvent();
  }

  if (tile.type === "HYPE") {
    hype += 35;
    log("⚡ Hype Boost. +35 hype.");
  }

  if (hype >= 100) {
    hype = 0;
    coins += 5000;
    followers += 12000;
    posts += 5;
    log("🌟 HYPE BURST! +5,000 coins +12,000 fans +5 posts.");
  }
}

function rollPost() {
  if (rolling) return;

  if (posts <= 0) {
    setDiceText("No posts left. Earn more posts.");
    log("❌ No posts left.");
    return;
  }

  rolling = true;
  posts -= 1;
  hype += 10;

  const diceIcon = document.getElementById("diceIcon");
  diceIcon.classList.add("rolling");
  setDiceText("Rolling...");

  let flashes = 0;
  const flashInterval = setInterval(() => {
    const temp = Math.floor(Math.random() * 6) + 1;
    setDiceText(`Rolling ${temp}...`);
    flashes++;
    if (flashes >= 6) clearInterval(flashInterval);
  }, 100);

  setTimeout(() => {
    const dice = Math.floor(Math.random() * 6) + 1;
    setDiceText(`Rolled ${dice}`);

    const oldPosition = position;
    position = (position + dice) % board.length;

    if (position < oldPosition) {
      coins += 1500;
      log("🏁 Completed a full board lap. +1,500 coins.");
    }

    const tile = board[position];
    log(`🎲 Landed on ${tile.icon} ${tile.name}.`);
    handleTile(tile);

    diceIcon.classList.remove("rolling");
    rolling = false;

    updateUI();
    renderBoard();
    renderLandmarks();
  }, 800);

  updateUI();
}

document.getElementById("postBtn").addEventListener("click", rollPost);

document.getElementById("upgradeBtn").addEventListener("click", () => {
  upgradeCheapestLandmark();
  updateUI();
  renderBoard();
  renderLandmarks();
});

renderBoard();
renderLandmarks();
updateUI();

log("🎲 Welcome to Memeopoly V3.");
log("🎯 Complete all 5 landmarks to unlock the next internet era.");
log("💰 Raids, attacks, cards and viral events help you grow faster.");
setDiceText("Tap POST to roll.");