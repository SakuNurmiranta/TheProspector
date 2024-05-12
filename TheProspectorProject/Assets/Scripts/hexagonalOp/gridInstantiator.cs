using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using System.Drawing;
public class gridInstantiator : MonoBehaviour
{
    private const float radialMod = 0.865f; //when radius is one, midline is 0.865 away from hex center.
    private const float radius = 1f;
    private GameObject thisHex;
    public Vector3[] hexesOld;
    public List<Vector3> hexes = new List<Vector3>();
   
    public float y_offsetGizmos; //this value is used to project the gizmos over the hexesOld
    public float gizmoThickness;

    

    private void OnValidate()
    {
      
        thisHex = transform.root.gameObject;
        Vector3 centerPoint = thisHex.transform.position;
        hexesOld = new Vector3[6];
        hexes.Clear();
        projectNeighbors(centerPoint);
       
    
    }

    private void OnDrawGizmos()
    {
        
        Vector3 centerPoint = thisHex.transform.position;

        DrawHex(centerPoint, radius); //rootHexGizmo
        drawGridFromCenter(hexes, radius);
    }
    public void DrawHex(Vector3 center, float radius)
    {
       
        for (int i = 0; i < 6; ++i)
        {
            
            float angle = Mathf.PI / 3f * i;


            Vector3 startPoint = center + new Vector3(radius * Mathf.Sin(angle), y_offsetGizmos, radius * Mathf.Cos(angle));


            float nextAngle = Mathf.PI / 3f * ((i + 1) % 6);
            Vector3 nextStartPoint = center + new Vector3(radius * Mathf.Sin(nextAngle), y_offsetGizmos, radius * Mathf.Cos(nextAngle));
     

            Handles.color = Color.red;

            Handles.DrawLine(startPoint, nextStartPoint, gizmoThickness);
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            Handles.Label(center + new Vector3(-0.25f, 0, .5f), "R", style);


        }
    }

    private void projectNeighbors(Vector3 center)
    {
        for (int i = 0; i < 6; i++)
        {
            if (i <= 6)
            {
                float nextAngle = Mathf.PI / 3f * ((i + 1) % 6);
                Vector3 projectNextGen = center + new Vector3((radius * Mathf.Cos(nextAngle) * radialMod) * 2, 0, (radius * Mathf.Sin(nextAngle) * radialMod) * 2);
                hexesOld[i] = projectNextGen;

                hexes.Add(projectNextGen);
                Debug.Log("Center " + i + " added, with value of " + projectNextGen);
            }
            else if (i > 6)
            { 
                //next circle of neighbors can be formulated into an algorithm
                //we need to further modify radialMod to shoot further around the center
                //alternatively we can repeat the first circle with each neighbor, as long as we check that the new hex hasn't already been drawn
            }

        }

    }

    private void drawGridFromCenter(List<Vector3> project, float radius)
    {
        for (int i = 0; i < project.Count; i++)
        {


            for (int j = 0; j < 6; j++)
            {

                float angle = Mathf.PI / 3f * j;


                Vector3 startPoint = project[i] + new Vector3(radius * Mathf.Sin(angle), y_offsetGizmos, radius * Mathf.Cos(angle));


                float nextAngle = Mathf.PI / 3f * ((j + 1) % 6);
                Vector3 nextStartPoint = project[i] + new Vector3(radius * Mathf.Sin(nextAngle), y_offsetGizmos, radius * Mathf.Cos(nextAngle));



                Handles.color = Color.red;

                Handles.DrawLine(startPoint, nextStartPoint, gizmoThickness);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                Handles.Label(project[i] + new Vector3(-0.25f, 0, .5f), i.ToString(), style);

            }



        }

    }
}




