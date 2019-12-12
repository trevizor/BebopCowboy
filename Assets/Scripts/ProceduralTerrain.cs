﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{

    //public Vector2 heightRange = new Vector2(0.1f, 0.2f);
    public Terrain terrain;
    public TerrainData terrainData;

    //----------- perlin noise
    private float perlinXScale = 0.005f;
    private float perlinYScale = 0.005f;
    private int perlinOffsetX = 50;
    private int perlinOffsetY = 50;
    private int perlinOctaves = 16;
    private float perlinPersistance = 0.4f;
    private float perlinHeightScale = 0.02f;


    public List<PerlinNoiseParameters> PerlinList = new List<PerlinNoiseParameters>();

    // Start is called before the first frame update
    void Start()
    {
        terrain = this.gameObject.GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        PerlinList = new List<PerlinNoiseParameters>();
        PerlinList.Add(
            new PerlinNoiseParameters("Mountains",
            0.005f,// _perlinXScale
            0.005f,
            50, // _perlinOffsetX
            50,
            16, // _perlinOctaves
            1f, // _perlinScaleModifier
            0.4f, // _perlinPersistance
            0.02f,
            0.4f,
            0f,
            1f,
            true,
            1f)
            );

        PerlinList.Add(
            new PerlinNoiseParameters("High Mountains", // _name
            0.005f, // _perlinXScale
            0.005f, // _perlinYScale
            48, // _perlinOffsetX
            51, // _perlinOffsetY
            10, // _perlinOctaves
            0.8f, // _perlinScaleModifier
            0.6f, // _perlinPersistance
            0.02f, // _perlinHeightScale
            0.55f, // _heightReduction
            0.01f, // _minimumClamp
            1f, // _maximumClamp
            true, //normalize
            0.4f)
            );
        PerlinList.Add(
            new PerlinNoiseParameters("Plains", // _name
            0.001f, // _perlinXScale
            0.001f, // _perlinYScale
            0, // _perlinOffsetX
            0, // _perlinOffsetY
            10, // _perlinOctaves
            0.8f, // _perlinScaleModifier
            1.1f, // _perlinPersistance
            0.02f, // _perlinHeightScale
            0.1f, // _heightReduction
            0f, // _minimumClamp
            1f, // _maximumClamp
            true, // _normalizeSize
            0.1f)
            );
        //GenerateTerrain();
        GeneratePerlin();
    }


    private float CalculatePerlinNoise (int x, int y, PerlinNoiseParameters _target)
    {
        float result = Mathf.Clamp(fBM((x + _target.perlinOffsetX),
                                        (y + _target.perlinOffsetY),
                                        _target.perlinOctaves,
                                        _target.perlinPersistance) - _target.heightReduction,
                                        _target.minimumClamp, _target.maximumClamp);
        if (_target.normalizeSize)
        {
            result *= 1f * _target.heightReduction;
        }
        result *= _target.heightMultiplier;

        return result;
    }

    private void GeneratePerlin()
    {
        float[,] heightMap;

        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                foreach (PerlinNoiseParameters targetPerlin in PerlinList) {
                    heightMap[x, y] += CalculatePerlinNoise(x, y, targetPerlin);
                };
                
            }

        }
        terrainData.SetHeights(0, 0, heightMap);
    }


    //fractal brownian motion
    public float fBM(float x, float y, int oct, float persistance, float perlinScaleModifier = 1f) {
        float total = 0f;
        float amplitude = 1;
        float maxValue = 0;
        float bPerlinScaleX = perlinXScale * perlinScaleModifier;
        float bPerlinScaleY = perlinYScale * perlinScaleModifier;
        for (int i = 0; i<oct; i++)
        {
            total += Mathf.PerlinNoise(x * bPerlinScaleX, y  * bPerlinScaleY) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            bPerlinScaleX *= 2.5f;
            bPerlinScaleY *= 2.5f;
        }

        return total / maxValue;

    }

}
