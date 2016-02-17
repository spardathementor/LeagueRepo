using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;

namespace LeeSin_GENESIS
{
    public enum ComboMode
    {
        OneVsOne,
        MoreAlly,
        MoreEnemy
    }

    class Program
    {
        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }
        private static string ChampionName = "LeeSin";
        private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Spell Q, W, E, R, Rnormal;
        private static float QMANA, WMANA, EMANA, RMANA;
        private static SpellSlot ignite, flash, smite;
        private static Items.Item
            VisionWard = new Items.Item(2043, 550f),
            OracleLens = new Items.Item(3364, 550f),
            WardN = new Items.Item(2044, 600f),
            TrinketN = new Items.Item(3340, 600f),
            SightStone = new Items.Item(2049, 600f),
            EOTOasis = new Items.Item(2302, 600f),
            EOTEquinox = new Items.Item(2303, 600f),
            EOTWatchers = new Items.Item(2301, 600f);

        private static Obj_AI_Base Marked;
        private static int LastTimeWardPlace;

        private static ComboMode ComboMode;
        private static SebbyLib.Prediction.PredictionInput PredictionRnormal;

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName) return;

            Config = new Menu(ChampionName + " GENESIS", ChampionName + " GENESIS", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();

            Config.SubMenu("Q Config").AddItem(new MenuItem("Q2delay", "Q2 Delay").SetValue(new Slider(500, 2000, 0)));

            Config.SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range").SetValue(false));


            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 330);
            R = new Spell(SpellSlot.R, 375);
            R.SetTargetted(0.20f,float.MaxValue);

            Rnormal = new Spell(SpellSlot.R, 700);

            Q.SetSkillshot(0.25f, 60f, 1800f, true, SkillshotType.SkillshotLine);
            Rnormal.SetSkillshot(0f, 70f, 1500f, false, SkillshotType.SkillshotLine);

            PredictionRnormal = new SebbyLib.Prediction.PredictionInput
            {
                Aoe = true,
                Collision = false,
                Speed = Rnormal.Speed,
                Delay = Rnormal.Delay,
                Range = Rnormal.Range,
                Radius = Rnormal.Width,
                Type = SebbyLib.Prediction.SkillshotType.SkillshotLine
            };


            flash = Player.GetSpellSlot("summonerflash");
            ignite = Player.GetSpellSlot("summonerdot");
            smite = Player.GetSpellSlot("summonersmite");
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("itemsmiteaoe"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmiteplayerganker"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmitequick"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmiteduel"); }

            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            return;
            if (R.IsReady() && COMBO)
            {
                int searchRange = 400;

                if (CanJump())
                    searchRange = 1000;

                foreach (var t in HeroManager.Enemies.Where(x => x.IsValidTarget(searchRange)))
                {
                    PredictionRnormal.From = R.GetPrediction(t).CastPosition;
                    
                    foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget() && x.NetworkId != t.NetworkId && x.Distance(t) < Rnormal.Range))
                    {
                        PredictionRnormal.Unit = enemy;

                        var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(PredictionRnormal);


                        //Render.Circle.DrawCircle(Rnormal.From, 50, System.Drawing.Color.Yellow, 1);
                        //Console.WriteLine(" " + poutput2.AoeTargetsHitCount);
                        //Render.Circle.DrawCircle(poutput2.CastPosition, 50, System.Drawing.Color.Orange, 1);

                        var castPos = poutput2.CastPosition;
                        var ext = castPos.Extend(PredictionRnormal.From, castPos.Distance(PredictionRnormal.From) + 250);
                        
                        //Render.Circle.DrawCircle(ext, 50, System.Drawing.Color.YellowGreen, 1);


                        if (Player.Distance(ext) < 150)
                        {
                            R.Cast(t);
                            return;
                        }
                        else if (W.IsReady() && Player.Distance(ext) < W.Range)
                        {
                            Jump(ext);
                            return;
                        }
                        else if (Player.Distance(ext) < 300)
                        {
                            Orbwalker.SetOrbwalkingPoint(ext);
                            return;
                        }
                    }
                }
            }
            else
            {
                Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Console.WriteLine(E.Instance.Name);
            if (Q.IsReady() && COMBO)
                Qlogic();
            if (W.IsReady())
                Wlogic();
            if (R.IsReady())
                Rlogic();
            else
                Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
        }

        private static void Rlogic()
        {
            if (COMBO && !TryKickAOE())
            {
                Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                if (CanJump())
                {
                    TryToGetPosAOE();
                }
            }
        }

        private static void Wlogic()
        {
            if (WFIRST)
            {
                //if (COMBO)
                    //Jump(Game.CursorPos);
            }
        }

        private static void Qlogic()
        {
            if(QFIRST)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Q.Cast(t);
                }
            }
            else
            {
                if (Marked.IsValidTarget())
                {
                    
                
                    if (Config.Item("Q2delay").GetValue<Slider>().Value < Utils.TickCount - Q.LastCastAttemptT)
                    {
                        Q.Cast();
                    }
                }
            }
            

        }

        private static void Jump(Vector3 position)
        {
            Obj_AI_Base obj = HeroManager.Allies.FirstOrDefault(x => x.IsValid && !x.IsDead && x.Distance(position) < 200 && x.Distance(position) < 200 && !x.IsMe);
            if (obj == null)
            {
                obj = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(x => x.IsValid && x.IsAlly && !x.IsDead && x.Distance(position) < 200);
            }

            if(obj == null)
            {
                if (TrinketN.IsReady())
                {
                    TrinketN.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
                else if (SightStone.IsReady())
                {
                    SightStone.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
                else if (WardN.IsReady())
                {
                    WardN.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
                else if (EOTOasis.IsReady())
                {
                    EOTOasis.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
                else if (EOTEquinox.IsReady())
                {
                    EOTEquinox.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
                else if (EOTWatchers.IsReady())
                {
                    EOTWatchers.Cast(position);
                    LastTimeWardPlace = Utils.TickCount;
                }
            }
            else
            {
                W.Cast(obj);
            }
        }

        private static bool QFIRST { get { return Q.Instance.Name == "BlindMonkQOne"; } }
        private static bool WFIRST { get { return W.Instance.Name == "BlindMonkWOne" && Game.Ping + 100 < Utils.TickCount - W.LastCastAttemptT; } }
        private static bool EFIRST { get { return E.Instance.Name == "BlindMonkEOne" && Game.Ping + 100 < Utils.TickCount - E.LastCastAttemptT; } }
        private static bool COMBO { get { return Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo; } }

        private static bool CanJump()
        {
            if (W.IsReady())
            {
                if(Utils.TickCount - LastTimeWardPlace < 250)
                    return true;
                if (TrinketN.IsReady() || SightStone.IsReady() || WardN.IsReady() || EOTOasis.IsReady() || EOTEquinox.IsReady() || EOTWatchers.IsReady())
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static bool TryToGetPosAOE()
        {
            foreach (var t in HeroManager.Enemies.Where(x => x.IsValidTarget(1000)))
            {
                PredictionRnormal.From = Prediction.GetPrediction(t, 0.40f).CastPosition;

                foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget() && x.NetworkId != t.NetworkId && x.Distance(t) < Rnormal.Range))
                {
                    PredictionRnormal.Unit = enemy;

                    var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(PredictionRnormal);
                    var castPos = poutput2.CastPosition;
                    var ext = castPos.Extend(PredictionRnormal.From, castPos.Distance(PredictionRnormal.From) + 250);

                    if (Player.Distance(ext) < W.Range)
                    {
                        Jump(ext);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool TryKickAOE()
        {
            foreach (var t in HeroManager.Enemies.Where(x => x.IsValidTarget(400)))
            {
                PredictionRnormal.From = R.GetPrediction(t).CastPosition;

                foreach (var enemy in HeroManager.Enemies.Where(x => x.IsValidTarget() && x.NetworkId != t.NetworkId && x.Distance(t) < Rnormal.Range))
                {
                    PredictionRnormal.Unit = enemy;

                    var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(PredictionRnormal);
                    var castPos = poutput2.CastPosition;
                    var ext = castPos.Extend(PredictionRnormal.From, castPos.Distance(PredictionRnormal.From) + 280);

                    if (Player.Distance(ext) < 120)
                    {
                        Orbwalker.SetOrbwalkingPoint(ext);
                        R.Cast(t);
                        return true;
                    }
                    else if (Player.Distance(ext) < 300)
                    {
                        Orbwalker.SetOrbwalkingPoint(ext);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (E.IsReady() && target.Type == GameObjectType.obj_AI_Hero && EFIRST)
            {
                E.Cast();
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (W.IsReady() && args.Target.Type == GameObjectType.obj_AI_Hero && !WFIRST && Player.HealthPercent < 95)
            {
                W.Cast();
            }
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (sender.IsEnemy && args.Buff.Name == "BlindMonkQOne")
            {
                Marked = null;
            }
        }

        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsEnemy && args.Buff.Name == "BlindMonkQOne")
            {
                Marked = sender;
            }
        }

        private static void ModeDetector()
        {

        }
    }
}
