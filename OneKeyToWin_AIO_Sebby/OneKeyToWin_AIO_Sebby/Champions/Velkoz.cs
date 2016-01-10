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
        private bool EQcastNow = false;
        public void LoadOKTW()
        {
            Q = new Spell(SpellSlot.Q, 1200);
            QSplit = new Spell(SpellSlot.Q, 1100);
            QDummy = new Spell(SpellSlot.Q, (float)Math.Sqrt(Math.Pow(Q.Range, 2) + Math.Pow(QSplit.Range, 2)));
            W = new Spell(SpellSlot.W, 1200);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 1550);

            Q.SetSkillshot(0.25f, 50f, 1300f, true, SkillshotType.SkillshotLine);
            QSplit.SetSkillshot(0.25f, 55f, 2100, true, SkillshotType.SkillshotLine);
            QDummy.SetSkillshot(0.25f, 55f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 85f, 1700f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 100f, 1500f, false, SkillshotType.SkillshotCircle);
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

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("E Config").SubMenu("Auto Q + E Gapcloser").AddItem(new MenuItem("Egapcloser" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("E Config").SubMenu("Use Q + E on").AddItem(new MenuItem("Eon" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R KS", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rcombo", "Extra combo dmg calculation", true).SetValue(true));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("R Config").SubMenu("Always R").AddItem(new MenuItem("Ralways" + enemy.ChampionName, enemy.ChampionName, true).SetValue(false));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu("Harras").AddItem(new MenuItem("harras" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQout", "Last hit Q minion out range AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmW", "Lane clear W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("Mana", "LaneClear Mana", true).SetValue(new Slider(80, 100, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("LCminions", " LaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleW", "Jungle clear W", true).SetValue(true));

            Game.OnUpdate += Game_OnGameUpdate;
            //GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            //Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            BestAim(Game.CursorPos);
        }

        private List<Vector3> AimQ(Vector3 finalPos)
        {
            var CircleLineSegmentN = 36;
            var radius = 500;
            var position = Player.Position;
            var posExtCursor = Player.Position.Extend(Game.CursorPos, radius);

            List<Vector3> points = new List<Vector3>();
            for (var i = 1; i <= CircleLineSegmentN; i++)
            {
                var angle = i * 2 * Math.PI / CircleLineSegmentN;
                var point = new Vector3(position.X + radius * (float)Math.Cos(angle), position.Y + radius * (float)Math.Sin(angle), position.Z);


                if (point.Distance(posExtCursor) < 430)
                {
                    points.Add(point);
                    Utility.DrawCircle(point, 20, System.Drawing.Color.Aqua, 1, 1);
                }
                
            }

            var point2 = points.OrderBy(x => x.Distance(finalPos));

            return point2.ToList();
        }

        private void BestAim(Vector3 predictionPos)
        {
            var pointList = AimQ(predictionPos);
            Vector2 start = Player.Position.To2D();
            var c1 = predictionPos.Distance(Player.Position);

            foreach ( var point in pointList )
            {
                for (var j = 400; j <= 1050; j = j + 50)
                {

                    var posExtend = Player.Position.Extend(point, j);

                    var a1 = Player.Distance(Player.Position.Extend(point, j));

                    var collision = Q.GetCollision(Player.Position.To2D(), new List<Vector2> { posExtend.To2D() } );
                    if (collision.Count > 0)
                        continue;

                    float b1 = (float)Math.Sqrt((c1 * c1) - (a1 * a1));

                    var pointA = Player.Position.Extend(point, a1);
                    Vector2 end = pointA.To2D();
                    var dir = (end - start).Normalized();
                    var pDir = dir.Perpendicular();

                    var rightEndPos = end + pDir * b1;
                    var leftEndPos = end - pDir * b1;

                    var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, ObjectManager.Player.Position.Z);
                    var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, ObjectManager.Player.Position.Z);
                    //Drawing.DrawLine(rStartPos, rEndPos, 1, System.Drawing.Color.Aqua);
                    //Drawing.DrawLine(lStartPos, lEndPos, 1, System.Drawing.Color.Aqua);
                    //Drawing.DrawLine(rStartPos, lStartPos, 1, System.Drawing.Color.Aqua);
                    //if(rEndPos.Distance(predictionPos) < 200 || lEndPos.Distance(predictionPos) < 200)
                    //Drawing.DrawLine(lEndPos, rEndPos, 1, System.Drawing.Color.Aqua);

                    if (lEndPos.Distance(predictionPos) < 50 || rEndPos.Distance(predictionPos) < 50)
                    {
                        Utility.DrawCircle(pointA, 20, System.Drawing.Color.Red, 1, 1);
                        Utility.DrawCircle(lEndPos, 20, System.Drawing.Color.Yellow, 1, 1);
                        Utility.DrawCircle(rEndPos, 20, System.Drawing.Color.Yellow, 1, 1);
                        return;
                    }
                    //return;
                }
                
            }
        }



        private void Game_OnGameUpdate(EventArgs args)
        {
           

        }
    }
}
