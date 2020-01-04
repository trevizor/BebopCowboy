using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetColors
{
    public Color sky = Color.white;
    public Color cloud = Color.white;
    public Color ground = Color.white;
    public Color grass = Color.white;
    public Color rock = Color.white;
    public Color snow = Color.white;
    public Color trees = Color.white;
}

[System.Serializable]
public class SplatHeights
{
    public Texture2D texture = null;
    public float minHeight = 0.1f;
    public float maxHeight = 0.2f;
    public Vector2 tileOffset = new Vector2(0f, 0f);
    public Vector2 tileSize = new Vector2(50f, 50f);
    public float minSteepness = 0.0f;
    public float maxSteepness = 25f;
    [Tooltip("Bright area color - most prominent")]
    public Color color1 = Color.white;
    [Tooltip("Dark area color - less prominent")]
    public Color color2 = Color.white;
    public Color redTint = Color.white;
    public Color greenTint = Color.white;
    public Color blueTint = Color.white;
    public float colorThreshold = 0.5f;
}

[System.Serializable]
public class VegetationData
{
    public GameObject mesh = null;
    public float minHeight = 0.1f;
    public float maxHeight = 0.2f;
    public float minSteepness = 0.0f;
    public float maxSteepness = 25f;
    public float minScale = 0.95f;
    public float maxScale = 1.05f;
    public int maximumTrees = 5000;
    public int treeSpacing = 5;
    public float density = 0.5f;
    public Color color1 = Color.white;
    public Color color2 = Color.gray;
    public Color lightColor = Color.white;
    public float perlinCutout = 0.5f;
    public PerlinNoiseParameters perlinDist = new PerlinNoiseParameters();
}

[System.Serializable]
public class DetailData
{
    public GameObject prototype = null;
    public Texture2D prototypeTexture = null;
    public float minHeight = 0.1f;
    public float maxHeight = 0.2f;
    public float minSteepness = 0.0f;
    public float maxSteepness = 25f;
    public Vector2 heightScaleRange = new Vector2(0.9f, 1.1f);
    public Vector2 widthScaleRange = new Vector2(0.9f, 1.1f);
    public float density = 0.5f;
    public float feather = 0.05f; //random applie to min and max height to create blending
    public Color color1 = Color.white;
    public Color color2 = Color.gray;
    public Color lightColor = Color.white;
    public float perlinCutout = 0.5f;
    public PerlinNoiseParameters perlinDist = new PerlinNoiseParameters();
}

public enum ErosionType
{
    Rain = 0,
    Thermal = 1,
    Tidal = 2,
    River = 3,
    Wind = 4,
    Canyon = 5
}

enum mergeMethod
{
    Additive = 0,
    Subtract = 1,
    Replace = 3,
    ReplaceLower = 4,
    ReplaceHigher = 5,
    AddOnLower = 6,
    AddOnHigher = 7,
    AddOnThresold = 8,
    SubOnLower = 9,
    SubOnHigher = 10,
    SubOnThresold = 11
};

[System.Serializable]
public class ErosionData
{
    public ErosionType type = ErosionType.Rain;
    public int erodeRepeat = 1;
    public float erosionStrength = 0.1f;
    public int springsPerRiver = 5;
    public float solubility = 0.01f;
    public int droplets = 10;
    public int erosionSmoothAmount = 0;
    public float thermalDisplacementMultiplier = 0.01f;
    public float dropletMinHeight = 0.3f;
    public Vector2Int windDisplacement = new Vector2Int(5, 2);
    public float CanyonWidth = 2f;
    public float CanyonDisplacement = 1.5f;
    public int CanyonRandomDirection = 30;
    public Vector2Int CanyonRandomCoverage = new Vector2Int(15, 40);
    public float minHeightMerge = 0.3f;
}


[System.Serializable]
public class PerlinNoiseParameters
{
    public string name = "Perlin";
    public float perlinXScale = 0.005f; //wave scale
    public float perlinYScale = 0.005f;
    public int perlinOffsetX = 50; //wave offset
    public int perlinOffsetY = 50;
    public int perlinOctaves = 16; //number of passes 
    public float perlinScaleModifier = 1f; //scale per octave
    public float perlinPersistance = 0.4f; //persistance per octave
    public float heightReduction = 0.0f; //reduces the original value
    public float minimumClamp = 0.0f; //clamps the value after height reduction
    public float maximumClamp = 1.0f;
    public bool normalizeSize = false; //normalizes based on the height reduction
    public float heightMultiplier = 1f; //height multiplier

    public PerlinNoiseParameters(
        string _name = "Perlin",
        float _perlinXScale = 0.005f,
        float _perlinYScale = 0.005f,
        int _perlinOffsetX = 50,
        int _perlinOffsetY = 50,
        int _perlinOctaves = 16,
        float _perlinScaleModifier = 1f,
        float _perlinPersistance = 0.4f,
        float _heightReduction = 0f,
        float _minimumClamp = 0.0f,
        float _maximumClamp = 1.0f,
        bool _normalizeSize = false,
        float _heightMultiplier = 1f
        )
    {
        _name = name;
        perlinXScale = _perlinXScale;
        perlinYScale = _perlinYScale;
        perlinOffsetX = _perlinOffsetX;
        perlinOffsetY = _perlinOffsetY;
        perlinOctaves = _perlinOctaves;
        perlinScaleModifier = _perlinScaleModifier;
        perlinPersistance = _perlinPersistance;
        heightReduction = _heightReduction;
        minimumClamp = _minimumClamp;
        maximumClamp = _maximumClamp;
        normalizeSize = _normalizeSize;
        heightMultiplier = _heightMultiplier;
    }
}


[System.Serializable]
public class VoronoiParameters
{
    public string name = "coronoi";
    public float minHeight = 0f;
    public float maxHeight = 1f;
    public float falloff = 1f; //smaller numbers make the slope gentler, higher make it steeper
    public float maxDistanceMultiplier = 1f; //needs more testing to check what exactly changes
    public int minInstances = 0;
    public int maxInstances = 3;
    public bool powerFallout = false;
    public float powerValue = 1f;
    public bool subtract = false;


    public VoronoiParameters(
        string _name = "cornoi",
        float _minHeight = 0f,
        float _maxHeight = 1f,
        float _falloff = 1f,
        float _maxDistanceMultiplier = 1f,
        int _minInst = 0,
        int _maxInst = 1,
        bool _powerFallout = false,
        float _powerValue = 1f
        )
    {
        name = _name;
        minHeight = _minHeight;
        maxHeight = _maxHeight;
        falloff = _falloff;
        maxDistanceMultiplier = _maxDistanceMultiplier;
        minInstances = _minInst;
        maxInstances = _maxInst;
        powerFallout = _powerFallout;
        powerValue = _powerValue;
    }


}

