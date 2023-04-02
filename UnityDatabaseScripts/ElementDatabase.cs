using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ElementDatabase
{
    //The element property, shows what element is strong and weak against;
    [System.Serializable]
    public class ElementPropertiesPlayFab
    {
        public Elements element;
        public bool ineffectiveAgainstSelf = true;
        public List<Elements> strongAgainst;
        public List<Elements> weakAgainst;
    }

    [System.Serializable]
    public class ElementModifier
    {
        public Elements TargetElement;
        public float DamageModifier;
    }

    [System.Serializable]
    public class ElementProperties_Prototype
    {
        public Elements element;
        public List<ElementModifier> ElementModifier;
    }

    public class ElementPropDatabasePlayFabList
    {
        public List<ElementPropertiesPlayFab> elementPropDatabasePlayFabList;

    }

    public enum Elements
    {
        Null,
        Ordinary,
        Fire,
        Water,
        Electric,
        Earth,
        Wind,
        Frost,
        Crystal,
        Nature,
        Brawler,
        Spirit,
        Magic,
        Psychic,
        Reptile,
        Toxic,
        Item
    }
    public List<ElementPropertiesPlayFab> elementPropDatabasePlayFab;
    public List<ElementProperties_Prototype> elementProp_Prototype;

    //Returns the value modifier of a particular element. Either -2, 0 or 2
    // -2 should cut the value by 1/2 while 2 should double the value
    public int FindElementValueModifier(Elements monsterBaseElement, Elements elementInput)
    {
        //For each element, find the element properties and check which value to return based on elementInput
        ElementPropertiesPlayFab elementProperties = FindElementProperties(monsterBaseElement);

        // Debug.Log(monsterBaseElement + "/" + elementInput + "/" + elementProperties.strongAgainst.Contains(elementInput) + "/" + elementProperties.weakAgainst.Contains(elementInput));

        //If the element is stronger than the input +2
        if (elementProperties.strongAgainst.Contains(elementInput))
        {
            return 2;
        }
        
        //If the element is weaker than the input -2
        if (elementProperties.weakAgainst.Contains(elementInput))
        {
            return -2;
        }
        //If the element is weaker against itself return -2
        
        if (elementProperties.ineffectiveAgainstSelf)
        {
           return -2;
        }

        return 0;
    }

    public static float FindElementValueModifier_Prototype(Elements targetMonsterBaseType, Elements usedSkillElement)
    {
        ElementProperties_Prototype skillElementProperties = FindElementProperties_Prototype(usedSkillElement);

        foreach(var elementModifier in skillElementProperties.ElementModifier)
        {
            //Check if target Monster's Type exist in elementModifier from the Skill Element
            if(elementModifier.TargetElement == targetMonsterBaseType)
            {
                return elementModifier.DamageModifier/100f;
            }
        }

        //If not found then
        return 1f;
    }


    //Returns the input element properties
    public ElementPropertiesPlayFab FindElementProperties(Elements element)
    {
        foreach (var elementProperty in elementPropDatabasePlayFab)
        {
            if (element == elementProperty.element)
            {
                return elementProperty;
            }
        }
        return null;
    }

    public static ElementProperties_Prototype FindElementProperties_Prototype(Elements element)
    {
        var ElementDatabaseJsonString = ElementDatabaseJson.ElementDataJson;
        var elementProp_Prototype = JsonConvert.DeserializeObject<List<ElementProperties_Prototype>>(ElementDatabaseJsonString);

        foreach (var elementProperty in elementProp_Prototype)
        {
            if (element == elementProperty.element)
                return elementProperty;
        }
        return null;
    }


    //public void SpawnElement(GameObject parentSkillContainer, SkillsDataBase.SkillInfo skillInfo)
    //{
    //    //Empty the canvas
    //    UtilityCanvas.instances.EmptyCanvas(parentSkillContainer);

    //    //Spawn the canvas

    //    //Logic for setting the data value
    //    var boxElement = spawnedPrefab.GetComponent<CCBoxElement>();
    //    foreach (var skill in skillInfo.ele)
    //    {
    //        //Sets the value of the child prefab
    //        var spawnedPrefab = UtilityCanvas.instances.SpawnCanvas(ccBoxElement, parentSkillContainer);

    //    }
    //    boxElement.setValue(skillInfo.skillElement);

    //}
}
