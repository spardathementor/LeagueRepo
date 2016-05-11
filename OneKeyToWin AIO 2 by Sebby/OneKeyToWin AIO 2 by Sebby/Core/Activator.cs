using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;
using LeagueSharp.SDK.UI;

namespace OneKeyToWin_AIO_2_by_Sebby.Core
{
    class Activator : Program
    {
        private SpellSlot heal, barrier, ignite, exhaust, flash, smite, teleport, cleanse;

        public static Items.Item

            //Cleans
            Mikaels = new Items.Item(3222, 600f),
            Quicksilver = new Items.Item(3140, 0),
            Mercurial = new Items.Item(3139, 0),
            Dervish = new Items.Item(3137, 0),
            //REGEN
            Potion = new Items.Item(2003, 0),
            ManaPotion = new Items.Item(2004, 0),
            Flask = new Items.Item(2041, 0),
            Biscuit = new Items.Item(2010, 0),
            Refillable = new Items.Item(2031, 0),
            Hunter = new Items.Item(2032, 0),
            Corrupting = new Items.Item(2033, 0),
            //attack
            Botrk = new Items.Item(3153, 550f),
            Cutlass = new Items.Item(3144, 550f),
            Youmuus = new Items.Item(3142, 650f),
            Hydra = new Items.Item(3074, 440f),
            Hydra2 = new Items.Item(3077, 440f),
            HydraTitanic = new Items.Item(3748, 150f),
            Hextech = new Items.Item(3146, 700f),
            FrostQueen = new Items.Item(3092, 850f),

            //def
            FaceOfTheMountain = new Items.Item(3401, 600f),
            Zhonya = new Items.Item(3157, 0),
            Seraph = new Items.Item(3040, 0),
            Solari = new Items.Item(3190, 600f),
            Randuin = new Items.Item(3143, 400f);

        public Activator()
        {
            teleport = Player.GetSpellSlot("SummonerTeleport");
            heal = Player.GetSpellSlot("summonerheal");
            barrier = Player.GetSpellSlot("summonerbarrier");
            ignite = Player.GetSpellSlot("summonerdot");
            exhaust = Player.GetSpellSlot("summonerexhaust");
            flash = Player.GetSpellSlot("summonerflash");
            cleanse = Player.GetSpellSlot("SummonerBoost");
            smite = Player.GetSpellSlot("summonersmite");

            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("itemsmiteaoe"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmiteplayerganker"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmitequick"); }
            if (smite == SpellSlot.Unknown) { smite = Player.GetSpellSlot("s5_summonersmiteduel"); }
        }

        private void PotionManagement()
        {
            if (Player.Health + 250 > Player.MaxHealth)
                return;

            if (Player.HealthPercent > 50 && Player.CountEnemyHeroesInRange(700) == 0)
                return;

            if (Player.HasBuff("RegenerationPotion") || Player.HasBuff("ItemMiniRegenPotion") || Player.HasBuff("ItemCrystalFlaskJungle") || Player.HasBuff("ItemDarkCrystalFlask") || Player.HasBuff("ItemCrystalFlask"))
                return;
            
            if (Potion.IsReady)
                Potion.Cast();
            else if (Biscuit.IsReady)
                Biscuit.Cast();
            else if (Hunter.IsReady)
                Hunter.Cast();
            else if (Corrupting.IsReady)
                Corrupting.Cast();
            else if (Refillable.IsReady)
                Refillable.Cast();
        }

        private bool CanUse(SpellSlot sum)
        {
            if (sum != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(sum) == SpellState.Ready)
                return true;
            else
                return false;
        }
    }
}
