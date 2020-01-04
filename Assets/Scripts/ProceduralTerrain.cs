using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTerrain : MonoBehaviour
{
    public Terrain terrain;
    public TerrainData terrainData;
    public int Seed = 42;
    public float terrainBaseHeight = 0.2f;
    public float waterLevel = 0.05f;
    public GameObject waterGO = null;
    
    public List<ErosionData> erosionList = new List<ErosionData>()
    {
        new ErosionData()
    };

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

    public List<PerlinNoiseParameters> perlinList = new List<PerlinNoiseParameters>() {
        new PerlinNoiseParameters()
    };
    public List<VoronoiParameters> voronoiList = new List<VoronoiParameters>()
    {
        new VoronoiParameters()
    };

    // Start is called before the first frame update
    void Start()
    {
        terrain = this.gameObject.GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        float[,] heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        heightMap = createEmptyHeight(terrainBaseHeight);
        terrainData.SetHeights(0, 0, heightMap);

        //maybe we can get some way to generate noise only on the voronoi surface? create a temp heightmap and apply some perlin only to it.
        if (Seed == 0)
            Seed = (int) Random.Range(0f, int.MaxValue);
        Random.InitState(Seed); //init state must come from the level seed
        Camera.main.backgroundColor = Color.white;
        //MidPointDisplacement();
        GenerateVoronoi();
        heightMap = GeneratePerlin();

        
        for (int i = 0; i <= 1; i++)
        {
            heightMap = Smooth(heightMap);
        }

        terrainData.SetHeights(0, 0, heightMap);

        foreach (ErosionData erodeTarget in erosionList)
        {
            Erode(erodeTarget);
        }



        
        

        GenerateVegetation();
        AddDetails();
        AddWater();
        SplatMaps();


        //then sets the fog and sky
        bool SkyColorAffectsLight = true;
        Color skyColor = new Color(107 / 255f, 172 / 255f, 255 / 255f, 1f); //azulzin
        //Color skyColor = new Color(255 / 255f, 191 / 138, 255 / 255f, 1f); //laranjinha
        //Color skyColor = new Color(255 / 255f, 200 / 255f, 200 / 255f, 1f); //vermelho do malzao
        //Color skyColor = new Color(238 / 255f, 189 / 255f, 255 / 255f, 1f); //roxo
        //Color skyColor = Color.white;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = skyColor;
        RenderSettings.fogStartDistance = 100;
        RenderSettings.fogEndDistance = 2000;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = skyColor;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        if (SkyColorAffectsLight)
        {
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = (skyColor + Color.white) / 2;
            RenderSettings.ambientGroundColor = (skyColor + Color.black + Color.black) / 3;
        } else
        {
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.ambientEquatorColor = Color.white;
            RenderSettings.ambientGroundColor = (Color.white + Color.black + Color.black) / 3;
        }
            

    }



    void Erode (ErosionData _target)
    {
        for(var i = 0; i<_target.erodeRepeat; i++)
        {
            switch (_target.type)
            {
                case ErosionType.Rain:
                    ErodeRain(_target);
                    break;
                case ErosionType.Tidal:
                    ErodeTidal(_target);
                    break;
                case ErosionType.Thermal:
                    ErodeThermal(_target);
                    break;
                case ErosionType.River:
                    ErodeRiver(_target);
                    break;
                case ErosionType.Wind:
                    ErodeWind(_target);
                    break;
                case ErosionType.Canyon:
                    ErodeCanyon(_target);
                    break;
            }
        }
    }


    void mergeHeightMap(float[,] _targetHeightMap, mergeMethod _merge, float minHeightMerge = 0f)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        switch (_merge)
        {
            case mergeMethod.Additive:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        heightMap[j, k] += _targetHeightMap[j, k];
                    }
                }
                break;
            case mergeMethod.Replace:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        heightMap[j, k] = _targetHeightMap[j, k];
                    }
                }
                break;
            case mergeMethod.ReplaceLower:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        if(heightMap[j, k] < _targetHeightMap[j, k])
                            heightMap[j, k] = _targetHeightMap[j, k];
                    }
                }
                break;
            case mergeMethod.ReplaceHigher:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        if (heightMap[j, k] > _targetHeightMap[j, k])
                            heightMap[j, k] = _targetHeightMap[j, k];
                    }
                }
                break;
            case mergeMethod.Subtract:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        heightMap[j, k] -= _targetHeightMap[j, k];
                    }
                }
                break;
            case mergeMethod.SubOnThresold:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        if(heightMap[j, k] < minHeightMerge)
                        {
                            //smoothes the difference the closer it is to the thresold
                            heightMap[j, k] -= (_targetHeightMap[j, k] * (minHeightMerge - heightMap[j, k]) / minHeightMerge);
                        }
                            
                    }
                }
                break;
            case mergeMethod.AddOnThresold:
                for (int j = 0; j < terrainData.heightmapWidth; j++)
                {
                    for (int k = 0; k < terrainData.heightmapHeight; k++)
                    {
                        if (heightMap[j, k] < minHeightMerge)
                        {
                            heightMap[j, k] += (_targetHeightMap[j, k] * (minHeightMerge - heightMap[j, k]) / minHeightMerge);
                        }

                    }
                }
                break;
        }
        
        terrainData.SetHeights(0, 0, heightMap);
    }

    void ErodeRain (ErosionData _target)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] rainHeightMap = createEmptyHeight();
        for (int i = 0; i<_target.droplets; i++)
        {
            int x = Random.Range(0, terrainData.heightmapWidth);
            int y = Random.Range(0, terrainData.heightmapHeight);
            if(heightMap[(int)x, (int)y] > _target.dropletMinHeight)
                rainHeightMap[x, y] = _target.erosionStrength;
        }
        for (int smoothAmount = 0; smoothAmount < _target.erosionSmoothAmount; smoothAmount++)
        {
            rainHeightMap = Smooth(rainHeightMap, 2);
        }

        mergeHeightMap(rainHeightMap, mergeMethod.Subtract);
    }

    void ErodeCanyon (ErosionData _target)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] canyonHeightmap = createEmptyHeight();
        int mapHeight = terrainData.heightmapHeight;
        int mapWidth = terrainData.heightmapWidth;
        List<Vector2> pointsList = new List<Vector2>();
        Vector2Int CanyonStart = new Vector2Int(0, Random.Range(0, terrainData.heightmapHeight));
        Vector2Int nexCanyonPoint = CanyonStart;
        Vector2Int lastCanyonPoint = nexCanyonPoint;

        while (nexCanyonPoint.x < terrainData.heightmapWidth && nexCanyonPoint.x >= 0 &&
            nexCanyonPoint.y < terrainData.heightmapHeight && nexCanyonPoint.y >= 0)
        {
            lastCanyonPoint = nexCanyonPoint;
            int randX = Random.Range(_target.CanyonRandomCoverage.x, _target.CanyonRandomCoverage.y) + nexCanyonPoint.x;
            int randY = nexCanyonPoint.y + Random.Range(_target.CanyonRandomDirection*-1, _target.CanyonRandomDirection);
            nexCanyonPoint = new Vector2Int(randX, randY);
            pointsList.AddRange(GetV2Points(lastCanyonPoint, nexCanyonPoint) );
        }

        Vector2[] points = pointsList.ToArray();
        for(int i = 0; i<points.Length; i++)
        {
            points[i].x += (int) (Random.Range(0f, _target.CanyonDisplacement) - _target.CanyonDisplacement / 2 );
            points[i].y += (int) (Random.Range(0f, _target.CanyonDisplacement) - _target.CanyonDisplacement / 2);
            if (points[i].y < mapHeight && points[i].y > 0 && points[i].x < mapWidth && points[i].x > 0)
                canyonHeightmap[(int)points[i].x, (int)points[i].y] = _target.erosionStrength + Random.Range(0f, _target.solubility);

            for (int nx =0; nx < _target.CanyonWidth; nx++)
            {
                for (int ny = 0; ny < _target.CanyonWidth; ny++)
                {
                    int nnx = (int) points[i].x + nx;
                    int nny = (int)points[i].y + ny;
                    if (nny < mapHeight && nny > 0 && nnx < mapWidth && nnx > 0 && ( Vector2.Distance(new Vector2(points[i].x, points[i].y), new Vector2(nnx, nny)) < _target.CanyonWidth) )
                    {
                        canyonHeightmap[nnx, nny] = _target.erosionStrength + Random.Range(0f, _target.solubility);
                    }
                    nnx = (int)points[i].x - nx;
                    nny = (int)points[i].y - ny;
                    if (nny < mapHeight && nny > 0 && nnx < mapWidth && nnx > 0 && (Vector2.Distance(new Vector2(points[i].x, points[i].y), new Vector2(nnx, nny)) < _target.CanyonWidth))
                    {
                        canyonHeightmap[nnx, nny] = _target.erosionStrength + Random.Range(0f, _target.solubility);
                    }
                }
            }
        }
        for (int i = 0; i < _target.erosionSmoothAmount; i++)
        {
            canyonHeightmap = Smooth(canyonHeightmap, 3);
        }
        mergeHeightMap(canyonHeightmap, mergeMethod.SubOnThresold, _target.minHeightMerge);
    }

    public List<Vector2> GetV2Points (Vector2 p1, Vector2 p2)
    {
        List<Vector2> points = new List<Vector2>();
        float lerpValue = 0f;
        float distance = 0.01f;
        while (lerpValue <= 1f)
        {
            points.Add(Vector2.Lerp(p1, p2, lerpValue));
            lerpValue += distance;
        }

        return points;
    }

    public List<Vector2> GetPoints(Vector2 p1, Vector2 p2)
    {
        List<Vector2> points = new List<Vector2>();

        // no slope (vertical line)
        if (p1.x == p2.x)
        {
            for (double y = p1.y; y <= p2.y; y++)
            {
                Vector2 p = new Vector2(p1.x, (float) y);
                points.Add(p);
            }
        }
        else
        {
            // swap p1 and p2 if p2.x < p1.x
            if (p2.x < p1.x)
            {
                Vector2 temp = p1;
                p1 = p2;
                p2 = temp;
            }

            double deltaX = p2.x - p1.x;
            double deltaY = p2.y - p1.y;
            double error = -1.0f;
            double deltaErr = Mathf.Abs((float) deltaY / (float) deltaX);

            double y = p1.y;
            for (double x = p1.x; x <= p2.x; x++)
            {
                Vector2 p = new Vector2( (float) x, (float) y);
                points.Add(p);
                error += deltaErr;
                while (error >= 0.0f)
                {
                    y++;
                    points.Add(new Vector2( (float) x, (float) y));
                    error -= 1.0f;
                }
            }
        }

        return points;
    }


    void ErodeTidal(ErosionData _target) //removes beaches and replaces then with small cliffs
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] erodedHeightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                List<Vector2> neightbours = GetNeighbours(x, y);
                foreach (Vector2 n in neightbours)
                {
                    if (heightMap[x, y] < waterLevel && heightMap[(int)n.x, (int)n.y] > waterLevel)
                    {
                        erodedHeightMap[x, y] = waterLevel;
                        erodedHeightMap[(int)n.x, (int)n.y] = waterLevel - _target.erosionStrength;
                    }
                }
            }
        }

        for (int i = 0; i < _target.erosionSmoothAmount; i++)
        {
            erodedHeightMap = Smooth(erodedHeightMap, 1);
        }


        mergeHeightMap(erodedHeightMap, mergeMethod.Replace);

    }
    void ErodeThermal(ErosionData _target)
    {
        float[,] erodedHeightMap = terrainData.GetHeights(0,0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        for(int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                List<Vector2> neightbours = GetNeighbours(x, y);
                foreach(Vector2 n in neightbours){
                    if(erodedHeightMap[x, y] > erodedHeightMap[(int)n.x, (int)n.y] + _target.erosionStrength)
                    {
                        float currentHeight = erodedHeightMap[x, y];
                        erodedHeightMap[x, y] -= currentHeight * _target.thermalDisplacementMultiplier;
                        erodedHeightMap[(int)n.x,(int)n.y] += currentHeight * _target.thermalDisplacementMultiplier;
                    }
                }
            }
        }

        for (int i = 0; i < _target.erosionSmoothAmount; i++)
        {
            erodedHeightMap = Smooth(erodedHeightMap, 2);
        }
            

        mergeHeightMap(erodedHeightMap, mergeMethod.Replace);
    }
    void ErodeRiver(ErosionData _target)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] erosionMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

        for(int i =0; i<_target.droplets; i++)
        {
            Vector2 dropletPosition = new Vector2(Random.Range(0, terrainData.heightmapWidth), Random.Range(0, terrainData.heightmapHeight));
            if (heightMap[(int)dropletPosition.x, (int)dropletPosition.y] > _target.dropletMinHeight)
            {
                erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] = _target.erosionStrength;
                for (int j = 0; j < _target.springsPerRiver; j++)
                {
                    erosionMap = RunRiver(dropletPosition, heightMap, erosionMap, terrainData.heightmapWidth, terrainData.heightmapHeight, _target);
                }
            }
                
            
        }
        
        for(int j = 0; j <_target.erosionSmoothAmount; j++)
        {
            erosionMap = Smooth(erosionMap);
        }
        //terrainData.SetHeights(0,0, erodedHeightMap);
        mergeHeightMap(erosionMap, mergeMethod.Subtract);

    }

    float[,] RunRiver(Vector2 dropletPosition, float[,] heightMap, float[,] erosionMap, int width, int height, ErosionData _target)
    {

        while (erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] > 0)
        {
            List<Vector2> neighbours = GetNeighbours((int) dropletPosition.x, (int) dropletPosition.y);
            neighbours = ShuffleList(neighbours);
            bool foundLower = false;
            foreach (Vector2 n in neighbours)
            {
                if (heightMap[(int)n.x, (int)n.y] < heightMap[(int)dropletPosition.x, (int)dropletPosition.y])
                {
                    erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletPosition.x,
                                                                (int)dropletPosition.y] - _target.solubility;
                    dropletPosition = n;
                    foundLower = true;
                    break;
                }
            }
            if (!foundLower)
            {
                erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] -= _target.solubility;
            }
        }
        return erosionMap;
    }

    private List<E> ShuffleList<E>(List<E> inputList)
    {
        List<E> randomList = new List<E>();

        Random r = new Random();
        int randomIndex = 0;
        while (inputList.Count > 0)
        {
            randomIndex = Random.Range(0, inputList.Count); 
            randomList.Add(inputList[randomIndex]); 
            inputList.RemoveAt(randomIndex); 
        }

        return randomList;
    }

    void ErodeWind(ErosionData _target)
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        int width = terrainData.heightmapWidth;
        int height = terrainData.heightmapHeight;

        for (int x = 0; x <= width; x += 1)
        {
            for(int y = 0; y <= height; y += (int) _target.windDisplacement.y)
            {
                float thisNoise = (float)Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 4;
                int nx = (int) (x + _target.windDisplacement.x);
                int ny = (int)(y + _target.windDisplacement.y/2 + thisNoise);
                int nny = (int)(y + thisNoise);

                if (!( (nx < 0) || (nx > (width -1)) || (ny < 0) || (ny > (height -1))) &&
                    (x<=terrainData.heightmapWidth && nny < terrainData.heightmapHeight) )
                {
                    heightMap[x, nny] -= _target.erosionStrength;
                    heightMap[nx, ny] += _target.erosionStrength;
                }
            }
        }

        for (int i = 0; i<= _target.erosionSmoothAmount; i++)
        {
            heightMap = Smooth(heightMap);
        }

        terrainData.SetHeights(0,0, heightMap);
    }


    void AddWater ()
    {
        GameObject water = GameObject.Find("water");
        if (!water)
        {
            water = Instantiate<GameObject>(waterGO, this.transform.position, this.transform.rotation);
            water.name = "water";
        }

        water.transform.position = this.transform.position + new Vector3(terrainData.size.x /2, waterLevel * terrainData.size.y, terrainData.size.z /2);
        water.transform.localScale = new Vector3(terrainData.size.x, 1, terrainData.size.z);
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
                    if (Random.Range(0f, 1f) >= detailList[i].density) continue;
                    if (CalculatePerlinNoise(x, y, detailList[i].perlinDist) <= detailList[i].perlinCutout) continue;
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
        int maximumTrees = 900000;
        int treeSpacing = 1;
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
                    if (CalculatePerlinNoise(z, x, currentVeg.perlinDist) >= currentVeg.perlinCutout) break;
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
        //Midpoint displacement
        float MPDheightDampenerPower = 2.0f;
        float MPDroughness = 2.0f;
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

    private List<Vector2> GetNeighbours (int x, int y)
    {
        List<Vector2> returnValue = new List<Vector2>();
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
        };
        foreach (Vector2 step in steps)
        {
            if ((x + step.x) > 0 && (x + step.x) < terrainData.heightmapWidth && //preventing out of bounds
                (y + step.y) > 0 && (y + step.y) < terrainData.heightmapHeight)
                returnValue.Add(new Vector2(x + step.x, y + step.y));
        }

        return returnValue;
    }

    private float[,] Smooth (float[,] _target, int _amount = 1)
    {
        
        //float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] smoothHeightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        //steps to get all 8 neighboor pixels
        Vector2[] steps = { };
        Vector2[] weakSteps = {
                    //new Vector2(0, -1),
                    //new Vector2(-1, 0),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1)
                };
        Vector2[] normalSteps = {
                    new Vector2(-1,-1),
                    new Vector2(0, -1),
                    new Vector2(1,-1),
                    new Vector2(-1, 0),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(-1,1),
                    new Vector2(0, 1),
                    new Vector2(1,1)
                };
        Vector2[] strongSteps = {
                    new Vector2(-1, -1),
                    new Vector2(0, -1),
                    new Vector2(1, -1),
                    new Vector2(-1, 0),
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(-1, 1),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
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
        switch (_amount)
        {
            case 1:
                steps = weakSteps;
                break;
            case 2:
                steps = normalSteps;
                break;
            case 3:
                steps = strongSteps;
                break;
                    
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
                        avgHeight += _target[(int)(x + step.x), (int)(y + step.y)];
                }
                avgHeight = avgHeight / steps.Length;
                smoothHeightMap[x, y] = avgHeight;
            }
        }
        return smoothHeightMap;
    }

    private void GenerateVoronoi ()
    {
        foreach (VoronoiParameters _target in voronoiList)
        {
            CalculateVoronoi(_target);
        }
        
    }

    private void CalculateVoronoi (VoronoiParameters _target)
    {
        //float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float[,] heightMap = createEmptyHeight();
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
        if (_target.subtract)
        {
            mergeHeightMap(heightMap, mergeMethod.Subtract);
        } else
        {
            mergeHeightMap(heightMap, mergeMethod.Additive);
        }
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

    private float[,] GeneratePerlin()
    {
        float[,] heightMap;
        heightMap = terrainData.GetHeights(0,0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                foreach (PerlinNoiseParameters targetPerlin in perlinList) {
                    heightMap[x, y] += CalculatePerlinNoise(x, y, targetPerlin);
                };
                
            }

        }
        return heightMap;
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

    public float[,] createEmptyHeight(float _defaultHeight = 0f)
    {
        float[,] emptyHeight = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapWidth; y++)
            {
                emptyHeight[x, y] = _defaultHeight;
            }
        }
        return emptyHeight;
    }
    
    public void AddDebugValues() {

        int perlinOffset = (int) Random.Range(0f, 1200f);

        perlinList.Add(
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

        perlinList.Add(
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
        perlinList.Add(
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
        perlinList.Add(
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

        voronoiList.Add(
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
        voronoiList.Add(
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
