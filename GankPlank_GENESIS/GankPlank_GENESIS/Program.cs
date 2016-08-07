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

            E.SetSkillshot(0.7f, 300f, 2000, false, SkillshotType.SkillshotCircle);


            Config = new Menu("Gankplan GENESIS", "Gankplan GENESIS", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new SebbyLib.Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
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
            Obj_GeneralParticleEmitter d;
            if(Player.Distance(sender.Position) < 400)
                Console.WriteLine(sender.Name + " " + sender.Type );
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
            BarrelList.RemoveAll(x => !x.IsValid || x.Health < 1);
            var eAmmo = Player.Spellbook.GetSpell(SpellSlot.E).Ammo;
            var barrelCount = BarrelList.Count();
            var barrelInAaRange = BarrelList.FirstOrDefault(x => SebbyLib.Orbwalking.InAutoAttackRange(x) && CanAa(x));
            var barrelInQRange = BarrelList.FirstOrDefault(x => x.IsValidTarget(Q.Range) && CanQ(x));
            var barrelFullHp = BarrelList.FirstOrDefault(x => SebbyLib.Orbwalking.InAutoAttackRange(x) && !CanQ(x));

            if (barrelFullHp != null)
                Orbwalker.ForceTarget(barrelFullHp);
            else
                Orbwalker.ForceTarget(null);


            if (eAmmo > 1 && Orbwalker.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Combo && E.IsReady())
            {
                var t = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if(t.IsValidTarget())
                {
                    var ePos = Player.ServerPosition.Extend(Game.CursorPos, 250);

                    if (ePos.Distance(Player.ServerPosition) < E.Range && !OktwCommon.CirclePoints(8, 250, ePos).Any(x => x.IsWall()) && !BarrelList.Exists(x => x.Distance(ePos) < 600) )
                        E.Cast(ePos);
                }
            }

            foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(1700)))
            {
                var eRadius = 290 + enemy.BoundingRadius;
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
                    foreach (var barrel in BarrelList)
                    {
                        var tryPosition = barrel.Position.Extend(pred.CastPosition, 670);

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
                                    E.Cast(tryPosition);
                                   // Q.CastOnUnit(barrelInQRange);
                                }
                            }
                            break;
                            
                        }
                        else if (tryPosition.Distance(Player.ServerPosition) < E.Range && eAmmo > 1)
                        {
                            E.Cast(tryPosition);
                        }
                    }
                }
                
            }
        }

        private static bool BarrelLink(Obj_AI_Minion barrel, Obj_AI_Minion barrel2)
        {
            if (barrel.HasBuff("gangplankebarrellink") && barrel2.HasBuff("gangplankebarrellink"))
                return true;
            else
                return false;
        }

        private static bool BarrelHitTarget(Obj_AI_Minion barrel, Obj_AI_Base target)
        {
            var eRadius = 280 + target.BoundingRadius;
            if (barrel.Distance(target.ServerPosition) > eRadius)
                return false;

            float t = 0.1f + Player.Distance(target) / 2600;
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
