using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SebbyLib;


namespace OneKeyToWin_AIO_Sebby.Champions
{
    class Ziggs : Base
    {

        public Ziggs()
        {
            Q = new Spell(SpellSlot.Q, 850f);

            Q1 = new Spell(SpellSlot.Q, 1400f);

            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 5300f);

            Q.SetSkillshot(0.25f, 120f, 1600f, false, SkillshotType.SkillshotCircle);
            Q1.SetSkillshot(0.75f, 120f, 1600f, true, SkillshotType.SkillshotLine);

            W.SetSkillshot(0.25f, 275f, 1750f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 100f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(1f, 500f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("noti", "Show R notification", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("qRange", "Q range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("wRange", "W range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("eRange", "E range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("rRange", "R range", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("Draw").AddItem(new MenuItem("onlyRdy", "Draw when skill rdy", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("autoQ", "Auto Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("harassQ", "Harass Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Q Config").AddItem(new MenuItem("QHarassMana", "Harass Mana", true).SetValue(new Slider(30, 100, 0)));

            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("Q Config").SubMenu("Use on:").AddItem(new MenuItem("Qon" + enemy.ChampionName, enemy.ChampionName).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("autoW", "Auto W", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("harassW", "Harass W", true).SetValue(false));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").AddItem(new MenuItem("WmodeCombo", "W combo mode", true).SetValue(new StringList(new[] { "always", "run - cheese" }, 1)));
            Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("W Gap Closer").AddItem(new MenuItem("WmodeGC", "Gap Closer position mode", true).SetValue(new StringList(new[] { "Dash end position", "My hero position" }, 0)));
            foreach (var enemy in HeroManager.Enemies)
                Config.SubMenu(Player.ChampionName).SubMenu("W Config").SubMenu("W Gap Closer").SubMenu("Cast on enemy:").AddItem(new MenuItem("WGCchampion" + enemy.ChampionName, enemy.ChampionName, true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("autoE", "Auto E if enemy in range", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("E Config").AddItem(new MenuItem("Emana", "E % minimum mana", true).SetValue(new Slider(20, 100, 0)));

            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoR", "Auto R", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("autoRzombie", "Auto R upon dying if can help team", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Renemy", "Don't R if enemy in x range", true).SetValue(new Slider(1500, 2000, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("RenemyA", "Don't R if ally in x range near target", true).SetValue(new Slider(800, 2000, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("R Config").AddItem(new MenuItem("Rturrent", "Don't R under turret", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQout", "Last hit Q minion out range AA", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmQ", "Lane clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("farmE", "Lane clear E", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("QLCminions", " QLaneClear minimum minions", true).SetValue(new Slider(2, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("ELCminions", " ELaneClear minimum minions", true).SetValue(new Slider(5, 10, 0)));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleQ", "Jungle clear Q", true).SetValue(true));
            Config.SubMenu(Player.ChampionName).SubMenu("Farm").AddItem(new MenuItem("jungleE", "Jungle clear E", true).SetValue(true));

            Config.SubMenu(Player.ChampionName).AddItem(new MenuItem("autoZombie", "Auto zombie mode COMBO / LANECLEAR", true).SetValue(true));
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            var t = TargetSelector.GetTarget(Q1.Range, TargetSelector.DamageType.Magical);
            if (t.IsValidTarget() )
            {

                if (Program.Combo)
                    CastQ(t);
                    
                
            }
        }

        private void CastQ(Obj_AI_Base t)
        {


            if(Player.Distance(t) < Q.Range+ 100)
                Program.CastSpell(Q, t);
            else
                Program.CastSpell(Q1, t);

        }
    }
}
