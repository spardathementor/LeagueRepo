using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;

namespace OneKeyToWin_AIO_Sebby.Core
{
    class OKTWdash
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Menu Config = Program.Config;
        private static Orbwalking.Orbwalker Orbwalker = Program.Orbwalker;
        private static Spell DashSpell;

        public OKTWdash(Spell qwer)
        {
            DashSpell = qwer;
            
            Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("DashMode", "Dash MODE", true).SetValue(new StringList(new[] { "Game Cursor", "Side", "Safe position" }, 2)));
            Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("EnemyCheck", "Block dash in x enemies ", true).SetValue(new Slider(3, 5, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("WallCheck", "Block dash in wall", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").AddItem(new MenuItem("TurretCheck", "Block dash under turret", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").SubMenu("Gapcloser").AddItem(new MenuItem("GapcloserMode", "Gapcloser MODE", true).SetValue(new StringList(new[] { "Game Cursor", "Away - safe position" }, 1)));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
                Config.SubMenu(Player.ChampionName).SubMenu(qwer.Slot + " Config").SubMenu("Gapcloser").AddItem(new MenuItem("EGCchampion" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (DashSpell.IsReady() && Config.Item("EGCchampion" + gapcloser.Sender.ChampionName, true).GetValue<bool>())
            {
                int GapcloserMode = Config.Item("GapcloserMode", true).GetValue<StringList>().SelectedIndex;
                if (GapcloserMode == 0)
                {
                    var bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
                    if (IsGoodPosition(bestpoint))
                        DashSpell.Cast(bestpoint);
                }
                else
                {
                    var points = OktwCommon.CirclePoints(10, DashSpell.Range, Player.Position);
                    var bestpoint = Player.Position.Extend(gapcloser.Sender.Position, -DashSpell.Range);
                    int enemies = bestpoint.CountEnemiesInRange(DashSpell.Range);
                    foreach (var point in points)
                    {
                        int count = point.CountEnemiesInRange(DashSpell.Range);
                        if (count < enemies)
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                        else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
                        {
                            enemies = count;
                            bestpoint = point;
                        }
                    }
                    if (IsGoodPosition(bestpoint))
                        DashSpell.Cast(bestpoint);
                }
            }
        }
        public Vector3 CastDash()
        {
            int DashMode = Config.Item("DashMode", true).GetValue<StringList>().SelectedIndex;

            Vector3 bestpoint = Vector3.Zero;
            if (DashMode == 0)
            {
                bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
                if (IsGoodPosition(bestpoint))
                    DashSpell.Cast(bestpoint);
            }
            else if (DashMode == 1)
            {
                var orbT = Orbwalker.GetTarget();
                if(orbT != null && orbT.Type == GameObjectType.obj_AI_Hero)
                {
                    Vector2 start = Player.Position.To2D();
                    Vector2 end = orbT.Position.To2D();
                    var dir = (end - start).Normalized();
                    var pDir = dir.Perpendicular();

                    var rightEndPos = end + pDir * Player.Distance(orbT);
                    var leftEndPos = end - pDir * Player.Distance(orbT);

                    var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Player.Position.Z);
                    var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Player.Position.Z);

                    if (Game.CursorPos.Distance(rEndPos) < Game.CursorPos.Distance(lEndPos))
                    {
                        bestpoint = Player.Position.Extend(rEndPos, DashSpell.Range);
                        if (IsGoodPosition(bestpoint))
                            DashSpell.Cast(bestpoint);
                    }
                    else
                    {
                        bestpoint = Player.Position.Extend(lEndPos, DashSpell.Range);
                        if (IsGoodPosition(bestpoint))
                            DashSpell.Cast(bestpoint);
                    }
                }
            }
            else if (DashMode == 2)
            {
                var points = OktwCommon.CirclePoints(12, DashSpell.Range, Player.Position);
                bestpoint = Player.Position.Extend(Game.CursorPos, DashSpell.Range);
                int enemies = bestpoint.CountEnemiesInRange(400);
                foreach (var point in points)
                {
                    int count = point.CountEnemiesInRange(400);
                    if (count < enemies)
                    {
                        enemies = count;
                        bestpoint = point;
                    }
                    else if (count == enemies && Game.CursorPos.Distance(point) < Game.CursorPos.Distance(bestpoint))
                    {
                        enemies = count;
                        bestpoint = point;
                    }
                }
                if(IsGoodPosition(bestpoint))
                    DashSpell.Cast(bestpoint);
            }

            if (!bestpoint.IsZero && bestpoint.CountEnemiesInRange(Player.BoundingRadius + Player.AttackRange + 100) == 0)
                return Vector3.Zero;

            return bestpoint;
        }

        public bool IsGoodPosition(Vector3 dashPos)
        {
            if (Config.Item("WallCheck", true).GetValue<bool>())
            {
                float segment = DashSpell.Range / 5;
                for (int i = 1; i <= 5; i++)
                {
                    if (Player.Position.Extend(dashPos, i * segment).IsWall())
                        return false;
                }
            }

            if (Config.Item("TurretCheck", true).GetValue<bool>())
            {
                if(dashPos.UnderTurret(true))
                    return false;
            }

            var enemyCheck = Config.Item("EnemyCheck", true).GetValue<Slider>().Value;
            var enemyCountDashPos = dashPos.CountEnemiesInRange(600);
            
            if (enemyCheck > enemyCountDashPos)
                return true;

            var enemyCountPlayer = Player.CountEnemiesInRange(400);

            if(enemyCountDashPos <= enemyCountPlayer)
                return true;

            return false;
        }
    }
}
