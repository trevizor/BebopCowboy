using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{

    //public Vector2 heightRange = new Vector2(0.1f, 0.2f);
    public Terrain terrain;
    public TerrainData terrainData;

    //Midpoint displacement
    float MPDheightMin = -2f;
    float MPDheightMax = 2f;
    float MPDheightDampenerPower = 2.0f;
    float MPDroughness = 2.0f;


    public List<PerlinNoiseParameters> PerlinList = new List<PerlinNoiseParameters>();
    public List<VoronoiParameters> VoronoiList = new List<VoronoiParameters>();

    // Start is called before the first frame update
    void Start()
    {
        terrain = this.gameObject.GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        float[,] heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        terrainData.SetHeights(0, 0, heightMap);

        PerlinList = new List<PerlinNoiseParameters>();
        VoronoiList = new List<VoronoiParameters>();
        Random.InitState(13666020); //init state must come from the level seed
        AddDebugValues();
        //MidPointDisplacement();
        GenerateVoronoi();
        Smooth();
        Smooth();
        GeneratePerlin();
    }

    public void MidPointDisplacement() //with these parameters it creates a few holes and hills
    {
        float[,] heightMap = terrainData.GetHeights(0,0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        int width = terrainData.heightmapWidth - 1;
        int squareSize = width;
        float heightMin = -0.01f;
        float heightMax = 1f;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);
        
        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] +
                                                     heightMap[cornerX, y] +
                                                     heightMap[x, cornerY] +
                                                     heightMap[cornerX, cornerY]) / 4.0f +
                                                    UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {

                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0
                        || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

                    //Calculate the square value for the bottom side  
                    heightMap[midX, y] = (float)((heightMap[midX, midY] +
                                                  heightMap[x, y] +
                                                  heightMap[midX, pmidYD] +
                                                  heightMap[cornerX, y]) / 4.0f +
                                                 UnityEngine.Random.Range(heightMin, heightMax));
                    //Calculate the square value for the top side   
                    heightMap[midX, cornerY] = (float)((heightMap[x, cornerY] +
                                                            heightMap[midX, midY] +
                                                            heightMap[cornerX, cornerY] +
                                                        heightMap[midX, pmidYU]) / 4.0f +
                                                       UnityEngine.Random.Range(heightMin, heightMax));

                    //Calculate the square value for the left side   
                    heightMap[x, midY] = (float)((heightMap[x, y] +
                                                            heightMap[pmidXL, midY] +
                                                            heightMap[x, cornerY] +
                                                  heightMap[midX, midY]) / 4.0f +
                                                 UnityEngine.Random.Range(heightMin, heightMax));
                    //Calculate the square value for the right side   
                    heightMap[cornerX, midY] = (float)((heightMap[midX, y] +
                                                            heightMap[midX, midY] +
                                                            heightMap[cornerX, cornerY] +
                                                            heightMap[pmidXR, midY]) / 4.0f +
                                                       UnityEngine.Random.Range(heightMin, heightMax));

                }
            }

            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }

        terrainData.SetHeights(0, 0, heightMap);
    }


    private void Smooth ()
    {
        
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] smoothHeightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        //steps to get all 8 neighboor pixels
        Vector2[] steps = {
                    new Vector2(-1,-1),
                    new Vector2(0,-1),
                    new Vector2(1,-1),
                    new Vector2(-1,0),
                    new Vector2(0,0),
                    new Vector2(1,0),
                    new Vector2(-1,1),
                    new Vector2(0,1),
                    new Vector2(1,1),
                    new Vector2(-1,-2),
                    new Vector2(0,-2),
                    new Vector2(1,-2),
                    new Vector2(-1,2),
                    new Vector2(0,2),
                    new Vector2(1,2),
                    new Vector2(-2,-1),
                    new Vector2(-2,0),
                    new Vector2(-2,1),
                    new Vector2(-2,-2),
                    new Vector2(-2,2),
                    new Vector2(2,-1),
                    new Vector2(2,0),
                    new Vector2(2,1),
                    new Vector2(2,-2),
                    new Vector2(2,2)
                };
        float avgHeight = 0f;

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                foreach( Vector2 step in steps)
                {
                    if( (x + step.x) > 0 && (x + step.x) < terrainData.heightmapWidth && //preventing out of bounds
                        (y + step.y) > 0 && (y + step.y) < terrainData.heightmapHeight)
                        avgHeight += heightMap[(int)(x + step.x), (int)(y + step.y)];
                }
                avgHeight = avgHeight / steps.Length;
                smoothHeightMap[x, y] = avgHeight;
            }
        }

        terrainData.SetHeights(0,0, smoothHeightMap);
    }

    private void GenerateVoronoi ()
    {
        foreach (VoronoiParameters _target in VoronoiList)
        {
            CalculateVoronoi(_target);
        }
        
    }

    private void CalculateVoronoi (VoronoiParameters _target)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        Vector3 peak;
        Vector2 peakLocation;
        float distanceToPeak;
        float maxDistance;
        int numOfInstances = Random.Range(_target.minInstances, _target.maxInstances);
        float targetSlopeHeight;

        for (int count = 0; count < numOfInstances; count++)
        {
            peak = new Vector3(
            Random.Range(0, terrainData.heightmapWidth),
            Random.Range(_target.minHeight, _target.maxHeight),
            Random.Range(0, terrainData.heightmapHeight));

            heightMap[(int)peak.x, (int)peak.z] = peak.y;
            peakLocation = new Vector2(peak.x, peak.z);
            maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapWidth * _target.maxDistanceMultiplier, terrainData.heightmapHeight * _target.maxDistanceMultiplier));

            for (int x = 0; x < terrainData.heightmapWidth; x++)
            {
                for (int y = 0; y < terrainData.heightmapHeight; y++)
                {
                    if (!(x == peak.x && y == peak.z))
                    {
                        distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y))/maxDistance;
                        
                        if (_target.powerFallout) {
                            targetSlopeHeight = peak.y - Mathf.Pow(distanceToPeak, _target.powerValue);
                        } else
                        {
                            targetSlopeHeight = peak.y - (distanceToPeak * _target.falloff);
                        }
                        
                        if (heightMap[x, y] < targetSlopeHeight)
                        {
                            heightMap[x, y] = targetSlopeHeight;
                        }
                    }
                }
            }
        }
        

        terrainData.SetHeights(0,0, heightMap);
    }

    private float CalculatePerlinNoise (int x, int y, PerlinNoiseParameters _target)
    {
        float result = Mathf.Clamp(fBM((x + _target.perlinOffsetX),
                                        (y + _target.perlinOffsetY),
                                        _target.perlinOctaves,
                                        _target.perlinPersistance,
                                        _target.perlinScaleModifier,
                                        _target.perlinXScale,
                                        _target.perlinYScale) - _target.heightReduction,
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
        heightMap = terrainData.GetHeights(0,0, terrainData.heightmapWidth, terrainData.heightmapHeight);

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
    public float fBM(float x, float y, int oct, float persistance, float perlinScaleModifier, float _perlinXScale, float _perlinYScale) {
        float total = 0f;
        float amplitude = 1;
        float maxValue = 0;
        float bPerlinScaleX = _perlinXScale * perlinScaleModifier;
        float bPerlinScaleY = _perlinYScale * perlinScaleModifier;
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

    public void AddDebugValues() {
        PerlinList.Add(
            new PerlinNoiseParameters("Mountains",
            0.005f,// _perlinXScale
            0.005f,
            50, // _perlinOffsetX
            50,
            16, // _perlinOctaves
            1f, // _perlinScaleModifier
            0.4f, // _perlinPersistance
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
            0.55f, // _heightReduction
            0.01f, // _minimumClamp
            1f, // _maximumClamp
            true, //normalize
            0.4f)
            );
        PerlinList.Add(
            new PerlinNoiseParameters("Plains Noise", // _name
            0.001f, // _perlinXScale
            0.001f, // _perlinYScale
            0, // _perlinOffsetX
            0, // _perlinOffsetY
            10, // _perlinOctaves
            0.8f, // _perlinScaleModifier
            1.1f, // _perlinPersistance
            0.1f, // _heightReduction
            0f, // _minimumClamp
            1f, // _maximumClamp
            true, // _normalizeSize
            0.1f)
            );
        PerlinList.Add(
            new PerlinNoiseParameters("Plains Level", // _name
            0.001f, // _perlinXScale
            0.001f, // _perlinYScale
            0, // _perlinOffsetX
            0, // _perlinOffsetY
            10, // _perlinOctaves
            12f, // _perlinScaleModifier
            1.1f, // _perlinPersistance
            0f, // _heightReduction
            0f, // _minimumClamp
            1f, // _maximumClamp
            true, // _normalizeSize
            0.4f)
            );

        VoronoiList.Add(
            new VoronoiParameters(
                "gentle slopes",
                0f,
                0.1f,
                0.4f,
                1f,
                2,
                5,
                true,
                1.2f
                )
            );
        VoronoiList.Add(
            new VoronoiParameters(
                "High Mountain",
                0.1f,
                0.5f,
                1f,
                0.6f,
                1,
                1,
                true,
                1.27f
                )
            );

    }

}
