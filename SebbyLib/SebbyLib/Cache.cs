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
        public static List<Obj_AI_Base> AllMinionsObj = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListEnemy = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListAlly = new List<Obj_AI_Base>();
        public static List<Obj_AI_Base> MinionsListNeutral = new List<Obj_AI_Base>();

        static Cache()
        {
            foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValid))
            {
                AddMinionObject(minion);
                if (!minion.IsAlly)
                    AllMinionsObj.Add(minion);
            }

            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion != null)
            {
                RemoveMinionObject(minion);
                if (!minion.IsAlly)
                    AllMinionsObj.Remove(minion);
            }
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion != null)
            {
                AddMinionObject(minion);
                if (!minion.IsAlly)
                    AllMinionsObj.Add(minion);
            }
        }

        private static void RemoveMinionObject(Obj_AI_Minion minion)
        {
            if (minion.MaxHealth >= 225)
            {

                if (minion.Team == GameObjectTeam.Neutral)
                {
                    MinionsListNeutral.Remove(minion);
                }
                else if (minion.MaxMana == 0 && minion.MaxHealth >= 300)
                {
                    if (minion.Team == GameObjectTeam.Unknown)
                        return;
                    else if (minion.Team != ObjectManager.Player.Team)
                        MinionsListEnemy.Remove(minion);
                    else if (minion.Team == ObjectManager.Player.Team)
                        MinionsListAlly.Remove(minion);
                }
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
                    if (minion.Team == GameObjectTeam.Unknown)
                        return;
                    else if (minion.Team != ObjectManager.Player.Team)
                        MinionsListEnemy.Add(minion);
                    else if (minion.Team == ObjectManager.Player.Team)
                        MinionsListAlly.Add(minion);
                }
            }
        }

        public static List<Obj_AI_Base> GetMinions(Vector3 from, float range = float.MaxValue, MinionTeam team = MinionTeam.Enemy)
        {
            if (team == MinionTeam.Enemy)
            {
                MinionsListEnemy.RemoveAll(minion => !IsValidMinion(minion));
                return MinionsListEnemy.FindAll(minion => CanReturn(minion, from, range));
            }
            else if (team == MinionTeam.Ally)
            {
                MinionsListAlly.RemoveAll(minion => !IsValidMinion(minion));
                return MinionsListAlly.FindAll(minion => CanReturn(minion, from, range));
            }
            else
            {
                MinionsListNeutral.RemoveAll(minion => !IsValidMinion(minion));
                return MinionsListNeutral.Where(minion => CanReturn(minion, from, range)).OrderByDescending(minion => minion.MaxHealth).ToList();
            }
        }

        public static List<Obj_AI_Base> GetAllMinions(Vector3 from, float range = float.MaxValue)
        {
            AllMinionsObj.RemoveAll(minion => !IsValidMinion(minion));
            return AllMinionsObj.FindAll(minion => CanReturn(minion, from, range));
        }

        private static bool IsValidMinion(Obj_AI_Base minion)
        {
            if (minion == null || !minion.IsValid || minion.IsDead)
                return false;
            else
                return true;
        }

        private static bool CanReturn(Obj_AI_Base minion, Vector3 from, float range)
        {
            
            if (minion != null && minion.IsValid && !minion.IsDead && minion.IsVisible && minion.IsTargetable)
            {
                if (range == float.MaxValue)
                    return true;
                else if (range == 0)
                {
                    if (Orbwalking.InAutoAttackRange(minion))
                        return true;
                    else
                        return false;
                }
                else if (Vector2.DistanceSquared((@from).To2D(), minion.Position.To2D()) < range * range)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
    }
}
