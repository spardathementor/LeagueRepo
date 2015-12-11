using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby
{
    class Vayne
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private Spell E, Q, R, W;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 300);
            E = new Spell(SpellSlot.E, 670);
            W = new Spell(SpellSlot.E, 670);
            R = new Spell(SpellSlot.R, 3000);

            E.SetTargetted(0.25f, 2200f);

            LoadMenuOKTW();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Interrupter2.OnInterruptableTarget +=Interrupter2_OnInterruptableTarget;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range))
                E.Cast(sender);
        }
        private void LoadMenuOKTW()
        {
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw only ready spells", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange2", "E push position", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("farmQ", "Q farm helper", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("QE", "try Q + E ", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q config").AddItem(new MenuItem("Qonly", "Q only after AA", true).SetValue(false));

            Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").AddItem(new MenuItem("gapQ", "Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").AddItem(new MenuItem("gapE", "E", true).SetValue(true));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("GapCloser").SubMenu("Use on").AddItem(new MenuItem("gap" + enemy.ChampionName, enemy.ChampionName).SetValue(true));


            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu(Player.ChampionName).SubMenu("E config").SubMenu("Use E ").AddItem(new MenuItem("stun" + enemy.ChampionName, enemy.ChampionName).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("useE", "OneKeyToCast E closest person", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))); //32 == space

            Config.SubMenu(Player.ChampionName).SubMenu("E config").AddItem(new MenuItem("Eks", "E KS", true).SetValue(true));


            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("visibleR", "Unvisable block AA ", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R config").AddItem(new MenuItem("autoQR", "Auto Q when R active ", true).SetValue(true));
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var target = gapcloser.Sender;

            if (!target.IsValidTarget(E.Range) && Config.Item("gap" + target.ChampionName).GetValue<bool>())
                return;
            if (E.IsReady() && Config.Item("gapE", true).GetValue<bool>() )
                E.Cast(target);
            if (Q.IsReady() && (!E.IsReady() || !Config.Item("gapE", true).GetValue<bool>()) && DashCheck(Player.Position.Extend(Game.CursorPos, Q.Range)) && Config.Item("gapQ", true).GetValue<bool>() )
                Q.Cast();
            return;
        }

        private void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("visibleR", true).GetValue<bool>() && Player.HasBuff("vaynetumblefade") && Player.CountEnemiesInRange(800) > 1)
                args.Process = false;

            foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(800) && GetWStacks(target) >= 1))
            {
                if (Orbwalking.InAutoAttackRange(target) && args.Target.Health > 3 * Player.GetAutoAttackDamage(target))
                    Orbwalker.ForceTarget(target);
            }
        }

        private void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!Q.IsReady())
                return;
            var dashPosition = Player.Position.Extend(Game.CursorPos, Q.Range);
            if (!DashCheck(dashPosition))
                return;

            var t = target as Obj_AI_Hero;

            if (t.IsValidTarget() && dashPosition.Distance(t.Position) < 500 && (Program.Combo || Program.Farm) && Config.Item("autoQ", true).GetValue<bool>() &&  (GetWStacks(t) == 1 || Player.HasBuff("vayneinquisition")))
            {
                Q.Cast(dashPosition, true);
                Program.debug("" + t.Name + GetWStacks(t));
            }
            else if (Program.Farm && Config.Item("farmQ", true).GetValue<bool>())
            {
                var minions = MinionManager.GetMinions(dashPosition, Player.AttackRange, MinionTypes.All);
                
                if (minions == null || minions.Count == 0)
                    return;
                
                int countMinions = 0;
                
                foreach (var minion in minions.Where(minion => minion.Health < Player.GetAutoAttackDamage(minion) + Q.GetDamage(minion)))
                {
                    countMinions++;
                }

                if (countMinions > 1)
                    Q.Cast(dashPosition, true);
            }
        }

        private float Wdmg(Obj_AI_Base target)
        {
            var dmg = (W.Level * 10 + 10) + ((0.03 + (W.Level * 0.01)) * target.MaxHealth );
            return (float)dmg;

        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            var dashPosition = Player.Position.Extend(Game.CursorPos, Q.Range);

            if (E.IsReady())
            {
                var ksTarget = Player;
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range) && target.Path.Count() < 2 ))
                {
                    if (CondemnCheck(Player.ServerPosition, target) && Config.Item("stun" + target.ChampionName).GetValue<bool>() )
                        E.Cast(target);
                    else if (Q.IsReady() && DashCheck(dashPosition) && Config.Item("QE", true).GetValue<bool>() && CondemnCheck(dashPosition, target))
                    {
                        Q.Cast(dashPosition);
                        Program.debug("Q + E");
                    }

                    if (Config.Item("Eks", true).GetValue<bool>())
                    {
                        var dmgE = E.GetDamage(target);
                        if (dmgE > target.Health || (GetWStacks(target) == 2 && dmgE + Wdmg(target) > target.Health))
                        {
                            ksTarget = target;
                            
                        }  
                    } 
                }

                if(ksTarget != Player)
                    E.Cast(ksTarget);
            }

            if (Program.LagFree(1) && Q.IsReady() && DashCheck(dashPosition))
            {
                if (Config.Item("autoQR", true).GetValue<bool>() && Player.HasBuff("vayneinquisition")  && Player.CountEnemiesInRange(1500) > 0 && Player.CountEnemiesInRange(670) != 1)
                {
                    Q.Cast(dashPosition, true);
                }
                if (Program.Combo && Config.Item("autoQ", true).GetValue<bool>() && !Config.Item("Qonly", true).GetValue<bool>())
                {
                    var t = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);

                    if (t.IsValidTarget() && !Orbwalking.InAutoAttackRange(t) && t.Position.Distance(Game.CursorPos) < t.Position.Distance(Player.Position) && dashPosition.CountEnemiesInRange(800) < 3 && !OktwCommon.IsFaced(t))
                    {
                        Q.Cast(dashPosition);
                    }
                }
            }

            if (Program.LagFree(2))
            {
                Obj_AI_Hero bestEnemy = null;
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(E.Range)))
                {
                    if (target.IsValidTarget(270) && target.IsMelee)
                    {
                        if (Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>() && DashCheck(dashPosition))
                            Q.Cast(dashPosition, true);
                        else if (E.IsReady() && Player.Health < Player.MaxHealth * 0.5)
                        {
                            E.Cast(target);
                            Program.debug("push");
                        }
                    }
                    if (bestEnemy == null)
                        bestEnemy = target;
                    else if (Player.Distance(target.Position) < Player.Distance(bestEnemy.Position))
                        bestEnemy = target;
                }
                if (Config.Item("useE", true).GetValue<KeyBind>().Active && bestEnemy != null)
                {
                    E.Cast(bestEnemy);
                }
            }

            if (Program.LagFree(3) && R.IsReady() )
            {
                if ( Config.Item("autoR", true).GetValue<bool>())
                {
                    if (Player.CountEnemiesInRange(700) > 2)
                        R.Cast();
                    else if (Program.Combo && Player.CountEnemiesInRange(600) > 1)
                        R.Cast();
                    else if (Player.Health < Player.MaxHealth * 0.5 && Player.CountEnemiesInRange(500) > 0)
                        R.Cast();
                }
            }
        }

        private bool DashCheck(Vector3 dash)
        {
            if (
                !Player.Position.Extend(dash, Q.Range).IsWall()
                && !Player.Position.Extend(dash, Q.Range - 100).IsWall()
                && !Player.Position.Extend(dash, Q.Range - 200).IsWall()
                && !Player.Position.Extend(dash, Q.Range - 300).IsWall()
                && dash.CountEnemiesInRange(800) < 3 && dash.CountEnemiesInRange(600) < 2 && dash.CountEnemiesInRange(300) < 1
                && (!dash.UnderTurret(true) || Program.Combo))
                return true;
            else
                return false;
        }

        private bool CondemnCheck(Vector3 fromPosition, Obj_AI_Hero target)
        {
            var prepos = E.GetPrediction(target);

            float pushDistance = 510;

            if (Player.Position != fromPosition)
                pushDistance = 400 ;

            int radius = 100;
            var start2 = target.ServerPosition;
            var end2 = prepos.CastPosition.Extend(fromPosition, -pushDistance);

            Vector2 start = start2.To2D();
            Vector2 end = end2.To2D();
            var dir = (end - start).Normalized();
            var pDir = dir.Perpendicular();

            var rightEndPos = end + pDir * radius;
            var leftEndPos = end - pDir * radius;


            var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, ObjectManager.Player.Position.Z);
            var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, ObjectManager.Player.Position.Z);


            var step = start2.Distance(rEndPos) / 10;
            for (var i = 0; i < 10; i++)
            {
                var pr = start2.Extend(rEndPos, step * i);
                var pl = start2.Extend(lEndPos, step * i);
                if (pr.IsWall() && pl.IsWall())
                    return true;
            }

            return false;
        }

        private int GetWStacks(Obj_AI_Base target)
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Name == "vaynesilvereddebuff")
                    return buff.Count;
            }
            return -1;
        }

        private List<Vector3> CirclePoint(float CircleLineSegmentN, float radius, Vector3 position)
        {
            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                points.Add(point);
            }
            return points;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range + E.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range + E.Range, System.Drawing.Color.Cyan, 1, 1);
            }

            if (E.IsReady() && Config.Item("eRange2", true).GetValue<bool>())
            {
                foreach (var target in Program.Enemies.Where(target => target.IsValidTarget(800)))
                {
                    var poutput = E.GetPrediction(target);

                    var pushDistance = 460;

                    var finalPosition = poutput.CastPosition.Extend(Player.ServerPosition, -pushDistance);
                    if (finalPosition.IsWall())
                        Render.Circle.DrawCircle(finalPosition, 100, System.Drawing.Color.Red);
                    else
                        Render.Circle.DrawCircle(finalPosition, 100, System.Drawing.Color.YellowGreen);
                }
            }
        }
    }
}
