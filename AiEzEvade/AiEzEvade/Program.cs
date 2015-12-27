using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace AiEzEvade
{
    class EEsettings
    {

        public int EvadeMode;
        
        public bool DodgeDangerous;
        public bool DodgeCircularSpells;
        public bool DodgeFOWSpells;
        public bool CheckSpellCollision;
        public bool ContinueMovement;

        public bool ClickOnlyOnce;
        public int TickLimiter;
        public int ReactionTime;
        public int SpellDetectionTime;
        public bool FastMovementBlock;

        public EEsettings(int evadeMode, bool dodgeDangerous, bool dodgeCircularSpells, bool dodgeFOWSpells, bool checkSpellCollision, bool continueMovement, bool clickOnlyOnce, int tickLimiter, int reactionTime, int spellDetectionTime, bool fastMovementBlock)
        {
            EvadeMode = evadeMode;
            DodgeDangerous = dodgeDangerous;
            DodgeCircularSpells = dodgeCircularSpells;
            DodgeFOWSpells = dodgeFOWSpells;
            CheckSpellCollision = checkSpellCollision;
            ContinueMovement = continueMovement;
            ClickOnlyOnce = clickOnlyOnce;
            TickLimiter = tickLimiter;
            ReactionTime = reactionTime;
            SpellDetectionTime = spellDetectionTime;
            FastMovementBlock = fastMovementBlock;
        }
    }

    class Program
    {
        static void Main(string[] args){CustomEvents.Game.OnGameLoad += Game_OnGameLoad;}

        public static EEsettings set = new EEsettings(2,true,true,true,false,false,true,100,100,100,false);
        public static Menu Config;
        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static int TickLimit = 0;
        public static int Mode = 0;

        private static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("AiEzEvade", "AiEzEvade", true);
            Config.AddToMainMenu();


            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Utils.TickCount - TickLimit > 100)
            {
                TickLimit = Utils.TickCount;

                var newMode = 0;

                if(Player.HealthPercent < 40)
                {
                    newMode = 2;
                }
                else if (Player.HealthPercent < 70)
                {
                    newMode = 1;
                }
                else 
                {
                    newMode = 0;
                }

                if (newMode != Mode )
                {
                    if(newMode==0)
                    {
                        set = new EEsettings(2,true,false,false,true,false,true,150,200,100,false);
                        SetEE(set);
                        Mode = newMode;
                        return;
                    }
                    if (newMode == 1)
                    {
                        set = new EEsettings(2,true,false,false,true,false,true,100,150,100,false);
                        SetEE(set);
                        Mode = newMode;
                        return;
                    }
                    if (newMode == 2)
                    {
                        set = new EEsettings(0,false,false,true,false,false,false,10,0,0, true);
                        SetEE(set);
                        Mode = newMode;
                        return;
                    }
                }
            }
        }

        private static void SetEE(EEsettings sets)
        {
            Menu.GetMenu("ezEvade", "ezEvade").Item("EvadeMode").SetValue<StringList>(new StringList(new[] { "Smooth", "Fastest", "Very Smooth" }, sets.EvadeMode));
            Menu.GetMenu("ezEvade", "ezEvade").Item("DodgeDangerous").SetValue<Boolean>(sets.DodgeDangerous);
            Menu.GetMenu("ezEvade", "ezEvade").Item("DodgeCircularSpells").SetValue<Boolean>(sets.DodgeCircularSpells);
            Menu.GetMenu("ezEvade", "ezEvade").Item("DodgeFOWSpells").SetValue<Boolean>(sets.DodgeFOWSpells);
            Menu.GetMenu("ezEvade", "ezEvade").Item("CheckSpellCollision").SetValue<Boolean>(sets.CheckSpellCollision);
            Menu.GetMenu("ezEvade", "ezEvade").Item("ContinueMovement").SetValue<Boolean>(sets.ContinueMovement);
            Menu.GetMenu("ezEvade", "ezEvade").Item("ContinueMovement").SetValue<Boolean>(sets.ClickOnlyOnce);
            Menu.GetMenu("ezEvade", "ezEvade").Item("TickLimiter").SetValue<Slider>(new Slider(sets.TickLimiter, 0, 500));
            Menu.GetMenu("ezEvade", "ezEvade").Item("ReactionTime").SetValue<Slider>(new Slider(sets.ReactionTime, 0, 500));
            Menu.GetMenu("ezEvade", "ezEvade").Item("SpellDetectionTime").SetValue<Slider>(new Slider(sets.SpellDetectionTime, 0, 1000));
            Menu.GetMenu("ezEvade", "ezEvade").Item("FastMovementBlock").SetValue<Boolean>(sets.FastMovementBlock);
        }
    }
}
