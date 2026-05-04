using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class SpellBuilder 
{

    private JObject spellData;

    private List<string> baseSpellIDs = new List<string>()
    {
        "arcane_bolt",
        "magic_missile",
        "arcane_blast",
        "arcane_spray"
    };

    private List<string> modiferSpellIDs = new List<string>()
    {
        "damage_amp",
        "speed_amp",
        "doubler",
        "splitter",
        "chaos",
        "homing"
    };
    
    public Spell Build(SpellCaster owner)
    {
        if (spellData == null)
        {
            Debug.LogError("Cannot build spell due to spells.json not being loaded");
            return new Spell(owner);
        }

        string baseSpellID = baseSpellIDs[Random.Range(0, baseSpellIDs.Count)];
        Spell spell = BuildBaseSpell(owner, baseSpellID);
        int modifierCount = Random.Range(0, 4);

        for (int i = 0; i < modifierCount; i++)
        {
            string modifierID = modiferSpellIDs[Random.Range(0, modiferSpellIDs.Count)];
            spell = AddModifier(owner, spell, modifierID);
        }

        return spell;
    }

   
    public SpellBuilder()
    {
        LoadSpellData();
    }


    private void LoadSpellData()
    {
        TextAsset spellJson = Resources.Load<TextAsset>("spells");

        if (spellJson == null)
        {
            Debug.LogError("Could not find spells.json in assets");
            return;
        }

        spellData = JObject.Parse(spellJson.text);
        Debug.Log("Loaded spells.json successfully");
    }

    private Spell BuildBaseSpell(SpellCaster owner, string spellID)
    {
        if (!spellData.ContainsKey(spellID))
        {
            Debug.LogWarning("Spell not found in spells.json: " + spellID);
            return new Spell(owner);
        }

        JObject attributes = (JObject)spellData[spellID];

        Spell spell = new Spell(owner);

        Debug.Log("Built sepll from json: " + attributes["name"]);


        return spell;
    }

    private Spell AddModifier(SpellCaster owner, Spell innerSpell, string modifierID)
    {
        if (!spellData.ContainsKey(modifierID))
        {
            Debug.LogWarning("Modifier not found in spells.json: " + modifierID);
            return innerSpell;
        }

        JObject attributes = (JObject)spellData[modifierID];

        Debug.Log("Added modifier: " + attributes["name"]);

        //Temp Use
        //Future reference could be like
        //return new ModifierSpell(owner, innerSpell, attributes);




        return innerSpell;
    }
}
