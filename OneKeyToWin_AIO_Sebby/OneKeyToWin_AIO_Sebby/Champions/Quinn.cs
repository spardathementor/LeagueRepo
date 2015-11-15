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
    class Quinn
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell Q, W, E, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        public Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 980);
            E = new Spell(SpellSlot.E, 700);
            W = new Spell(SpellSlot.W, 2100);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 80f, 1150, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.25f, 2000f);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("harrasQ", "Harass Q", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("harrasE", "Harass E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("AGC", "AntiGapcloser E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("Int", "Interrupter E", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += afterAttack;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(1))
                SetMana();
            if (Program.LagFree(2) && Q.IsReady() && !Player.IsWindingUp && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();

            if (Program.LagFree(4) && R.IsReady())
                LogicR();
        }


        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && Config.Item("Int", true).GetValue<bool>() && sender.IsValidTarget(E.Range))
                E.CastOnUnit(sender);
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            if(Config.Item("autoE", true).GetValue<bool>())
                LogicE();
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && Config.Item("AGC", true).GetValue<bool>() )
            {
                var t = gapcloser.Sender;
                if (t.IsValidTarget(E.Range))
                {
                    E.Cast(t);
                }
            }
        }

        private void LogicR()
        {
            
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget(Q.Range))
            {
                if (Q.GetDamage(t) > t.Health)
                    Program.CastSpell(Q, t);
                else if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                else if ((Program.Farm && Player.Mana > RMANA + EMANA + QMANA + WMANA) && Config.Item("harrasQ", true).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true))
                {
                    Program.CastSpell(Q, t);
                }
            }
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if ( t.IsValidTarget(E.Range) && !t.HasBuff("QuinnW"))
            {
                if (E.GetDamage(t) > t.Health)
                    E.Cast(t);
                else if (Program.Combo && Player.Mana > RMANA + EMANA)
                    E.Cast(t);
                else if ((Program.Farm && Player.Mana > RMANA + EMANA + QMANA + WMANA) && Config.Item("harrasE", true).GetValue<bool>() && !ObjectManager.Player.UnderTurret(true))
                {
                    E.Cast(t);
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
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
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
        }
    }
}
