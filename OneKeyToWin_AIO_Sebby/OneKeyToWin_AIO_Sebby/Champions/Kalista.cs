using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Kalista
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Spell Q, Q2, W, E, R;
        public float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        private int wCount = 0;
        private float grabTime = Game.Time, lastecast = 0f;

        private static Obj_AI_Hero AllyR;

        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1200);
            Q2 = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 5000);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1500f);

            Q.SetSkillshot(0.25f, 40f, 1200f, true, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(0.25f, 40f, 1200f, false, SkillshotType.SkillshotLine);

            LoadMenuOKTW();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.E )
            {
                lastecast = Game.Time;
            }
        }

        private void LoadMenuOKTW()
        {
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("Q Config").SubMenu("Harras Q").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("qMana", "Q harass mana %", true).SetValue(new Slider(50, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("qMode", "Q combo mode", true).SetValue(new StringList(new[] { "Always", "OKTW logic" }, 1)));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("countE", "Auto E if x stacks", true).SetValue(new Slider(10, 30, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Edmg", "E % dmg adjust", true).SetValue(new Slider(100, 150, 50)));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Edead", "Cast E before Kalista dead", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Ekillmin", "Cast E minion kill + harras target", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("Wdragon", "Auto W Dragon, Baron, Blue, Red", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Balista Config").AddItem(new MenuItem("balista", "Balista R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Balista Config").AddItem(new MenuItem("rangeBalista", "Balista min range", true).SetValue(new Slider(300, 1400, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmEcount", "Auto E if x minions", true).SetValue(new Slider(2, 10, 1)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQcount", "Lane clear Q if x minions", true).SetValue(new Slider(2, 10, 1)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("minionE", "Auto E big minion", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").SubMenu("E Config").AddItem(new MenuItem("jungleE", "Jungle ks E", true).SetValue(true));
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    lastecast = Game.Time;
                    Orbwalking.ResetAutoAttackTimer();
                }
                if (args.SData.Name == "kalistaw")
                {
                    wCount++;
                }
            }

            if (R.IsReady() && sender.IsAlly && args.SData.Name == "RocketGrab" && Player.Distance(sender.Position) < R.Range && Player.Distance(sender.Position) > Config.Item("rangeBalista", true).GetValue<Slider>().Value)
            {
                grabTime = Game.Time;
            }

            if (!sender.IsEnemy || sender.IsMinion)
                return;

            if (E.IsReady()  && Config.Item("Edead", true).GetValue<bool>() && sender.IsValidTarget(1500))
            {
                double dmg = 0;

                if (args.Target != null && args.Target.IsMe)
                {
                    dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
                }
                else 
                {
                    var castArea = Player.Distance(args.End) * (args.End - Player.ServerPosition).Normalized() + Player.ServerPosition;
                    if (castArea.Distance(Player.ServerPosition) < Player.BoundingRadius / 2)
                    {
                        dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
                    }
                }

                if (Player.Health - dmg < (Player.CountEnemiesInRange(600) * Player.Level * 10 ) + (Player.Level * 10))
                {
                     CastE();
                }
            }
            if (R.IsReady())
            {
                if (AllyR != null && AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range)
                {
                    if (AllyR.Health < AllyR.CountEnemiesInRange(600) * AllyR.Level * 30)
                    {
                        R.Cast();
                    }
                    double dmg = 0;

                    if (args.Target != null && args.Target.NetworkId == AllyR.NetworkId)
                    {
                        dmg = dmg + sender.GetSpellDamage(Player, args.SData.Name);
                    }
                    else
                    {
                        var castArea = AllyR.Distance(args.End) * (args.End - AllyR.ServerPosition).Normalized() + AllyR.ServerPosition;
                        if (castArea.Distance(AllyR.ServerPosition) < AllyR.BoundingRadius / 2)
                        {
                            dmg = dmg + sender.GetSpellDamage(AllyR, args.SData.Name);
                        }
                    }

                    if (AllyR.Health - dmg < (Player.CountEnemiesInRange(600) * AllyR.Level * 10) + (AllyR.Level * 10))
                    {
                        R.Cast();
                    }
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Q.IsReady() && Player.Mana > QMANA + EMANA)
            {
                var t = gapcloser.Sender;
                if (t.IsValidTarget(Q.Range) )
                    Q.Cast(t);
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("Recall") || Player.IsRecalling())
                return;

            if (R.IsReady() && AllyR != null && Config.Item("balista", true).GetValue<bool>() &&  AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range && AllyR.ChampionName == "Blitzcrank" && Player.Distance(AllyR.Position) > Config.Item("rangeBalista", true).GetValue<Slider>().Value)
            {
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget() && !enemy.IsDead && enemy.HasBuff("rocketgrab2")))
                {
                    R.Cast();
                }
                if (Game.Time - grabTime < 1)
                    return;
            }

            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (E.IsReady())
            {
                LogicE();
                JungleE();
            }

            if (Program.LagFree(1) && Q.IsReady() && !Player.IsWindingUp && !Player.IsDashing())
                LogicQ();
            if (Program.LagFree(2) && Program.LaneClear && Q.IsReady() && !Player.IsWindingUp && !Player.IsDashing() && Config.Item("farmQ", true).GetValue<bool>())
                FarmQ();

            if (Program.LagFree(3) && R.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                LogicR();

            if (Program.LagFree(4) && W.IsReady() && Program.None && !Player.IsWindingUp && !Player.IsDashing())
                LogicW();
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                var poutput = Q.GetPrediction(t);
                var col = poutput.CollisionObjects;
                bool cast = true;
                foreach (var colobj in col)
                {
                    if (Q.GetDamage(colobj) < colobj.Health)
                        cast = false;
                }

                var qDmg = Q.GetDamage(t) + Player.GetAutoAttackDamage(t);
                var eDmg = GetEdmg(t);

                if (qDmg > t.Health && eDmg < t.Health && Player.Mana > QMANA + EMANA)
                    castQ(cast, t);
                else if ((qDmg * 1.1) + eDmg > t.Health && eDmg < t.Health && Player.Mana > QMANA + EMANA && Orbwalking.InAutoAttackRange(t))
                    castQ(cast, t);
                else if (Program.Combo && Player.Mana > RMANA +  QMANA + EMANA )
                {
                    if(Config.Item("qMode", true).GetValue<StringList>().SelectedIndex == 0)
                        castQ(cast, t);
                    else if (!Orbwalking.InAutoAttackRange(t) || CountMeleeInRange(400) > 0)
                        castQ(cast, t);
                }
                else if (Program.Farm && !Orbwalking.InAutoAttackRange(t) && Config.Item("haras" + t.ChampionName).GetValue<bool>() && !Player.UnderTurret(true) && Player.ManaPercent > Config.Item("qMana", true).GetValue<Slider>().Value)
                    castQ(cast, t);
                if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        castQ(cast, t);
                }
            }
        }

        private void FarmQ()
        {
            var minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            int countMinion = 0;
            Obj_AI_Base bestMinion = null;

                foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range) && Q.GetDamage(minion) > minion.Health))
                {
                    var poutput = Q.GetPrediction(minion);
                    var col = poutput.CollisionObjects;

                    if(col.Count == 0)
                        continue;

                    foreach (var colobj in col)
                    {
                        if (Q.GetDamage(colobj) > colobj.Health)
                        {
                            countMinion++;
                            bestMinion = minion;
                        }
                        else
                        {
                            countMinion = 0;
                            bestMinion = null;
                            continue;
                        }
                    }
                countMinion = countMinion / 3;
                countMinion += 1;
            }
            if (bestMinion != null && countMinion >= Config.Item("farmQcount", true).GetValue<Slider>().Value)
                Q2.Cast(bestMinion);
        
        }

        private void LogicE()
        {
            var countE = Config.Item("countE", true).GetValue<Slider>().Value;
            bool eBigMinion = Config.Item("minionE", true).GetValue<bool>();
            int count = 0;
            int outRange = 0;
            var minions = MinionManager.GetMinions(Player.ServerPosition, E.Range - 50);

            if (!Player.IsDashing() && !Player.IsWindingUp)
            {
                foreach (var minion in minions.Where(minion => minion.IsValidTarget(E.Range )))
                {
                    if (minion.Health < E.GetDamage(minion) - minion.HPRegenRate)
                    {
                        if (GetPassiveTime(minion) > 0.5 && HealthPrediction.GetHealthPrediction(minion, 500, 250) > Player.GetAutoAttackDamage(minion))
                        {
                            count++;
                            if (!Orbwalking.InAutoAttackRange(minion))
                            {
                                outRange++;
                            }
                            if (eBigMinion)
                            {
                                var minionName = minion.BaseSkinName.ToLower();
                                if (minionName.Contains("siege") || minionName.Contains("super"))
                                {
                                    outRange++;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && OktwCommon.ValidUlt(target)))
            {
                var eDmg = GetEdmg(target);
                if (target.Health < eDmg)
                {
                    CastE();
                }
                if (0 < eDmg && count > 0 && Config.Item("Ekillmin", true).GetValue<bool>())
                {
                    CastE();

                }
                if (GetEStacks(target) >= countE && (GetPassiveTime(target) < 0.5 || Player.CountEnemiesInRange(750) == 0) && Player.Mana > RMANA + EMANA)
                {
                    CastE();
                }
            }

            if (Program.Farm && count > 0 && Config.Item("farmE", true).GetValue<bool>())
            {
                if (outRange > 0)
                {
                    CastE();
                }
                if ((count >= Config.Item("farmEcount", true).GetValue<Slider>().Value || ((Player.UnderTurret(false) && !Player.UnderTurret(true)) && Player.Mana > RMANA + QMANA + EMANA)))
                {
                    CastE();

                }
            }
        }

        private void LogicW()
        {
            if (Config.Item("Wdragon", true).GetValue<bool>() &&  !Orbwalker.GetTarget().IsValidTarget() && !Program.Combo && Player.CountEnemiesInRange(800)==0)
            {
                if (wCount > 0)
                {
                    Vector3 baronPos;
                    baronPos.X = 5232;
                    baronPos.Y = 10788;
                    baronPos.Z = 0;
                    if (Player.Distance(baronPos) < 5000)
                        W.Cast(baronPos);
                }
                if (wCount == 0)
                {
                    Vector3 dragonPos;
                    dragonPos.X = 9919f;
                    dragonPos.Y = 4475f;
                    dragonPos.Z = 0f;
                    if (Player.Distance(dragonPos) < 5000)
                        W.Cast(dragonPos);
                    else
                        wCount ++;
                    return;
                }

                if (wCount == 1)
                {
                    Vector3 redPos;
                    redPos.X = 8022;
                    redPos.Y = 4156;
                    redPos.Z = 0;
                    if (Player.Distance(redPos) < 5000)
                        W.Cast(redPos);
                    else
                        wCount++;
                    return;
                }
                if (wCount == 2)
                {
                    Vector3 bluePos;
                    bluePos.X = 11396;
                    bluePos.Y = 7076;
                    bluePos.Z = 0;
                    if (Player.Distance(bluePos) < 5000)
                        W.Cast(bluePos);
                    else
                        wCount++;
                    return;
                }
                if (wCount > 2)
                {
                    wCount = 0;
                }
            }
        }

        private void JungleE()
        {

            if (!Config.Item("jungleE", true).GetValue<bool>())
                return;

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                var dmg = GetEdmg(mob);


                if (mob.Name.Contains("Baron") && Player.HasBuff("barontarget"))
                {
                    dmg = dmg * 0.5f;
                }
                if (mob.Name.Contains("Dragon") && Player.HasBuff("s5test_dragonslayerbuff"))
                {
                    dmg = dmg * (1 - (0.07f * ObjectManager.Player.GetBuffCount("s5test_dragonslayerbuff")));
                }

                if (mob.Health < dmg)
                    CastE();
            }
        }

        private float GetEdmg( Obj_AI_Base t)
        {

            var eDamage = E.GetDamage(t);

            if (Player.HasBuff("summonerexhaust"))
                eDamage = eDamage * 0.6f;

            if (t.HasBuff("ferocioushowl"))
                eDamage = eDamage * 0.7f;

            if (t is Obj_AI_Hero)
            {
                var champion = (Obj_AI_Hero)t;
                if (champion.ChampionName == "Blitzcrank" && !champion.HasBuff("BlitzcrankManaBarrierCD") && !champion.HasBuff("ManaBarrier"))
                {
                    eDamage -= champion.Mana / 2f;
                }
            }

            eDamage -= t.HPRegenRate;
            eDamage -= t.PercentLifeStealMod * 0.005f * t.FlatPhysicalDamageMod;
            eDamage = eDamage * 0.01f * (float)Config.Item("Edmg", true).GetValue<Slider>().Value;

            return eDamage;
        }

        private int CountMeleeInRange(float range)
        {
            int count = 0;
            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(range) && target.IsMelee))
            {
                count++;
            }
            return count;
        }

        private void CastE()
        {
            if (Game.Time - lastecast < 0.4)
            {
                return;
            }
            else
            { 
                
                E.Cast();
            }
        }

        void castQ(bool cast, Obj_AI_Base t)
        {
            if (cast)
                Program.CastSpell(Q2, t);
            else
                Program.CastSpell(Q, t);
        }

        private float GetPassiveTime(Obj_AI_Base target)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == "kalistaexpungemarker")
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }

        private int GetEStacks(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "kalistaexpungemarker")
                    return buff.Count;
            }
            return 0;
        }

        private void LogicR()
        {

            if (AllyR == null)
            {
                foreach (var ally in Program.Allies.Where(ally => !ally.IsDead && !ally.IsMe && ally.HasBuff("kalistacoopstrikeally")))
                {
                    AllyR = ally;
                    break;
                }
            }
            else if (AllyR.IsVisible && AllyR.Distance(Player.Position) < R.Range)
            {
                if (AllyR.Health < AllyR.CountEnemiesInRange(600) * AllyR.Level * 30)
                {
                    R.Cast();
                }
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

        public static void drawText(string msg, Obj_AI_Base Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1] - 200, color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            foreach (var enemy in Program.Enemies.Where(target => target.IsValidTarget(E.Range + 500) && target.IsEnemy))
            {
                float hp = enemy.Health - E.GetDamage(enemy);
                int stack = GetEStacks(enemy);
                float dmg = (float)Player.GetAutoAttackDamage(enemy) * 2f;
                if (stack > 0)
                    dmg = (float)Player.GetAutoAttackDamage(enemy) + (E.GetDamage(enemy) / (float)stack);

                if (hp > 0)
                    drawText((int)((E.GetDamage(enemy) / enemy.Health) * 100) + " %", enemy, System.Drawing.Color.GreenYellow);
                else
                    drawText("KILL E", enemy, System.Drawing.Color.Red);
            }

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                drawText((int)((E.GetDamage(mob) / mob.Health) * 100) + " %", mob, System.Drawing.Color.GreenYellow);

            }

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

    }
}
