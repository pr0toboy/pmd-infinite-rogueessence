using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RogueEssence
{
    public class TitleScene : BaseScene
    {
        const int ENTER_WAIT_TIME = 90;
        const int ENTER_FLASH_TIME = 60;

        // Séquence d'intro (Genèse) : suite d'images plein écran jouée AVANT le
        // menu, façon EoS. Frames brutes livrées dans le mod (Content/Intro/N.png)
        // et chargées au runtime — aucune dépendance au pipeline d'assets .dir.
        const string INTRO_PATTERN = "Content/Intro/{0}.png";
        const int INTRO_FRAME_HOLD = 5;   // ticks-logiques (60 fps) par image -> ~12 fps
        const int INTRO_HEIGHT = 184;     // letterbox 16:9 dans l'écran 320x240

        private bool hideTitle;
        private ulong startTime;

        private bool introDone;
        private ulong introStart;
        private List<Texture2D> introFrames;

        public static List<IInteractable> TitleMenuSaveState;

        public TitleScene(bool hideTitle) : base()
        {
            this.hideTitle = hideTitle;
        }

        public override void Exit()
        {
            disposeIntro();
        }

        public override void Begin()
        {
            //set up title, fade, and start music
            GameManager.Instance.BGM(GraphicsManager.TitleBGM, true);
            startTime = GraphicsManager.TotalFrameTick;

            //jouer l'intro seulement à l'arrivée fraîche sur le titre (pas au retour du menu)
            introDone = hideTitle;
            if (!introDone)
                loadIntro();
        }

        private void loadIntro()
        {
            introFrames = new List<Texture2D>();
            try
            {
                while (true)
                {
                    string path = PathMod.ModPath(String.Format(INTRO_PATTERN, introFrames.Count));
                    if (!File.Exists(path))
                        break;
                    using (FileStream stream = File.OpenRead(path))
                        introFrames.Add(BaseSheet.ImportTex(stream));
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error loading title intro frames\n", ex));
            }
            if (introFrames.Count == 0)
                introDone = true;
            else
                introStart = GraphicsManager.TotalFrameTick;
        }

        private int introIndex()
        {
            ulong elapsed = (GraphicsManager.TotalFrameTick - introStart) / (ulong)FrameTick.FrameToTick(1);
            return (int)(elapsed / INTRO_FRAME_HOLD);
        }

        private void endIntro()
        {
            disposeIntro();
            introDone = true;
            //repartir le minutage du « appuyez sur start » après l'intro
            startTime = GraphicsManager.TotalFrameTick;
        }

        private void disposeIntro()
        {
            if (introFrames == null)
                return;
            foreach (Texture2D tex in introFrames)
            {
                if (tex != null)
                    tex.Dispose();
            }
            introFrames = null;
        }

        public override IEnumerator<YieldInstruction> ProcessInput()
        {
            if (!introDone)
            {
                bool skip = GameManager.Instance.InputManager.AnyKeyPressed() || GameManager.Instance.InputManager.AnyButtonPressed();
                if (skip || introIndex() >= introFrames.Count)
                {
                    if (skip)
                        GameManager.Instance.SE("Menu/Confirm");
                    endIntro();
                }
                yield return new WaitForFrames(1);
                yield break;
            }
            if (!hideTitle && GameManager.Instance.InputManager.AnyKeyPressed() || GameManager.Instance.InputManager.AnyButtonPressed())
            {
                GameManager.Instance.SE("Menu/Confirm");
                hideTitle = true;
            }
            if (hideTitle)
            {
                DataManager.Instance.SetProgress(null);
                DataManager.Instance.LoadProgress();
                if (TitleMenuSaveState == null)
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine(new TopMenu()));
                else
                {
                    List<IInteractable> save = TitleMenuSaveState;
                    TitleMenuSaveState = null;
                    MenuManager.Instance.LoadMenuState(save);
                    yield return CoroutineManager.Instance.StartCoroutine(MenuManager.Instance.ProcessMenuCoroutine());
                }
            }
            else
                yield return new WaitForFrames(1);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float window_scale = GraphicsManager.WindowZoom;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(new Vector3(window_scale, window_scale, 1)));

            if (!introDone && introFrames != null && introFrames.Count > 0)
            {
                //l'écran est déjà nettoyé en noir -> bandes letterbox gratuites
                int idx = introIndex();
                if (idx < 0) idx = 0;
                if (idx >= introFrames.Count) idx = introFrames.Count - 1;
                Rectangle dest = new Rectangle(0, (GraphicsManager.ScreenHeight - INTRO_HEIGHT) / 2, GraphicsManager.ScreenWidth, INTRO_HEIGHT);
                spriteBatch.Draw(introFrames[idx], dest, Color.White);
                spriteBatch.End();
                return;
            }

            BaseSheet bg = GraphicsManager.GetBackground(GraphicsManager.TitleBG);
            bg.Draw(spriteBatch, new Vector2(), null);

            if (!hideTitle)
            {
                BaseSheet title = GraphicsManager.Title;
                title.Draw(spriteBatch, new Vector2(GraphicsManager.ScreenWidth / 2 - title.Width / 2, 0), null);

                if ((GraphicsManager.TotalFrameTick - startTime) > (ulong)FrameTick.FrameToTick(ENTER_WAIT_TIME)
                    && ((GraphicsManager.TotalFrameTick - startTime) / (ulong)FrameTick.FrameToTick(ENTER_FLASH_TIME / 2)) % 2 == 0)
                {
                    BaseSheet subtitle = GraphicsManager.Subtitle;
                    subtitle.Draw(spriteBatch, new Vector2(GraphicsManager.ScreenWidth / 2 - subtitle.Width / 2, GraphicsManager.ScreenHeight * 3 / 4), null);
                }
            }
            spriteBatch.End();
        }

    }
}
