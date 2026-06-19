using System;
using System.IO;
using System.Collections.Generic;
using RogueEssence.Content;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueElements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RogueEssence
{
    /// <summary>
    /// Cinématique de FIN (PMD Genèse), jouée après le boss final Arceus Original.
    /// Rendue AU RUNTIME (pas de frames bakées) : 2 décors (nuit/jour) + sprites
    /// (bêtas fixes + ÉQUIPE RÉELLE lue depuis la save) + effets + captions + BGM.
    /// Chorégraphie de référence : PMDC/tools/gen_outro.py (scènes A/B/C/D, 12 fps).
    ///
    /// Lancée par GAME:PlayEndCinematic (pont ScriptGame -> new EndScene()).
    /// Avancement TEMPS dans Update() (comme l'intro de TitleScene) ; sortie via le
    /// contrat EndCinematicReturn (retour village + épilogue Mikon).
    ///
    /// Repère espace : la choré vit en 320x184 (comme gen_outro). On letterbox dans
    /// l'écran réel : LB = (ScreenHeight - CINE_H)/2. Un point choré (cx, cy) -> logique
    /// écran (cx, cy + LB). Les sprites sont mis à l'échelle par passe (matrice spriteBatch).
    /// </summary>
    public class EndScene : BaseScene
    {
        // 12 fps logiques : 5 ticks (60 fps) par frame de chorégraphie, comme l'intro.
        const int FRAME_HOLD = 5;

        // Bornes de scènes (gen_outro : 78 + 138 + 132 + 180 = 528).
        const int A_LEN = 78, B_LEN = 138, C_LEN = 132, D_LEN = 180;
        const int A_END = A_LEN;                 // 78
        const int B_END = A_END + B_LEN;         // 216
        const int C_END = B_END + C_LEN;         // 348
        const int TOTAL_FRAMES = C_END + D_LEN;  // 528

        // Espace chorégraphie (gen_outro W/H).
        const int CINE_W = 320, CINE_H = 184;

        // BGM d'ending (asset Content/Music/'Dont Ever Forget.ogg', fourni par manaphy).
        const string ENDING_BGM = "Dont Ever Forget";

        // Décors (mod). Chargés au runtime, resize vers la bande letterbox.
        const string BG_NIGHT = "Content/Outro/bg_night.png";
        const string BG_DAY = "Content/Outro/bg_day.png";
        // Arceus = sprite STATIQUE (apparaît/scale/brille, pas d'anim). On contourne
        // le .chara (493.chara = 4.18 Mo / 20 formes, échoue au décodage) via ce PNG
        // dédié (Normal Arceus, extrait de RawAsset/0493/0000 par manaphy).
        const string ARCEUS_TEX = "Content/Outro/arceus.png";
        // Glow radial (128x128, blanc->transparent) blitté en ADDITIF sur Arceus pendant
        // la charge (scène B) = brillance "divine" sans bidouiller la luminosité (asset manaphy).
        const string ARCEUS_GLOW = "Content/Outro/arceus_glow.png";

        // Échelles (gen_outro : Arceus 2.3 base, bêtas 1.3, équipe 1.9).
        const float K_ARCEUS = 2.3f, K_BETA = 1.3f, K_TEAM = 1.9f;

        // Positions chorégraphie (gen_outro). Bêtas = rangée arrière ; équipe = avant-plan.
        static readonly int[] XB = { 78, 110, 142, 178, 210, 242 };
        const int YB = CINE_H - 30;   // 154 (bas des bêtas)
        static readonly int[] XT = { 120, 155, 190, 225 };
        const int YT = CINE_H - 16;   // 168 (bas de l'équipe)

        static readonly Color CAP_COLOR = new Color(248, 230, 190);

        // Bêtas (MonsterID FIXES). species = id in-engine en minuscules (PAS le dex
        // SpriteCollab de gen_outro) ; species/forms confirmés par manaphy (FormName).
        private List<MonsterID> betas;
        private List<MonsterID> team;   // équipe réelle (ActiveTeam)

        private Texture2D bgNight, bgDay, arceusTex, glowTex;
        private int idleAnim, hopAnim;

        private ulong startTime;
        private bool done;

        public EndScene() : base()
        {
        }

        public override void Begin()
        {
            startTime = GraphicsManager.TotalFrameTick;
            done = false;

            GameManager.Instance.BGM(ENDING_BGM, true);

            string skin = DataManager.Instance.DefaultSkin;
            betas = new List<MonsterID>
            {
                new MonsterID("missingno", 7, skin, Gender.Genderless), // Mikon (FormName "Mikon")
                new MonsterID("missingno", 9, skin, Gender.Genderless), // Hakogame
                new MonsterID("hoppip",    2, skin, Gender.Genderless), // Beta Hoppip
                new MonsterID("sunkern",   1, skin, Gender.Genderless), // Beta Sunkern
                new MonsterID("probopass", 1, skin, Gender.Genderless), // Beta Probopass
                new MonsterID("hawlucha",  2, skin, Gender.Genderless), // Beta Hawlucha
            };
            // Équipe RÉELLE (option a validée par Alexis) : on lit ActiveTeam.
            team = new List<MonsterID>();
            if (DataManager.Instance.Save != null && DataManager.Instance.Save.ActiveTeam != null)
            {
                foreach (Character chara in DataManager.Instance.Save.ActiveTeam.Players)
                {
                    team.Add(chara.Appearance);
                    if (team.Count >= XT.Length)
                        break;
                }
            }

            idleAnim = GraphicsManager.IdleAction;
            hopAnim = GraphicsManager.GetAnimIndex("Hop");
            if (hopAnim < 0)
                hopAnim = idleAnim;

            bgNight = tryLoadTex(BG_NIGHT);
            bgDay = tryLoadTex(BG_DAY);
            arceusTex = tryLoadTex(ARCEUS_TEX);
            glowTex = tryLoadTex(ARCEUS_GLOW);
        }

        private static Texture2D tryLoadTex(string modPath)
        {
            try
            {
                string path = PathMod.ModPath(modPath);
                if (!File.Exists(path))
                    return null;
                using (FileStream stream = File.OpenRead(path))
                    return BaseSheet.ImportTex(stream);
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(new Exception("Error loading outro background " + modPath + "\n", ex));
                return null;
            }
        }

        public override void Exit()
        {
            if (bgNight != null) { bgNight.Dispose(); bgNight = null; }
            if (bgDay != null) { bgDay.Dispose(); bgDay = null; }
            if (arceusTex != null) { arceusTex.Dispose(); arceusTex = null; }
            if (glowTex != null) { glowTex.Dispose(); glowTex = null; }
        }

        /// <summary>Frame de chorégraphie courante (0..TOTAL_FRAMES), avancée par le temps.</summary>
        private int CurrentFrame()
        {
            ulong elapsed = (GraphicsManager.TotalFrameTick - startTime) / (ulong)FrameTick.FrameToTick(FRAME_HOLD);
            return (int)elapsed;
        }

        public override void Update(FrameTick elapsedTime)
        {
            if (CurrentFrame() >= TOTAL_FRAMES)
                done = true;
        }

        public override IEnumerator<YieldInstruction> ProcessInput()
        {
            // Skippable : n'importe quelle touche termine la cinématique.
            if (GameManager.Instance.InputManager.AnyKeyPressed() || GameManager.Instance.InputManager.AnyButtonPressed())
            {
                GameManager.Instance.SE("Menu/Confirm");
                done = true;
            }

            if (done)
            {
                // Contrat de sortie (golemastoc) : retour village + épilogue Mikon.
                GameManager.Instance.SceneOutcome = GameManager.Instance.EndCinematicReturn();
                yield break;
            }

            yield return new WaitForFrames(1);
        }

        // ----- helpers -------------------------------------------------------

        private static float Ease(float t)
        {
            return t * t * (3f - 2f * t);
        }

        // Enveloppe d'alpha des captions (gen_outro fade_a).
        private static float FadeA(float p, float fin, float fout)
        {
            if (p < fin) return p / fin;
            if (p > fout) return Math.Max(0f, 1f - (p - fout) / (1f - fout));
            return 1f;
        }

        private static int LB()
        {
            return (GraphicsManager.ScreenHeight - CINE_H) / 2;
        }

        // Dessine un décor plein (resize vers la bande letterbox 320x184).
        private void drawBg(SpriteBatch sb, Texture2D tex, Color color)
        {
            if (tex == null)
                return;
            Rectangle dest = new Rectangle(0, LB(), GraphicsManager.ScreenWidth, CINE_H);
            sb.Draw(tex, dest, color);
        }

        // Rect plein en coords logiques (x en espace choré ; y -> +LB).
        private void fillRect(SpriteBatch sb, float x, float y, float w, float h, Color color)
        {
            GraphicsManager.Pixel.Draw(sb, new Rectangle((int)Math.Round(x), (int)Math.Round(y + LB()), Math.Max(1, (int)Math.Round(w)), Math.Max(1, (int)Math.Round(h))), null, color);
        }

        // Overlay plein écran (flash blanc / fondu noir).
        private void fullOverlay(SpriteBatch sb, Color color)
        {
            GraphicsManager.Pixel.Draw(sb, new Rectangle(0, 0, GraphicsManager.ScreenWidth, GraphicsManager.ScreenHeight), null, color);
        }

        // Ligne (fissure) : rect 1px pivoté. p0 en coords choré.
        private void drawLine(SpriteBatch sb, float x0, float y0, float len, float angleRad, Color color)
        {
            GraphicsManager.Pixel.Draw(sb, new Vector2(x0, y0 + LB()), new Rectangle(0, 0, 1, 1),
                new Vector2(0f, 0.5f), color, new Vector2(len, 1f), angleRad, SpriteEffects.None);
        }

        // Sprite mis à l'échelle k autour d'une ancre (centre-x, bas) en coords choré.
        private void drawActor(SpriteBatch sb, float z, MonsterID mon, int anim, Dir8 dir, float cx, float cyBottom, float k, float alpha)
        {
            if (alpha <= 0f)
                return;
            CharSheet sheet = GraphicsManager.GetChara(mon.ToCharID());
            if (sheet == null)
                return;
            float topLeftX = cx - sheet.TileWidth * k / 2f;
            float topLeftY = (cyBottom + LB()) - sheet.TileHeight * k;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null,
                Matrix.CreateScale(new Vector3(z * k, z * k, 1)));
            sheet.DrawChar(sb, anim, false, dir, new Vector2(topLeftX / k, topLeftY / k), CharSheet.DefaultFrame, Color.White * alpha);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null,
                Matrix.CreateScale(new Vector3(z, z, 1)));
        }

        // Arceus = texture PNG dédiée (contournement du .chara qui échoue au décodage),
        // ancre top-left en coords choré, échelle k. Dessiné dans la passe de base
        // (matrice z) : le scale k s'applique via spriteBatch.Draw.
        private void drawArceus(SpriteBatch sb, float tlx, float tly, float k, float alpha)
        {
            if (arceusTex == null || alpha <= 0f)
                return;
            sb.Draw(arceusTex, new Vector2(tlx, tly + LB()), null, Color.White * alpha, 0f, Vector2.Zero, k, SpriteEffects.None, 0f);
        }

        // Glow radial ADDITIF centré sur (cx, cy) coords choré. Bascule en BlendState.Additive
        // le temps du blit (brillance "divine"), puis restaure la passe AlphaBlend de base.
        private void drawGlow(SpriteBatch sb, float z, float cx, float cy, float scale, float alpha)
        {
            if (glowTex == null || alpha <= 0f)
                return;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, null,
                Matrix.CreateScale(new Vector3(z, z, 1)));
            sb.Draw(glowTex, new Vector2(cx, cy + LB()), null, Color.White * alpha, 0f,
                new Vector2(glowTex.Width / 2f, glowTex.Height / 2f), scale, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null,
                Matrix.CreateScale(new Vector3(z, z, 1)));
        }

        // Bêtas (dos/face) + équipe, anim donnée, alpha + décalage shake.
        private void drawGroups(SpriteBatch sb, float z, int anim, Dir8 dir, float ab, float at, float shake)
        {
            for (int i = 0; i < betas.Count && i < XB.Length; i++)
                drawActor(sb, z, betas[i], anim, dir, XB[i] + shake, YB, K_BETA, ab);
            for (int i = 0; i < team.Count && i < XT.Length; i++)
                drawActor(sb, z, team[i], anim, dir, XT[i] + shake, YT, K_TEAM, at);
        }

        private void drawCaption(SpriteBatch sb, string text, float alpha, bool big)
        {
            if (string.IsNullOrEmpty(text) || alpha <= 0f)
                return;
            FontSheet font = GraphicsManager.TextFont;
            Color col = CAP_COLOR * alpha;
            string[] lines = text.Split('\n');
            int lineH = font.CharHeight + 2;
            int totalH = lines.Length * lineH;
            // big -> centré verticalement ; sinon vers le bas de la bande choré.   // VERIFY taille "big"
            int y0 = big ? (LB() + (CINE_H - totalH) / 2) : (LB() + CINE_H - 16 - totalH);
            for (int i = 0; i < lines.Length; i++)
            {
                int w = font.SubstringWidth(lines[i]);
                int x = (GraphicsManager.ScreenWidth - w) / 2;
                font.DrawText(sb, x, y0 + i * lineH, lines[i], null, DirV.Up, DirH.Left, col);
            }
        }

        // ----- rendu ---------------------------------------------------------

        public override void Draw(SpriteBatch spriteBatch)
        {
            float z = GraphicsManager.WindowZoom;
            int f = CurrentFrame();
            if (f < 0) f = 0;
            if (f >= TOTAL_FRAMES) f = TOTAL_FRAMES - 1;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null,
                Matrix.CreateScale(new Vector3(z, z, 1)));

            if (f < A_END)
                drawSceneA(spriteBatch, z, f);
            else if (f < B_END)
                drawSceneB(spriteBatch, z, f - A_END);
            else if (f < C_END)
                drawSceneC(spriteBatch, z, f - B_END);
            else
                drawSceneD(spriteBatch, z, f - C_END);

            spriteBatch.End();
        }

        // A : bêtas + équipe (de dos) regardent Arceus apparaître. (nuit)
        private void drawSceneA(SpriteBatch sb, float z, int i)
        {
            float p = i / (float)(A_LEN - 1);
            drawBg(sb, bgNight, Color.White);

            float a = Ease(Math.Min(1f, p * 1.4f));   // Arceus fade-in
            float arcW = arceusTex != null ? arceusTex.Width * K_ARCEUS : 0;
            drawArceus(sb, (CINE_W - arcW) / 2f, 14, K_ARCEUS, a);

            drawGroups(sb, z, idleAnim, Dir8.Up, 1f, 1f, 0f);

            drawCaption(sb, "Ensemble, ils ont fait face.", FadeA(p, 0.18f, 0.85f), false);
        }

        // B : Arceus détruit tout : montée/charge, piliers, fissures, onde, fondu blanc. (nuit)
        private void drawSceneB(SpriteBatch sb, float z, int i)
        {
            float p = i / (float)(B_LEN - 1);
            float shake = p > 0.3f ? (float)(4 * p * Math.Sin(i * 1.3)) : 0f;

            drawBg(sb, bgNight, Color.White);

            // Arceus grandit + monte (brillance approximée). // VERIFY brillance/halo de charge
            float k = K_ARCEUS + 1.3f * Ease(Math.Min(1f, p * 1.2f));
            float arcW = arceusTex != null ? arceusTex.Width * k : 0;
            float arcY = Math.Max(-10f, 14f - 26f * p);
            drawArceus(sb, (CINE_W - arcW) / 2f + shake, arcY, k, 1f);

            // Halo de charge "divin" : glow PNG additif centré sur Arceus, scale + alpha
            // croissants pendant la charge (remplace l'ancien carré blanc approx).
            float arcH = (arceusTex != null ? arceusTex.Height : 44) * k;
            float gp = Ease(p);
            drawGlow(sb, z, CINE_W / 2f + shake, arcY + arcH / 2f, 0.6f + 1.1f * gp, 0.35f + 0.6f * gp);

            // Piliers de lumière (Jugement) p>0.35.
            if (p > 0.35f)
            {
                float pj = (p - 0.35f) / 0.65f;
                float w = 2f + 10f * pj;
                int[] px = { 50, 120, 200, 270 };
                foreach (int x in px)
                    fillRect(sb, x - w / 2f + shake, 0, w, CINE_H, new Color(255, 248, 220));
            }

            // Fissures du sol (rayons depuis cx, cy).
            float cx = CINE_W / 2f, cy = CINE_H - 22;
            int[] angs = { 200, 225, 250, 270, 290, 315, 340 };
            float L = 170f * Ease(p);
            foreach (int ang in angs)
            {
                double rad = ang * Math.PI / 180.0;
                // gen_outro aplatit le y *0.45 ; ici longueur pleine selon l'angle. // VERIFY aplatissement
                drawLine(sb, cx + shake, cy, L, (float)rad, new Color(255, 240, 180));
            }

            // Onde de choc circulaire p>0.45 : anneau ellipse non primitif ici. // VERIFY anneau exact

            // Bêtas + équipe (de dos) engloutis (fade out).
            float af = Math.Max(0f, 1f - p * 1.25f);
            drawGroups(sb, z, idleAnim, Dir8.Up, af, af, shake);

            // Tout englouti par le blanc (p>0.7).
            if (p > 0.7f)
                fullOverlay(sb, Color.White * Ease((p - 0.7f) / 0.3f));

            string cap = (0.15f < p && p < 0.55f) ? "Le créateur efface son brouillon."
                       : (0.55f <= p && p < 0.72f) ? "Et leur monde s'effaça…" : null;
            if (cap != null)
                drawCaption(sb, cap, FadeA(p, 0.16f, 0.72f), false);
        }

        // C : téléport vers le vrai monde : blanc -> aube, faisceaux, réapparition (Hop). (jour)
        private void drawSceneC(SpriteBatch sb, float z, int i)
        {
            float p = i / (float)(C_LEN - 1);
            float e = Ease(Math.Min(1f, p * 1.3f));

            drawBg(sb, bgDay, Color.White);
            // sky = blend(blanc, jour, e) -> overlay blanc d'alpha (1-e).
            if (e < 1f)
                fullOverlay(sb, Color.White * (1f - e));

            // Faisceaux de téléport (colonnes lumineuses) sur chaque x.
            float ba = 70f * Math.Max(0f, 1f - Math.Abs(p - 0.5f) * 2f);
            if (ba > 0f)
            {
                Color beam = new Color(255, 250, 220) * (ba / 255f);
                foreach (int x in XB) fillRect(sb, x - 9, 0, 18, CINE_H, beam);
                foreach (int x in XT) fillRect(sb, x - 9, 0, 18, CINE_H, beam);
            }

            // Réapparition joyeuse (Hop), alpha croissant.
            float ap = Ease(Math.Max(0f, (p - 0.35f) / 0.65f));
            drawGroups(sb, z, hopAnim, Dir8.Down, ap, ap, 0f);

            drawCaption(sb, "Mais eux, il les épargna.", FadeA(p, 0.18f, 0.85f), false);
        }

        // D : clôture : les oubliés accueillis ; "On a sauvé les siens" ; fondu noir + FIN. (jour)
        private void drawSceneD(SpriteBatch sb, float z, int i)
        {
            int seg = D_LEN / 3;   // 60

            drawBg(sb, bgDay, Color.White);
            drawGroups(sb, z, hopAnim, Dir8.Down, 1f, 1f, 0f);

            if (i < seg)
            {
                float t = i / (float)seg;
                drawCaption(sb, "Les imparfaits, les oubliés —\nun monde a fini par les accueillir.", FadeA(t, 0.18f, 0.85f), false);
            }
            else if (i < 2 * seg)
            {
                float t = (i - seg) / (float)seg;
                drawCaption(sb, "On a sauvé les siens.", FadeA(t, 0.18f, 0.85f), false);
            }
            else
            {
                float t = (i - 2 * seg) / (float)(D_LEN - 2 * seg);
                fullOverlay(sb, Color.Black * Ease(Math.Min(1f, t * 1.5f)));
                drawCaption(sb, t < 0.55f ? "Genèse" : "FIN", FadeA(t, 0.12f, 0.9f), true);
            }
        }
    }
}
