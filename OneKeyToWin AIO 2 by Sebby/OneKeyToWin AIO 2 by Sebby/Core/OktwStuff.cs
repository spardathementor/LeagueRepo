using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.SDK;
using SebbyLib;
using SharpDX;

namespace OneKeyToWin_AIO_2_by_Sebby.Core
{
    class OktwStuff : Program
    {
        public static bool IsSpellHeroCollision(Obj_AI_Hero t, Spell QWER, int extraWith = 50)
        {
            foreach (var hero in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(QWER.Range + QWER.Width, true, QWER.RangeCheckFrom) && t.NetworkId != hero.NetworkId))
            {
                var prediction = QWER.GetPrediction(hero);
                var powCalc = Math.Pow((QWER.Width + extraWith + hero.BoundingRadius), 2);
                if (prediction.UnitPosition.ToVector2().DistanceSquared(QWER.From.ToVector2(), QWER.GetPrediction(t).CastPosition.ToVector2(),  true) <= powCalc)
                {
                    return true;
                }
                else if (prediction.UnitPosition.ToVector2().Distance(QWER.From.ToVector2(), t.ServerPosition.ToVector2(), true) <= powCalc)
                {
                    return true;
                }

            }
            return false;
        }

        public static bool InAutoAttackRange(AttackableUnit target)
        {


            var myRange = Player.AttackRange + Player.BoundingRadius + target.BoundingRadius;

            return
                Vector2.DistanceSquared(
                    target is Obj_AI_Base ? ((Obj_AI_Base)target).ServerPosition.ToVector2() : target.Position.ToVector2(),
                    Player.ServerPosition.ToVector2()) <= myRange * myRange;
        }

        public static bool CanHarras()
        {
            if (!ObjectManager.Player.IsWindingUp && !ObjectManager.Player.IsUnderEnemyTurret() && Orbwalker.CanMove(50,false) && !ShouldWait())
                return true;
            else
                return false;
        }

        public static bool ShouldWait()
        {
            var attackCalc = (int)(ObjectManager.Player.AttackDelay * 1000 * 2);
            return
                GameObjects.EnemyMinions.Any( minion => minion.IsValidTarget() && HealthPrediction.LaneClearHealthPrediction(minion, attackCalc, 30) <= Player.GetAutoAttackDamage(minion));
        }

        public static float GetKsDamage(Obj_AI_Hero t, Spell QWER)
        {
            var totalDmg = QWER.GetDamage(t);
            totalDmg -= t.HPRegenRate;

            if (totalDmg > t.Health)
            {
                if (ObjectManager.Player.HasBuff("summonerexhaust"))
                    totalDmg = totalDmg * 0.6f;

                if (t.HasBuff("ferocioushowl"))
                    totalDmg = totalDmg * 0.7f;

                if (t.ChampionName == "Blitzcrank" && !t.HasBuff("BlitzcrankManaBarrierCD") && !t.HasBuff("ManaBarrier"))
                {
                    totalDmg -= t.Mana / 2f;
                }
            }
            //if (Thunderlord && !Player.HasBuff( "masterylordsdecreecooldown"))
            //totalDmg += (float)Player.CalcDamage(t, Damage.DamageType.Magical, 10 * Player.Level + 0.1 * Player.FlatMagicDamageMod + 0.3 * Player.FlatPhysicalDamageMod);
            totalDmg += (float)OktwCommon.GetIncomingDamage(t);
            return totalDmg;
        }
    }
}
