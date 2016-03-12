using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SebbyLib
{
    public class Cache
    {
        public static List<Obj_AI_Base> MinionsListEnemy = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListAlly= new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListNeutral = new List<Obj_AI_Base>();

        static Cache()
        {
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValid))
                AddMinionObject(minion);
            
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion != null)
            {
                AddMinionObject(minion);
            }
        }

        private static void AddMinionObject(Obj_AI_Minion minion)
        {
            if (minion.MaxHealth >= 225)
            {
                if (minion.Team == GameObjectTeam.Neutral)
                {
                    MinionsListNeutral.Add(minion);
                }
                else if (minion.MaxMana == 0 && minion.MaxHealth >= 300)
                {
                    if (minion.Team != ObjectManager.Player.Team)
                        MinionsListEnemy.Add(minion);
                    else if (minion.Team == ObjectManager.Player.Team)
                        MinionsListAlly.Add(minion);
                }
            }
        }

        public static List<Obj_AI_Base> GetMinions(Vector3 from, float range, MinionTeam team = MinionTeam.Enemy)
        {
            if (team == MinionTeam.Enemy)
            {
                MinionsListEnemy.RemoveAll(minion => minion == null || !minion.IsValid || !minion.IsTargetable || minion.IsDead || minion.IsInvulnerable  || minion.Health < 1);
                return MinionsListEnemy.FindAll(minion => minion.IsVisible && from.Distance(minion.Position) < range );
            }
            else if (team == MinionTeam.Ally)
            {
                MinionsListAlly.RemoveAll(minion => minion == null || !minion.IsValid || !minion.IsTargetable || minion.IsDead || minion.IsInvulnerable || minion.Health < 1);
                return MinionsListAlly.FindAll(minion => minion.IsVisible && from.Distance(minion.Position) < range);
            }
            else
            {
                MinionsListNeutral.RemoveAll(minion => minion == null || !minion.IsValid || !minion.IsTargetable || minion.IsDead || minion.IsInvulnerable || minion.Health < 1);
                return MinionsListNeutral.Where(minion => minion.IsVisible && from.Distance(minion.Position) < range).OrderBy(minion => minion.MaxHealth).ToList();
            }
        }
    }
}
