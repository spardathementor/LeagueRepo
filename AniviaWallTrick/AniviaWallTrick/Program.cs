using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace AniviaWallTrick
{
    class Program
    {
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Spell Q, W, E, R;

        private static Obj_AI_Hero Anivia = null, Vayne = null, Poppy = null;

        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName == "Anivia")
            {
                foreach (var ally in HeroManager.Allies)
                {
                    if (ally.ChampionName == "Vayne")
                        Vayne = ally;
                    if (ally.ChampionName == "Poppy")
                        Poppy = ally;
                }

                W = new Spell(SpellSlot.W, 950);
                W.SetSkillshot(0.6f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);


            }
            else if (Player.ChampionName == "Vayne")
            {
                foreach (var ally in HeroManager.Allies)
                {
                    if (ally.ChampionName == "Anivia")
                        Vayne = ally;
                }

                E = new Spell(SpellSlot.E, 670);


            }
            else if (Player.ChampionName == "Poppy")
            {
                foreach (var ally in HeroManager.Allies)
                {
                    if (ally.ChampionName == "Anivia")
                        Vayne = ally;
                }

                E = new Spell(SpellSlot.E, 525);


            }
            else
            {
                return;
            }

            if(Vayne == null && Anivia == null && Poppy == null)
                return;

            Config = new Menu("AniviaWallTrick " + Player.ChampionName + " plugin", "AniviaWallTrick " + Player.ChampionName + " plugin", true);
            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if(E.IsReady() && Anivia != null && Anivia.IsValid && !Anivia.IsDead && Player.Distance(Anivia) < 500)
            {
                var rSlot = Anivia.Spellbook.Spells[1];
                var time = rSlot.CooldownExpires - Game.Time;
                
                if (time < 0)
                {
                    var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                    {
                        E.Cast(t);
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsAlly && !sender.IsMinion && Player.ChampionName == "Anivia" && W.IsReady() )
            {
                if(args.SData.Name == "VayneCondemnMissile")
                {

                    var position = args.Target.Position.Extend(sender.Position, -470);
                    if (Player.Distance(position) < W.Range)
                        W.Cast(position);
                }
                else if (args.SData.Name == "PoppyE")
                {
                    var position = args.Target.Position.Extend(sender.Position, -420);
                    if (Player.Distance(position) < W.Range)
                        W.Cast(position);
                }
            }
        }
    }
}
