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
    class Karthus
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Vector3 Epos = Vector3.Zero;
        private float DragonDmg = 0;
        private double DragonTime = 0;

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 875);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 520);
            R = new Spell(SpellSlot.R, 20000);

            Q.SetSkillshot(1f, 160f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.5f, 50f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show notification", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRangeMini", "R range minimap", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("gapQ", "Auto Q Gap Closer", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harrasQ", "Harass Q", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Q Config").SubMenu("Use on:").AddItem(new MenuItem("Qon" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("autoE", "Auto E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("Emana", "E % minimum mana", true).SetValue(new Slider(20, 100, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").AddItem(new MenuItem("Wdmg", "W dmg % hp", true).SetValue(new Slider(10, 100, 0)));
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team == Player.Team))
            {
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("skillshot" + ally.ChampionName, "skillshot", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("targeted" + ally.ChampionName, "targeted", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("HardCC" + ally.ChampionName, "Hard CC", true).SetValue(true));
                Config.SubMenu(Player.ChampionName).SubMenu("W Shield Config").SubMenu("Shield ally").SubMenu(ally.ChampionName).AddItem(new MenuItem("Poison" + ally.ChampionName, "Poison", true).SetValue(true));
            }

            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQout", "Last hit Q minion out range AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", "LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle clear E", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            //Drawing.OnDraw += Drawing_OnDraw;
           // Drawing.OnEndScene += Drawing_OnEndScene;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {

            if (Program.LagFree(0))
            {
                SetMana();
            }
            if (Program.LagFree(1) && Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();
            if (Program.LagFree(2) && E.IsReady() && Config.Item("autoE", true).GetValue<bool>())
                LogicE();
            if (Program.LagFree(3) && R.IsReady())
                LogicR();
            if (Program.LagFree(4) && W.IsReady())
                LogicW();
        }

        private void LogicR()
        {
            if (Config.Item("autoR", true).GetValue<bool>())

            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(R.Range) && Player.CountEnemiesInRange(Q.Range+ 300) == 0 && target.CountAlliesInRange(600) == 0 && OktwCommon.ValidUlt(target)))
                {
                    float predictedHealth = target.Health + target.HPRegenRate * 5;
                    float Rdmg = OktwCommon.GetKsDamage(target, R);

                    if (Player.HasBuff("itemmagicshankcharge"))
                    {
                        if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                        {
                            Rdmg += (float)Player.CalcDamage(target, Damage.DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                        }
                    }

                    if (Rdmg > predictedHealth)
                    {
                        R.Cast();
                        Program.debug("R normal");
                    }
                }
            }
        }
        private float GetQDamage(Obj_AI_Base t)
        {
            var minions = MinionManager.GetMinions(t.Position, Q.Width);

            foreach (var minion in minions)
            {
                return Q.GetDamage(t, 1);
            }

            return Q.GetDamage(t);
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget() && Config.Item("Qon" + t.ChampionName).GetValue<bool>())
            {
                if (Q.GetDamage(t) > t.Health)
                    Program.CastSpell(Q, t);
                if (Program.Combo && Player.Mana > RMANA + QMANA)
                    Program.CastSpell(Q, t);
                if (Program.Farm && Config.Item("harrasQ", true).GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    Program.CastSpell(Q, t);
                foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range) && !OktwCommon.CanMove(enemy)))
                    Program.CastSpell(Q, t);
            }

            if (!Program.None && !Program.Combo && Player.Mana > RMANA + QMANA * 2)
            {
                var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
                if (Config.Item("farmQout", true).GetValue<bool>())
                {
                    foreach (var minion in allMinions.Where(minion => minion.IsValidTarget(Q.Range) && (!Orbwalker.InAutoAttackRange(minion) || Program.LaneClear) ))
                    {
                        var hpPred = HealthPrediction.GetHealthPrediction(minion, 1200);
                        if (hpPred < GetQDamage(minion) && hpPred > minion.FlatPhysicalDamageMod)
                        {
                            Q.Cast(minion);
                            return;
                        }
                    }
                }
                if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmQ", true).GetValue<bool>())
                {
                    var farmPos = Q.GetCircularFarmLocation(allMinions, Q.Width);
                    if (farmPos.MinionsHit >= Config.Item("LCminions", true).GetValue<Slider>().Value)
                        Q.Cast(farmPos.Position);
                }
            }
        }

        private void LogicW()
        {
            if (Program.Combo && Player.Mana > RMANA + WMANA)
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(W.Range) && W.GetPrediction(t).CastPosition.Distance(t.Position) > 100)
                {
                    if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                    {
                        if (t.Position.Distance(Player.ServerPosition) < t.Position.Distance(Player.Position))
                            Program.CastSpell(W, t);
                    }
                    else
                    {
                        if (t.Position.Distance(Player.ServerPosition) > t.Position.Distance(Player.Position))
                            Program.CastSpell(W, t);
                    }
                }
            }
        }

        private void LogicE()
        {
            if (!Config.Item("autoE", true).GetValue<bool>() || Program.None)
                return;

            if ( Player.HasBuff("KarthusDefile"))
            {
                if (Player.ManaPercent < Config.Item("Emana", true).GetValue<Slider>().Value)
                    E.Cast();
                if (Player.CountEnemiesInRange(E.Range) == 0)
                    E.Cast();
            }
            else 
            {
                if (Player.ManaPercent > Config.Item("Emana", true).GetValue<Slider>().Value && Player.CountEnemiesInRange(E.Range) > 0)
                {
                    E.Cast();
                }
            }
        }

        private void Jungle()
        {
            if (Program.LaneClear && Player.Mana > RMANA + WMANA + RMANA + WMANA)
            {
                var mobs = MinionManager.GetMinions(Player.ServerPosition, 700, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (Q.IsReady() && Config.Item("jungleQ", true).GetValue<bool>())
                    {
                        Q.Cast(mob.ServerPosition);
                        return;
                    }
                    if (E.IsReady() && Config.Item("jungleE", true).GetValue<bool>())
                    {
                        E.Cast(mob.ServerPosition);
                        return;
                    }
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
    }
}
