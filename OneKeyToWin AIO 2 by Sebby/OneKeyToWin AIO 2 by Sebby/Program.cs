using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.UI;
using SebbyLib;
using SPrediction;
using LeagueSharp.SDK.Enumerations;

namespace OneKeyToWin_AIO_2_by_Sebby
{
    class Program
    {
        static void Main(string[] args) { Events.OnLoad += Load; }

        private static int tickIndex = 0;
        public static int AIOmode;

        public static Orbwalker Orbwalker { get; } = Variables.Orbwalker;
        public static TargetSelector TargetSelector { get; } = Variables.TargetSelector;

        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public static Spell Q, W, E, R;
        public static float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public static Menu MainMenu { get; set; }
        public static Menu MenuPrediction, MenuAdvance;
        public static Menu MenuHero, MenuDraw, MenuQ, MenuW, MenuE, MenuR, MenuFarm, MenuHarass;

        public static bool Combo { get { return Orbwalker.ActiveMode == OrbwalkingMode.Combo; } }
        public static bool None { get { return Orbwalker.ActiveMode == OrbwalkingMode.None; } }
        public static bool Farm { get { return Orbwalker.ActiveMode == OrbwalkingMode.Hybrid || Orbwalker.ActiveMode == OrbwalkingMode.LaneClear;}}

        private static void Load(object sender, EventArgs e)
        {
            MainMenu = new Menu("OktwAio2", "OKTW AIO 2 by Sebby", true);

            MainMenu.Add(new MenuList<string>("AioMode", "Aio Mode", new[] { "Utility and Champion", "Only Champion", "Only Utility" }));

            AIOmode = MainMenu["AioMode"].GetValue<MenuList>().Index;

            if (AIOmode != 2)
            {
                MenuPrediction = MainMenu.Add(new Menu("MenuPrediction", "Prediction"));
                MenuPrediction.Add(new MenuList<string>("PredictionMODE", "Prediction MODE", new[] { "OKTW© PREDICTION", "SDK" }));
                MenuPrediction.Add(new MenuList<string>("HitChance", "Hit Chance", new[] { "Very High", "High", "Medium" }));
                switch (Player.ChampionName)
                {
                    case "Jinx":
                        new Champions.Jinx();
                        break;
                }
                new Core.ChampionStuff();
            }
            new Core.DrawsOktw();
            MainMenu.Attach();
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            SetLagFreeIndex();
        }

        private static void SetLagFreeIndex()
        {
            tickIndex++;

            if (tickIndex > 4)
                tickIndex = 0;
        }

        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }

        public static void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            if (MenuPrediction["PredictionMODE"].GetValue<MenuList>().Index == 0)
            {
                SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;
                bool aoe2 = false;

                if (QWER.Type == SkillshotType.SkillshotCircle)
                {
                    CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotCircle;
                    aoe2 = true;
                }

                if (QWER.Width > 80 && !QWER.Collision)
                    aoe2 = true;

                var predInput2 = new SebbyLib.Prediction.PredictionInput
                {
                    Aoe = aoe2,
                    Collision = QWER.Collision,
                    Speed = QWER.Speed,
                    Delay = QWER.Delay,
                    Range = QWER.Range,
                    From = Player.ServerPosition,
                    Radius = QWER.Width,
                    Unit = target,
                    Type = CoreType2
                };
                var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

                //var poutput2 = QWER.GetPrediction(target);

                if (QWER.Speed != float.MaxValue && OktwCommon.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                    return;

                if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 0)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.VeryHigh)
                        QWER.Cast(poutput2.CastPosition);
                    else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                    {
                        QWER.Cast(poutput2.CastPosition);
                    }

                }
                else if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 1)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
                        QWER.Cast(poutput2.CastPosition);

                }
                else if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 2)
                {
                    if (poutput2.Hitchance >= SebbyLib.Prediction.HitChance.Medium)
                        QWER.Cast(poutput2.CastPosition);
                }
            }
            else if (MenuPrediction["PredictionMODE"].GetValue<MenuList>().Index == 1)
            {
                if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 0)

                    QWER.CastIfHitchanceEquals(target, HitChance.VeryHigh);
                    return;
                }
                else if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 1)
                {
                    QWER.CastIfHitchanceEquals(target, HitChance.High);
                    return;
                }
                else if (MenuPrediction["HitChance"].GetValue<MenuList>().Index == 2)
                {
                    QWER.CastIfHitchanceEquals(target, HitChance.Medium);
                    return;
                }
            
             }
    }
}
