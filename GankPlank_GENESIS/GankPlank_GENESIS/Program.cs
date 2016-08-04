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

            E.SetSkillshot(0.7f, 300f, 2500, false, SkillshotType.SkillshotCircle);

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


            if (eAmmo > 1 && barrelCount == 0 && E.IsReady())
            {
                var t = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
                if(t.IsValidTarget())
                {
                    var healthDecayRate = Player.Level >= 13 ? 0.5f : (Player.Level >= 7 ? 1f : 2f);
                    var predPos = SebbyLib.Prediction.Prediction.GetPrediction(t, healthDecayRate * 2 - 0.5f);

                    var ePos = Player.Position.Extend(predPos.CastPosition, 500);
                    if (predPos.CastPosition.Distance(Player.Position) < Q.Range)
                        ePos = predPos.CastPosition;

                    if (!OktwCommon.CirclePoints(8, 450, ePos).Any(x => x.IsWall()))
                        E.Cast(ePos);
                }
            }

            foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.IsValidTarget(1500)))
            {
                var eRadius = 290 + enemy.BoundingRadius;
                var pred = E.GetPrediction(enemy, true);
                Utility.DrawCircle(pred.CastPosition, 100, System.Drawing.Color.Yellow, 1, 1);
                foreach (var barrel in BarrelList)
                {
                    if (barrel.Distance(pred.CastPosition) < eRadius && barrel.Distance(pred.UnitPosition) < eRadius)
                    {
                        //continue;
                        if (barrelInAaRange != null)
                        {
                            if(barrelInAaRange == barrel)
                                Orbwalker.ForceTarget(barrelInAaRange);
                            else if (barrelInAaRange.HasBuff("gangplankebarrellink") && barrel.HasBuff("gangplankebarrellink"))
                                Orbwalker.ForceTarget(barrelInAaRange);
                        }
                        else if (Q.IsReady() && barrelInQRange != null)
                        {
                            if(barrelInQRange == barrel )
                                Q.CastOnUnit(barrelInQRange);   
                            else if (barrelInQRange.HasBuff("gangplankebarrellink") && barrel.HasBuff("gangplankebarrellink"))
                                Q.CastOnUnit(barrelInQRange);
                        }
                        break;
                    }
                    else if (E.IsReady())
                    {
                        var tryPosition = barrel.Position.Extend(pred.CastPosition, 670);

                        if (tryPosition.Distance(Player.ServerPosition) < E.Range)
                        {
                            if (tryPosition.Distance(pred.CastPosition) < eRadius && tryPosition.Distance(pred.UnitPosition) < eRadius)
                            {
                                if (barrelInAaRange != null)
                                {
                                    if (barrelInAaRange == barrel)
                                    {
                                        E.Cast(tryPosition);
                                        Orbwalker.ForceTarget(barrelInAaRange);
                                    }
                                    else if (barrelInAaRange.HasBuff("gangplankebarrellink") && barrel.HasBuff("gangplankebarrellink"))
                                    {
                                        E.Cast(tryPosition);
                                        Orbwalker.ForceTarget(barrelInAaRange);
                                    }
                                }
                                else if (Q.IsReady() && barrelInQRange != null)
                                {
                                    if (barrelInQRange == barrel)
                                    {
                                        E.Cast(tryPosition);
                                        Q.CastOnUnit(barrelInQRange);
                                    }
                                    else if (barrelInQRange.HasBuff("gangplankebarrellink") && barrel.HasBuff("gangplankebarrellink"))
                                    {
                                        E.Cast(tryPosition);
                                        Q.CastOnUnit(barrelInQRange);
                                    } 
                                }
                                break;
                            }
                        }
                    }
                }
            }
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
