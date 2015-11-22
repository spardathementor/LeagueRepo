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

namespace OneKeyToMeme
{


    class Program
    {
        private static bool showMenu = false;
        private static Render.Sprite 
            Co;
        private static float angle = 0;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static Bitmap LoadImg(string imgName)
        {
            var bitmap = Resources.ResourceManager.GetObject(imgName) as Bitmap;
            if (bitmap == null)
            {
                Console.WriteLine(imgName + ".png not found.");
            }
            return bitmap;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Co = new Render.Sprite(LoadImg("Co"), new Vector2(100, 100));
            Co.Add(0);
            



            
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            Co.OnDraw();

        }

    }
}
    