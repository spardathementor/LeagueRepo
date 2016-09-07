using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;

namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Teemo
    {
        private Spell E, Q, R, W;
        public static SebbyLib.Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Menu Config = Program.Config;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 680);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 400);

            Q.SetTargetted(0.5f, 1500f);
            R.SetSkillshot(1.5f, 120f, 1000f, false, SkillshotType.SkillshotCircle);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification & line", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("Q On").SubMenu("Q Config").AddItem(new MenuItem("qUseOn" + enemy.ChampionName, enemy.ChampionName).SetValue(true));


            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("comboR", "run R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Raoe", "aoe", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRslow", "On slow", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRcc", "On CC", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRdash", "On dash", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("telR", "On zhonya, teleport", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("bushR2", "Bush above 3 ammo", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("bushR", "Auto W bush after enemy enter", true).SetValue(true));
            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {

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
            RMANA = R.Instance.ManaCost;
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
                R.Range = 160 + 250 * R.Level;
            }
            

            if (Q.IsReady() && SebbyLib.Orbwalking.CanMove(50) && Config.Item("autoW", true).GetValue<bool>())
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() &&  Config.Item("autoW", true).GetValue<bool>())
                LogicW();

            if (R.IsReady() && Config.Item("autoR", true).GetValue<bool>())
                LogicR();
        }

        private void LogicW()
        {
            if (Player.Mana < RMANA + WMANA)
                return;

            if (Player.CountEnemiesInRange(300) > 0 && Player.IsMoving)
                W.Cast();

            if (Program.Combo)
            {
                
                var t = TargetSelector.GetTarget(800, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget() && !Orbwalker.InAutoAttackRange(t))
                    W.Cast();
            }
        }

        private void LogicR()
        {
            if (Player.Mana > RMANA + QMANA)
            {
                if (Program.LagFree(1))
                {
                    foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(R.Range + 100)))
                    {
                        if(Config.Item("autoRcc", true).GetValue<bool>() && !OktwCommon.CanMove(enemy))
                            R.Cast(enemy);
                        if (Config.Item("autoRdash", true).GetValue<bool>())
                            R.CastIfHitchanceEquals(enemy, HitChance.Dashing);
                        if (Config.Item("autoRslow", true).GetValue<bool>() && enemy.HasBuffOfType(BuffType.Slow))
                            Program.CastSpell(R, enemy);
                        if (Config.Item("Raoe", true).GetValue<bool>())
                            R.CastIfWillHit(enemy, 2);
                        if (Config.Item("comboR", true).GetValue<bool>() && OktwCommon.IsMovingInSameDirection(Player, enemy))
                        {
                            var predPos = R.GetPrediction(enemy);
                            if(predPos.CastPosition.Distance(enemy.Position) > 200 && predPos.Hitchance >= HitChance.Low)
                                R.Cast(predPos.CastPosition);
                        }
                    }
                }

                if (Program.LagFree(2) && Config.Item("telR", true).GetValue<bool>())
                {
                    var trapPos = OktwCommon.GetTrapPos(R.Range);
                    if (!trapPos.IsZero)
                        R.Cast(trapPos);
                }

                if ((int)(Game.Time * 10) % 2 == 0 && Config.Item("bushR2", true).GetValue<bool>() && Utils.TickCount - R.LastCastAttemptT > 4000)
                {
                    if (Player.Spellbook.GetSpell(SpellSlot.R).Ammo > 1 && Player.CountEnemiesInRange(800) == 0)
                    {
                        var points = OktwCommon.CirclePoints(8, R.Range, Player.Position);
                        foreach (var point in points)
                        {
                            if (NavMesh.IsWallOfGrass(point, 0))
                            {
                                if (!OktwCommon.CirclePoints(8, 120, point).Any(x => x.IsWall()))
                                {
                                    R.Cast(point);
                                    return;
                                }
                            }
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
                if (OktwCommon.GetKsDamage(t, Q) > t.Health)
                    Q.Cast(t);

                if (!Config.Item("qUseOn" + t.ChampionName).GetValue<bool>())
                    return;

                if (Program.Combo)
                    Q.Cast(t);
                else if (Program.Farm && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + WMANA + QMANA + QMANA)
                    Q.Cast(t);
            }
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {

        }

        private void Drawing_OnDraw(EventArgs args)
        {
            
        }
    }
}
