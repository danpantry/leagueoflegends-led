﻿using LedDashboard.Modules.BasicAnimation;
using LedDashboard.Modules.LeagueOfLegends.ChampionModules;
using LedDashboard.Modules.LeagueOfLegends.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace LedDashboard.Modules.LeagueOfLegends
{
    class LeagueOfLegendsModule : LEDModule
    {

        // Constants

        HSVColor HealthColor = new HSVColor(0.29f, 0.79f, 1f);
        HSVColor HurtColor = new HSVColor(0.09f, 0.8f, 1f);
        HSVColor DeadColor = new HSVColor(0f, 0.8f, 0.77f);
        HSVColor NoManaColor = new HSVColor(0.52f, 0.66f, 1f);

        // Variables

        Led[] leds;

        ActivePlayer activePlayer;
        List<Champion> champions;
        Champion playerChampion;
        List<Event> gameEvents;

        ChampionModule championModule;
        AnimationModule animationModule;

        ulong msSinceLastExternalFrameReceived = 30000;
        ulong msAnimationTimerThreshold = 1500; // how long to wait for animation data until health bar kicks back in.

        // Events

        public event LEDModule.FrameReadyHandler NewFrameReady;


        /// <summary>
        /// Creates a new <see cref="LeagueOfLegendsModule"/> instance.
        /// </summary>
        /// <param name="ledCount">Number of LEDs in the strip</param>
        public static LeagueOfLegendsModule Create(int ledCount)
        {
            return new LeagueOfLegendsModule(ledCount);
        }

        /// <summary>
        /// The current module that is sending information to the LED strip.
        /// </summary>
        LEDModule CurrentLEDSource;

        private LeagueOfLegendsModule(int ledCount)
        {
            
            // League of Legends integration Initialization
            Process[] pname = Process.GetProcessesByName("League of Legends");
            if (pname.Length == 0) throw new InvalidOperationException("Game client is not open.");

            // Queries the game information
            QueryPlayerInfo();

            // LED Initialization
            //reverseOrder = reverse;
            this.leds = new Led[ledCount];
            for (int i = 0; i < ledCount; i++)
                leds[i] = new Led();

            // Load animation module
            animationModule = AnimationModule.Create(ledCount);
            animationModule.NewFrameReady += OnNewFrameReceived;

            // Load champion module. Different modules will be loaded depending on the champion.
            // If there is no suitable module for the selected champion, just the health bar will be displayed.

            // TODO: Make this easily extendable when there are many champion modules
            if (playerChampion.RawChampionName.ToLower().Contains("velkoz"))
            {
                championModule = VelKozModule.Create(ledCount, activePlayer);
                championModule.NewFrameReady += OnNewFrameReceived;
                championModule.TriedToCastOutOfMana += OnAbilityCastNoMana;
            }
            CurrentLEDSource = championModule;

            // Sets up a task to always check for updated player info
            Task.Run(async () =>
            {
                while (true)
                {
                    QueryPlayerInfo();
                    await Task.Delay(150);
                }
            });

            // start frame timer
            Task.Run(FrameTimer);

        }
        
        /// <summary>
        /// Queries updated game data from the LoL live client API.
        /// </summary>
        private void QueryPlayerInfo()
        {
            
            string json;
            try
            {
                json = WebRequestUtil.GetResponse("https://127.0.0.1:2999/liveclientdata/allgamedata");
            }
            catch (WebException e)
            {
                // TODO: Account for League client disconnects, game ended, etc. without crashing the whole program
                throw new InvalidOperationException("Couldn't connect with the game client", e); 
            }

            var gameData = JsonConvert.DeserializeObject<dynamic>(json);
            gameEvents = (gameData.events.Events as JArray).ToObject<List<Event>>();
            // Get active player info
            activePlayer = ActivePlayer.FromData(gameData.activePlayer);
            // Get player champion info (IsDead, Items, etc)
            champions = (gameData.allPlayers as JArray).ToObject<List<Champion>>();
            playerChampion = champions.Find(x => x.SummonerName == activePlayer.SummonerName);
            // Update active player based on player champion data
            activePlayer.IsDead = playerChampion.IsDead;
            // Update champion LED module information
            if (championModule != null) championModule.UpdatePlayerInfo(activePlayer);
            
        }

        /// <summary>
        /// Task that periodically updates the health bar.
        /// </summary>
        private async Task FrameTimer()
        {
            while(true)
            {
                if(msSinceLastExternalFrameReceived >= msAnimationTimerThreshold)
                {
                    UpdateHealthBar();
                }
                await Task.Delay(30);
                msSinceLastExternalFrameReceived += 30;
            }
        }

        /// <summary>
        /// Called by a <see cref="LEDModule"/> when a new frame is available to be processed.
        /// </summary>
        /// <param name="s">Module that sent the message</param>
        /// <param name="data">LED data</param>
        private void OnNewFrameReceived(object s, Led[] data)
        {
            if (s != CurrentLEDSource) return; // If it's from a different source that what we're listening too, ignore it
            NewFrameReady?.Invoke(this, data);
            msSinceLastExternalFrameReceived = 0;
        }

        /// <summary>
        /// Updates the health bar.
        /// </summary>
        private void UpdateHealthBar()
        {
            if (playerChampion.IsDead)
            {
                for (int i = 0; i < leds.Length; i++)
                {
                    this.leds[i].Color(DeadColor);
                }
            } else
            {
                float maxHealth = activePlayer.Stats.MaxHealth;
                float currentHealth = activePlayer.Stats.CurrentHealth;
                float healthPercentage = currentHealth / maxHealth;
                int ledsToTurnOn = (int)(healthPercentage * leds.Length);
                for (int i = 0; i < leds.Length; i++)
                {
                   // int index = reverseOrder ? this.leds.Length - 1 - i : i;
                    if (i < ledsToTurnOn)
                        this.leds[i].MixNewColor(HealthColor, true);
                    else
                    {
                        if (this.leds[i].color.AlmostEqual(HealthColor))
                        {
                            this.leds[i].Color(HurtColor);
                        }
                        else
                        {
                            this.leds[i].FadeToBlackBy(0.05f);
                        }
                    }

                }
            }
            
            NewFrameReady?.Invoke(this,this.leds);
        }

        /// <summary>
        /// Called when the player tried to cast an ability but was out of mana.
        /// </summary>
        private void OnAbilityCastNoMana()
        {
            CurrentLEDSource = animationModule;
            animationModule.ColorBurst(NoManaColor, 0.3f).ContinueWith((t) =>
            {
                CurrentLEDSource = championModule;
            });
        }


    }
}
