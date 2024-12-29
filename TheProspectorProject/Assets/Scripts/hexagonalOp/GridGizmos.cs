using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

namespace hexagonalOp
{
    //this class is responsible for drawing the hexes in the scene view, based on a selected tile. It contains calculations for drawing with hex logic, but in itself is only editor type code.
    //This is a mess, and I need to seriously rework it, not just on the level of execution but also on the level of "why in the first place".
    public class GridGizmos : MonoBehaviour
    {
        private const float RadialMod = 0.865f; //when radius is one, midline is 0.865 away from hex center. 
        private const float Radius = 1f;
        private GameObject _thisHex;

        [Range(0, 18)] public int hexCount = 7;
        public List<Vector3> hexes = new List<Vector3>();


        [FormerlySerializedAs("y_offsetGizmos")] public float yOffsetGizmos; //this value is used to project the gizmos over the hexesOld
        public float gizmoThickness;

        SceneView _sceneView = SceneView.lastActiveSceneView;
        public float distanceThreshold = 10.0f; //need these to manipulate number draw distance

        private void Awake()
        {
            _thisHex = transform.root.gameObject;
            hexes.Clear();
        }

        private void OnValidate()
        {
            _thisHex = transform.root.gameObject;
            Vector3 centerPoint = _thisHex.transform.position;
            ProjectNeighbors(centerPoint);
        }

        private void OnDrawGizmosSelected()
        {
            // thisHex = transform.root.gameObject;
            Vector3 centerPoint = _thisHex.transform.position;

            DrawHex(centerPoint, Radius); //rootHexGizmo
            hexes.Clear();

            ProjectNeighbors(centerPoint);
            DrawGridFromCenter(hexes, Radius);
        }

        public void DrawHex(Vector3 center, float radius)
        {
            for (int i = 0; i < 6; ++i)
            {
                float angle = Mathf.PI / 3f * i;


                Vector3 startPoint =
                    center + new Vector3(radius * Mathf.Sin(angle), yOffsetGizmos, radius * Mathf.Cos(angle));


                float nextAngle = Mathf.PI / 3f * ((i + 1) % 6);
                Vector3 nextStartPoint = center + new Vector3(radius * Mathf.Sin(nextAngle), yOffsetGizmos,
                    radius * Mathf.Cos(nextAngle));


                Handles.color = Color.red;

                Handles.DrawLine(startPoint, nextStartPoint, gizmoThickness);
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                Handles.Label(center + new Vector3(-0.25f, 0, .5f), "R", style);
            }
        }

        private void ProjectNeighbors(Vector3 center) //this function projects the hexes to the scene view, and looks awful
        {
            for (int i = 0; i < hexCount; i++)
            {
                bool even = (i % 2) == 0;
                float nextAngle = Mathf.PI / 3f * ((i + 1) % 6);
                if (i < 6)
                {
                    Vector3 projectNextGen = center + new Vector3((Radius * Mathf.Cos(nextAngle) * RadialMod) * 2, 0,
                        (Radius * Mathf.Sin(nextAngle) * RadialMod) * 2);

                    hexes.Add(projectNextGen);
                }
                else if (i >= 6 && even && i < 12)
                {
                    Vector3 projectNextGen = center + new Vector3(-(Radius * Mathf.Cos(nextAngle) * RadialMod) * 4, 0,
                        -(Radius * Mathf.Sin(nextAngle) * RadialMod) * 4);

                    hexes.Add(projectNextGen);
                }
                else if (i > 6 && !even && i < 12)
                {
                    Vector3 projectNextGen = center + new Vector3(-(Radius * Mathf.Sin(nextAngle) * RadialMod) * 3.5f,
                        0, (Radius * Mathf.Cos(nextAngle) * RadialMod) * 3.5f);

                    hexes.Add(projectNextGen);
                }
                else if (i >= 12 && even)
                {
                    Vector3 projectNextGen = center + new Vector3((Radius * Mathf.Cos(nextAngle) * RadialMod) * 4, 0,
                        (Radius * Mathf.Sin(nextAngle) * RadialMod) * 4);

                    hexes.Add(projectNextGen);
                }
                else if (i > 12 && !even)
                {
                    Vector3 projectNextGen = center + new Vector3(-(Radius * Mathf.Sin(nextAngle) * RadialMod) * 3.5f,
                        0, -(Radius * Mathf.Cos(nextAngle) * RadialMod) * 3.5f);

                    hexes.Add(projectNextGen);
                }
            }
        }

        private void DrawGridFromCenter(List<Vector3> project, float radius) 
        {
            for (int i = 0; i < project.Count; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    float angle = Mathf.PI / 3f * j;


                    Vector3 startPoint = project[i] + new Vector3(radius * Mathf.Sin(angle), yOffsetGizmos,
                        radius * Mathf.Cos(angle));


                    float nextAngle = Mathf.PI / 3f * ((j + 1) % 6);
                    Vector3 nextStartPoint = project[i] + new Vector3(radius * Mathf.Sin(nextAngle), yOffsetGizmos,
                        radius * Mathf.Cos(nextAngle));


                    Handles.color = Color.red;

                    Handles.DrawLine(startPoint, nextStartPoint, gizmoThickness);
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.red;
                    if (_sceneView != null)
                    {
                        Vector3 viewpoint = _sceneView.camera.transform.position;
                        float distance = Vector3.Distance(viewpoint, project[i]);
                        if (distance <= distanceThreshold)
                            Handles.Label(project[i] + new Vector3(-0.25f, 0 + yOffsetGizmos, .5f), i.ToString(),
                                style);
                    }
                }
            }
        }
    }
}