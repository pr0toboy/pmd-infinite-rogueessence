using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RogueEssence.Data
{
    /// <summary>
    /// Genèse — couche méta PERMANENTE cross-run (« Fragments de lumière » + niveaux
    /// d'améliorations du Miroir), stockée dans un fichier DÉDIÉ découplé du save de run
    /// (RogueProgress) ET du MainProgress.
    ///
    /// POURQUOI : le mode roguelike (StartRogue -> RogueProgress) ne crée JAMAIS de
    /// MainProgress. Or l'ancienne « ferry » de méta (RogueProgress.EndGame) était gardée
    /// par `LoadMainGameState() != null` -> sautée faute de MainProgress -> la méta
    /// repartait à 0 à chaque run. Ce fichier est la SOURCE DE VÉRITÉ : chargé au démarrage
    /// de run (StartRogue), et RÉÉCRIT IMMÉDIATEMENT à chaque mutation (gain de Fragments,
    /// dépense / achat au Miroir) via les bindings Lua (ScriptGame.Add/Spend/SetMetaUpgrade).
    /// Une mort / un crash ne perdent donc jamais rien.
    ///
    /// EXIGENCES : écriture immédiate + ATOMIQUE (temp + rename), emplacement STABLE
    /// (SAVE/, jamais wipé — pas SAVE/ROGUE/), migration douce (fichier absent => 0).
    /// </summary>
    public static class MetaSave
    {
        private const string META_FILE = "genese_meta.json";

        public class MetaData
        {
            public int MetaCurrency;
            public Dictionary<string, int> MetaUpgrades = new Dictionary<string, int>();
        }

        private static string MetaPath()
        {
            // SAVE/ (stable) — surtout PAS SAVE/ROGUE/ qui est wipé entre les runs.
            return PathMod.ModSavePath(DataManager.SAVE_PATH, META_FILE);
        }

        /// <summary>Charge la méta permanente (0 / vide si le fichier n'existe pas).</summary>
        public static MetaData Load()
        {
            try
            {
                string path = MetaPath();
                if (!File.Exists(path))
                    return new MetaData();
                MetaData data = JsonConvert.DeserializeObject<MetaData>(File.ReadAllText(path));
                if (data == null)
                    return new MetaData();
                if (data.MetaUpgrades == null)
                    data.MetaUpgrades = new Dictionary<string, int>();
                return data;
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
                return new MetaData();
            }
        }

        /// <summary>Écrit la méta permanente de façon ATOMIQUE (temp + rename).</summary>
        public static void Save(int currency, Dictionary<string, int> upgrades)
        {
            try
            {
                MetaData data = new MetaData();
                data.MetaCurrency = currency;
                data.MetaUpgrades = new Dictionary<string, int>(upgrades ?? new Dictionary<string, int>());

                string path = MetaPath();
                string dir = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(data));
                // Rename atomique (remplace l'existant) — anti-corruption si crash en cours d'écriture.
                File.Move(tmp, path, true);
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
            }
        }

        /// <summary>Persiste la méta courante d'un GameProgress (helper des bindings).</summary>
        public static void SaveFrom(GameProgress save)
        {
            if (save == null)
                return;
            Save(save.MetaCurrency, save.MetaUpgrades);
        }
    }
}
