using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;

namespace GankPlank_GENESIS
{
    class Program
    {
        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }

        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static SebbyLib.Orbwalking.Orbwalker Orbwalker;
        private  static Menu Config;
        private static MissileClient Fuse = null;
        private static List<Obj_AI_Minion> BarrelList = new List<Obj_AI_Minion>();
        private static Spell Q, W, E, R;
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Gangplank") { return; }

            Q = new Spell(SpellSlot.Q, 625);
            E = new Spell(SpellSlot.E, 1000);
            W = new Spell(SpellSlot.W);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0.5f, 260f, 2300, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Gankplan GENESIS", "Gankplan GENESIS", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new SebbyLib.Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Config.SubMenu("W option").SubMenu("Anti CC").AddItem(new MenuItem("CSSdelay", "Delay x ms").SetValue(new Slider(0, 1000, 0)));
            Config.SubMenu("W option").SubMenu("Anti CC").AddItem(new MenuItem("cleanHP", "Use only under % HP").SetValue(new Slider(80, 100, 0)));
            //Config.SubMenu("W option").SubMenu("Buff type").AddItem(new MenuItem("CleanSpells", "ZedR FizzR MordekaiserR PoppyR VladimirR").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Stun", "Stun").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Snare", "Snare").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Charm", "Charm").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Fear", "Fear").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Suppression", "Suppression").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Taunt", "Taunt").SetValue(true));
            Config.SubMenu("W option").SubMenu("Anti CC").SubMenu("Buff type").AddItem(new MenuItem("Blind", "Blind").SetValue(true));
            Config.SubMenu("W option").AddItem(new MenuItem("heal", "Heal under %").SetValue(new Slider(30, 100, 0)));

            Config.SubMenu("R Config").AddItem(new MenuItem("useR", "Anti escape key", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            var barrel = sender as Obj_AI_Minion;
            if (barrel != null && barrel.Name == "Barrel")
                BarrelList.Remove(barrel);
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var missil = sender as MissileClient;
            if (missil != null && missil.SData.Name.Contains("BarrelFuseMissile"))
            {
                Fuse = missil;
                Console.WriteLine(missil.SData.Name);
            }
            var barrel = sender as Obj_AI_Minion;
            if (barrel != null && barrel.Name == "Barrel")
                BarrelList.Add(barrel);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if(Fuse != null && Fuse.IsValid)
                Utility.DrawCircle(Fuse.Position, 100, System.Drawing.Color.Yellow, 1, 1);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsRecalling())
                return;

            if (R.IsReady())
                LogicR();

            if (W.IsReady())
                LogicW();
           
            BarrelList.RemoveAll(x => !x.IsValid || x.Health < 1);
            var eAmmo = Player.Spellbook.GetSpell(SpellSlot.E).Ammo;
            var barrelCount = BarrelList.Count();
            var barrelInAaRange = BarrelList.FirstOrDefault(x => SebbyLib.Orbwalking.InAutoAttackRange(x) && CanAa(x));
            var barrelInQRange = BarrelList.FirstOrDefault(x => x.IsValidTarget(Q.Range) && CanQ(x) && !SebbyLib.Orbwalking.InAutoAttackRange(x));
            var barrelFullHp = BarrelList.FirstOrDefault(x => SebbyLib.Orbwalking.InAutoAttackRange(x) && !CanQ(x));

            if (LaneClear && barrelInAaRange != null)
            {
                Orbwalker.ForceTarget(barrelInAaRange);
            }

            if (Q.IsReady() && barrelInQRange == null)
                LogicQ();

            if (barrelFullHp != null)
                Orbwalker.ForceTarget(barrelFullHp);
            else
                Orbwalker.ForceTarget(null);

            if (Combo && eAmmo > 1 && E.IsReady())
            {
                var t = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if(t.IsValidTarget())
                {
                    var ePos = Player.ServerPosition.Extend(Game.CursorPos, 250);

                    if (ePos.Distance(Player.ServerPosition) < E.Range && !OktwCommon.CirclePoints(8, 250, ePos).Any(x => x.IsWall()) && !BarrelList.Exists(x => x.Distance(ePos) < 600))
                        E.Cast(ePos);
                }
            }

            foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(1700)))
            {
                var pred = E.GetPrediction(enemy, true);
                var barrelHitTarget = BarrelList.FirstOrDefault(x => BarrelHitTarget(x, enemy));

                if (barrelHitTarget != null)
                {
                    if (barrelInAaRange != null)
                    {
                        if (barrelInAaRange == barrelHitTarget)
                            Orbwalker.ForceTarget(barrelInAaRange);
                        else if (BarrelLink(barrelInAaRange, barrelHitTarget))
                            Orbwalker.ForceTarget(barrelInAaRange);
                    }
                    else if (Q.IsReady() && barrelInQRange != null)
                    {
                        if (barrelInQRange == barrelHitTarget)
                            Q.CastOnUnit(barrelInQRange);
                        else if (BarrelLink(barrelInQRange, barrelHitTarget))
                            Q.CastOnUnit(barrelInQRange);
                    }
                    return;
                }
                else if (E.IsReady())
                {
                    // duo barrel
                    var eRadius = 250 + enemy.BoundingRadius;
                    foreach (var barrel in BarrelList)
                    {
                        var tryPosition = barrel.Position.Extend(pred.CastPosition, 640);
                        
                        if (tryPosition.Distance(Player.ServerPosition) < E.Range && tryPosition.Distance(pred.CastPosition) < eRadius && tryPosition.Distance(pred.UnitPosition) < eRadius && !BarrelList.Exists(x => x.Distance(tryPosition) < 400))
                        {
                            if (barrelInAaRange != null)
                            {
                                if (barrelInAaRange == barrel)
                                {
                                    E.Cast(tryPosition);
                                    //Orbwalker.ForceTarget(barrelInAaRange);
                                }
                                else if (BarrelLink(barrelInAaRange, barrel))
                                {
                                    E.Cast(tryPosition);
                                    //Orbwalker.ForceTarget(barrelInAaRange);
                                }
                            }
                            else if (Q.IsReady() && barrelInQRange != null)
                            {
                                if (barrelInQRange == barrel)
                                {
                                    E.Cast(tryPosition);
                                    //Q.CastOnUnit(barrelInQRange);
                                }
                                else if (BarrelLink(barrelInQRange, barrel))
                                {
                                    //Q.CastOnUnit(barrelInQRange);
                                    E.Cast(tryPosition);
                                }
                            }
                            return;
                        }
                    }
                    // triple barrel
                    if (eAmmo > 1)
                    {
                        foreach (var barrel in BarrelList)
                        {
                            var tryPosition = barrel.Position.Extend(pred.CastPosition, 670);
                            if (tryPosition.Distance(Player.ServerPosition) < E.Range)
                            {
                                E.Cast(tryPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void LogicR()
        {
            if (Config.Item("useR", true).GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                    R.Cast(Player.Position.Extend(Prediction.GetPrediction(t,0.25f).CastPosition, Player.Distance(t) + 550));
            }
        }

        private static void LogicQ()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (t.IsValidTarget())
            {
                if (Combo || Farm)
                    Q.Cast(t);

                
            }
            else if (Farm)
            {
                foreach (var minion in Cache.MinionsListEnemy.Where(x => x.IsValidTarget(Q.Range) && !SebbyLib.Orbwalking.InAutoAttackRange(x)))
                {
                    var t2 = (int)(0.25 * 1000) + Game.Ping / 2 + 1000 * (int)Math.Max(0, Player.Distance(minion) - Player.BoundingRadius) / 2600;
                    var predHp = SebbyLib.HealthPrediction.GetHealthPrediction(minion, t2);

                    if (predHp > 1 && predHp < Q.GetDamage(minion))
                        Q.CastOnUnit(minion);
                }
            }
        }

        public static void Clean()
        {
            Utility.DelayAction.Add(Config.Item("CSSdelay").GetValue<Slider>().Value, () => W.Cast());
        }

        public static void LogicW()
        {
            if (Player.Health - OktwCommon.GetIncomingDamage(Player) < Player.MaxHealth * Config.Item("heal").GetValue<Slider>().Value * 0.01)
                W.Cast();

            if (Player.HealthPercent >= Config.Item("cleanHP").GetValue<Slider>().Value)
                return;

            if (Player.HasBuffOfType(BuffType.Stun) && Config.Item("Stun").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Snare) && Config.Item("Snare").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Charm) && Config.Item("Charm").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Fear) && Config.Item("Fear").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Stun) && Config.Item("Stun").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Taunt) && Config.Item("Taunt").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Suppression) && Config.Item("Suppression").GetValue<bool>())
                Clean();
            if (Player.HasBuffOfType(BuffType.Blind) && Config.Item("Blind").GetValue<bool>())
                Clean();
        }

        public static bool LaneClear { get { return Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.LaneClear; } }

        public static bool Farm { get { return Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Freeze; } }

        public static bool None { get { return Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.None; } }

        public static bool Combo { get { return Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Combo; } }

        private static bool BarrelLink(Obj_AI_Minion barrel, Obj_AI_Minion barrel2)
        {
            if (barrel.HasBuff("gangplankebarrellink") && barrel2.HasBuff("gangplankebarrellink"))
                return true;
            else
                return false;
        }

        private static bool BarrelHitTarget(Obj_AI_Minion barrel, Obj_AI_Base target)
        {
            var eRadius = 270 + target.BoundingRadius;
            if (barrel.Distance(target.ServerPosition) > eRadius)
                return false;

            float t = 0.2f + Player.Distance(target) / 2000;
            var predPos = SebbyLib.Prediction.Prediction.GetPrediction(target, t);
            Utility.DrawCircle(predPos.CastPosition, 100, System.Drawing.Color.Yellow, 1, 1);
             
            if (barrel.Distance(predPos.CastPosition) < eRadius)
                return true;
            else
                return false;
        }

        private static bool CanAa(Obj_AI_Minion barrel)
        {
            if (barrel.Health == 1)
                return true;
            else if (barrel.Health == 3)
                return false;

            var t = (int)(Player.AttackCastDelay * 1000) + Game.Ping / 2 + 1000 * (int)Math.Max(0, Player.Distance(barrel) - Player.BoundingRadius) / float.MaxValue;

            var barrelBuff = barrel.Buffs.FirstOrDefault( b =>b.Name.Equals("gangplankebarrelactive", StringComparison.InvariantCultureIgnoreCase));

            if (barrelBuff != null && barrel.Health <= 2f)
            {
                var healthDecayRate = Player.Level >= 13 ? 0.5f : (Player.Level >= 7 ? 1f : 2f);
                var nextHealthDecayTime = Game.Time < barrelBuff.StartTime + healthDecayRate ? barrelBuff.StartTime + healthDecayRate : barrelBuff.StartTime + healthDecayRate * 2;

                if (nextHealthDecayTime <= Game.Time + t / 1000f)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        private static bool CanQ(Obj_AI_Minion barrel)
        {
            if (barrel.Health == 1)
                return true;
            else if (barrel.Health == 3)
                return false;

            var t = (int)(0.25 * 1000) + Game.Ping / 2 + 1000 * (int)Math.Max(0, Player.Distance(barrel) - Player.BoundingRadius) / 2600;

            var barrelBuff = barrel.Buffs.FirstOrDefault(b => b.Name.Equals("gangplankebarrelactive", StringComparison.InvariantCultureIgnoreCase));

            if (barrelBuff != null && barrel.Health <= 2f)
            {
                var healthDecayRate = Player.Level >= 13 ? 0.5f : (Player.Level >= 7 ? 1f : 2f);
                var nextHealthDecayTime = Game.Time < barrelBuff.StartTime + healthDecayRate ? barrelBuff.StartTime + healthDecayRate : barrelBuff.StartTime + healthDecayRate * 2;

                if (nextHealthDecayTime <= Game.Time + t / 1000f)
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
    }
}
