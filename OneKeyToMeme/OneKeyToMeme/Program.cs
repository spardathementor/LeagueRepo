using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;
using UtilityPlus.SpellTracker;

namespace OneKeyToMeme
{
    
    class Program
    {
        private static Render.Sprite sImage;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            sImage = new Render.Sprite(ImageLoader.Load("kaczorek"), new Vector2(100, 100));
            sImage.Scale = new Vector2(100, 100);
            sImage.Add(0);
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            sImage.OnDraw();    
        }
    }
}
    