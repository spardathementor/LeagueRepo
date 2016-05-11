using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;
using LeagueSharp.SDK.UI;
using SebbyLib;
using OneKeyToWin_AIO_2_by_Sebby.Core;

namespace OneKeyToWin_AIO_2_by_Sebby.Champions
{
    class Jinx : Program
    {
        public Jinx()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 920f);
            R = new Spell(SpellSlot.R, 3000f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 100f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);
            

            MenuHero = MainMenu.Add(new Menu("MenuHero", Player.ChampionName));
                MenuDraw = MenuHero.Add(new Menu("MenuDraw", "Draw"));
                    MenuDraw.Add(new MenuBool("Q", "Q range", true));
                    MenuDraw.Add(new MenuBool("W", "W range", true));
                    MenuDraw.Add(new MenuBool("E", "E range", true));
                    MenuDraw.Add(new MenuBool("R", "R range", true));

                MenuQ = MenuHero.Add(new Menu("MenuQ", "Q Config"));
                    MenuQ.Add(new MenuBool("Auto", "Auto", true));
                    MenuQ.Add(new MenuBool("Mix", "Mix", true));

                MenuW = MenuHero.Add(new Menu("MenuW", "W Config"));
                    MenuW.Add(new MenuBool("Auto", "Auto", true));
                    MenuW.Add(new MenuBool("Mix", "Mix", true));
                MenuE = MenuHero.Add(new Menu("MenuE", "E Config"));
                    MenuE.Add(new MenuBool("Auto", "Auto", true));
                    MenuE.Add(new MenuBool("telE", "Auto E on teleport", true));
                    MenuE.Add(new MenuBool("comboE", "Auto E combo logic", true));

                MenuR = MenuHero.Add(new Menu("MenuR", "R Config"));
                MenuR.Add(new MenuBool("Auto", "Auto", true));
                MenuR.Add(new MenuBool("Rturrent", "Don't R under turret", true));
                MenuR.Add(new MenuKeyBind("cast", "Lane Clear", System.Windows.Forms.Keys.T, KeyBindType.Toggle));

            MenuFarm = MenuHero.Add(new Menu("MenuFarm", "Farm"));
                    MenuFarm.Add(new MenuSlider("Mana", "LaneClear mana ", 50));
                    MenuFarm.Add(new MenuBool("Q", "Farm Q", true));

            Orbwalker.OnAction += OnAction;
            Game.OnUpdate += Game_OnUpdate;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (LagFree(0))
                SetMana();

            if (LagFree(1) && Q.IsReady() && MenuQ["Auto"] )
                LogicQ();
            if (LagFree(2) && W.IsReady() && MenuW["Auto"])
                LogicW();
            if (LagFree(3) && E.IsReady() && MenuE["Auto"])
                LogicE();
            if (LagFree(4) && R.IsReady() && MenuR["Auto"])
                LogicR();
        }

        private void LogicQ()
        {
            if (Farm && MenuFarm["Q"] && MenuFarm["Farm"].GetValue<MenuKeyBind>().Active && !FishBoneActive && !Player.IsWindingUp && Orbwalker.GetTarget() == null && Orbwalker.CanAttack() &&  Player.Mana > RMANA + WMANA + EMANA + 10)
            {
                foreach (var minion in GameObjects.EnemyMinions.Where(minion => minion.IsValidTarget(bonusRange() + 30) && !OktwStuff.InAutoAttackRange(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion)))
                {
                    var hpPred = Health.GetPrediction(minion, 400);
                    if (hpPred < Player.GetAutoAttackDamage(minion) * 1.1 && hpPred > 5)
                    {
                        Orbwalker.ForceTarget = minion;
                        Q.Cast();
                        return;
                    }
                }
            }

            var t = TargetSelector.GetTarget(bonusRange() + 60, DamageType.Physical);
            if (t.IsValidTarget())
            {
                if (!FishBoneActive && (!OktwStuff.InAutoAttackRange(t) || t.CountEnemyHeroesInRange(250) > 2) && Orbwalker.GetTarget() == null)
                {

                    var distance = GetRealDistance(t);
                    if (Combo && (Player.Mana > RMANA + WMANA + 10 || Player.GetAutoAttackDamage(t) * 3 > t.Health))
                        Q.Cast();
                    else if (Farm && MenuQ["Mix"] && MenuHarass["H" + t.ChampionName] && !Player.IsWindingUp && Orbwalker.CanAttack() &&  !Player.IsUnderEnemyTurret() && Player.Mana > RMANA + WMANA + EMANA + 20 && distance < bonusRange() + t.BoundingRadius + Player.BoundingRadius)
                        Q.Cast();
                }
            }
            else if (!FishBoneActive && Combo && Player.Mana > RMANA + WMANA + 20 && Player.CountEnemyHeroesInRange(2000) > 0)
                Q.Cast();
            else if (FishBoneActive && Combo && Player.Mana < RMANA + WMANA + 20)
                Q.Cast();
            else if (FishBoneActive && Combo && Player.CountEnemyHeroesInRange(2000) == 0)
                Q.Cast();
            else if (FishBoneActive && (Farm || Orbwalker.ActiveMode == OrbwalkingMode.LastHit))
            {
                Q.Cast();
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W);
            if (t.IsValidTarget())
            {
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && enemy.Distance(Player) > bonusRange()))
                {
                    var comboDmg = OktwStuff.GetKsDamage(t, W);
                    if (R.IsReady() && Player.Mana > RMANA + WMANA + 20)
                    {
                        comboDmg += R.GetDamage(enemy);
                    }
                    if (comboDmg > enemy.Health && OktwCommon.ValidUlt(enemy))
                    {
                        CastSpell(W, enemy);
                        return;
                    }
                }

                if (Player.CountEnemyHeroesInRange(bonusRange()) == 0)
                {
                    if (Combo && Player.Mana > RMANA + WMANA + 10)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && GetRealDistance(enemy) > bonusRange()).OrderBy(enemy => enemy.Health))
                            CastSpell(W, enemy);
                    }
                    else if (Farm && Player.Mana > RMANA + EMANA + WMANA + WMANA + 40 && OktwStuff.CanHarras())
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && MenuHarass["H" + t.ChampionName]))
                            CastSpell(W, enemy);
                    }
                }
                if (!None && Player.Mana > RMANA + WMANA && Player.CountEnemyHeroesInRange(GetRealPowPowRange(t)) == 0)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
        }

        private void LogicE()
        {
            if (Player.Mana > RMANA + EMANA && MenuE["Auto"])
            {
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                {
                    E.Cast(enemy.Position);
                    return;
                }

                if (MenuE["telE"])
                {
                    foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.IsEnemy && Obj.Distance(Player.ServerPosition) < E.Range && (Obj.HasBuff("teleport_target") || Obj.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                    {
                        E.Cast(Object.Position);
                    }
                }

                if (Combo && Player.IsMoving && MenuE["comboE"] && Player.Mana > RMANA + EMANA + WMANA)
                {
                    var t = TargetSelector.GetTarget(E);
                    if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200 && (int)E.GetPrediction(t).Hitchance == 5)
                    {
                        E.CastIfWillHit(t, 2);
                        if (t.HasBuffOfType(BuffType.Slow))
                        {
                            CastSpell(E, t);
                        }
                        else
                        {
                            if (E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                            {
                                if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                                {
                                    if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                                        CastSpell(E, t);
                                }
                                else
                                {
                                    if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                                        CastSpell(E, t);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void LogicR()
        {
            if (Player.IsUnderEnemyTurret() && MenuR["Rturrent"])
                return;

            if (Environment.TickCount - W.LastCastAttemptT > 1200 && MenuR["Auto"])
            {
                foreach (var t in GameObjects.EnemyHeroes.Where(t => t.IsValidTarget(R.Range) && OktwCommon.ValidUlt(t)))
                {
                    var predictedHealth = t.Health - OktwCommon.GetIncomingDamage(t);
                    var Rdmg = GetRDamage(t);
                    if (Rdmg > predictedHealth && !OktwStuff.IsSpellHeroCollision(t, R) && GetRealDistance(t) > bonusRange() + 200)
                    {
                        if (GetRealDistance(t) > bonusRange() + 300 + t.BoundingRadius && t.CountAllyHeroesInRange(600) == 0 && Player.CountEnemyHeroesInRange(400) == 0)
                        {
                            CastSpell(R, t);
                        }
                        else if (t.CountEnemyHeroesInRange(200) > 2)
                        {
                            CastSpell(R, t);
                        }
                    }
                }
            }
        }

        private void OnAction(object sender, OrbwalkingActionArgs e)
        {
            if (e.Type == OrbwalkingType.BeforeAttack)
            {
                if (!Q.IsReady() || !MenuQ["Auto"] || !FishBoneActive)
                    return;

                var t = e.Target as Obj_AI_Hero;

                if (t != null)
                {
                    var realDistance = GetRealDistance(t) - 50;
                    if (Combo && (realDistance < GetRealPowPowRange(t) || (Player.Mana < RMANA + 20 && Player.GetAutoAttackDamage(t) * 3 < t.Health)))
                        Q.Cast();
                    else if (Farm && MenuQ["Mix"] && (realDistance > bonusRange() || realDistance < GetRealPowPowRange(t) || Player.Mana < RMANA + EMANA + WMANA + WMANA))
                        Q.Cast();
                }

                var minion = e.Target as Obj_AI_Minion;
                if (Farm && minion != null)
                {
                    var realDistance = GetRealDistance(minion);

                    if (realDistance < GetRealPowPowRange(minion) || Player.ManaPercent < MenuFarm["Mana"])
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private float GetRDamage(Obj_AI_Hero target)
        {
            var maxDmg = new double[] { 160, 224, 288 }[R.Level - 1] + new double[] { 20, 24, 28 }[R.Level - 1] / 100 * (target.MaxHealth - target.Health) + 0.8 * Player.FlatPhysicalDamageMod;

            var dis = Player.Distance(target);
            if (Player.Distance(target) > 1500)
            {
                return (float)Player.CalculateDamage(target, DamageType.Physical, maxDmg);
            }
            else
            {
                var x = dis / 100;
                var y = x * 6;
                return (float)Player.CalculateDamage(target, DamageType.Physical, maxDmg * 0.01 * y);
            } 
        }

        private bool FishBoneActive { get { return Player.HasBuff("JinxQ"); } }

        private float bonusRange() { return 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level; }

        private float GetRealDistance(Obj_AI_Base target)
        {
            return Player.ServerPosition.Distance(Movement.GetPrediction(target, 0.05f).CastPosition) + Player.BoundingRadius + target.BoundingRadius;
        }

        private float GetRealPowPowRange(GameObject target)
        {
            return 650f + Player.BoundingRadius + target.BoundingRadius;
        }

        private void SetMana()
        {
            if ((MenuAdvance["mana"] && Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = 10;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = WMANA - Player.PARRegenRate * W.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }
    }
}
