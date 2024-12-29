using UnityEngine;

namespace hexagonalOp
{
    //this class is responsible for creating the board, made of instances of a hex tile.
    //The most obscure part of the calculation is the offset required to adjust the rows neatly together.
    public class GridInstantiator : MonoBehaviour
    {
        //hexxspexx: height 2, width 0.865f*2 
        private const float XAxisOffset = 0.865f; // <- this const is used to place the rows of tiles correctly aligned
        private float _hexWidth = XAxisOffset * 2f, _hexHeight = 1.5f;

        public int mWidth = 10;
        public int mHeight = 10;
        public GameObject objectToSpawn;

        void Start()
        {
            CreateGrid();
        }


        void CreateGrid()
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
                            new Vector3(_hexWidth * width, 0, _hexHeight * height),
                            Quaternion.LookRotation(Vector3.up));
                    }
                    else
                    {
                        // Instantiate the object with the x-axis offset
                        spawnedObject = Instantiate(objectToSpawn,
                            new Vector3((_hexWidth * width) + XAxisOffset, 0, _hexHeight * height),
                            Quaternion.LookRotation(Vector3.up));
                    }

                    // Set the instantiated object as a child of this GameObject
                    spawnedObject.transform.SetParent(this.transform, true);
                }

                odd = !odd; // Toggle between odd/even row
            }
        }
    }
}