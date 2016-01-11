using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Velkoz
    {
        private Menu Config = Program.Config;
        public static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private Spell E, Q, R, W , QSplit, QDummy;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private static List<Obj_AI_Minion> BallsList = new List<Obj_AI_Minion>();
        private MissileClient QMissile = null;
        private List<Vector3> pointList;
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1200);
            QSplit = new Spell(SpellSlot.Q, 1100);
            QDummy = new Spell(SpellSlot.Q, (float)Math.Sqrt(Math.Pow(Q.Range, 2) + Math.Pow(QSplit.Range, 2)));
            W = new Spell(SpellSlot.W, 1100);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 1550);

            Q.SetSkillshot(0.25f, 50f, 1300f, true, SkillshotType.SkillshotLine);
            QSplit.SetSkillshot(0.15f, 60f, 2100f, false, SkillshotType.SkillshotLine);
            QDummy.SetSkillshot(0.4f, 55f, 1300, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 85f, 1700f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 120f, 1500f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.3f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harrasQ", "Harass Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("QHarassMana", "Harass Mana", true).SetValue(new Slider(30, 100, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harrasW", "Harass W", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto Q + E combo, ks", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("harrasE", "Harass Q + E", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("EInterrupter", "Auto Q + E Interrupter", true).SetValue(true));
         
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R KS", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rcombo", "Extra combo dmg calculation", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("Always R").AddItem(new MenuItem("Ralways" + enemy.ChampionName, enemy.ChampionName, true).SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", " LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            //Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsValid<MissileClient>() && sender.IsAlly)
            {
                MissileClient missile = (MissileClient)sender;
                if (missile.SData.Name != null && missile.SData.Name == "VelkozQMissile")
                    QMissile = missile;
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                SetMana();
            }

            if (Q.IsReady() && Config.Item("autoQ", true).GetValue<bool>())
                LogicQ();
            if (Program.LagFree(3) && E.IsReady() && Config.Item("autoE", true).GetValue<bool>())
                LogicE();
            if (Program.LagFree(4) && W.IsReady() && Config.Item("autoW", true).GetValue<bool>())
                LogicW();
        }

        private void LogicE()
        {
            var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (Program.Combo && Player.Mana > RMANA + EMANA)
                    Program.CastSpell(E, t);
                else if (Program.Farm && OktwCommon.CanHarras() && Config.Item("harrasE", true).GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + EMANA + WMANA + EMANA)
                    Program.CastSpell(E, t);
                else
                {
                    var eDmg = OktwCommon.GetKsDamage(t, E);
                    var qDmg = Q.GetDamage(t);
                    if (eDmg > t.Health)
                        Program.CastSpell(E, t);
                    else if (qDmg + eDmg > t.Health && Player.Mana > QMANA + EMANA)
                        Program.CastSpell(E, t);
                }
                if (!Program.None && Player.Mana > RMANA + EMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(E.Range) && !OktwCommon.CanMove(enemy)))
                        E.Cast(enemy);
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmE", true).GetValue<bool>() && Player.Mana > RMANA + EMANA)
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All);
                var farmPosition = E.GetCircularFarmLocation(minionList, E.Width);

                if (farmPosition.MinionsHit >= Config.Item("LCminions", true).GetValue<Slider>().Value)
                    E.Cast(farmPosition.Position);
            }
        }

        private void LogicW()
        {
            var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (Program.Combo && Player.Mana > RMANA + WMANA)
                    Program.CastSpell(W, t);
                else if (Program.Farm && OktwCommon.CanHarras() && Config.Item("harrasW", true).GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.Mana > RMANA + WMANA + EMANA + QMANA + WMANA)
                    Program.CastSpell(W, t);
                else
                {
                    var wDmg = OktwCommon.GetKsDamage(t, W);
                    var qDmg = Q.GetDamage(t);
                    if (wDmg > t.Health)
                        Program.CastSpell(W, t);
                    else if (qDmg + wDmg > t.Health && Player.Mana > QMANA + WMANA)
                        Program.CastSpell(W, t);
                }
                if (!Program.None && Player.Mana > RMANA + WMANA)
                {
                    foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && !OktwCommon.CanMove(enemy)))
                        W.Cast(enemy, true);
                }
            }
            else if (Program.LaneClear && Player.ManaPercent > Config.Item("Mana", true).GetValue<Slider>().Value && Config.Item("farmW", true).GetValue<bool>() && Player.Mana > RMANA + WMANA)
            {
                var minionList = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All);
                var farmPosition = W.GetLineFarmLocation(minionList, W.Width);

                if (farmPosition.MinionsHit >= Config.Item("LCminions", true).GetValue<Slider>().Value)
                    W.Cast(farmPosition.Position);
            }
        }

        private void LogicQ()
        {
            var t = TargetSelector.GetTarget(QDummy.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget())
            {
                if (Q.Instance.Name == "VelkozQ")
                {
                    if (Program.LagFree(1) || Program.LagFree(2))
                    {
                        if (Program.Combo && Player.Mana > RMANA + QMANA )
                            CastQ(t);
                        else if (Program.Farm && OktwCommon.CanHarras() && Config.Item("harrasQ", true).GetValue<bool>() && Config.Item("harras" + t.ChampionName).GetValue<bool>() && Player.ManaPercent > Config.Item("QHarassMana", true).GetValue<Slider>().Value)
                            CastQ(t);
                        else
                        {
                            var qDmg = OktwCommon.GetKsDamage(t, Q);
                            var wDmg = W.GetDamage(t);
                            if (qDmg > t.Health )
                                CastQ(t);
                            else if(qDmg + wDmg > t.Health && Player.Mana > QMANA + WMANA)
                                CastQ(t);
                        }
                        if (!Program.None && Player.Mana > RMANA + QMANA)
                        {

                            foreach (var enemy in Program.Enemies.Where(enemy => enemy.IsValidTarget(QDummy.Range) && !OktwCommon.CanMove(enemy)))
                                CastQ(t);
                        }
                    }
                }
                else
                {
                    DetonateQ(t);
                }
            }
        }

        private void CastQ(Obj_AI_Base t)
        {
            var Qpred = Q.GetPrediction(t);
            if (Qpred.Hitchance >= HitChance.High)
            {
                Program.CastSpell(Q, t);
            }
            else
            {
                var pred = QDummy.GetPrediction(t);
                if (pred.Hitchance >= HitChance.High)
                {
                    if (Program.LagFree(1))
                        pointList = AimQ(pred.CastPosition);
                    if (Program.LagFree(2))
                        BestAim(pred.CastPosition);
                }
            }
        }

        private void DetonateQ(Obj_AI_Base t)
        {
            if (QMissile != null && QMissile.IsValid)
            {
                var realPosition = QMissile.StartPosition.Extend(QMissile.EndPosition, QMissile.StartPosition.Distance(QMissile.Position) + Game.Ping / 2 + 80);
                //Q.Cast();

                QSplit.UpdateSourcePosition(realPosition, realPosition);

                Vector2 start = QMissile.StartPosition.To2D();
                Vector2 end = realPosition.To2D();
                var radius = QSplit.Range;

                var dir = (end - start).Normalized();
                var pDir = dir.Perpendicular();

                var rightEndPos = end + pDir * radius;
                var leftEndPos = end - pDir * radius;

                var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, ObjectManager.Player.Position.Z);
                var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, ObjectManager.Player.Position.Z);

                if (QSplit.WillHit(t, rEndPos) || QSplit.WillHit(t, lEndPos))
                    Q.Cast();
            }

        }

        private List<Vector3> AimQ(Vector3 finalPos)
        {
            var CircleLineSegmentN = 36;
            var radius = 500;
            var position = Player.Position;

            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);
                if (point.Distance(Player.Position.Extend(finalPos, radius)) < 430)
                {
                    points.Add(point);
                    //Utility.DrawCircle(point, 20, System.Drawing.Color.Aqua, 1, 1);
                }
            }

            var point2 = points.OrderBy(x => x.Distance(finalPos));
            points = point2.ToList();
            points.RemoveAt(0);
            points.RemoveAt(1);
            return points;
        }

        private void BestAim(Vector3 predictionPos)
        {
            Vector2 start = Player.Position.To2D();
            var c1 = predictionPos.Distance(Player.Position);
            var playerPos2d = Player.Position.To2D();

            foreach ( var point in pointList )
            {
                for (var j = 400; j <= 1150; j = j + 50)
                {
                    var posExtend = Player.Position.Extend(point, j);

                    var collision = Q.GetCollision(playerPos2d, new List<Vector2> { posExtend.To2D() } );

                    if (collision.Count > 0)
                        break; 

                    var a1 = Player.Distance(posExtend);
                    float b1 = (float)Math.Sqrt((c1 * c1) - (a1 * a1));

                    if (b1 > QSplit.Range)
                        continue;

                    var pointA = Player.Position.Extend(point, a1);

                    Vector2 end = pointA.To2D();
                    var dir = (end - start).Normalized();
                    var pDir = dir.Perpendicular();

                    var rightEndPos = end + pDir * b1;
                    var leftEndPos = end - pDir * b1;

                    var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, ObjectManager.Player.Position.Z);
                    var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, ObjectManager.Player.Position.Z);

                    if (lEndPos.Distance(predictionPos) < QSplit.Width)
                    {
                        var collisionS = QSplit.GetCollision(pointA.To2D(), new List<Vector2> { lEndPos.To2D() });
                        if (collisionS.Count > 0)
                            continue;

                        Q.Cast(pointA);
                        return;
                    }
                    if ( rEndPos.Distance(predictionPos) < QSplit.Width)
                    {
                        var collisionR = QSplit.GetCollision(pointA.To2D(), new List<Vector2> { rEndPos.To2D() });
                        if (collisionR.Count > 0)
                            continue;

                        Q.Cast(pointA);
                        return;
                    }
                }
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("qRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (Q.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1, 1);
            }
            if (Config.Item("wRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (W.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1, 1);
            }
            if (Config.Item("eRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (E.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1, 1);
            }
            if (Config.Item("rRange", true).GetValue<bool>())
            {
                if (Config.Item("onlyRdy", true).GetValue<bool>())
                {
                    if (R.IsReady())
                        Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
                }
                else
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1, 1);
            }
        }

        private void SetMana()
        {
            if ((Config.Item("manaDisable", true).GetValue<bool>() && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Instance.ManaCost;
        }
    }
}
