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


    void createGrid()
    {
        bool odd = true; // Every other row of hex needs an offset on the x-axis
        for (int height = 0; height < mHeight; height++)
        {
            for (int width = 0; width < mWidth; width++)
            {
                GameObject spawnedObject;
            
                if (odd)
                {
                    // Instantiate the object at the calculated position
                    spawnedObject = Instantiate(objectToSpawn, 
                        new Vector3(hexWidth * width, 0, hexHeight * height), 
                        Quaternion.LookRotation(Vector3.up));
                }
                else
                {
                    // Instantiate the object with the x-axis offset
                    spawnedObject = Instantiate(objectToSpawn, 
                        new Vector3((hexWidth * width) + xAxisOffset, 0, hexHeight * height), 
                        Quaternion.LookRotation(Vector3.up));
                }

                // Set the instantiated object as a child of this GameObject
                spawnedObject.transform.SetParent(this.transform, true);
            }
            odd = !odd; // Toggle between odd/even row
        }
    }
}
