using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;

namespace RogueEssence.Menu
{
    public class RogueMenu : SingleStripMenu
    {
        // Fixed destination for the infinite-dungeon fangame. When the zone exists
        // in the loaded data, the "New" entry bypasses RogueDestMenu and goes
        // straight to team-name → starter → partner. Otherwise we fall back to
        // the vanilla dungeon-picker so this menu stays usable in Origins runs.
        private const string INFINITE_DUNGEON_ZONE = "infinite_dungeon";

        public RogueMenu() : this(MenuLabel.ROGUE_MENU) { }
        public RogueMenu(string label)
        {
            Label = label;
            List<MenuTextChoice> choices = new List<MenuTextChoice>();
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_TOP_NEW"), () => { StartNewRun(); }));
            if (DataManager.Instance.FoundRecords(PathMod.ModSavePath(DataManager.ROGUE_PATH), DataManager.QUICKSAVE_EXTENSION))
                choices.Add(new MenuTextChoice(Text.FormatKey("MENU_TOP_LOAD"), () => { MenuManager.Instance.AddMenu(new QuicksaveMenu(), false); }));
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_INFO"), () => { MenuManager.Instance.AddMenu(new RogueInfoMenu(), false); }));

            Initialize(new Loc(16, 16), CalculateChoiceLength(choices, 72), choices.ToArray(), 0);
        }

        private static void StartNewRun()
        {
            if (DataManager.Instance.DataIndices[DataManager.DataType.Zone].ContainsKey(INFINITE_DUNGEON_ZONE))
            {
                RogueConfig config = new RogueConfig();
                config.Destination = INFINITE_DUNGEON_ZONE;
                config.DestinationRandomized = false;
                config.Seed = MathUtils.Rand.NextUInt64();
                config.SeedRandomized = true;
                MenuManager.Instance.AddMenu(new RogueTeamInputMenu(config), false);
            }
            else
            {
                MenuManager.Instance.AddMenu(new RogueDestMenu(), false);
            }
        }
    }
}
