const board = [
  { type: "GO", name: "GO VIRAL" },
  { type: "COINS", name: "Ad Revenue" },
  { type: "ATTACK", name: "Cancel Raid" },
  { type: "LANDMARK", name: "Bedroom Setup" },
  { type: "EVENT", name: "Meme Card" },
  { type: "RAID", name: "Algorithm Raid" },
  { type: "LANDMARK", name: "Cheap Mic" },
  { type: "HYPE", name: "Hype Boost" },
  { type: "ATTACK", name: "Copyright Strike" },
  { type: "LANDMARK", name: "Gaming PC" },
  { type: "COINS", name: "Sponsor Deal" },
  { type: "RAID", name: "Follower Heist" },
  { type: "LANDMARK", name: "Ring Light" },
  { type: "EVENT", name: "Viral Event" },
  { type: "ATTACK", name: "Shadowban Rival" },
  { type: "LANDMARK", name: "Meme Studio" }
];

const eras = [
  "Bedroom Meme Era",
  "Vine House Era",
  "YouTube Shorts Era",
  "TikTok Empire Era",
  "Meme HQ Era"
];

const landmarks = [
  { name: "Bedroom Setup", level: 0 },
  { name: "Cheap Mic", level: 0 },
  { name: "Gaming PC", level: 0 },
  { name: "Ring Light", level: 0 },
  { name: "Meme Studio", level: 0 }
];

const memeCards = [
  "Doge",
  "Pepe",
  "Distracted Boyfriend",
  "NPC Streamer",
  "Side Eye Cat",
  "Crying Laughing Guy",
  "Sigma Baby",
  "Ohio Boss"
];

let position = 0;
let coins = 5000;
let followers = 0;
let hype = 0;
let posts = 40;
let era = 0;
let cardCollection = [];

function log(text) {
  const logBox = document.getElementById("log");
  logBox.innerHTML += `<div>${text}</div>`;
  logBox.scrollTop = logBox.scrollHeight;
}

function updateMessage(text) {
  document.getElementById("message").innerText = text;
}

function updateUI() {
  document.getElementById("coins").innerText = coins;
  document.getElementById("followers").innerText = followers;
  document.getElementById("hype").innerText = hype;
  document.getElementById("posts").innerText = posts;
}

function renderBoard() {
  const boardDiv = document.getElementById("board");
  boardDiv.innerHTML = "";

  board.forEach((tile, i) => {
    const div = document.createElement("div");
    div.className = "tile" + (i === position ? " player" : "");

    if (tile.type === "LANDMARK") {
      const lm = landmarks.find(l => l.name === tile.name);
      div.innerText = `${tile.name}\nLv.${lm.level}/5`;
    } else {
      div.innerText = tile.name;
    }

    boardDiv.appendChild(div);
  });
}

function getLandmarkCost(level) {
  return 500 + level * 750 + era * 1000;
}

function buildLandmark(name) {
  const lm = landmarks.find(l => l.name === name);

  if (!lm || lm.level >= 5) {
    log("✅ This landmark is already maxed.");
    return;
  }

  const cost = getLandmarkCost(lm.level);

  if (coins < cost) {
    log(`❌ Need ${cost} coins to upgrade ${name}.`);
    return;
  }

  coins -= cost;
  lm.level++;
  followers += 250 * lm.level;

  log(`🏗️ Upgraded ${name} to Level ${lm.level}.`);

  checkEraComplete();
}

function checkEraComplete() {
  const complete = landmarks.every(l => l.level >= 5);

  if (complete) {
    era++;
    coins += 5000;
    followers += 10000;
    posts += 20;

    landmarks.forEach(l => l.level = 0);

    if (era >= eras.length) {
      updateMessage("🏆 YOU BUILT THE ULTIMATE MEME EMPIRE!");
      log("🏆 Final empire complete. You won Memeopoly.");
    } else {
      updateMessage(`🌍 New Era Unlocked: ${eras[era]}`);
      log(`🌍 Era complete! Welcome to ${eras[era]}.`);
    }
  }
}

function collectCard() {
  const card = memeCards[Math.floor(Math.random() * memeCards.length)];

  if (!cardCollection.includes(card)) {
    cardCollection.push(card);
    followers += 1500;
    log(`🃏 New Meme Card collected: ${card}! +1500 followers`);
  } else {
    coins += 800;
    log(`🃏 Duplicate ${card}. Converted to +800 coins.`);
  }

  if (cardCollection.length === memeCards.length) {
    coins += 10000;
    followers += 25000;
    cardCollection = [];
    log("📚 Meme album complete! +10,000 coins +25,000 followers.");
  }
}

function raid() {
  const targets = [
    "NPC Influencer",
    "Crypto Bro Streamer",
    "Reaction Channel",
    "Fake Guru",
    "Comment Section Warrior"
  ];

  const target = targets[Math.floor(Math.random() * targets.length)];
  const reward = 1000 + Math.floor(Math.random() * 4000);

  coins += reward;
  followers += Math.floor(reward / 2);

  log(`💰 Algorithm Raid on ${target}! Stole ${reward} coins.`);
}

function attack() {
  const targets = [
    "Clout Chaser",
    "Drama Streamer",
    "Copycat Page",
    "Spam Bot Army",
    "Rival Meme Lord"
  ];

  const target = targets[Math.floor(Math.random() * targets.length)];
  const reward = 700 + Math.floor(Math.random() * 2500);

  coins += reward;
  hype += 15;

  log(`⚔️ Shadowbanned ${target}! +${reward} coins +15 hype.`);
}

function viralEvent() {
  const events = [
    () => {
      coins += 1500;
      log("🔥 Viral repost chain! +1500 coins.");
    },
    () => {
      followers += 3000;
      log("🚀 Your meme hit the explore page! +3000 followers.");
    },
    () => {
      coins -= 700;
      log("💀 Bad take backlash. -700 coins.");
    },
    () => {
      posts += 8;
      log("🎁 Free post pack! +8 posts.");
    },
    () => {
      hype += 35;
      log("⚡ Internet loves it! +35 hype.");
    }
  ];

  events[Math.floor(Math.random() * events.length)]();
}

function hypeBurst() {
  if (hype >= 100) {
    hype = 0;
    coins += 3000;
    followers += 7500;
    posts += 5;

    log("🚀 HYPE BURST! +3000 coins +7500 followers +5 posts.");
  }
}

function handleTile(tile) {
  if (tile.type === "GO") {
    coins += 1000;
    followers += 500;
    log("🏁 GO VIRAL bonus! +1000 coins +500 followers.");
  }

  if (tile.type === "COINS") {
    const reward = 800 + era * 400;
    coins += reward;
    log(`💵 Ad revenue! +${reward} coins.`);
  }

  if (tile.type === "LANDMARK") {
    buildLandmark(tile.name);
  }

  if (tile.type === "EVENT") {
    if (Math.random() > 0.5) {
      collectCard();
    } else {
      viralEvent();
    }
  }

  if (tile.type === "RAID") {
    raid();
  }

  if (tile.type === "ATTACK") {
    attack();
  }

  if (tile.type === "HYPE") {
    hype += 30;
    log("⚡ Hype Boost! +30 hype.");
  }

  hypeBurst();
}

document.getElementById("postBtn").addEventListener("click", () => {
  if (posts <= 0) {
    updateMessage("No posts left. You need to earn more posts.");
    log("❌ No posts remaining.");
    return;
  }

  posts--;

  const dice = Math.floor(Math.random() * 6) + 1;
  document.getElementById("dice").innerText = dice;

  const oldPosition = position;
  position = (position + dice) % board.length;

  if (position < oldPosition) {
    coins += 1000;
    log("🏁 Completed the board lap! +1000 coins.");
  }

  hype += 10;

  updateMessage(`${eras[era]} — landed on ${board[position].name}.`);

  handleTile(board[position]);

  updateUI();
  renderBoard();
});

document.getElementById("upgradeBtn").addEventListener("click", () => {
  const cheapest = landmarks
    .filter(l => l.level < 5)
    .sort((a, b) => getLandmarkCost(a.level) - getLandmarkCost(b.level))[0];

  if (!cheapest) {
    log("✅ All landmarks complete.");
    return;
  }

  buildLandmark(cheapest.name);

  updateUI();
  renderBoard();
});

renderBoard();
updateUI();

log("🎲 Welcome to Memeopoly V2.");
log("🏗️ Upgrade all 5 landmarks to complete an era.");
log("💰 Land on raids, attacks, events and boosts to grow.");
updateMessage(`Current Era: ${eras[era]}`);