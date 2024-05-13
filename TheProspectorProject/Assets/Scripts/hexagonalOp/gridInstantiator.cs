using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class gridInstantiator : MonoBehaviour
{
    private const float radialMod = 0.865f; //when radius is one, midline is 0.865 away from hex center.
    private const float radius = 1f;
    private GameObject thisHex;

    [Range(0,18)]public int hexCount = 7;
    public List<Vector3> hexes = new List<Vector3>();
   
   
    public float y_offsetGizmos; //this value is used to project the gizmos over the hexesOld
    public float gizmoThickness;

    SceneView sceneView = SceneView.lastActiveSceneView; public float distanceThreshold = 10.0f; //need these to manipulate number draw distance

    private void Awake()
    {
        thisHex = transform.root.gameObject;
        Vector3 centerPoint = thisHex.transform.position;
        hexes.Clear();
    }
    private void OnValidate()
    {
        thisHex = transform.root.gameObject;
        Vector3 centerPoint = thisHex.transform.position;
        projectNeighbors(centerPoint);
    }

    private void OnDrawGizmosSelected()
    {
       // thisHex = transform.root.gameObject;
        Vector3 centerPoint = thisHex.transform.position;
        
        DrawHex(centerPoint, radius); //rootHexGizmo
        hexes.Clear();
       
        projectNeighbors(centerPoint);
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
        for (int i = 0; i < hexCount; i++)
        {
            bool even = (i % 2) == 0;
            float nextAngle = Mathf.PI / 3f * ((i + 1) % 6);
            if (i < 6)
            {

                Vector3 projectNextGen = center + new Vector3((radius * Mathf.Cos(nextAngle) * radialMod) * 2, 0, (radius * Mathf.Sin(nextAngle) * radialMod) * 2);

                hexes.Add(projectNextGen);
            }
            else if (i >= 6 && even && i < 12)
            {
                Vector3 projectNextGen = center + new Vector3(-(radius * Mathf.Cos(nextAngle) * radialMod) * 4, 0, -(radius * Mathf.Sin(nextAngle) * radialMod) * 4);

                hexes.Add(projectNextGen);
            }
            else if (i > 6 && !even && i < 12)
            {

                Vector3 projectNextGen = center + new Vector3(-(radius * Mathf.Sin(nextAngle) * radialMod) * 3.5f, 0, (radius * Mathf.Cos(nextAngle) * radialMod) * 3.5f);

                hexes.Add(projectNextGen);
            }
            else if (i >= 12 && even)
            {
                Vector3 projectNextGen = center + new Vector3((radius * Mathf.Cos(nextAngle) * radialMod) * 4, 0, (radius * Mathf.Sin(nextAngle) * radialMod) * 4);

                hexes.Add(projectNextGen);
            }
            else if (i > 12 && !even)
            {
                Vector3 projectNextGen = center + new Vector3(-(radius * Mathf.Sin(nextAngle) * radialMod) * 3.5f, 0, -(radius * Mathf.Cos(nextAngle) * radialMod) * 3.5f);
                
                hexes.Add(projectNextGen);
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
                if (sceneView != null)
                {
                    Vector3 viewpoint = sceneView.camera.transform.position;
                    float distance = Vector3.Distance(viewpoint, project[i]);
                    if (distance <= distanceThreshold) Handles.Label(project[i] + new Vector3(-0.25f, 0 + y_offsetGizmos, .5f), i.ToString(), style);
                }
            }



        }

    }
}




