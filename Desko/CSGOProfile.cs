using CSGSI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows;
using CSGSI.Nodes;
using Newtonsoft.Json.Linq;
using CSGSI.Events;

namespace Desko
{
    //define all the game states
    enum GameState
    {
        Off,
        Undefined,
        FreezeTime,
        Live,
        Planted,
        Exploded,
        Defused,
        CounterTerroristsWin,
        TerrorWin
    };

    internal class CSGOProfile
    {
        GameStateListener gsl;
        MainWindow main;

        GameState _state = GameState.Off;

        int bombBeeps = 0; //how many beeps have the bomb emitted
        int nextBeep = 0; //the interval until next beep should occur
        int bombLastBeep = Environment.TickCount; //when was the last beep of the bomb?

        public CSGOProfile(MainWindow mainWindowInstance)
        {
            main = mainWindowInstance;
        }

        public void StartCSGOGameStateSyncProfile()
        {
            gsl = new GameStateListener(3000);
            gsl.EnableRaisingIntricateEvents = true;

            //gsl.BombPlanted += BombPlanted;

            gsl.EnableRaisingIntricateEvents = true;
            gsl.NewGameState += Gsl_NewGameState;
            gsl.PlayerFlashed += PlayerFlashed;

            if (!gsl.Start())
            {
                Environment.Exit(0);
            }

            Debug.WriteLine("Listening...");
        }

        private async void PlayerFlashed(PlayerFlashedEventArgs e)
        {
            Debug.WriteLine("FLASHHHH!!!!!!");
            Debug.WriteLine("FLASHHHH!!!!!!");
            Debug.WriteLine("FLASHHHH!!!!!!");
            Debug.WriteLine("FLASHHHH!!!!!!");
            Debug.WriteLine("FLASHHHH!!!!!!");
            

        }

        private async void Gsl_NewGameState(CSGSI.GameState gs)
        {
            Debug.WriteLine(gs.Round.Phase);
            Debug.WriteLine(gs.Bomb.State);
            //Debug.WriteLine(gs.Previously.Bomb.State);

            var json = JObject.Parse(gs.JSON);
            Debug.WriteLine(json);

            try
            {
                var bomb = json["round"]["bomb"];

                Debug.WriteLine(bomb);
                Debug.WriteLine(bomb);
                Debug.WriteLine(bomb);
                Debug.WriteLine(bomb);
                Debug.WriteLine(bomb);
                Debug.WriteLine(bomb);

                if (bomb is not null)
                {
                    string bombState = bomb.ToString();
                    Debug.WriteLine("***************************************");
                    Debug.WriteLine(bombState);
                    Debug.WriteLine(_state.ToString());
                    Debug.WriteLine("***************************************");
                    
                    if (!string.Equals(bombState, _state.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        switch (bombState)
                        {
                            case "planted":
                                for (int i = 0; i < 10; i++)
                                {
                                    Debug.WriteLine("Bomb has been planted.");
                                }

                                _state = GameState.Planted;
                                BombPlanted();
                                break;
                        }
                }

                var player = json["player"];
                
                if (player is not null)
                    if (player["state"]["flashed"].Value<int>() > 0)
                    {
                        var color = main._currentColor;
                        await main.Fade(Color.FromRgb(255, 255, 255), color);
                    }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public void StopCSGOGameStateSyncProfile()
            => gsl.Stop();

        private async void BombPlanted( /*BombPlantedEventArgs e*/)
        {
            Debug.WriteLine("Bomb has been planted.");

            _state = GameState.Planted;
            bombBeeps = 0; //we have had 0 beeps so far 
            bombLastBeep = Environment.TickCount; //set the time of activating the bomb          
            await Task.Delay(600); //this delay is used to match up the timing with the first beep.
            blinkRed(); //blink the first time

            while (_state == GameState.Planted)
            {
                // calculate the next beep time
                nextBeep = (int)(0.13 * Math.Pow(bombBeeps, 2) - 20 * bombBeeps +
                                 990); //if the bomb beep sequence changes we need to update this polynomial...

                if ((Environment.TickCount - bombLastBeep) > nextBeep)
                {
                    bombLastBeep = Environment.TickCount;
                    blinkRed();
                    bombBeeps++;
                }

                if (
                    bombBeeps >
                    80) //after 80 beeps the bomb is exploding no matter what so we just light up the red LEDs
                {
                    bombBeeps = 0;
                    main.SetColor(0, 255, 0);
                    _state = GameState.Exploded;
                }
            }
        }

        private async void blinkRed()
        {
            main.SetColor(0, 255, 0);
            await Task.Delay(75);
            main.SetColor(0, 0, 0);
        }
    }
}