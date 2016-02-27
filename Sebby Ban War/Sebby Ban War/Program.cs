using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Sebby_Ban_War
{
    class Program
    {
        public static Menu Config;

        public static int LastMouseTime = Utils.TickCount;
        public static Vector2 LastMousePos = Game.CursorPos.To2D();
        public static int NewPathTime = Utils.TickCount;
        public static int LastType = 0; // 0 Move , 1 Attack, 2 Cast spell

        static void Main(string[] args) { CustomEvents.Game.OnGameLoad += Game_OnGameLoad; }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Config = new Menu("Sebby Ban War", "Sebby Ban War", true);
            Config.AddToMainMenu();
            Config.AddItem(new MenuItem("ClickTime", "Minimum Click Time (120)").SetValue(new Slider(150, 300, 0)));
            Config.AddItem(new MenuItem("Info", "0 - 120 scripter"));
            Config.AddItem(new MenuItem("Info2", "120 - 200 pro player"));
            Config.AddItem(new MenuItem("Info3", "200 + normal player"));
            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot != SpellSlot.Q && args.Slot != SpellSlot.W && args.Slot != SpellSlot.E && args.Slot != SpellSlot.R)
                return;

            if (args.EndPosition.IsZero)
                return;

            var screenPos = Drawing.WorldToScreen(args.EndPosition);
            if (Utils.TickCount - LastMouseTime < LastMousePos.Distance(screenPos) / 10)
            {
                Console.WriteLine("BLOCK SPELL");
                args.Process = false;
                return;
            }
            LastType = 2;
            LastMouseTime = Utils.TickCount;
            LastMousePos = screenPos;
        }

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            var screenPos = Drawing.WorldToScreen(args.TargetPosition);
           
            //Console.WriteLine(args.Order);
            if (Utils.TickCount - LastMouseTime < Config.Item("ClickTime").GetValue<Slider>().Value + (LastMousePos.Distance(screenPos) / 10))
            {
                Console.WriteLine("BLOCK " + args.Order);
                args.Process = false;
                return;
                
            }

            //Console.WriteLine("DIS " + LastMousePos.Distance(screenPos) + " TIME " + (Utils.TickCount - LastMouseTime));
            if (args.Order == GameObjectOrder.AttackUnit)
                LastType = 1;
            else
                LastType = 0;

            LastMouseTime = Utils.TickCount;
            LastMousePos = screenPos;
        }
    }
}
