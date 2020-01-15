using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionBuilder : MonoBehaviour
{
    public Vector3 citySize = new Vector3(15, 10, 10);
    public Vector3 citySpawnPos = new Vector3();
    public GameObject[] cityBlocks;
    // Start is called before the first frame update
    private float blockSize = 50;

    void Start()
    {
        citySpawnPos = gameObject.transform.position;
        for (int x = 0; x<citySize.x; x++)
        {
            for(int y = 0; y<citySize.y; y++)
            {
                for(int z = 0; z<citySize.z; z++)
                {
                    GameObject myPrefab = cityBlocks[(int)Random.Range(0, cityBlocks.Length)];
                    Instantiate(myPrefab, new Vector3(citySpawnPos.x + x * blockSize, citySpawnPos.y + y *blockSize, citySpawnPos.z + z *blockSize), Quaternion.identity);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
