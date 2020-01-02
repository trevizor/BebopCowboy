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

public class DataTypes
{



}
