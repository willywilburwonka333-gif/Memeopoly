using System;
using UnityEngine;

namespace Memeopoly
{
    // All meta-progression is deterministic and stored inside GameSave.
    // These features don't need a server, external SDK, adverts or in-app purchases.
    public static class MemeopolyProgression
    {
        public static readonly string[] Rivals =
        {
            "The Algorithm", "The Copycat", "The Trend Thief"
        };

        public static readonly string[] Tokens =
        {
            "SHARKO", "CROCWING", "VIRAL CAT"
        };

        public static readonly string[] Album =
        {
            "Doge", "Pepe", "NPC Streamer", "Side Eye Cat", "Sigma Baby",
            "Ohio Boss", "Viral Rat", "Distracted Boyfriend"
        };

        private static int UtcDay(long unixMs)
        {
            DateTime utc = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime;
            return utc.Year * 10000 + utc.Month * 100 + utc.Day;
        }

        public static bool SyncDaily(GameSave s, long unixMs)
        {
            int day = UtcDay(unixMs);
            if (s.dailyDay == day) return false;
            s.dailyDay = day;
            s.dailyRolls = 0;
            s.dailyBuilds = 0;
            s.dailyHeists = 0;
            s.dailyRewardMask = 0;
            return true;
        }

        public static int CardCount(GameSave s)
        {
            int count=0;
            for (int i=0;i<Album.Length;i++)
                if (Array.IndexOf(s.cards,Album[i]) >= 0) count++;
            return count;
        }

        public static int DailyGoal(int quest)
        {
            if (quest == 0) return 8;
            if (quest == 1) return 3;
            if (quest == 2) return 1;
            return 0;
        }

        public static int DailyProgress(GameSave s,int quest)
        {
            if (quest == 0) return s.dailyRolls;
            if (quest == 1) return s.dailyBuilds;
            if (quest == 2) return s.dailyHeists;
            return 0;
        }

        public static string DailyLabel(int quest)
        {
            if (quest == 0) return "Roll 8 times • 4,500 COINS";
            if (quest == 1) return "Build 3 stages • 8 DICE";
            if (quest == 2) return "Play 1 heist • 8,000 COINS";
            return "Unknown mission";
        }

        public static bool CanClaim(GameSave s,int quest)
        {
            if (quest<0 || quest>2) return false;
            return (s.dailyRewardMask & (1<<quest)) == 0 &&
                DailyProgress(s,quest) >= DailyGoal(quest);
        }

        public static bool ClaimDaily(GameSave s,int quest,long unixMs,out string message)
        {
            SyncDaily(s,unixMs);
            message="Mission not ready yet.";
            if (!CanClaim(s,quest)) return false;
            s.dailyRewardMask |= 1<<quest;
            if (quest==0) s.coins += 4500;
            if (quest==1) s.dice = Math.Min(MemeopolyRules.MaxDice,s.dice+8);
            if (quest==2) s.coins += 8000;
            message="MISSION COMPLETE! "+DailyLabel(quest);
            return true;
        }

        public static void OnRoll(GameSave s,long unixMs)
        {
            SyncDaily(s,unixMs);
            s.dailyRolls++;
        }

        public static void OnBuild(GameSave s,long unixMs)
        {
            SyncDaily(s,unixMs);
            s.dailyBuilds++;
        }

        public static string ResolveHeist(GameSave s,int chest,System.Random rng)
        {
            if (!s.pendingHeist || chest<0 || chest>2) return "No heist waiting.";
            s.pendingHeist=false;
            s.totalHeists++;
            s.dailyHeists++;
            int mult=Math.Max(1,Math.Min(10,s.pendingMultiplier));
            int[] prize={1800,3300,5400};
            int[] success={90,70,45};
            bool win=rng.Next(100)<success[chest];
            s.pendingMultiplier=1;
            if(win)
            {
                int reward=(prize[chest]+rng.Next(0,900))*mult;
                s.coins=Math.Min(1000000000,s.coins+reward);
                s.followers=Math.Min(1000000000,s.followers+500*mult);
                return "ALGORITHM HEIST! +"+reward.ToString("N0")+" coins!";
            }
            int loss=Math.Min(s.coins,650*mult);
            s.coins-=loss;
            return "HEIST BUSTED! Security took "+loss.ToString("N0")+" coins.";
        }

        public static string ResolveRival(GameSave s,int rival,System.Random rng)
        {
            if (!s.pendingAttack || rival<0 || rival>2) return "No rival waiting.";
            s.pendingAttack=false;
            s.totalAttacks++;
            int mult=Math.Max(1,Math.Min(10,s.pendingMultiplier));
            s.pendingMultiplier=1;
            int[] success={88,68,48};
            int[] loot={1100,2600,4800};
            bool win=rng.Next(100)<success[rival];
            if(win)
            {
                int reward=(loot[rival]+rng.Next(0,500))*mult;
                s.coins=Math.Min(1000000000,s.coins+reward);
                s.followers=Math.Min(1000000000,s.followers+400*mult);
                return Rivals[rival]+" defeated! +"+reward.ToString("N0")+" coins.";
            }
            if(s.shields>0)
            {
                s.shields--;
                return Rivals[rival]+" struck back! Shield absorbed the hit.";
            }
            int penalty=Math.Min(s.coins,(450+350*rival)*mult);
            s.coins-=penalty;
            return Rivals[rival]+" counterattacked! -"+penalty.ToString("N0")+" coins.";
        }

        public static bool AwardCompleteAlbum(GameSave s)
        {
            if(s.albumRewardPaid || CardCount(s)<Album.Length) return false;
            s.albumRewardPaid=true;
            s.coins=Math.Min(1000000000,s.coins+18000);
            s.dice=Math.Min(MemeopolyRules.MaxDice,s.dice+10);
            s.followers=Math.Min(1000000000,s.followers+25000);
            return true;
        }
    }
}
