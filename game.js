const board = [
  { type: "GO", name: "GO VIRAL" },
  { type: "CHANNEL", name: "Meme Page", cost: 500, followers: 100 },
  { type: "EVENT", name: "Trending Topic" },
  { type: "CHANNEL", name: "TikTok Clips", cost: 800, followers: 150 },
  { type: "BOOST", name: "Hype Boost" },
  { type: "CHANNEL", name: "Instagram Reels", cost: 1200, followers: 250 },
  { type: "EVENT", name: "Drama Alert" },
  { type: "CHANNEL", name: "YouTube Shorts", cost: 1600, followers: 350 },
  { type: "BANK", name: "Sponsor Deal" },
  { type: "CHANNEL", name: "Discord Community", cost: 2200, followers: 500 },
  { type: "EVENT", name: "Algorithm Change" },
  { type: "CHANNEL", name: "Viral Network", cost: 3000, followers: 800 }
];

let position = 0;
let coins = 5000;
let followers = 0;
let hype = 0;
let posts = 30;

function log(text) {
  const logBox = document.getElementById("log");
  logBox.innerHTML += `<div>${text}</div>`;
  logBox.scrollTop = logBox.scrollHeight;
}

function renderBoard() {
  const boardDiv = document.getElementById("board");
  boardDiv.innerHTML = "";

  board.forEach((tile, i) => {
    const div = document.createElement("div");

    let cls = "tile";

    if (i === position) cls += " player";
    if (tile.owned) cls += " owned";

    div.className = cls;

    if (tile.type === "CHANNEL") {
      div.innerText =
        `${tile.name}\nLv.${tile.level || 1}`;
    } else {
      div.innerText = tile.name;
    }

    boardDiv.appendChild(div);
  });
}

function updateUI() {
  document.getElementById("coins").innerText = coins;
  document.getElementById("followers").innerText = followers;
  document.getElementById("hype").innerText = hype;
  document.getElementById("posts").innerText = posts;
}

function eventCard() {
  const roll = Math.floor(Math.random() * 4);

  if (roll === 0) {
    coins += 500;
    log("🔥 Viral meme! +500 coins");
  }

  if (roll === 1) {
    followers += 500;
    log("🚀 Trend explosion! +500 followers");
  }

  if (roll === 2) {
    coins -= 300;
    log("💀 Shadowban! -300 coins");
  }

  if (roll === 3) {
    hype += 20;
    log("⚡ Meme surge! +20 hype");
  }
}

function handleTile(tile) {
  if (tile.type === "GO") {
    coins += 300;
    log("🏁 GO VIRAL! +300 coins");
  }

  if (tile.type === "CHANNEL") {
    if (!tile.owned) {
      if (coins >= tile.cost) {
        coins -= tile.cost;
        tile.owned = true;
        tile.level = 1;
        log(`📺 Bought ${tile.name}`);
      }
    } else {
      const payout = tile.followers * tile.level;
      followers += payout;
      coins += Math.floor(payout / 2);
      log(`💰 ${tile.name} generated ${payout} followers`);
    }
  }

  if (tile.type === "EVENT") {
    eventCard();
  }

  if (tile.type === "BOOST") {
    hype += 15;
    log("⚡ Hype boosted!");
  }

  if (tile.type === "BANK") {
    coins += 1000;
    log("🤝 Sponsor deal! +1000 coins");
  }

  if (hype >= 100) {
    hype = 0;
    followers += 2000;
    coins += 1000;
    log("🚀 HYPE BURST! +2000 followers +1000 coins");
  }
}

document.getElementById("postBtn").addEventListener("click", () => {
  if (posts <= 0) {
    log("No posts remaining.");
    return;
  }

  posts--;

  const dice = Math.floor(Math.random() * 6) + 1;

  document.getElementById("dice").innerText = dice;

  position += dice;

  if (position >= board.length) {
    position %= board.length;
    coins += 500;
    log("🏁 Completed a lap! +500 coins");
  }

  hype += 10;

  handleTile(board[position]);

  if (followers >= 100000) {
    document.getElementById("message").innerText =
      "🏆 YOU BUILT A MEME EMPIRE!";
  }

  updateUI();
  renderBoard();
});

document.getElementById("upgradeBtn").addEventListener("click", () => {
  const tile = board[position];

  if (
    tile.type === "CHANNEL" &&
    tile.owned
  ) {
    const cost = tile.level * 500;

    if (coins >= cost) {
      coins -= cost;
      tile.level++;
      log(`⬆️ Upgraded ${tile.name} to Level ${tile.level}`);
    } else {
      log("Not enough coins.");
    }
  } else {
    log("Land on an owned channel first.");
  }

  updateUI();
  renderBoard();
});

renderBoard();
updateUI();

log("🎲 Welcome to Memeopoly.");
log("Reach 100,000 followers to win.");