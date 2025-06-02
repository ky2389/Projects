using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PokeAPIPokemon
{
    public int id;
    public string name;
    public List<PokeAPIType> types;
    public List<PokeAPIStat> stats;
    public int base_experience;
    public int height;
    public int weight;
    public List<PokeAPIMove> moves;
    public PokeAPISprites sprites;
}

[Serializable]
public class PokeAPIType
{
    public int slot;
    public PokeAPITypeInfo type;
}

[Serializable]
public class PokeAPITypeInfo
{
    public string name;
    public string url;
}

[Serializable]
public class PokeAPIStat
{
    public int base_stat;
    public int effort;
    public PokeAPIStatInfo stat;
}

[Serializable]
public class PokeAPIStatInfo
{
    public string name;
    public string url;
}

[Serializable]
public class PokeAPIMove
{
    public PokeAPIMoveInfo move;
    public List<PokeAPIVersionGroupDetail> version_group_details;
}

[Serializable]
public class PokeAPIMoveInfo
{
    public string name;
    public string url;
}

[Serializable]
public class PokeAPIVersionGroupDetail
{
    public int level_learned_at;
    public PokeAPIMoveLearnMethod move_learn_method;
    public PokeAPIVersionGroup version_group;
}

[Serializable]
public class PokeAPIMoveLearnMethod
{
    public string name;
    public string url;
}

[Serializable]
public class PokeAPIVersionGroup
{
    public string name;
    public string url;
}

[Serializable]
public class PokeAPISprites
{
    public string front_default;
    public string back_default;
    public string front_shiny;
    public string back_shiny;
} 