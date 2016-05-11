using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.UI;
using LeagueSharp.SDK.Enumerations;
using System.Windows.Forms;
using SebbyLib;

namespace OneKeyToWin_AIO_2_by_Sebby.Core
{
    class ChampionStuff : Program
    {
        public ChampionStuff()
        {
            MenuHarass = MenuHero.Add(new LeagueSharp.SDK.UI.Menu("MenuHarass", "Harass list"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
            {
                MenuHarass.Add(new MenuBool("H" + enemy.ChampionName, enemy.ChampionName, true));
            }

            MenuFarm.Add(new MenuKeyBind("Farm", "Lane Clear", Keys.N, KeyBindType.Toggle));

            MenuAdvance = MainMenu.Add(new LeagueSharp.SDK.UI.Menu("adv", "Advanced options"));
            MenuAdvance.Add(new MenuBool("mana", "Disable mana manager in combo", false));
            MenuAdvance.Add(new MenuBool("support", "Support Mode", false));
            MenuAdvance.Add(new MenuBool("comboAa", "Disable auto-attack in combo mode", false));
            Game.OnWndProc += Game_OnWndProc;
            Orbwalker.OnAction += OnAction;
        }

        private void OnAction(object sender, OrbwalkingActionArgs e)
        {
            if (AIOmode == 2)
                return;

            if (e.Type == OrbwalkingType.BeforeAttack)
            {
                if (Combo && MenuAdvance["comboAa"])
                {
                    var t = (Obj_AI_Hero)e.Target;
                    if (6 * Player.GetAutoAttackDamage(t) < t.Health - OktwCommon.GetIncomingDamage(t) && !t.HasBuff("luxilluminatingfraulein") && !Player.HasBuff("sheen"))
                        e.Process = false;
                }

                if (Farm && MenuAdvance["support"])
                {
                    if (e.Target.Type == GameObjectType.obj_AI_Minion) e.Process = false;
                }
            }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            //((MenuBool)MenuFarm.Components[""]).Value = ((MenuBool)MenuFarm.Components[""]).Value;
        }
    }
}
