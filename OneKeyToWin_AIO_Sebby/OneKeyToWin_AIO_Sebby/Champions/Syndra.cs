using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Syndra
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Spell E, Q, R, W, EQ, Eany;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private static List<Obj_AI_Minion> BallsList = new List<Obj_AI_Minion>();
        private bool EQcastNow = false;
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 790);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 675);
            EQ = new Spell(SpellSlot.Q, Q.Range + 500);
            Eany = new Spell(SpellSlot.Q, Q.Range + 500);

            Q.SetSkillshot(0.6f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 140f, 1600f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.35f, 100, 2500f, false, SkillshotType.SkillshotLine);
            EQ.SetSkillshot(0.5f, 100f, 2500f, false, SkillshotType.SkillshotLine);
            Eany.SetSkillshot(0.35f, 50f, 2000f, false, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harrasQ", "Harass Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("QHarassMana", "Harass Mana", true).SetValue(new Slider(30, 100, 0)));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Q Config").SubMenu("Use on:").AddItem(new MenuItem("Qon" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harass W", true).SetValue(false));


            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("WmodeCombo", "W combo mode", true).SetValue(new StringList(new[] { "always", "run - cheese" }, 1)));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("W Gap Closer").AddItem(new MenuItem("WmodeGC", "Gap Closer position mode", true).SetValue(new StringList(new[] { "Dash end position", "My hero position" }, 0)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("W Gap Closer").SubMenu("Cast on enemy:").AddItem(new MenuItem("WGCchampion" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E if enemy in range", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Emana", "E % minimum mana", true).SetValue(new Slider(20, 100, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQout", "Last hit Q minion out range AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("QLCminions", " QLaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("ELCminions", " ELaneClear minimum minions", true).SetValue(new Slider(5, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle clear E", true).SetValue(true));

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;

            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.Q && EQcastNow && E.IsReady())
            {
                var customeDelay = E.Delay - ((Player.Distance(args.StartPosition)) / E.Speed);
                Program.debug("DEL " + customeDelay);
                Utility.DelayAction.Add((int)(customeDelay * 1000), () => E.Cast(args.StartPosition));
            }
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Type == GameObjectType.obj_AI_Minion && sender.Name == "Seed")
            {
                var ball = sender as Obj_AI_Minion;
                BallsList.Add(ball);
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!E.IsReady())
                EQcastNow = false;

            if (Program.LagFree(1))
            { 
                SetMana();
                BallCleaner();
            }


            if (Program.LagFree(1) && E.IsReady() && Config.Item("autoE", true).GetValue<bool>())
                LogicE();

            if (Program.LagFree(2) && Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && Config.Item("autoW", true).GetValue<bool>())
                LogicW();

            if (Program.LagFree(4) && R.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                LogicR();
        }

        private void TryBallE(Obj_AI_Hero t)
        {
            if (Q.IsReady())
            {
                CastQE(t);
            }
            else
            {
                var ePred = Eany.GetPrediction(t);
                if (ePred.Hitchance >= HitChance.VeryHigh)
                {
                    var playerToCP = Player.Distance(ePred.CastPosition);
                    foreach (var ball in BallsList.Where(ball => Player.Distance(ball.Position) < E.Range))
                    {
                        var ballFinalPos = Player.ServerPosition.Extend(ball.Position, playerToCP);
                        if (ballFinalPos.Distance(ePred.CastPosition) < 50)
                            E.Cast(ball.Position);
                    }
                }
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(Eany.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (OktwCommon.GetKsDamage(t, E) > t.Health)
                    TryBallE(t);
                if (Program.Combo && Player.Mana > RMANA + EMANA)
                    TryBallE(t);
            }
        }

        private void LogicR()
        {
            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(R.Range)))
            {
                if (enemy.Health < OktwCommon.GetKsDamage(enemy, R) + (R.GetDamage(enemy,1) * (R.Instance.Ammo - 3)))
                {
                    if (enemy.Health - OktwCommon.GetIncomingDamage(enemy) > 0)
                    {
                        R.Cast(enemy);
                    }
                }
            }
        }

        private void LogicW()
        {
            if (W.Instance.ToggleState == 1)
            {
                var t = TargetSelector.GetTarget(W.Range - 100, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                {
                    if (OktwCommon.GetKsDamage(t, W) > t.Health)
                        CatchW();
                    if (Program.Combo && Player.Mana > RMANA + QMANA + WMANA)
                        CatchW();
                    if (Program.Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("harrasQ", true).GetValue<bool>()
                        && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.ManaPercent > Config.Item("QHarassMana", true).GetValue<Slider>().Value)
                        CatchW();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                {
                    Program.CastSpell(W, t);
                }
            }
        }   

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget() && Config.Item("Qon" + t.ChampionName).GetValue<bool>())
            {
                if (OktwCommon.GetKsDamage(t, Q) > t.Health)
                    Program.CastSpell(Q, t);
                if (Program.Combo && Player.Mana > RMANA + QMANA + WMANA)
                    Program.CastSpell(Q, t);
                if (Program.Farm && Orbwalking.CanAttack() && !Player.IsWindingUp && Config.Item("harrasQ", true).GetValue<bool>()
                    && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.ManaPercent > Config.Item("QHarassMana", true).GetValue<Slider>().Value)
                    Program.CastSpell(Q, t);
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                    Program.CastSpell(Q, t);
            }
            if (Player.IsWindingUp)
                return;

            if (!Program.None && !Program.Combo && Player.Mana > RMANA + QMANA * 2)
            {
                var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

                if (Config.Item("farmQout", true).GetValue<bool>())
                {
                    foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(Q.Range) && (!Orbwalker.InAutoAttackRange(minion) || (!minion.UnderTurret(true) && minion.UnderTurret()))))
                    {
                        var hpPred = HealthPrediction.GetHealthPrediction(minion, 1100);
                        if (hpPred < Q.GetDamage(minion) * 0.9 && hpPred > minion.Health - hpPred * 2)
                        {
                            Q.Cast(minion);
                            return;
                        }
                    }
                }

                if (Program.LaneClear && Config.Item("farmQ", true).GetValue<bool>() && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value)
                {
                    foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(Q.Range) && Orbwalker.InAutoAttackRange(minion)))
                    {
                        var hpPred = HealthPrediction.GetHealthPrediction(minion, 1100);
                        if (hpPred < Q.GetDamage(minion) * 0.9 && hpPred > minion.Health - hpPred * 2)
                        {
                            Q.Cast(minion);
                            return;
                        }
                    }
                }

                if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmQ", true).GetValue<bool>())
                {
                    var farmPos = Q.GetCircularFarmLocation(allMinions, Q.Width);
                    if (farmPos.MinionsHit >= Config.Item("QLCminions", true).GetValue<Slider>().Value)
                        Q.Cast(farmPos.Position);
                }
            }
        }

        private void CatchW()
        {
            var catchRange = 925;
            Obj_AI_Base obj = null;
            if (BallsList.Count > 0)
            {
                obj = BallsList.Find(ball => ball.Distance(Player) < catchRange);
            }

            if(obj == null)
            {
                obj = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).FirstOrDefault();
            }
            if (obj != null)
            {
                W.Cast(obj.Position);
            }
        }

        private void CastQE(Obj_AI_Base target)
        {
            Core.SkillshotType CoreType2 = Core.SkillshotType.SkillshotLine;


            var predInput2 = new Core.PredictionInput
            {
                Aoe = false,
                Collision = EQ.Collision,
                Speed = EQ.Speed,
                Delay = EQ.Delay,
                Range = EQ.Range,
                From = Player.ServerPosition,
                Radius = EQ.Width,
                Unit = target,
                Type = CoreType2
            };
            var poutput2 = Core.Prediction.GetPrediction(predInput2);

            //var poutput2 = QWER.GetPrediction(target);

            if (OktwCommon.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                return;
            Vector3 castQpos = poutput2.CastPosition;

            if (Player.Distance(castQpos) > Q.Range)
                castQpos = Player.Position.Extend(castQpos, Q.Range);

            if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 0)
            {
                if (poutput2.Hitchance >= Core.HitChance.VeryHigh)
                {
                    EQcastNow = true;
                    Q.Cast(castQpos);
                }

            }
            else if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 1)
            {
                if (poutput2.Hitchance >= Core.HitChance.High)
                {
                    EQcastNow = true;
                    Q.Cast(castQpos);
                }

            }
            else if (Config.Item("HitChance", true).GetValue<StringList>().SelectedIndex == 2)
            {
                if (poutput2.Hitchance >= Core.HitChance.Medium)
                {
                    EQcastNow = true;
                    Q.Cast(castQpos);
                }
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("wRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
            if (Config.Item("eRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable", true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }

        private void BallCleaner()
        {
            if (BallsList.Count > 0)
            {
                BallsList.RemoveAll(ball => !ball.IsValid || ball.Mana == 19);
            }
        }
    }
}
