using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SebbyLib;
namespace Universal_Tear_Stacker
{
    class Program
    {
        static void Main(string[] args) {CustomEvents.Game.OnGameLoad += Game_OnGameLoad;}
        public static Menu Config;
        private static Spell Q, W, E, R;
        private static int timer = 0;

        private static int Tear = 3070;
        private static int Manamune = 3004;
        private static int Archangel = 3003;

        private static void Game_OnGameLoad(EventArgs args)
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Config = new Menu("Universal Tear Stacker", "Universal Tear Stacker", true);
            Config.AddToMainMenu();
            Config.SubMenu("Order").AddItem(new MenuItem("1", "1", true).SetValue(new StringList(new[] { "Q", "W", "E" }, 0)));
            Config.SubMenu("Order").AddItem(new MenuItem("2", "2", true).SetValue(new StringList(new[] { "Q", "W", "E" }, 1)));
            Config.SubMenu("Order").AddItem(new MenuItem("3", "3", true).SetValue(new StringList(new[] { "Q", "W", "E" }, 2)));

            Config.SubMenu("Spells").AddItem(new MenuItem("Q", "Q", true).SetValue(false));
            Config.SubMenu("Spells").AddItem(new MenuItem("W", "W", true).SetValue(false));
            Config.SubMenu("Spells").AddItem(new MenuItem("E", "E", true).SetValue(false));
            Config.AddItem(new MenuItem("disable", "disable key").SetValue(new KeyBind(32, KeyBindType.Press))); //32 == space
            Config.AddItem(new MenuItem("mana", "Minimum MANA %", true).SetValue(new Slider(90, 100, 0)));

            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // TIMER
            if (Utils.TickCount - timer < 4100)
                return;

            timer = Utils.TickCount;

            if (Config.Item("disable").GetValue<KeyBind>().Active)
                return;
            // MANA CHECK
            if(ObjectManager.Player.ManaPercent < Config.Item("mana", true).GetValue<Slider>().Value)
                return;

            // CHECK ITEM 
            if (!Items.HasItem(Tear) && !Items.HasItem(Manamune) && !Items.HasItem(Archangel))
                return;

            if (Utils.TickCount - Q.LastCastAttemptT < 4000 || Utils.TickCount - W.LastCastAttemptT < 4000 || Utils.TickCount - E.LastCastAttemptT < 4000)
                return;

            if (ObjectManager.Player.CountEnemiesInRange(2000) > 0 || Cache.GetMinions(ObjectManager.Player.Position, 1000, MinionTeam.NotAlly).Any())
                return;

            int lvl1 = Config.Item("1", true).GetValue<StringList>().SelectedIndex;
            int lvl2 = Config.Item("2", true).GetValue<StringList>().SelectedIndex;
            int lvl3 = Config.Item("3", true).GetValue<StringList>().SelectedIndex;

            if (lvl1 == 0 && Q.IsReady() && Config.Item("Q", true).GetValue<bool>())
                SpellbookCastSpell(Q);
            else if (lvl1 == 1 && W.IsReady() && Config.Item("W", true).GetValue<bool>())
                SpellbookCastSpell(W);
            else if (lvl1 == 2 && E.IsReady() && Config.Item("E", true).GetValue<bool>())
                SpellbookCastSpell(E);
            else if (lvl2 == 0 && Q.IsReady() && Config.Item("Q", true).GetValue<bool>())
                SpellbookCastSpell(Q);
            else if (lvl2 == 1 && W.IsReady() && Config.Item("W", true).GetValue<bool>())
                SpellbookCastSpell(W);
            else if (lvl2 == 2 && E.IsReady() && Config.Item("E", true).GetValue<bool>())
                SpellbookCastSpell(E);
            else if (lvl3 == 0 && Q.IsReady() && Config.Item("Q", true).GetValue<bool>())
                SpellbookCastSpell(Q);
            else if (lvl3 == 1 && W.IsReady() && Config.Item("W", true).GetValue<bool>())
                SpellbookCastSpell(W);
            else if (lvl3 == 2 && E.IsReady() && Config.Item("E", true).GetValue<bool>())
                SpellbookCastSpell(E);

        }

        private static void SpellbookCastSpell(Spell spell)
        {
            spell.Cast(ObjectManager.Player.ServerPosition);
            spell.Cast(ObjectManager.Player);
        }
    }
}
