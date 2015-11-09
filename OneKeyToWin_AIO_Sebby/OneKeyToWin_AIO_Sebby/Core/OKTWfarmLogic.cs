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
            Orbwalking.Attack = false;
            var turrentDmg = turret.GetAutoAttackDamage(minion) * 1.1;

            var hits = minion.Health / turrentDmg;

            var playerDmg = Player.GetAutoAttackDamage(minion) * 1.1;
            var minionHel = minion.Health * 0.9;
            
            var hpAfter = minionHel % turrentDmg;

            if (minionHel  < turrentDmg + playerDmg && minionHel > turrentDmg)
            {
                //Program.debug(" minion HP " + (int)minionHel + " turretDmg " + (int)turrentDmg);
                //Program.debug("OVER HPAfter " + hpAfter + " MyDamage " + (int)Player.GetAutoAttackDamage(minion) + " HITS " + (int)hits + " tur " + turrentDmg);
                return true;
            }
            if ((hpAfter > playerDmg || hpAfter < 5) && hits > 0 )
            {
                //Program.debug(" minion HP " + (int)minionHel + " turretDmg " + (int)turrentDmg);
                //Program.debug("HPAfter " + hpAfter + " MyDamage " + (int)Player.GetAutoAttackDamage(minion) + " HITS " + (int)hits + " tur " + turrentDmg);
                Orbwalker.ForceTarget(minion);
                Orbwalking.Attack = true;
                return false;
            }
            else
            {
               // Program.debug(" minion HP " + (int)minionHel + " turretDmg " + (int)turrentDmg);
                //Program.debug("OK HPAfter " + hpAfter + " MyDamage " + (int)Player.GetAutoAttackDamage(minion) + " HITS " + (int)hits + " tur " + turrentDmg);
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

                var minions = MinionManager.GetMinions(turret.Position,900, MinionTypes.All);

                if (minionAgro.IsValidTarget() && Orbwalking.InAutoAttackRange(minionAgro) && Player.GetAutoAttackDamage(minionAgro) > HealthPrediction.GetHealthPrediction(minionAgro, 400))
                {
                    Orbwalking.Attack = true;
                    return;
                }
                
                foreach (var minion in minions.Where(minion => minion.IsValidTarget() && minion.UnderTurret(false) && Orbwalking.InAutoAttackRange(minion)))
                {
                    if (Player.GetAutoAttackDamage(minion) > HealthPrediction.LaneClearHealthPrediction(minion, 400))
                    {
                        Orbwalking.Attack = true;
                        return;
                    }
                    Orbwalking.Attack = false;
                }

                var minions2 = minions.OrderBy(minion => turret.Distance(minion.Position));
                int count = 0;

                if ((Game.Time - minionTime > 1.1 && Game.Time - minionTime < 1.2) || (Game.Time - minionTime > 1.4))
                {
                    foreach (var minion in minions.Where(minion => minion.IsValidTarget() && minion.UnderTurret(false) && Orbwalking.InAutoAttackRange(minion)))
                    {
                        if (minionAgro.IsValidTarget() && Orbwalking.InAutoAttackRange(minionAgro) && MinionOK(minionAgro, turret))
                        {

                        }
                        else if (MinionOK(minion, turret))
                        {
                            //Program.debug("Minion OK");
                            count++;
                        }
                        else
                            return;

                        if (count > 1)
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
