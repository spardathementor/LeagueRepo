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
    class TwistedFate
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, W, E, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private string temp = null;
        private bool cardok = true;
        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1400);
            E = new Spell(SpellSlot.E, 700);
            W = new Spell(SpellSlot.W, 1200);
            R = new Spell(SpellSlot.R, 5500);

            Q.SetSkillshot(0.25f, 40f, 1000, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 40f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, 2000f);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRangeMini", "R range minimap", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("harrasQ", "Harass Q", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasE", "Harass E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("AGC", "AntiGapcloser E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("Int", "Interrupter E", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("useR", "Semi-manual cast R key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space 
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Renemy", "Don't R if enemy in x range", true).SetValue(new Slider(1000, 2000, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("RenemyA", "Don't R if ally in x range near target", true).SetValue(new Slider(800, 2000, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("turetR", "Don't R under turret ", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnDraw += Drawing_OnDraw;

        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if(W.IsReady())
                LogicW();
            else
            {
                temp = null;
                cardok = false;
            }

            if(Q.IsReady())
                LogicQ();
                
            if (R.IsReady())
            {
                if(W.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                    LogicR();

                if (Config.Item("useR", true).GetValue<KeyBind>().Active)
                {
                    if (Player.HasBuff("destiny_marker"))
                    {
                        var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                        if (t.IsValidTarget())
                        {
                            R.Cast(t);
                        }
                    }
                    else
                    {
                        R.Cast();
                    }
                }
            }
                //Program.debug("" + (W.Instance.CooldownExpires - Game.Time));
        }

        private void LogicR()
        {
            if (Player.CountEnemiesInRange(Config.Item("Renemy", true).GetValue<Slider>().Value) == 0)
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() && t.Distance(Player.Position) > Q.Range && t.CountAlliesInRange(Config.Item("RenemyA", true).GetValue<Slider>().Value) == 0)
                {
                    if (Q.GetDamage(t) + W.GetDamage(t) + Player.GetAutoAttackDamage(t) * 3 > t.Health && t.CountEnemiesInRange(1000) < 3)
                    {
                        var rPos = R.GetPrediction(t).CastPosition;
                        if (Config.Item("turetR", true).GetValue<bool>())
                        {
                            if (!rPos.UnderTurret(true))
                                R.Cast(rPos);
                        }
                        else
                        {
                            R.Cast(rPos);
                        }
                    }
                }
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (OktwCommon.GetKsDamage(t, Q)> t.Health && !Orbwalking.InAutoAttackRange(t))
                    Program.CastSpell(Q, t);

                if (Player.Mana > RMANA + QMANA)
                {
                    if (W.Instance.CooldownExpires - Game.Time < W.Instance.Cooldown - 1.3 && (W.Instance.CooldownExpires - Game.Time  > 2 || Player.CountEnemiesInRange(900) == 0))
                    {
                        if (Program.Combo)
                            Program.CastSpell(Q, t);
                        if (Program.Farm && !Player.UnderTurret(true) && OktwCommon.CanHarras())
                            Program.CastSpell(Q, t);
                    }

                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                        Q.Cast(enemy, true);
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmQ", true).GetValue<bool>() && Player.Mana > RMANA + QMANA)
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All);
                var farmPosition = Q.GetLineFarmLocation(minionList, Q.Width);
                if (farmPosition.MinionsHit > Config.Item("LCminions", true).GetValue<Slider>().Value)
                    Q.Cast(farmPosition.Position);
            }
        }

        private void LogicW()
        {
            var wName = W.Instance.Name;
            var t = TargetSelector.GetTarget(1100, TargetSelector.DamageType.Magical);

            if (wName == "PickACard")
            {
                if(R.IsReady() && (Player.HasBuff("destiny_marker") || Player.HasBuff("gate")))
                    W.Cast();
                else if (t.IsValidTarget() && Program.Combo)
                    W.Cast();
                else if ( Program.Farm && Orbwalker.GetTarget() != null && Orbwalker.GetTarget().Type == GameObjectType.obj_AI_Minion)
                    W.Cast();
                else if (Program.Farm && Player.CountEnemiesInRange(800) > 0)
                    W.Cast();
            }
            else
            {
                if (temp == null)
                    temp = wName;
                else if (temp != wName)
                    cardok = true;

                if(cardok)
                {
                    if (R.IsReady() && (Player.HasBuff("destiny_marker") || Player.HasBuff("gate")))
                    {
                        if (wName == "goldcardlock")
                            W.Cast();
                    }
                    else if (Player.CountEnemiesInRange(800) > 0 || ( Orbwalker.GetTarget() != null && Orbwalker.GetTarget().Type == GameObjectType.obj_AI_Hero))
                    {
                        if (wName == "goldcardlock")
                            W.Cast();
                    }
                    else if (Player.ManaPercent > 90 && Program.LaneClear)
                    {
                        if (wName == "redcardlock")
                            W.Cast();
                    }
                    else if ((Player.ManaPercent < 90 && Program.Farm) || Player.Mana < RMANA + QMANA)
                    {
                        if (wName == "bluecardlock")
                            W.Cast();
                    } 
                    else 
                    {
                        if (wName == "goldcardlock")
                            W.Cast();
                    }
                }
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {

            if (Config.Item("rRangeMini", true).GetValue<bool>())
            {
                if (R.IsReady())
                    Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);
            }
            else
                Utility.DrawCircle(Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 20, true);


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

            if (R.IsReady() && Player.HasBuff("destiny_marker"))
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() )
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "AUTO R TARGET: " + t.ChampionName + " Heal " + t.Health + " My damage: " + (Q.GetDamage(t) + W.GetDamage(t) + Player.GetAutoAttackDamage(t) * 3));
                }
            }
        }
    }
}
