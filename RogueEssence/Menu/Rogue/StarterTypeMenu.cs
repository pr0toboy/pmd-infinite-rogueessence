using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;

namespace RogueEssence.Menu
{
    /// <summary>
    /// Fangame « Explorateurs de l'Infini » — quiz de sélection du starter par
    /// TYPE (« Quel type te ressemble ? »). Le joueur choisit un type, puis un
    /// starter parmi le pool figé de ce type (cf. EOS-PLAN.md, pool définitif
    /// 2026-06-11 : 45 starters à 2 évolutions, jouables, 18 types). Remplace
    /// l'écran de choix vanilla (GetStartersList) pour la zone infinite_dungeon,
    /// ce qui exclut aussi les espèces parasites débloquées via RogueUnlock.
    /// </summary>
    public class StarterTypeMenu : MultiPageMenu
    {
        private const int SLOTS_PER_PAGE = 12;
        private static int defaultChoice;

        private RogueConfig config;

        // Ordre d'affichage des types (les 3 classiques d'abord). element id -> pool.
        public static readonly (string element, string[] species)[] POOL = new (string, string[])[]
        {
            ("fire",     new[] { "charmander", "cyndaquil", "torchic", "chimchar", "tepig", "fennekin", "litten", "scorbunny", "fuecoco" }),
            ("water",    new[] { "squirtle", "totodile", "mudkip", "piplup", "oshawott", "froakie", "popplio", "sobble", "quaxly" }),
            ("grass",    new[] { "bulbasaur", "chikorita", "treecko", "turtwig", "snivy", "chespin", "rowlet", "grookey", "sprigatito" }),
            ("normal",   new[] { "whismur", "lillipup" }),
            ("ground",   new[] { "trapinch", "sandile" }),
            ("psychic",  new[] { "ralts", "gothita" }),
            ("ice",      new[] { "vanillite", "swinub" }),
            ("fighting", new[] { "machop", "timburr" }),
            ("electric", new[] { "shinx", "mareep" }),
            ("ghost",    new[] { "gastly", "duskull" }),
            ("dragon",   new[] { "deino", "gible" }),
            ("rock",     new[] { "geodude", "larvitar" }),
            ("steel",    new[] { "honedge", "beldum" }),
            ("flying",   new[] { "starly", "pidgey" }),
            ("fairy",    new[] { "cleffa", "flabebe" }),
            ("poison",   new[] { "nidoran_m", "zubat" }),
            ("dark",     new[] { "impidimp", "pawniard" }),
            ("bug",      new[] { "grubbin", "caterpie" }),
        };

        public StarterTypeMenu(RogueConfig config) : this(MenuLabel.ROGUE_CHAR_MENU, config) { }
        public StarterTypeMenu(string label, RogueConfig config)
        {
            Label = label;
            this.config = config;

            List<MenuChoice> choices = new List<MenuChoice>();
            foreach ((string element, string[] species) in POOL)
            {
                if (species.Length == 0)
                    continue;
                string el = element;
                string[] pool = species;
                ElementData elementData = DataManager.Instance.GetElement(el);
                choices.Add(new MenuTextChoice(elementData.GetIconName(), () => { chooseType(pool); }));
            }

            int actualChoice = System.Math.Min(System.Math.Max(0, defaultChoice), choices.Count - 1);
            IChoosable[][] box = SortIntoPages(choices.ToArray(), SLOTS_PER_PAGE);
            int totalSlots = SLOTS_PER_PAGE;
            if (box.Length == 1)
                totalSlots = box[0].Length;
            int startPage = actualChoice / SLOTS_PER_PAGE;
            int startIndex = actualChoice % SLOTS_PER_PAGE;

            Initialize(new Loc(16, 16), 128, "Quel type te ressemble ?", box, startIndex, startPage, totalSlots);
        }

        protected override void ChoiceChanged()
        {
            defaultChoice = CurrentChoiceTotal;
            base.ChoiceChanged();
        }

        private void chooseType(string[] pool)
        {
            List<string> starters = new List<string>(pool);
            MenuManager.Instance.AddMenu(new CharaChoiceMenu(config, starters), false);
        }
    }
}
