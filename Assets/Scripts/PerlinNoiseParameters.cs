using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseParameters
{
    public string name = "Perlin";
    public float perlinXScale = 0.005f;
    public float perlinYScale = 0.005f;
    public int perlinOffsetX = 50;
    public int perlinOffsetY = 50;
    public int perlinOctaves = 16;
    public float perlinScaleModifier = 1f;
    public float perlinPersistance = 0.4f;
    public float perlinHeightScale = 0.02f;
    public float heightReduction = 0.0f;
    public float minimumClamp = 0.0f;
    public float maximumClamp = 1.0f;
    public bool normalizeSize = false;
    public float heightMultiplier = 1f;

    public PerlinNoiseParameters(
        string _name = "Perlin",
        float _perlinXScale = 0.005f,
        float _perlinYScale = 0.005f,
        int _perlinOffsetX = 50,
        int _perlinOffsetY = 50,
        int _perlinOctaves = 16,
        float _perlinScaleModifier = 1f,
        float _perlinPersistance = 0.4f,
        float _perlinHeightScale = 0.02f,
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
        perlinHeightScale = _perlinHeightScale;
        heightReduction = _heightReduction;
        minimumClamp = _minimumClamp;
        maximumClamp = _maximumClamp;
        normalizeSize = _normalizeSize;
        heightMultiplier = _heightMultiplier;
    }
}
