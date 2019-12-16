using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{

    //public Vector2 heightRange = new Vector2(0.1f, 0.2f);
    public Terrain terrain;
    public TerrainData terrainData;

    //Midpoint displacement
    float MPDheightDampenerPower = 2.0f;
    float MPDroughness = 2.0f;

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
        public Vector2 tileOffset = new Vector2(0f,0f);
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

    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };
    public List<VegetationData> vegetationList = new List<VegetationData>()
    {
        new VegetationData()
    };
    public List<DetailData> detailList = new List<DetailData>()
    {
        new DetailData()
    };

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
        //maybe we can get some way to generate noise only on the voronoi surface? create a temp heightmap and apply some perlin only to it.
        Random.InitState(2); //init state must come from the level seed
        Camera.main.backgroundColor = Color.white;
        AddDebugValues();
        //MidPointDisplacement();
        GenerateVoronoi();
        
        Smooth();
        Smooth();
        Smooth();
        Smooth();
        Smooth();
        GeneratePerlin();
        Smooth();
        GenerateVegetation();
        AddDetails();
        SplatMaps();
    }

    void AddDetails ()
    {
        DetailPrototype[] newDetailPrototypes;
        newDetailPrototypes = new DetailPrototype[detailList.Count];
        int di = 0;
        foreach (DetailData d in detailList)
        {
            newDetailPrototypes[di] = new DetailPrototype();
            newDetailPrototypes[di].prototype = d.prototype;
            newDetailPrototypes[di].prototypeTexture = d.prototypeTexture;
            newDetailPrototypes[di].healthyColor = d.color1;
            newDetailPrototypes[di].dryColor = d.color2;
            newDetailPrototypes[di].minHeight = d.heightScaleRange.x;
            newDetailPrototypes[di].maxHeight = d.heightScaleRange.y;
            newDetailPrototypes[di].minWidth = d.heightScaleRange.x;
            newDetailPrototypes[di].maxWidth = d.heightScaleRange.y;
            if (newDetailPrototypes[di].prototype)
            {
                newDetailPrototypes[di].usePrototypeMesh = true;
                newDetailPrototypes[di].renderMode = DetailRenderMode.VertexLit;
            } else
            {
                newDetailPrototypes[di].usePrototypeMesh = false;
                newDetailPrototypes[di].renderMode = DetailRenderMode.Grass;
            }
            di++;
        }
        terrainData.detailPrototypes = newDetailPrototypes;

        int detailSpacing = 1;
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth,
                                            terrainData.heightmapHeight);
        for (int i = 0; i< terrainData.detailPrototypes.Length; i++)
        {
            int[,] detailMap = new int[terrainData.detailResolution, terrainData.detailResolution];
            DetailData currentDetail = detailList[i];
            float minSteepness = currentDetail.minSteepness;
            float maxSteepness = currentDetail.maxSteepness;
            float minHeight = currentDetail.minHeight;
            float maxHeight = currentDetail.maxHeight;

            for (int y = 0; y < terrainData.detailHeight; y += detailSpacing)
            {
                for (int x = 0; x < terrainData.detailWidth; x += detailSpacing)
                {
                    if (Random.Range(0f, 1f) > detailList[i].density) continue;
                    int xHM = (int)(x / (float)terrainData.detailResolution * terrainData.heightmapWidth);
                    int yHM = (int)(y / (float)terrainData.detailResolution * terrainData.heightmapHeight);

                    float thisHeight = heightMap[yHM, xHM] + Random.Range(-currentDetail.feather, currentDetail.feather); //adds the feather to simulate the blending
                    float steepness = terrainData.GetSteepness(xHM / (float)terrainData.size.x,
                                                                yHM / (float)terrainData.size.z);
                    if (thisHeight >= minHeight && thisHeight <= maxHeight &&
                        steepness >= minSteepness && steepness <= maxSteepness)
                    {    
                        detailMap[y, x] = 1;
                    }
                        
                }
            }
            terrainData.SetDetailLayer(0,0, i, detailMap);
        }


    }

    void GenerateVegetation ()
    {
        Vector3 terrainOriginalSize = terrainData.size;
        terrainData.size = new Vector3(1000, 500, 1000);
        int terrainLayer = LayerMask.GetMask("Terrain");
        int maximumTrees = 20000;
        int treeSpacing = 10;
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetationList.Count];
        int tindex = 0;
        foreach (VegetationData t in vegetationList)
        {
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.mesh;
            tindex++;
        }

        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();
        terrainData.treeInstances = allVegetation.ToArray(); //clearing old trees
        for (int z = treeSpacing; z <terrainData.size.z- treeSpacing; z += treeSpacing)
        {
            for (int x = treeSpacing; x < terrainData.size.x- treeSpacing; x += treeSpacing)
            {
                //TODO create vegetation groups
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    VegetationData currentVeg = vegetationList[tp];
                    if (Random.Range(0.0f, 1f) > currentVeg.density) break;
                    float minHeight = currentVeg.minHeight;
                    float maxHeight = currentVeg.maxHeight;
                    float minSteepness = currentVeg.minSteepness;
                    float maxSteepness = currentVeg.maxSteepness;
                    int newZ = z + Mathf.RoundToInt(Random.Range(-treeSpacing, treeSpacing));
                    int newX = x + Mathf.RoundToInt(Random.Range(-treeSpacing, treeSpacing));

                    float thisHeight = terrainData.GetHeight(newX, newZ) / terrainData.size.y;
                    float steepness = terrainData.GetSteepness(newX, newZ);
                    if (thisHeight >= minHeight && thisHeight <= maxHeight &&
                        steepness >= minSteepness && steepness <= maxSteepness)
                    {
                        TreeInstance instance = new TreeInstance();
                        instance.prototypeIndex = tp;
                        instance.position = new Vector3(newX / terrainData.size.x,
                                                        terrainData.size.y,
                                                        newZ / terrainData.size.z);
                        

                        instance.rotation = Random.Range(0, 360);
                        instance.color = Color.Lerp(currentVeg.color1, currentVeg.color2, Random.Range(0f, 1f));
                        instance.lightmapColor = currentVeg.lightColor;
                        instance.heightScale = Random.Range(currentVeg.minScale, currentVeg.maxScale);
                        instance.widthScale = Random.Range(currentVeg.minScale, currentVeg.maxScale); //TODO: add min width and max width

                        instance.position = new Vector3(instance.position.x * terrainData.size.x/terrainData.alphamapWidth,
                                instance.position.y,
                                instance.position.z * terrainData.size.z/terrainData.alphamapHeight);

                        allVegetation.Add(instance);
                        if (allVegetation.Count >= maximumTrees) goto TREESDONE;
                    }
                };
            };
        };

    TREESDONE:
        
        List<TreeInstance> newVegetation = new List<TreeInstance>();

        foreach (TreeInstance tree in allVegetation)
        {
            TreeInstance newTree = tree;
            Vector3 treeWorldPos = new Vector3(tree.position.x * terrainData.size.x,
                            terrainData.size.y,
                            tree.position.z * terrainData.size.z
                            );
            RaycastHit hit;
            int layerMask = 1 << terrainLayer;
            if (Physics.Raycast(treeWorldPos, -Vector3.up, out hit, 1000, Physics.DefaultRaycastLayers))
            {
                float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                newTree.position = new Vector3(tree.position.x, treeHeight, tree.position.z);
            }

            newVegetation.Add(newTree);
        }

        terrainData.size = terrainOriginalSize; //TODO: some treess keep floating because of the new terrain scale. maybe do the raycast after the scale?
        allVegetation = newVegetation;
        terrainData.treeInstances = allVegetation.ToArray();
        

    }

    
    public void SplatMaps ()
    {
        TerrainLayer[] newSplatPrototypes;
        newSplatPrototypes = new TerrainLayer[splatHeights.Count];int spindex = 0;
        foreach (SplatHeights sh in splatHeights)
        {
            //here we will edit the texture!
            Texture2D newTexture = new Texture2D(sh.texture.width, sh.texture.height);
            
            Color[] texturePixels = sh.texture.GetPixels(0, 0, sh.texture.width, sh.texture.height);
            Color targetColor = sh.color1; //temporary, this later gets converted between dark and light colors

            for (int c = 0; c< texturePixels.Length; c++)
            {
                Color col = texturePixels[c];
                Color tempCol = Color.black;
                float grayValue = (col.r + col.g + col.b)*1.2f / 3f;
                //col.r = col.b = col.g = grayValue;
                targetColor = Color.Lerp(sh.color2, sh.color1, grayValue - sh.colorThreshold);
                tempCol.r += sh.redTint.r * col.r;
                tempCol.g += sh.redTint.g * col.r;
                tempCol.b += sh.redTint.b * col.r;
                tempCol.r += sh.greenTint.r * col.g;
                tempCol.g += sh.greenTint.g * col.g;
                tempCol.b += sh.greenTint.b * col.g;
                tempCol.r += sh.blueTint.r * col.b;
                tempCol.g += sh.blueTint.g * col.b;
                tempCol.b += sh.blueTint.b * col.b;
                
                tempCol.a = 0f;
                texturePixels[c] = tempCol;
            }
            newTexture.SetPixels(0,0, newTexture.width, newTexture.height, texturePixels);
            newSplatPrototypes[spindex] = new TerrainLayer();
            newSplatPrototypes[spindex].diffuseTexture = newTexture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            newSplatPrototypes[spindex].diffuseTexture.Apply(true);
            spindex++;
        }
        terrainData.terrainLayers = newSplatPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,,] splatmapData = new float[terrainData.alphamapWidth,
                                                terrainData.alphamapHeight,
                                                terrainData.alphamapLayers];
        for (int x = 0; x<terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    float blendNoise = Mathf.PerlinNoise(x*0.005f, y * 0.005f) * 0.2f;
                    float thisHeightStart = splatHeights[i].minHeight;
                    float thisHeightEnd = splatHeights[i].maxHeight + blendNoise;
                    //get steepness uses X and Y inverted. Still not happy with the current results
                    float steepness = terrainData.GetSteepness(y / (float) terrainData.heightmapHeight, x / (float)terrainData.heightmapWidth);
                    if ( (heightMap[x,y] >= thisHeightStart && heightMap[x,y] <= thisHeightEnd) &&
                        (steepness >= splatHeights[i].minSteepness && steepness <= splatHeights[i].maxSteepness))
                    {
                        float height = heightMap[x, y];
                        splat[i] = 1;
                    }
                }
                splat = NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }
            }

        }

        terrainData.SetAlphamaps(0,0, splatmapData);

    }

    public float[] NormalizeVector (float [] vec)
    {
        float total = 0f;
        for (int x = 0; x< vec.Length; x++)
        {
            total += vec[x];
        }
        for (int y = 0; y < vec.Length; y++)
        {
            vec[y] /= total;
        }

        return vec;
    }

    public void MidPointDisplacement() //with these parameters it creates a few holes and hills
    {
        float[,] heightMap = terrainData.GetHeights(0,0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        int width = terrainData.heightmapWidth - 1;
        int squareSize = width;
        float heightMin = -0.02f;
        float heightMax = 0.7f;
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
                    new Vector2(1,1)
                    /*new Vector2(-1,-2),
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
                    new Vector2(2,2)*/
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

        int perlinOffset = (int) Random.Range(0f, 1200f);

        PerlinList.Add(
            new PerlinNoiseParameters("Mountains",
            0.005f,// _perlinXScale
            0.005f,
            perlinOffset, // _perlinOffsetX
            perlinOffset,
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
            perlinOffset, // _perlinOffsetX
            perlinOffset, // _perlinOffsetY
            10, // _perlinOctaves
            0.8f, // _perlinScaleModifier
            0.6f, // _perlinPersistance
            0.45f, // _heightReduction
            0.01f, // _minimumClamp
            1f, // _maximumClamp
            true, //normalize
            0.5f)
            );
        PerlinList.Add(
            new PerlinNoiseParameters("Plains Noise", // _name
            0.001f, // _perlinXScale
            0.001f, // _perlinYScale
            perlinOffset, // _perlinOffsetX
            perlinOffset, // _perlinOffsetY
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
            perlinOffset, // _perlinOffsetX
            perlinOffset, // _perlinOffsetY
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
                0,
                6,
                true,
                1.2f
                )
            );
        VoronoiList.Add(
            new VoronoiParameters(
                "High Mountain",
                0.2f,
                0.45f,
                1f,
                0.32f,
                0,
                4,
                true,
                0.89f
                )
            );

    }

}
