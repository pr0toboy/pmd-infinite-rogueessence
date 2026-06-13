using RogueEssence.Dev;
using RogueEssence.Dungeon;
using System;
using System.Collections.Generic;

namespace RogueEssence.Data
{
    [Serializable]
    public class StartParams
    {
        public List<StartChar> Chars;
        public int Personality;
        public ZoneLoc Map;
        public int Level;
        public int MaxLevel;
        /// <summary>
        /// Niveau de référence pour le CALCUL DES STATS (normaliseur des formules).
        /// Découplé de MaxLevel (= plafond de niveau atteignable) pour permettre de
        /// dépasser le niv100 SANS rescaler/nerfer tout l'équilibre : les stats
        /// restent calées sur StatLevel pendant que MaxLevel peut monter (un niveau
        /// > StatLevel donne donc des stats proportionnellement plus hautes).
        /// Par défaut = MaxLevel (comportement vanilla) si absent du XML.
        /// </summary>
        public int StatLevel;
        public List<string> Teams;
    }


    [Serializable]
    public class StartChar
    {
        [MonsterID(0, false, false, true, true)]
        public MonsterID ID;
        public string Name;

        public StartChar()
        {
            Name = "";
        }
        public StartChar(MonsterID id, string name)
        {
            ID = id;
            Name = name;
        }

        public override string ToString()
        {
            return ID.ToString();
        }
    }
}
