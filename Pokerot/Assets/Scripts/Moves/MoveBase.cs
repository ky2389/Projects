using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Pokemon/Create new move")]
public class MoveBase : ScriptableObject
{
    [SerializeField] string name;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] PokemonType type;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] bool alwaysHits;
    [SerializeField] int pp;
    [SerializeField] int priority;

    [SerializeField] MoveCategory category;
    [SerializeField] MoveEffects effects;
    [SerializeField] List<SecondaryEffects> secondaries;
    [SerializeField] MoveTarget target;

    public string Name {
        get { return name; }
        set { name = value; }
    }

    public string Description {
        get { return description; }
        set { description = value; }
    }

    public PokemonType Type {
        get { return type; }
        set { type = value; }
    }

    public int Power {
        get { return power; }
        set { power = value; }
    }

    public int Accuracy {
        get { return accuracy; }
        set { accuracy = value; }
    }

    public bool AlwaysHits {
        get { return alwaysHits; }
        set { alwaysHits = value; }
    }

    public int PP {
        get { return pp; }
        set { pp = value; }
    }

    public int Priority {
        get { return priority; }
        set { priority = value; }
    }

    public MoveCategory Category {
        get { return category; }
        set { category = value; }
    }

    public MoveEffects Effects {
        get { return effects; }
        set { effects = value; }
    }

    public List<SecondaryEffects> Secondaries {
        get { return secondaries; }
        set { secondaries = value; }
    }

    public MoveTarget Target {
        get { return target; }
        set { target = value; }
    }
}

[System.Serializable]
public class MoveEffects
{
    [SerializeField] List<StatBoost> boosts;
    [SerializeField] ConditionID status;
    [SerializeField] ConditionID volatileStatus;

    public List<StatBoost> Boosts {
        get { return boosts; }
    }

    public ConditionID Status {
        get { return status; }
    }

    public ConditionID VolatileStatus {
        get { return volatileStatus; }
    }
}

[System.Serializable]
public class SecondaryEffects : MoveEffects
{
    [SerializeField] int chance;
    [SerializeField] MoveTarget target;

    public int Chance {
        get { return chance;  }
    }

    public MoveTarget Target {
        get { return target; }
    }
}

[System.Serializable]
public class StatBoost
{
    public Stat stat;
    public int boost;
}

public enum MoveCategory
{
    Physical, Special, Status
}

public enum MoveTarget
{
    Foe, Self
}
