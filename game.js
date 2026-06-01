const boardSize = 24;
let position = 0;
let coins = 5000;
let energy = 40;
let hype = 0;

const tiles = [
  { type: "GO" },
  { type: "CHANNEL", name: "Group Chats", price: 600, rent: 80, level: 0 },
  { type: "EVENT" },
  { type: "CHANNEL", name: "Facebook Pages", price: 700, rent: 90, level: 0 },
  { type: "TAX", amount: 200 },
  { type: "BOOST" },
  { type: "CHANNEL", name: "Instagram Reels", price: 1000, rent: 140, level: 0 },
  { type: "EVENT" },
  { type: "CHANNEL", name: "TikTok Trends", price: 1200, rent: 180, level: 0 },
  { type: "BANK" },
  { type: "CHANNEL", name: "YouTube Shorts", price: 1300, rent: 200, level: 0 },
  { type: "EVENT" },
  { type: "CHANNEL", name: "YouTube Channels", price: 1600, rent: 240, level: 0 },
  { type: "TAX", amount: 300 },
  { type: "BOOST" },
  { type: "CHANNEL", name: "X Feeds", price: 1400, rent: 210, level: 0 },
  { type: "EVENT" },
  { type: "CHANNEL", name: "Meme Pages", price: 1800, rent: 280, level: 0 },
  { type: "BANK" },
  { type: "CHANNEL", name: "Discord Servers", price: 1500, rent: 230, level: 0 },
  { type: "EVENT" },
  { type: "CHANNEL", name: "Viral Networks", price: 2400, rent: 380, level: 0 },
  { type: "TAX", amount: 400 },
  { type: "BOOST" }
];

function log(msg) {
  const logDiv = document.getElementById("log");
  logDiv.innerHTML += `<div>${msg}</div>`;
  logDiv.scrollTop = logDiv.scrollHeight;
}

function renderBoard() {
  const board = document.getElementById("board");
  board.innerHTML = "";
  tiles.forEach((t, i) => {
    const div = document.createElement("div");
    div.className = "tile" + (i === position ? " player" : "");
    div.innerText = t.type === "CHANNEL"
      ? `${t.name}\nLv ${t.level}`
      : t.type;
    board.appendChild(div);
  });
}

function updateStats() {
  document.getElementById("coins").innerText = coins;
  document.getElementById("energy").innerText = energy;
  document.getElementById("hype").innerText = hype;
}

function handleTile(tile) {
  if (tile.type === "GO") {
    log("You're back at GO.");
  }

  if (tile.type === "CHANNEL") {
    if (!tile.owned) {
      if (coins >= tile.price) {
        coins -= tile.price;
        tile.owned = true;
        log(`You bought ${tile.name} for ${tile.price} coins.`);
      } else {
        log(`Not enough coins to buy ${tile.name}.`);
      }
    } else {
      const rent = tile.rent * (1 + tile.level * 0.5);
      coins += rent;
      hype += 10;
      log(`Your ${tile.name} earned ${rent} engagement.`);
    }
  }

  if (tile.type === "EVENT") {
    const events = [
      "Algorithm boost! +200 coins.",
      "Shadowban! -150 coins.",
      "Trend surge! +20 hype.",
      "Drama wave! -10 hype."
    ];

    const e = events[Math.floor(Math.random() * events.length)];
    log(e);

    if (e.includes("+200")) coins += 200;
    if (e.includes("-150")) coins -= 150;
    if (e.includes("+20")) hype += 20;
    if (e.includes("-10")) hype = Math.max(0, hype - 10);
  }

  if (tile.type === "BOOST") {
    energy += 2;
    log("Boost tile! +2 energy.");
  }

  if (tile.type === "TAX") {
    coins -= tile.amount;
    log(`Tax hit! -${tile.amount} coins.`);
  }

  if (tile.type === "BANK") {
    coins += 100;
    energy += 1;
    log("Bank bonus! +100 coins, +1 energy.");
  }
}

document.getElementById("postBtn").addEventListener("click", () => {
  if (energy <= 0) {
    log("Out of energy. Come back later.");
    return;
  }

  energy--;

  const move = Math.floor(Math.random() * 6) + 1;
  hype = Math.min(100, hype + 15);

  const oldPos = position;
  position = (position + move) % boardSize;

  if (position < oldPos) {
    coins += 200;
    log("Passed GO! +200 coins.");
  }

  log(`Post went live. Reach: ${move}`);

  handleTile(tiles[position]);

  updateStats();
  renderBoard();
});

renderBoard();
updateStats();
log("Game started. Hit POST to go viral.");