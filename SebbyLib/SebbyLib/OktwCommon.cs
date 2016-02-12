using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SebbyLib
{
    public class OktwCommon
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static int LastAATick = Utils.GameTimeTickCount;
        public static bool YasuoInGame = false;
        public static bool Thunderlord = false;

        public static bool
            blockMove = false,
            blockAttack = false,
            blockSpells = false;

        private static List<UnitIncomingDamage> IncomingDamageList = new List<UnitIncomingDamage>();
        private static List<Obj_AI_Hero> ChampionList = new List<Obj_AI_Hero>();

        public static void Load()
        {
            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;
            Game.OnWndProc += Game_OnWndProc;
        }

        public static double GetIncomingDamage(Obj_AI_Hero target, float time = 0.5f, bool skillshots = true)
        {
            double totalDamage = 0;

            foreach (var damage in IncomingDamageList.Where(damage => damage.TargetNetworkId == target.NetworkId && Game.Time - time < damage.Time))
            {
                if (skillshots)
                {
                    totalDamage += damage.Damage;
                }
                else
                {
                    if (!damage.Skillshot)
                        totalDamage += damage.Damage;
                }
            }

            return totalDamage;
        }

        public static bool CanHarras()
        {
            if (!Player.IsWindingUp && !Player.UnderTurret(true) && Orbwalking.CanMove(50) && !ShouldWait())
                return true;
            else
                return false;
        }

        public static float GetEchoLudenDamage(Obj_AI_Hero target)
        {
            float totalDamage = 0;

            if (Player.HasBuff("itemmagicshankcharge"))
            {
                if (Player.GetBuff("itemmagicshankcharge").Count == 100)
                {
                    totalDamage += (float)Player.CalcDamage(target, Damage.DamageType.Magical, 100 + 0.1 * Player.FlatMagicDamageMod);
                }
            }
            return totalDamage;
        }

        private static bool ShouldWait()
        {
            return
                MinionManager.GetMinions(Player.AttackRange + 300, MinionTypes.All, MinionTeam.Enemy).Any(minion =>
                           minion.IsValidTarget() && HealthPrediction.LaneClearHealthPrediction(minion, 300, 50) <= Player.GetAutoAttackDamage(minion));
        }

        public static bool IsSpellHeroCollision(Obj_AI_Hero t, Spell QWER, int extraWith = 50)
        {
            foreach (var hero in HeroManager.Enemies.FindAll(hero => hero.IsValidTarget(QWER.Range + QWER.Width, true, QWER.RangeCheckFrom) && t.NetworkId != hero.NetworkId))
            {
                var prediction = QWER.GetPrediction(hero);
                var powCalc = Math.Pow((QWER.Width + extraWith + hero.BoundingRadius), 2);
                if (prediction.UnitPosition.To2D().Distance(QWER.From.To2D(), QWER.GetPrediction(t).CastPosition.To2D(), true, true) <= powCalc)
                {
                    return true;
                }
                else if (prediction.UnitPosition.To2D().Distance(QWER.From.To2D(), t.ServerPosition.To2D(), true, true) <= powCalc)
                {
                    return true;
                }

            }
            return false;
        }

        public static float GetKsDamage(Obj_AI_Hero t, Spell QWER)
        {
            var totalDmg = QWER.GetDamage(t);
            totalDmg -= t.HPRegenRate;

            if (totalDmg > t.Health)
            {
                if (Player.HasBuff("summonerexhaust"))
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
            totalDmg += (float)GetIncomingDamage(t);
            return totalDmg;
        }
        public static bool ValidUlt(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.PhysicalImmunity)
                || target.HasBuffOfType(BuffType.SpellImmunity)
                || target.IsZombie
                || target.IsInvulnerable
                || target.HasBuffOfType(BuffType.Invulnerability)
                || target.HasBuffOfType(BuffType.SpellShield)
                || target.Health - GetIncomingDamage(target) < 1
                )
                return false;
            else
                return true;
        }

        public static bool CanMove(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) ||
                target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Knockback) ||
                target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || target.IsStunned || 
                (target.IsChannelingImportantSpell() && !target.IsMoving) || target.MoveSpeed < 50 || Prediction.GetPrediction(target, 0.5f).Hitchance > HitChance.Dashing)
            {
                return false;
            }
            else
                return true;
        }

        public static int GetBuffCount(Obj_AI_Base target, String buffName)
        {
            foreach (var buff in target.Buffs.Where(buff => buff.Name == buffName))
            {
                if (buff.Count == 0)
                    return 1;
                else
                    return buff.Count;
            }
            return 0;
        }

        public static int CountEnemyMinions(Obj_AI_Base target, float range)
        {
            var allMinions = MinionManager.GetMinions(target.Position, range);
            if (allMinions != null)
                return allMinions.Count;
            else
                return 0;
        }

        public static float GetPassiveTime(Obj_AI_Base target, String buffName)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Name == buffName)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {

        }

        private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.Target != null)
            {
                if (args.Target.Type == GameObjectType.obj_AI_Hero && !sender.IsMelee && args.Target.Team != sender.Team)
                {
                    IncomingDamageList.Add(new UnitIncomingDamage { Damage = sender.GetSpellDamage((Obj_AI_Base)args.Target, args.SData.Name), TargetNetworkId = args.Target.NetworkId, Time = Game.Time, Skillshot = false });
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            float time = Game.Time - 2;
            IncomingDamageList.RemoveAll(damage => time < damage.Time);
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            /////////////////  HP prediction
            if (args.Target != null)
            {
                if (args.Target.Type == GameObjectType.obj_AI_Hero && args.Target.Team != sender.Team && sender.IsMelee)
                {

                    IncomingDamageList.Add(new UnitIncomingDamage { Damage = sender.GetSpellDamage((Obj_AI_Base)args.Target, args.SData.Name), TargetNetworkId = args.Target.NetworkId, Time = Game.Time, Skillshot = false });
                }
            }
            else
            {
                foreach (var champion in ChampionList.Where(champion => champion.IsValid && !champion.IsDead && champion.Team != sender.Team && champion.Distance(sender) < 2000))
                {
                    var castArea = champion.Distance(args.End) * (args.End - champion.ServerPosition).Normalized() + champion.ServerPosition;
                    if (castArea.Distance(champion.ServerPosition) < champion.BoundingRadius / 2)
                        IncomingDamageList.Add(new UnitIncomingDamage { Damage = sender.GetSpellDamage(champion, args.SData.Name), TargetNetworkId = champion.NetworkId, Time = Game.Time, Skillshot = true });
                }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (blockSpells)
            {
                args.Process = false;
            }
        }

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (blockMove && !args.IsAttackMove)
            {
                args.Process = false;
            }
            if (blockAttack && args.IsAttackMove)
            {
                args.Process = false;
            }
        }
    }

    class UnitIncomingDamage
    {
        public int TargetNetworkId { get; set; }
        public float Time { get; set; }
        public double Damage { get; set; }
        public bool Skillshot { get; set; }
    }
}
