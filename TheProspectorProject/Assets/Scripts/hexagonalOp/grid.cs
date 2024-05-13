using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class grid : MonoBehaviour
{

    //hexxspexx: height 2, width 0.865f*2
    private const float xAxisOffset = 0.865f;
    private float hexWidth = xAxisOffset * 2f, hexHeight = 1.5f;

    public int mWidth = 10;
    public int mHeight = 10;
    public GameObject objectToSpawn;
    void Start()
    {
        createGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void createGrid()
    {
        bool odd = true; //every other row of hex needs an offset on the x-axis
        for (int height = 0; height < mHeight; height++) 
        {
            
            for (int width = 0; width < mWidth; width++) 
            {
                if (odd)
                {
                    Instantiate(objectToSpawn, new Vector3((hexWidth * width), 0, (hexHeight * height)), Quaternion.LookRotation(Vector3.up));
                   
                }
                else
                {
                    Instantiate(objectToSpawn, new Vector3((hexWidth * width) + xAxisOffset, 0, (hexHeight * height)), Quaternion.LookRotation(Vector3.up));
                   
                }
            }
            odd = !odd;
        }
    }
}
