using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Jinx_Genesis
{
    class Program
    {
        private static string ChampionName = "Jinx";

        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Spell Q, W, E, R;
        private static float QMANA, WMANA, EMANA ,RMANA;
        private static bool FishBoneActive= false, Combo = false, Farm = false;

        private static List<Obj_AI_Hero> Enemies = new List<Obj_AI_Hero>();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName) return;
            LoadMenu();
            Q = new Spell(SpellSlot.Q, Player.AttackRange);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 75f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    Enemies.Add(hero);
                }
            }

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Game.PrintChat("<font color=\"#ff00d8\">GENESIS</font>Jinx<font color=\"#000000\">by Sebby</font> - <font color=\"#00BFFF\">Loaded</font>");
        }

        private static void LoadMenu()
        {
            Config = new Menu(ChampionName + " GENESIS", ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.AddToMainMenu();
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qcombo", "Combo Q").SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qharass", "Harass Q").SetValue(true));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qchange", "Q change mode FishBone -> MiniGun").SetValue(new StringList(new[] { "Real Time", "Before AA"}, 0)));
            Config.SubMenu("Q Config").AddItem(new MenuItem("Qaoe", "Force FishBone if can hit x target").SetValue(new Slider(3, 5, 0)));
            Config.SubMenu("Q Config").AddItem(new MenuItem("QmanaIgnore", "Ignore mana if can kill in x AA").SetValue(new Slider(2, 10, 0)));

            Config.SubMenu("W Config").AddItem(new MenuItem("Wcombo", "Combo W").SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wharass", "Harass W").SetValue(true));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wts", "Harass mode").SetValue(new StringList(new[] { "Target selector", "All in range" }, 0)));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wmode", "W mode").SetValue(new StringList(new[] { "Out range MiniGun", "Out range FishBone", "Custome range" }, 0)));
            Config.SubMenu("W Config").AddItem(new MenuItem("Wcustome", "Custome range").SetValue(new Slider(600, 1500, 0)));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu("W Config").SubMenu("Harass enemy:").AddItem(new MenuItem("haras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("LaneClearmode", "LaneClear").SetValue(true));
            Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("Mixedmode", "Mixed").SetValue(true));
            Config.SubMenu("MISC").SubMenu("Use harass mode").AddItem(new MenuItem("LastHitmode", "LastHit").SetValue(true));

            Config.SubMenu("Mana Manager").AddItem(new MenuItem("QmanaCombo", "Q combo mana").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("QmanaHarass", "Q harass mana").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("WmanaCombo", "W combo mana").SetValue(new Slider(20, 100, 0)));
            Config.SubMenu("Mana Manager").AddItem(new MenuItem("WmanaHarass", "W harass mana").SetValue(new Slider(40, 100, 0)));
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!Q.IsReady() || !(args.Target is Obj_AI_Hero))
                return;

            if (Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 1)
            {
                Console.WriteLine(args.Target.Name);
                var t = (Obj_AI_Hero)args.Target;
                if (FishBoneActive && t.IsValidTarget())
                {
                    FishBoneToMiniGun(t);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            SetValues();
            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
                Config.Item("Wcustome").Show(true);
            else
                Config.Item("Wcustome").Show(false);

            if (Q.IsReady())
                Qlogic();
            if (Q.IsReady())
                Wlogic();
        }

        private static bool WValidRange(Obj_AI_Base t)
        {
            var range = GetRealDistance(t);

            if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 0)
            {
                if (range > GetRealPowPowRange(t))
                    return true;
                else
                    return false;

            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 1)
            {
                if (range > Q.Range)
                    return true;
                else
                    return false;
            }
            else if (Config.Item("Wmode").GetValue<StringList>().SelectedIndex == 2)
            {
                if(range > Config.Item("Wcustome").GetValue<Slider>().Value && !Orbwalking.InAutoAttackRange(t))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Wlogic()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (t.IsValidTarget() && WValidRange(t))
            {
                if (Combo && Config.Item("Wcombo").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaCombo").GetValue<Slider>().Value)
                {
                    CastSpell(W, t);
                }
                else if (Farm && Config.Item("Wcombo").GetValue<bool>() && Player.ManaPercent > Config.Item("WmanaHarass").GetValue<Slider>().Value)
                {
                    if (Config.Item("Wts").GetValue<StringList>().SelectedIndex == 0)
                    {
                        if (Config.Item("haras" + t.ChampionName).GetValue<bool>())
                            CastSpell(W, t);
                    }
                    else
                    {
                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && WValidRange(t) && Config.Item("haras" + enemy.ChampionName).GetValue<bool>()))
                            CastSpell(W, enemy);
                    }
                }
            }
        }

        private static void Qlogic()
        {
            if (FishBoneActive)
            {
                if(Config.Item("Qchange").GetValue<StringList>().SelectedIndex == 0 && Config.Item("Qcombo").GetValue<bool>() && Orbwalker.GetTarget() != null && Orbwalker.GetTarget() is Obj_AI_Hero)
                {
                    var t = (Obj_AI_Hero)Orbwalker.GetTarget();
                    FishBoneToMiniGun(t);
                }
                else
                {
                    if (Farm && Config.Item("Qharass").GetValue<bool>())
                        Q.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(Q.Range + 60, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if ((!Orbwalking.InAutoAttackRange(t) || t.CountEnemiesInRange(250) >= Config.Item("Qaoe").GetValue<Slider>().Value))
                    {
                        if (Combo && Config.Item("Qcombo").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                        {
                            Q.Cast();
                        }
                        if (Farm && Config.Item("Qharass").GetValue<bool>() && (Player.ManaPercent > Config.Item("QmanaHarass").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                        {
                            Q.Cast();
                        }
                    }
                }
                else
                {
                    if (Combo && Player.ManaPercent > Config.Item("QmanaCombo").GetValue<Slider>().Value)
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void CastSpell(Spell QWER, Obj_AI_Base target)
        {
            QWER.Cast(target);
        }

        private static void FishBoneToMiniGun(Obj_AI_Base t)
        {
            var realDistance = GetRealDistance(t);

            if(realDistance < GetRealPowPowRange(t) && t.CountEnemiesInRange(250) < Config.Item("Qaoe").GetValue<Slider>().Value)
            {
                if (Combo && Config.Item("Qcombo").GetValue<bool>() && (Player.ManaPercent < Config.Item("QmanaCombo").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                    Q.Cast();
                else if (Farm && Config.Item("Qharass").GetValue<bool>() && (Player.ManaPercent < Config.Item("QmanaHarass").GetValue<Slider>().Value || Player.GetAutoAttackDamage(t) * Config.Item("QmanaIgnore").GetValue<Slider>().Value < t.Health))
                    Q.Cast();
            }
        }

        private static float GetRealDistance(Obj_AI_Base target) { return Player.ServerPosition.Distance(target.ServerPosition) + Player.BoundingRadius + target.BoundingRadius; }

        private static float GetRealPowPowRange(GameObject target) { return 650f + Player.BoundingRadius + target.BoundingRadius; }

        private static void SetValues()
        {
            if (Player.AttackRange > 525f)
                FishBoneActive = true;
            else
                FishBoneActive = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo = true;
            else
                Combo = false;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Farm = true;
            else
                Farm = false;

            Q.Range = 670f + Player.BoundingRadius + 25f * Player.Spellbook.GetSpell(SpellSlot.Q).Level;

            QMANA = 10f;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;
        }
    }
}
