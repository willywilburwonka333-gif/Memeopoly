using System;
using System.IO;
using UnityEngine;

namespace Memeopoly
{
    [Serializable]
    public sealed class LandmarkSave
    {
        public string name;
        public int level;
    }

    [Serializable]
    public sealed class LegacySave
    {
        public int worldIndex;
        public int position;
        public int coins;
        public int followers;
        public int dice;
        public int shields;
        public int hype;
        public int posts;
        public int turn;
        public long diceUpdatedAt;
        public LandmarkSave[] landmarks;
        public string[] cards;
    }

    [Serializable]
    public sealed class GameSave
    {
        public int schema = 1;
        public int worldIndex;
        public int position;
        public int coins = 5000;
        public int followers;
        public int dice = 30;
        public int shields = 1;
        public int hype;
        public int posts = 12;
        public int turn;
        public int totalRolls;
        public int[] buildings = new int[6]; // Six landmarks, level 0 through 5.
        public string[] cards = new string[0];
        public int lastDieA = 1;
        public int lastDieB = 1;
        public long diceUpdatedAt;
        public bool worldRewardPaid;
        // Persistent extra gameplay state; older v1 saves deserialize with safe defaults.
        public int selectedToken;
        public bool soundMuted;
        public bool pendingHeist;
        public bool pendingAttack;
        public int pendingMultiplier = 1;
        public int totalHeists;
        public int totalAttacks;
        public bool albumRewardPaid;
        public int dailyDay;
        public int dailyRolls;
        public int dailyBuilds;
        public int dailyHeists;
        public int dailyRewardMask;
    }

    public struct TurnOutcome
    {
        public int dieA, dieB, steps, destination, laps;
        public int coinDelta, followerDelta;
        public string message;
    }

    // Deterministic rules layer: gameplay never depends on animations or frame rate.
    public static class MemeopolyRules
    {
        public const int BoardSpaces = 60;
        public const int LandmarkCount = 6;
        public const int MaxLevel = 5;
        public const int MaxDice = 50;
        public const long DiceRegenMilliseconds = 300000L;

        public static readonly string[] Landmarks =
        {
            "Sharko Surf Shack", "Crocwing Flight Academy", "Prima Mocha Café",
            "Viral Video Tower", "Meme Factory", "Algorithm Palace"
        };

        public static readonly string[] Worlds =
        {
            "Meme Street", "Viral Avenue", "Influencer District", "Brainrot Boulevard",
            "Otaku City", "Meme Metropolis", "Glitch Kingdom", "Legendary Meme Empire"
        };

        // Tile order is identical to the existing browser board (60 spaces).
        public static readonly string[] TileKinds = (
            "GO,COINS,LANDMARK,CARD,ATTACK,LANDMARK,HYPE,EVENT,LANDMARK,RAID,SHIELD,LANDMARK,CARD,EVENT,LANDMARK," +
            "COINS,HYPE,LANDMARK,ATTACK,RAID,LANDMARK,COINS,CARD,LANDMARK,EVENT,SHIELD,LANDMARK,HYPE,COINS,LANDMARK," +
            "GO,COINS,LANDMARK,CARD,ATTACK,LANDMARK,HYPE,EVENT,LANDMARK,RAID,SHIELD,LANDMARK,CARD,EVENT,LANDMARK," +
            "COINS,HYPE,LANDMARK,ATTACK,RAID,LANDMARK,COINS,CARD,LANDMARK,EVENT,SHIELD,LANDMARK,HYPE,COINS,LANDMARK").Split(',');

        private static readonly string[] CardNames =
        {
            "Doge", "Pepe", "NPC Streamer", "Side Eye Cat", "Sigma Baby",
            "Ohio Boss", "Viral Rat", "Distracted Boyfriend"
        };

        public static GameSave NewGame(long nowMs)
        {
            return new GameSave { diceUpdatedAt = nowMs };
        }

        public static int TotalUpgrades(GameSave s)
        {
            int sum = 0;
            foreach (int level in s.buildings) sum += level;
            return sum;
        }

        public static int UpgradeCost(GameSave s, int index)
        {
            if (index < 0 || index >= LandmarkCount || s.buildings[index] >= MaxLevel) return -1;
            int era = Math.Min(4, TotalUpgrades(s) / 6);
            return 500 + s.buildings[index] * 750 + era * 350;
        }

        public static bool CanUpgrade(GameSave s, int index)
        {
            int cost = UpgradeCost(s, index);
            return cost > 0 && s.coins >= cost && !Completed(s);
        }

        public static bool Completed(GameSave s) { return TotalUpgrades(s) >= 30; }

        public static string Upgrade(GameSave s, int index)
        {
            if (!CanUpgrade(s, index)) return "Not enough coins or this landmark is complete.";
            int before = TotalUpgrades(s);
            int cost = UpgradeCost(s, index);
            s.coins -= cost;
            int level = ++s.buildings[index];
            s.followers += level * 500;
            int after = before + 1;
            string message = Landmarks[index] + " built to stage " + level + "/5!";
            if (after % 6 == 0 && after < 30)
            {
                s.coins += 5000;
                s.followers += 10000;
                s.posts += 5;
                message += " New era! +5,000 coins.";
            }
            if (after == 30 && !s.worldRewardPaid)
            {
                s.worldRewardPaid = true;
                s.coins += 50000;
                s.followers += 100000;
                message += " WORLD COMPLETE! +50,000 coins. Next world unlocked.";
            }
            return message;
        }

        public static void RegenerateDice(GameSave s, long nowMs)
        {
            if (s.diceUpdatedAt <= 0 || s.diceUpdatedAt > nowMs) s.diceUpdatedAt = nowMs;
            if (s.dice >= MaxDice) { s.dice = MaxDice; s.diceUpdatedAt = nowMs; return; }
            long intervals = (nowMs - s.diceUpdatedAt) / DiceRegenMilliseconds;
            if (intervals <= 0) return;
            int restored = (int)Math.Min(intervals, MaxDice - s.dice);
            s.dice += restored;
            s.diceUpdatedAt += restored * DiceRegenMilliseconds;
            if (s.dice == MaxDice) s.diceUpdatedAt = nowMs;
        }

        public static TurnOutcome Roll(GameSave s, int multiplier, int dieA, int dieB, System.Random rng)
        {
            if (multiplier != 1 && multiplier != 2 && multiplier != 5 && multiplier != 10)
                throw new ArgumentOutOfRangeException("multiplier");
            if (s.dice < multiplier || Completed(s) || s.pendingHeist || s.pendingAttack)
                throw new InvalidOperationException("Cannot roll until the pending event is completed.");
            if (dieA < 1 || dieA > 6 || dieB < 1 || dieB > 6)
                throw new ArgumentOutOfRangeException("dieA", "Dice must be 1 through 6.");

            s.dice -= multiplier;
            s.turn++;
            s.totalRolls++;
            s.lastDieA = dieA;
            s.lastDieB = dieB;
            int steps = dieA + dieB;
            int start = s.position;
            int laps = (start + steps) / BoardSpaces;
            s.position = (start + steps) % BoardSpaces;
            int coinsBefore = s.coins, followersBefore = s.followers;
            if (laps > 0)
            {
                s.coins += laps * 1500 * multiplier;
                s.followers += laps * 250 * multiplier;
            }
            string message = ResolveTile(s, s.position, multiplier, rng);
            return new TurnOutcome
            {
                dieA = dieA, dieB = dieB, steps = steps, destination = s.position,
                laps = laps, coinDelta = s.coins - coinsBefore,
                followerDelta = s.followers - followersBefore, message = message
            };
        }

        private static string ResolveTile(GameSave s, int index, int mult, System.Random rng)
        {
            switch (TileKinds[index])
            {
                case "GO": return "GO VIRAL! Lap bonus collected on crossing.";
                case "COINS":
                    int coins = (1000 + Math.Min(4, TotalUpgrades(s) / 6) * 500) * mult;
                    s.coins += coins;
                    return "Creator payday +" + coins.ToString("N0") + " coins";
                case "LANDMARK": return "Your empire is growing! Tap a city building to upgrade.";
                case "CARD":
                    string card = CardNames[rng.Next(CardNames.Length)];
                    if (Array.IndexOf(s.cards, card) >= 0)
                    {
                        s.coins += 2500 * mult;
                        return "Duplicate " + card + "! +2,500 coins x" + mult;
                    }
                    var next = new string[s.cards.Length + 1];
                    Array.Copy(s.cards, next, s.cards.Length);
                    next[next.Length - 1] = card;
                    s.cards = next;
                    s.followers += 3000;
                    if (MemeopolyProgression.AwardCompleteAlbum(s))
                        return "MEME ALBUM COMPLETE! +18,000 coins, +10 dice!";
                    return "Rare meme collected: " + card + "!";
                case "SHIELD":
                    s.shields = Math.Min(3, s.shields + 1);
                    return "Shield drop! " + s.shields + "/3 protection.";
                case "HYPE":
                    s.hype += 30 * mult;
                    if (s.hype >= 100)
                    {
                        s.hype %= 100;
                        s.followers += 15000;
                        s.coins += 5000;
                        return "HYPE BURST! +5,000 coins and 15,000 followers.";
                    }
                    return "Trending now! Hype +" + 30 * mult;
                case "RAID":
                    s.pendingHeist = true;
                    s.pendingMultiplier = mult;
                    return "ALGORITHM HEIST! Choose a vault to raid.";
                case "ATTACK":
                    s.pendingAttack = true;
                    s.pendingMultiplier = mult;
                    return "RIVAL CHALLENGE! Choose a creator to confront.";
                case "EVENT":
                    if (rng.Next(2) == 0)
                    {
                        s.followers += 1800 * mult;
                        return "VIRAL EVENT! +" + (1800 * mult).ToString("N0") + " followers.";
                    }
                    s.coins += 1200 * mult;
                    return "Surprise sponsorship! +" + (1200 * mult).ToString("N0") + " coins.";
                default: return "Keep rolling!";
            }
        }

        public static bool NextWorld(GameSave s)
        {
            if (!Completed(s)) return false;
            s.worldIndex++;
            s.position = 0;
            s.buildings = new int[LandmarkCount];
            s.worldRewardPaid = false;
            return true;
        }

        public static bool TryImportLegacy(string json, long nowMs, out GameSave s)
        {
            s = null;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                LegacySave old = JsonUtility.FromJson<LegacySave>(json);
                if (old == null || old.landmarks == null || old.landmarks.Length == 0 || old.cards == null) return false;
                s = NewGame(nowMs);
                s.worldIndex = Mathf.Clamp(old.worldIndex, 0, 999);
                s.position = Mathf.Clamp(old.position, 0, BoardSpaces - 1);
                s.coins = Mathf.Clamp(old.coins, 0, 1000000000);
                s.followers = Mathf.Clamp(old.followers, 0, 1000000000);
                s.dice = Mathf.Clamp(old.dice, 0, MaxDice);
                s.posts = Mathf.Clamp(old.posts, 0, 1000000000);
                s.turn = Mathf.Max(0, old.turn);
                s.shields = Mathf.Clamp(old.shields, 0, 3);
                s.hype = Mathf.Clamp(old.hype, 0, 99);
                s.cards = old.cards;
                s.diceUpdatedAt = old.diceUpdatedAt > 0 ? old.diceUpdatedAt : nowMs;
                for (int i = 0; i < Math.Min(old.landmarks.Length, LandmarkCount); i++)
                    s.buildings[i] = Mathf.Clamp(old.landmarks[i].level, 0, MaxLevel);
                s.worldRewardPaid = Completed(s);
                return true;
            }
            catch (Exception) { return false; }
        }

        public static GameSave Sanitise(GameSave s, long nowMs)
        {
            if (s == null) return NewGame(nowMs);
            s.position = Mathf.Clamp(s.position, 0, 59);
            s.worldIndex = Mathf.Clamp(s.worldIndex, 0, 999);
            s.coins = Mathf.Clamp(s.coins, 0, 1000000000);
            s.followers = Mathf.Clamp(s.followers, 0, 1000000000);
            s.dice = Mathf.Clamp(s.dice, 0, 50);
            s.shields = Mathf.Clamp(s.shields, 0, 3);
            s.hype = Mathf.Clamp(s.hype, 0, 99);
            s.turn = Math.Max(0, s.turn);
            s.lastDieA = Mathf.Clamp(s.lastDieA, 1, 6);
            s.lastDieB = Mathf.Clamp(s.lastDieB, 1, 6);
            if (s.buildings == null || s.buildings.Length != 6)
            {
                int[] copy = new int[6];
                if (s.buildings != null) Array.Copy(s.buildings, copy, Math.Min(6, s.buildings.Length));
                s.buildings = copy;
            }
            for (int i = 0; i < 6; i++) s.buildings[i] = Mathf.Clamp(s.buildings[i], 0, 5);
            if (s.cards == null) s.cards = new string[0];
            s.selectedToken = Mathf.Clamp(s.selectedToken,0,2);
            s.pendingMultiplier = s.pendingMultiplier==2 || s.pendingMultiplier==5 ||
                s.pendingMultiplier==10 ? s.pendingMultiplier : 1;
            // Both events cannot be active at once; an old save wins by heist priority.
            if (s.pendingHeist) s.pendingAttack = false;
            s.totalHeists = Mathf.Max(0,s.totalHeists);
            s.totalAttacks = Mathf.Max(0,s.totalAttacks);
            s.dailyRolls = Mathf.Clamp(s.dailyRolls,0,100000);
            s.dailyBuilds = Mathf.Clamp(s.dailyBuilds,0,100000);
            s.dailyHeists = Mathf.Clamp(s.dailyHeists,0,100000);
            s.dailyRewardMask &= 7;
            if (s.diceUpdatedAt <= 0) s.diceUpdatedAt = nowMs;
            return s;
        }
    }

    public static class GamePersistence
    {
        private const string SaveName = "memeopoly-unity-v1.json";
        public static string Location { get { return Path.Combine(Application.persistentDataPath, SaveName); } }

        public static GameSave Load()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                if (File.Exists(Location))
                    return MemeopolyRules.Sanitise(JsonUtility.FromJson<GameSave>(File.ReadAllText(Location)), now);
            }
            catch (Exception e) { Debug.LogWarning("Memeopoly save read failed: " + e.Message); }
            try
            {
                string data = PlayerPrefs.GetString(SaveName, "");
                if (data.Length > 0)
                    return MemeopolyRules.Sanitise(JsonUtility.FromJson<GameSave>(data), now);
            }
            catch (Exception e) { Debug.LogWarning("Memeopoly preferences read failed: " + e.Message); }
            return MemeopolyRules.NewGame(now);
        }

        public static void Save(GameSave save)
        {
            string json = JsonUtility.ToJson(save, true);
            try { File.WriteAllText(Location, json); }
            catch (Exception e) { Debug.LogWarning("Memeopoly file write failed: " + e.Message); }
            PlayerPrefs.SetString(SaveName, json);
            PlayerPrefs.Save();
        }

        public static bool HasUnitySave()
        {
            return File.Exists(Location) || PlayerPrefs.HasKey(SaveName);
        }
    }
}