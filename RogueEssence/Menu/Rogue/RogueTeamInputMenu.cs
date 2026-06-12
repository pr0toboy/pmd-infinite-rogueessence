using RogueElements;
using Microsoft.Xna.Framework.Input;
using RogueEssence.Data;

namespace RogueEssence.Menu
{
    public class RogueTeamInputMenu : TextInputMenu
    {
        private const string INFINITE_DUNGEON_ZONE = "infinite_dungeon";

        public override int MaxLength { get { return 96; } }
        private RogueConfig config;
        private bool randomized;

        public RogueTeamInputMenu(RogueConfig config) : this(MenuLabel.ROGUE_TEAM_NAME_MENU, config) { }
        public RogueTeamInputMenu(string label, RogueConfig config)
        {
            Label = label;
            this.config = config;
            Initialize(RogueEssence.Text.FormatKey("INPUT_TEAM_TITLE"), RogueEssence.Text.FormatKey("INPUT_TEAM_DESC"), 256);
        }

        public override void Update(InputManager input)
        {
            if (input.BaseKeyPressed(Keys.Tab))
            {
                randomized = true;
                //tab will replace the current line with a suggestion
                GameManager.Instance.SE("Menu/Skip");
                Text.SetText(DataManager.Instance.Start.Teams[MathUtils.Rand.Next(DataManager.Instance.Start.Teams.Count)]);

                UpdatePickerPos();
            }
            else
            {
                string prevText = Text.Text;
                base.Update(input); 
                if (prevText != Text.Text)
                {
                    randomized = false;
                }
            }

        }

        protected override void Confirmed()
        {
            if (Text.Text != "" && Text.Text.Trim() == "")
            {
                GameManager.Instance.SE("Menu/Cancel");
                return;
            }

            GameManager.Instance.SE("Menu/Confirm");
            if (Text.Text == "")
            {
                randomized = true;
                Text.SetText(DataManager.Instance.Start.Teams[MathUtils.Rand.Next(DataManager.Instance.Start.Teams.Count)]);
                UpdatePickerPos();
            }

            config.TeamName = Text.Text;
            config.TeamRandomized = randomized;
            // Méta-progression : à la reprise (checkpoint débloqué + équipe conservée),
            // pas de nouveau starter — StartRogue restaure l'ancienne équipe et
            // démarre au segment du checkpoint.
            if (RogueProgress.CanResumeAtCheckpoint(config.Destination))
            {
                MenuManager.Instance.ClearMenus();
                GameManager.Instance.SceneOutcome = RogueProgress.StartRogue(config);
            }
            else if (config.Destination == INFINITE_DUNGEON_ZONE && !DiagManager.Instance.DevMode)
                // Fangame (joueur) : quiz de sélection par type (pool 45 figé).
                // En -dev on garde l'écran de choix complet (CharaChoiceMenu /
                // GetStartersList) pour tester n'importe quelle espèce comme starter.
                MenuManager.Instance.AddMenu(new StarterTypeMenu(config), false);
            else
                MenuManager.Instance.AddMenu(new CharaChoiceMenu(config), false);
        }
    }
}
