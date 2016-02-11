using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Ezreal
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell Q, W, E, R;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        Vector3 CursorPosition = Vector3.Zero;

        public double lag = 0;
        public double WCastTime = 0;
        public double QCastTime = 0;
        public float DragonDmg = 0;
        public double DragonTime = 0;
        public bool Esmart = false;
        public double OverKill = 0;
        public double OverFarm = 0;
        public double diag = 0;
        public double diagF = 0;
        public int Muramana = 3042;
        public int Tear = 3070;
        public int Manamune = 3004;

        public string MsgDebug = "wait";
        public double NotTime = 0;

        public static Core.OKTWdash Dash;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 3000f);
            
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1.1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Items").AddItem(new MenuItem("mura", "Auto Muramana", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Items").AddItem(new MenuItem("stack", "Stack Tear if full mana", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("wPush", "W ally (push tower)", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harass W", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("smartE", "SmartCast E key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("smartEW", "SmartCast E + W key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Dash = new Core.OKTWdash(E);

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rcc", "R cc", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Raoe", "R aoe 3 enemy", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rjungle", "R Jungle stealer", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rdragon", "Dragon", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rbaron", "Baron", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rred", "Red", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rblue", "Blue", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("R Jungle stealer").AddItem(new MenuItem("Rally", "Ally stealer", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rturrent", "Don't R under turret", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("MaxRangeR", "Max R range", true).SetValue(new Slider(3000, 5000, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("MinRangeR", "Min R range", true).SetValue(new Slider(900, 5000, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Farm Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LC", "LaneClear", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(50, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCP", "FAST LaneClear", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Harass").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("debug", "Debug", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("apEz", "AP Ezreal", true).SetValue(false));

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += afterAttack;
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;

            if (W.IsReady() && Config.Item("wPush", true).GetValue<bool>() && target.IsValid<Obj_AI_Turret>() && Player.Mana > RMANA + EMANA + QMANA + WMANA + WMANA + RMANA)
            {
                foreach (var ally in Program.Allies)
                {
                    if (!ally.IsMe && ally.IsAlly && ally.Distance(Player.Position) < 600)
                        W.Cast(ally);
                }
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (R.IsReady() && Config.Item("Rjungle", true).GetValue<bool>())
            {
                KsJungle();
            }
            else
                DragonTime = 0;

            if (E.IsReady())
            {
                if (Program.LagFree(1) && Config.Item("autoE", true).GetValue<bool>() && Program.Combo)
                    LogicE();

                if (Config.Item("smartE", true).GetValue<KeyBind>().Active)
                    Esmart = true;
                if (Config.Item("smartEW", true).GetValue<KeyBind>().Active && W.IsReady())
                {
                    CursorPosition = Game.CursorPos;
                    W.Cast(CursorPosition);
                }
                if (Esmart && Player.Position.Extend(Game.CursorPos, E.Range).CountEnemiesInRange(500) < 4)
                    E.Cast(Player.Position.Extend(Game.CursorPos, E.Range), true);
                
                if (!CursorPosition.IsZero)
                    E.Cast(Player.Position.Extend(CursorPosition, E.Range), true);
            }
            else
            {
                CursorPosition = Vector3.Zero;
                Esmart = false;
            }

            if (Program.LagFree(2) && Q.IsReady())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && Config.Item("autoW", true).GetValue<bool>())
                LogicW();

            if ( R.IsReady())
            {
                if (Config.Item("useR", true).GetValue<KeyBind>().Active)
                {
                    var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        R.Cast(t, true, true);
                }

                if (Program.LagFree(4))
                    LogicR();
            }
        }

        private void LogicQ()
        {
            if (Config.Item("mura", true).GetValue<bool>())
            {
                int Mur = Items.HasItem(Muramana) ? 3042 : 3043;
                if (Program.Combo && Items.HasItem(Mur) && Items.CanUseItem(Mur) && Player.Mana > RMANA + EMANA + QMANA + WMANA)
                {
                    if (!Player.HasBuff("Muramana"))
                        Items.UseItem(Mur);
                }
                else if (Player.HasBuff("Muramana") && Items.HasItem(Mur) && Items.CanUseItem(Mur))
                    Items.UseItem(Mur);
            }
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Player.CountEnemiesInRange(900) > 0)
                t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                
                if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if (Program.Farm && Player.Mana > RMANA + EMANA + QMANA + WMANA && OktwCommon.CanHarras())
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                    {
                        Program.CastSpell(Q, enemy);
                    }
                }
                else
                {
                    var qDmg = OktwCommon.GetKsDamage(t, Q);
                    var wDmg = W.GetDamage(t);
                    if (qDmg > t.Health)
                    {
                        Program.CastSpell(Q, t);
                        OverKill = Game.Time;
                    }
                    else if (t.IsValidTarget(W.Range) && qDmg + wDmg > t.Health)
                    {
                        Program.CastSpell(Q, t);
                        OverKill = Game.Time;
                    }
                }

                if (!Program.None && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
            if (Farm && Player.Mana > RMANA + EMANA + WMANA + QMANA * 3)
            {
                farmQ();
                lag = Game.Time;
            }
            else if (Config.Item("stack", true).GetValue<bool>()&& Utils.TickCount - Q.LastCastAttemptT > 4000 && !Player.HasBuff("Recall") && Player.Mana > Player.MaxMana * 0.95 && Program.None && (Items.HasItem(Tear) || Items.HasItem(Manamune)))
            {
                Q.Cast(Player.ServerPosition);
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (Program.Combo && Player.Mana > RMANA + WMANA + EMANA)
                    Program.CastSpell(W, t);
                else if (Program.Farm && Config.Item("harrasW", true).GetValue<bool>() && Config.Item("haras" + t.ChampionName).GetValue<bool>() && (Player.Mana > Player.MaxMana * 0.8 || Config.Item("apEz", true).GetValue<bool>()) && Player.Mana > RMANA + WMANA + EMANA + QMANA && OktwCommon.CanHarras())
                    Program.CastSpell(W, t);
                else
                {
                    var qDmg = Q.GetDamage(t);
                    var wDmg = OktwCommon.GetKsDamage(t, W);
                    if (wDmg > t.Health)
                    {
                        Program.CastSpell(W, t);
                        OverKill = Game.Time;
                    }
                    else if (wDmg + qDmg > t.Health && Q.IsReady())
                    {
                        Program.CastSpell(W, t);
                    }
                }

                if (!Program.None && Player.Mana > RMANA + WMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
            var dashPosition = Player.Position.Extend(Game.CursorPos, E.Range);

            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(270) && target.IsMelee))
            {
                var dashPos = Dash.CastDash();
                if (!dashPos.IsZero)
                {
                    E.Cast(dashPos);
                }
            }

            if (t.IsValidTarget() && Player.HealthPercent > 40 && !Player.UnderTurret(true) && (Game.Time - OverKill > 0.3) && dashPosition.CountEnemiesInRange(900) < 3)
            {
                if ( t.Distance(Game.CursorPos) + 300 < t.Position.Distance(Player.Position) && !Orbwalking.InAutoAttackRange(t))
                {
                    var dmgCombo = 0f;

                    if (t.IsValidTarget(950))
                    {
                        dmgCombo = (float)Player.GetAutoAttackDamage(t) + E.GetDamage(t);
                    }

                    if (Q.IsReady() && Player.Mana > QMANA + EMANA && Q.WillHit(dashPosition, Q.GetPrediction(t).UnitPosition))
                        dmgCombo = Q.GetDamage(t);

                    if (W.IsReady() && Player.Mana > QMANA + EMANA + WMANA )
                    {
                        dmgCombo += W.GetDamage(t);
                    }

                    if (dmgCombo > t.Health && OktwCommon.ValidUlt(t))
                    {
                        E.Cast(dashPosition);
                        OverKill = Game.Time;
                        Program.debug("E ks combo");
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Player.UnderTurret(true) && Config.Item("Rturrent", true).GetValue<bool>())
                return;

            if (Config.Item("autoR", true).GetValue<bool>() && Player.CountEnemiesInRange(800) == 0 && Game.Time - OverKill > 0.6)
            {
                R.Range = Config.Item("MaxRangeR", true).GetValue<Slider>().Value;
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && OktwCommon.ValidUlt(target)))
                {
                    double predictedHealth = target.Health - OktwCommon.GetIncomingDamage(target);

                    if ( Config.Item("Rcc", true).GetValue<bool>() && target.IsValidTarget(Q.Range + E.Range) && target.Health < Player.MaxHealth && !OktwCommon.CanMove(target))
                    {
                        R.Cast(target, true, true);
                    }

                    double Rdmg = R.GetDamage(target);

                    if (Rdmg > predictedHealth)
                        Rdmg = getRdmg(target);

                    if (Rdmg > predictedHealth && target.CountAlliesInRange(500) == 0 && Player.Distance(target) > Config.Item("MinRangeR", true).GetValue<Slider>().Value)
                    {
                        Program.CastSpell(R,target);
                        Program.debug("R normal");
                    }
                    if ( Program.Combo && Config.Item("Raoe", true).GetValue<bool>() && Player.CountEnemiesInRange(1000) == 0)
                    {
                        R.CastIfWillHit(target, 3, true);
                    }
                }
            }
        }


        private bool DashCheck(Vector3 dash)
        {
            if ((!dash.UnderTurret(true) || Program.Combo))
                return true;
            else
                return false;
        }

        private double getRdmg(Obj_AI_Base target)
        {
            var rDmg = R.GetDamage(target);
            var dmg = 0;
            PredictionOutput output = R.GetPrediction(target);
            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
            direction.Normalize();
            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
            foreach (var enemy in enemies)
            {
                PredictionOutput prediction = R.GetPrediction(enemy);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range, MinionTypes.All);
            foreach (var minion in allMinionsR)
            {
                PredictionOutput prediction = R.GetPrediction(minion);
                Vector3 predictedPosition = prediction.CastPosition;
                Vector3 v = output.CastPosition - Player.ServerPosition;
                Vector3 w = predictedPosition - Player.ServerPosition;
                double c1 = Vector3.Dot(w, v);
                double c2 = Vector3.Dot(v, v);
                double b = c1 / c2;
                Vector3 pb = Player.ServerPosition + ((float)b * v);
                float length = Vector3.Distance(predictedPosition, pb);
                if (length < (R.Width + 100 + minion.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                    dmg++;
            }
            //if (Config.Item("debug", true).GetValue<bool>())
            //    Game.PrintChat("R collision" + dmg);
            if (dmg == 0)
                return rDmg;
            else if (dmg > 7)
                return rDmg * 0.7;
            else
                return rDmg - (rDmg * 0.1 * dmg);

        }

        private float GetPassiveTime()
        {
            return
                ObjectManager.Player.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "ezrealrisingspellforce")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
        }
        public void debug(string msg)
        {
            MsgDebug = msg;
            NotTime = Game.Time;
            if (Config.Item("debug", true).GetValue<bool>())
                Console.WriteLine(msg);
        }

        private bool Farm
        {
            get { return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit); }
        }

        public void farmQ()
        {
            if (Program.LaneClear)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 800, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    Q.Cast(mob, true);
                }
            }

            if (!Config.Item("farmQ", true).GetValue<bool>())
                return;
            
            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            int orbTarget = 0;
            if (Orbwalker.GetTarget() != null)
                orbTarget = Orbwalker.GetTarget().NetworkId;

            foreach (var minion in minions.Where(minion => minion.IsValidTarget() && orbTarget != minion.NetworkId && !Orbwalker.InAutoAttackRange(minion) && minion.Health < Q.GetDamage(minion)))
            {
                if (Q.Cast(minion) == Spell.CastStates.SuccessfullyCasted)
                    return;
            }

            if (Config.Item("LC", true).GetValue<bool>() && Program.LaneClear && !Orbwalking.CanAttack() && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value)
            {
                var LCP = Config.Item("LCP", true).GetValue<bool>();
                var PT = Game.Time - GetPassiveTime() > -1.5 || !E.IsReady();

                foreach (var minion in minions.Where(minion => minion.IsValidTarget() && Orbwalker.InAutoAttackRange(minion) && orbTarget != minion.NetworkId))
                {
                    var hpPred = HealthPrediction.GetHealthPrediction(minion, 300);
                    if (hpPred < 10)
                        continue;
                    var dmgMinion = minion.GetAutoAttackDamage(minion);
                    var qDmg = Q.GetDamage(minion);
                    if (hpPred < qDmg)
                    {
                        if (hpPred > dmgMinion)
                        {
                            if (Q.Cast(minion) == Spell.CastStates.SuccessfullyCasted)
                                return;
                        }
                    }
                    else if (PT || LCP)
                    {
                        if(hpPred > dmgMinion + qDmg)
                        {
                            if (Q.Cast(minion) == Spell.CastStates.SuccessfullyCasted)
                                return;
                        }
                    }
                }
            }
        }

        private void KsJungle()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, float.MaxValue, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            foreach (var mob in mobs)
            {
                //debug(mob.SkinName);
                if (((mob.SkinName == "SRU_Dragon" && Config.Item("Rdragon", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Baron" && Config.Item("Rbaron", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Red" && Config.Item("Rred", true).GetValue<bool>())
                    || (mob.SkinName == "SRU_Blue" && Config.Item("Rblue", true).GetValue<bool>()))
                    && (mob.CountAlliesInRange(1000) == 0 || Config.Item("Rally", true).GetValue<bool>())
                    && mob.Health < mob.MaxHealth
                    && mob.Distance(Player.Position) > 1000
                    )
                {
                    if (DragonDmg == 0)
                        DragonDmg = mob.Health;

                    if (Game.Time - DragonTime > 3)
                    {
                        if (DragonDmg - mob.Health > 0)
                        {
                            DragonDmg = mob.Health;
                        }
                        DragonTime = Game.Time;
                    }
                    else
                    {
                        var DmgSec = (DragonDmg - mob.Health) * (Math.Abs(DragonTime - Game.Time) / 3);
                        //Program.debug("DS  " + DmgSec);
                        if (DragonDmg - mob.Health > 0)
                        {
                            debug(mob.SkinName + " " + (DmgSec / 3) + " dmg per sec");
                            var timeTravel = GetUltTravelTime(Player, R.Speed, R.Delay, mob.Position);
                            var timeR = (mob.Health - R.GetDamage(mob)) / (DmgSec / 3);
                            //Program.debug("timeTravel " + timeTravel + "timeR " + timeR + "d " + R.GetDamage(mob));
                            if (timeTravel > timeR)
                                R.Cast(mob.Position);
                            
                        }
                        else
                            DragonDmg = mob.Health;
                        
                        //Program.debug("" + GetUltTravelTime(ObjectManager.Player, R.Speed, R.Delay, mob.Position));
                    }
                }
            }
        }

        private float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            float distance = Vector3.Distance(source.ServerPosition, targetpos);
            float missilespeed = speed;

            return (distance / missilespeed + delay);
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable" ,true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
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

        public static void drawText(string msg, Obj_AI_Hero Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
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
                        Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }


            if (Config.Item("noti", true).GetValue<bool>())
            {
                if (Game.Time - NotTime < 10)
                {
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.Red, MsgDebug);
                }
                else
                {
                    MsgDebug = "wait";
                    Drawing.DrawText(Drawing.Width * 0.01f, Drawing.Height * 0.5f, System.Drawing.Color.GreenYellow, MsgDebug);
                }

                var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {

                    var poutput = Q.GetPrediction(target);
                    if ((int)poutput.Hitchance == 5)
                        Render.Circle.DrawCircle(poutput.CastPosition, 50, System.Drawing.Color.YellowGreen);
                    if (Q.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                    else if (Q.GetDamage(target) + W.GetDamage(target) + E.GetDamage(target) > target.Health)
                    {
                        Render.Circle.DrawCircle(target.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "Q + W + E kill: " + target.ChampionName + " have: " + target.Health + "hp");
                    }
                }
            }
        }
    }
}
