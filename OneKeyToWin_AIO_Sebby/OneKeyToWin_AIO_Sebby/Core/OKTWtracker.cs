using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace OneKeyToWin_AIO_Sebby.Core
{
    class ChampionInfo
    {
        public Obj_AI_Hero Hero { get; set; }

        public Vector3 LastVisablePos { get; set; }
        public float LastVisableTime { get; set; }
        public Vector3 PredictedPos { get; set; }
        public Vector3 LastWayPoint { get; set; }

        public float StartRecallTime { get; set; }
        public float AbortRecallTime { get; set; }
        public float FinishRecallTime { get; set; }
        public bool IsJungler { get; set; }
        public double IncomingDamage = 0;
        public Render.Sprite NormalSprite;
        public Render.Sprite HudSprite;
        public Render.Sprite MinimapSprite;
        public Render.Sprite SquareSprite;

        public ChampionInfo(Obj_AI_Hero hero)
        {
            Hero = hero;
            if (hero.IsEnemy)
                NormalSprite = ImageLoader.CreateRadrarIcon(hero.ChampionName + "_Square_0", System.Drawing.Color.Red);
            else
                NormalSprite = ImageLoader.CreateRadrarIcon(hero.ChampionName + "_Square_0", System.Drawing.Color.GreenYellow);
            SquareSprite = ImageLoader.GetSprite(hero.ChampionName + "_Square_0");
            HudSprite = ImageLoader.CreateRadrarIcon(hero.ChampionName + "_Square_0", System.Drawing.Color.DarkGoldenrod, 100);
            MinimapSprite = ImageLoader.CreateMinimapSprite(hero.ChampionName + "_Square_0");
            LastVisableTime = Game.Time;
            LastVisablePos = hero.Position;
            PredictedPos = hero.Position;
            IsJungler = hero.Spellbook.Spells.Any(spell => spell.Name.ToLower().Contains("smite"));

            StartRecallTime = 0;
            AbortRecallTime = 0;
            FinishRecallTime = 0;

        }
    }

    class OKTWtracker
    {
        public static HashSet<ChampionInfo> ChampionInfoList = new HashSet<ChampionInfo>();

        private Vector3 EnemySpawn;

        public void LoadOKTW()
        {
            EnemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy).Position;
            foreach (var hero in HeroManager.AllHeroes)
            {
                ChampionInfoList.Add(new ChampionInfo(hero));
            }

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
        }

        private static void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            var unit = sender as Obj_AI_Hero;

            if (unit == null || !unit.IsValid || unit.IsAlly)
                return;
            
            var ChampionInfoOne = ChampionInfoList.Find(x => x.Hero.NetworkId == sender.NetworkId);

            var recall = Packet.S2C.Teleport.Decoded(unit, args);

            if (recall.Type == Packet.S2C.Teleport.Type.Recall)
            {
                switch (recall.Status)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        ChampionInfoOne.StartRecallTime = Game.Time;
                        break;
                    case Packet.S2C.Teleport.Status.Abort:
                        ChampionInfoOne.AbortRecallTime = Game.Time;
                        break;
                    case Packet.S2C.Teleport.Status.Finish:
                        ChampionInfoOne.FinishRecallTime = Game.Time;
                        var spawnPos = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy).Position;
                        ChampionInfoOne.LastVisablePos = spawnPos;
                        ChampionInfoOne.PredictedPos = spawnPos;
                        ChampionInfoOne.LastWayPoint = spawnPos;
                        ChampionInfoOne.LastVisableTime = Game.Time;
                        break;
                }
            }
        }

        private void OnUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                foreach (var extra in ChampionInfoList.Where(x => x.Hero.IsEnemy))
                {
                    extra.IncomingDamage = SebbyLib.OktwCommon.GetIncomingDamage2(extra.Hero, 0.5f);
                    var enemy = extra.Hero;
                    if (enemy.IsDead)
                    {
                        extra.LastVisablePos = EnemySpawn;
                        extra.LastVisableTime = Game.Time;
                        extra.PredictedPos = EnemySpawn;
                        extra.LastWayPoint = EnemySpawn;
                    }
                    else if (enemy.IsVisible)
                    {
                        extra.LastWayPoint = extra.Hero.GetWaypoints().Last().To3D();
                        extra.PredictedPos = enemy.Position.Extend(extra.LastWayPoint, 125);
                        extra.LastVisablePos = enemy.Position;
                        extra.LastVisableTime = Game.Time;
                    }
                }
            }

            if(Program.LagFree(3))
            {
                foreach (var extra in ChampionInfoList)
                {
                    extra.NormalSprite.VisibleCondition = sender => !extra.Hero.IsDead;
                    extra.HudSprite.VisibleCondition = sender => !extra.Hero.IsDead;
                    extra.IncomingDamage = SebbyLib.OktwCommon.GetIncomingDamage2(extra.Hero, 0.5f);
                }
            }
        }
    }
}
