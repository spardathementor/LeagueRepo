using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
namespace OneKeyToWin_AIO_Sebby.Core
{
    class OKTWfarmLogic
    {
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;

        public Obj_AI_Base minionAgro = null;

        public float minionTime = 0;
        public void LoadOKTW()
        {
            Game.OnUpdate +=Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Type == GameObjectType.obj_AI_Turret && sender.IsAlly && Player.Distance(sender.Position) < 1000)
            {
                minionAgro = (Obj_AI_Base)args.Target;
                minionTime = Game.Time;
            }
        }

        private bool MinionOK(Obj_AI_Base minion , Obj_AI_Turret turret)
        {
            
            var turrentDmg = turret.GetAutoAttackDamage(minion);
            
            var hits = (int)(minion.Health / turrentDmg);

            var playerDmg = Player.GetAutoAttackDamage(minion);
            var minionHel = HealthPrediction.LaneClearHealthPrediction(minion, 400);
            
            var hpAfter = minionHel % turrentDmg;

            if ((hpAfter > playerDmg) && hpAfter > 5 && (hits>0 || minionAgro!=minion))
            {
                //Program.debug(" minion HP " + (int)minionHel + " turretDmg " + (int)turrentDmg);
               // Program.debug("HPAfter " + hpAfter + " MyDamage " + (int)Player.GetAutoAttackDamage(minion) + " HITS " + (int)hits + " tur " + turrentDmg);
                Orbwalker.ForceTarget(minion);
                Orbwalking.Attack = true;
                return false;
            }
            else
            {
                //Program.debug("else HPAfter " + hpAfter + " MyDamage " + (int)Player.GetAutoAttackDamage(minion) + " HITS " + (int)hits + " tur " + turrentDmg);
                return true;
            } 
        }

        private void Game_OnUpdate(EventArgs args)
        {
            //Program.debug("dmg " + Player.AttackSpeedMod);
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                Orbwalking.Attack = true;
                return;
            }

            foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(turret => turret.IsAlly && Player.Distance(turret.Position)<1000))
            {
                //Program.debug(" turretDmgQQQQQ " + (int)turret.FlatPhysicalDamageMod);
                var minions = MinionManager.GetMinions(turret.Position,900, MinionTypes.All);

                if (minionAgro.IsValidTarget() && Orbwalking.InAutoAttackRange(minionAgro) && Player.GetAutoAttackDamage(minionAgro) > HealthPrediction.GetHealthPrediction(minionAgro, 70))
                {
                    Orbwalker.ForceTarget(minionAgro);
                    Orbwalking.Attack = true;
                    //Program.debug(" Force AGRO ");
                    return;
                }
                
                foreach (var minion in minions.Where(minion => minion.IsValidTarget() && minion.UnderTurret(false) && Orbwalking.InAutoAttackRange(minion)))
                {
                    if (Player.GetAutoAttackDamage(minion) > HealthPrediction.LaneClearHealthPrediction(minion, 50))
                    {
                        Orbwalker.ForceTarget(minion);
                        Orbwalking.Attack = true;
                        //Program.debug(" Force Minion ");
                        return;
                    }
                    Orbwalking.Attack = false;
                }

                var minions2 = minions.OrderBy(minion => turret.Distance(minion.Position));
                int count = 0;

                if ( (Game.Time - minionTime > 0.8))
                {
                    foreach (var minion in minions.Where(minion => minion.IsValidTarget() && minion.UnderTurret(false) && Orbwalking.InAutoAttackRange(minion)))
                    {
                        if (minionAgro.IsValidTarget() && Orbwalking.InAutoAttackRange(minionAgro) )
                        {
                            if(MinionOK(minionAgro, turret))
                                count++;
                        }
                        if (MinionOK(minion, turret))
                        {
                            count++;
                        }
                        else
                            return;

                        if (count > 2)
                        {
                            //Program.debug("2 minion OK");
                            return;
                        }
                    }
                }
                return; 
            }
            Orbwalking.Attack = true;
        }
    }
}
