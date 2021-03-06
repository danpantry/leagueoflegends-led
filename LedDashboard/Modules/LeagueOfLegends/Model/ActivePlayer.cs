﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedDashboard.Modules.LeagueOfLegends.Model
{
    public class ActivePlayer
    {
        public AbilityLoadout AbilityLoadout;
        public ChampionStats Stats
        {
            get { return stats; }
            set
            {
                stats = value;
            }
        }
        private ChampionStats stats;
        public float CurrentGold;
        public List<Rune> Runes;
        public int Level;
        public string SummonerName;

        public bool IsDead;
        public bool IsOnZhonyas;


        public static ActivePlayer FromData(dynamic data, bool isDead = false, bool isOnZhonyas = false)
        {
            
            return new ActivePlayer()
            {
                AbilityLoadout = AbilityLoadout.FromData(data.abilities),
                Stats = (data.championStats as JObject).ToObject<ChampionStats>(),
                CurrentGold = data.currentGold,
                Runes = (data.fullRunes.generalRunes as JArray).ToObject<List<Rune>>(),
                Level = data.level,
                SummonerName = data.summonerName,
                IsDead = isDead,
                IsOnZhonyas = isOnZhonyas
        };
        }

        public void UpdateFromData(dynamic data)
        {
            this.AbilityLoadout = AbilityLoadout.FromData(data.abilities);
            this.Stats = (data.championStats as JObject).ToObject<ChampionStats>();
            this.CurrentGold = data.currentGold;
            this.Runes = (data.fullRunes.generalRunes as JArray).ToObject<List<Rune>>();
            this.Level = data.level;
            this.SummonerName = data.summonerName;
        }
    }
}
