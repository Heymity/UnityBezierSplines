using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Text;

namespace Bezier.Editor
{
    [CustomEditor(typeof(BezierSpline))]
    public class BezierSplineEditor : UnityEditor.Editor
    {
        private SerializedProperty verticesProperty;
        private SerializedProperty loopProperty;
        private SerializedProperty is2DProperty;
        private SerializedProperty auto3DNormalProperty;

        private BezierSpline spline;
        private Transform handleTransform;
        private Quaternion handleRotation;
        private int pageIndex = 0;
        private bool addingCurvesThruScene = false;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();

            if(GUILayout.Toggle(pageIndex == 0, "Spline", EditorStyles.miniButtonLeft)) pageIndex = 0;
            if(GUILayout.Toggle(pageIndex == 1, "Vertices", EditorStyles.miniButtonMid)) pageIndex = 1;
            if(GUILayout.Toggle(pageIndex == 2, "Point", EditorStyles.miniButtonMid)) pageIndex = 2;
            if(GUILayout.Toggle(pageIndex == 3, "Tools", EditorStyles.miniButtonRight)) pageIndex = 3;

            EditorGUILayout.EndHorizontal();
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;

            switch (pageIndex)
            {
                case 0:
                    DoSplinePage();
                    break;
                case 1:
                    DoVerticesPage();
                    break;
                case 2:
                    DoPointPage();
                    break;
                case 3:
                    DoTools();
                    break;
            }

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private void DoSplinePage()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(loopProperty);
            EditorGUILayout.PropertyField(is2DProperty);
            
            if (!is2DProperty.boolValue)
                EditorGUILayout.PropertyField(auto3DNormalProperty);

            EditorGUILayout.LabelField("Spline Lenght (Aprox.): ", spline.GetAproximatedLenght().ToString());

            addingCurvesThruScene = GUILayout.Toggle(addingCurvesThruScene, "Add Curves At Scene", EditorStyles.miniButton);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Spline Data");
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
            }
        }

        private void DoVerticesPage()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(verticesProperty);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spline, "Vertices Data");
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
            }
        }

        private void DoPointPage()
        {
            var selectedPointIndex = Mathf.RoundToInt(selectedIndex);
            if (selectedPointIndex < 0)
            {
                EditorGUILayout.LabelField("Select a point.");
                return;
            }

            var pointProp = verticesProperty.GetArrayElementAtIndex(selectedPointIndex);
            var point = spline.Vertices[selectedPointIndex];
            var pointPosProp = pointProp.FindPropertyRelative("point");
            var controllPointAfter = pointProp.FindPropertyRelative("controllPointAfter");
            var controllPointBefore = pointProp.FindPropertyRelative("controllPointBefore");
            var lockMode = pointProp.FindPropertyRelative("lockMode");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Point");
            EditorGUILayout.PropertyField(pointPosProp, GUIContent.none);
            EditorGUILayout.LabelField("Controll Point Before");
            EditorGUILayout.PropertyField(controllPointBefore, GUIContent.none);
            EditorGUILayout.LabelField("Controll Point After");
            EditorGUILayout.PropertyField(controllPointAfter, GUIContent.none); 
            EditorGUILayout.LabelField("Up Direction");

            var newDir = EditorGUILayout.Vector3Field(GUIContent.none, point.Up);
            var newRot = EditorGUILayout.Vector3Field("Rotation", point.Rotation.eulerAngles);

            EditorGUILayout.PropertyField(lockMode);

            if (EditorGUI.EndChangeCheck())
            {
                point.Up = newDir;
                point.Rotation = Quaternion.Euler(newRot);

                Undo.RecordObject(spline, "Point Changed");
                verticesProperty.serializedObject.ApplyModifiedProperties();

                SceneView.lastActiveSceneView.Repaint();
            }
        }

        private void DoTools()
        {
            EditorGUI.BeginChangeCheck();

            stepsPerCurve = EditorGUILayout.IntField("Steps Per Curve", stepsPerCurve);

            directions.DoEditor("Show Directions", () => directions.Value = EditorGUILayout.Slider("Direction Line Scale", directions, 0.1f, 15));

            normals.DoEditor("Show Normals", () => normals.Value = EditorGUILayout.Slider("Normal Line Scale", normals.Value, 0.1f, 15));

            acceleration.DoEditor("Show Acceleration", () => acceleration.Value = EditorGUILayout.Slider("Acceleration Line Scale", acceleration, 0.05f, 10));

            curvature.DoEditor("Show Curvature", () => curvature.Value = EditorGUILayout.Slider("Curvature Line Scale", curvature, 0.1f, 10));

            curvatureCircle.DoEditor("Show Curvature Circle", () => curvatureCircle.Value = EditorGUILayout.Slider("Curvature Circle Position", curvatureCircle, 0f, 1f));

            orientation.DoEditor("Show Orientation", () =>
            {
                var newValue = (0f, 0f);
                newValue.Item1 = EditorGUILayout.Slider("Orientation Scale", orientation.Value.scale, 0.1f, 10f);
                newValue.Item2 = EditorGUILayout.Slider("Orientation Offset", orientation.Value.offset, 0f, 1f);

                orientation.Value = newValue;

                if (!orientation) expandedPoints = false;
                expandedPoints.DoEditor("Draw Expanded Points", () =>
                {
                    expandedPoints.Value = EditorGUILayout.Slider("Drawing Distance", expandedPoints.Value, 0.05f, 10f);
                    expandedPointsSteps = EditorGUILayout.IntSlider("Expanded Points Steps", expandedPointsSteps, 0, 10);
                    mirrorExpandedPoints = EditorGUILayout.Toggle("Mirror Points", mirrorExpandedPoints);
                });
            });

            interstages.DoEditor("Draw Interstages", () => interstages.Value = EditorGUILayout.Slider("Interstage Drawing Position", interstages, 0f, 1f));

            showBoundingBox = EditorGUILayout.Toggle("Show Bounding Box", showBoundingBox);
            showAllBoundingBoxes = EditorGUILayout.Toggle("Show All Bounding Boxes", showAllBoundingBoxes);

            atTBoundingBox.DoEditor("Show Bounding Box At T", () =>
            {
                EditorGUI.BeginDisabledGroup(!atTBoundingBox);
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Bounding Box At T...");
                EditorGUILayout.LabelField("Bounding Box At Curve...");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                var newValue = EditorGUILayout.Slider(atTBoundingBox, 0f, 1f);
                if (newValue != atTBoundingBox.Value)
                    atTBoundingBox.Value = newValue;

                var splineLenght = spline.CurveCount - 1;
                var newCurveValue = EditorGUILayout.IntSlider(ConvertToCurveIndex(atTBoundingBox.Value), 0, splineLenght);
                if (ConvertToCurveIndex(atTBoundingBox.Value) != newCurveValue)
                    atTBoundingBox.Value = (float)newCurveValue / splineLenght;


                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUI.EndDisabledGroup();

                int ConvertToCurveIndex(float t) => (int)(t * (spline.CurveCount - 1));
            });

            if (EditorGUI.EndChangeCheck())
                SceneView.lastActiveSceneView.Repaint();                            
        }

        private int stepsPerCurve = 30;

        private ToggleableProp<float> directions = (false, 2f);
        private ToggleableProp<float> normals = (false, 1f);
        private ToggleableProp<float> acceleration = (false, 0.2f);
        private ToggleableProp<float> curvature = (false, 0.2f);
        private ToggleableProp<float> curvatureCircle = (false, 0f);
        private ToggleableProp<(float scale, float offset)> orientation = (false, (0.1f, 0f));
        private ToggleableProp<float> interstages = (false, 0f);
        private ToggleableProp<float> atTBoundingBox = (false, 0f);
        private ToggleableProp<float> expandedPoints = (false, 0f);

        private int expandedPointsSteps = 2;
        private bool mirrorExpandedPoints = true;
        private bool showBoundingBox = false;
        private bool showAllBoundingBoxes = false;

        private void Serialize()
        {
            var prefs = new StringBuilder();
            prefs.Append($"{JsonUtility.ToJson(directions)};");
            prefs.Append($"{JsonUtility.ToJson(normals)};");
            prefs.Append($"{JsonUtility.ToJson(acceleration)};");
            prefs.Append($"{JsonUtility.ToJson(curvature)};");
            prefs.Append($"{JsonUtility.ToJson(curvatureCircle)};");
            prefs.Append($"{JsonUtility.ToJson(orientation)};");
            prefs.Append($"{JsonUtility.ToJson(interstages)};");
            prefs.Append($"{JsonUtility.ToJson(atTBoundingBox)};");
            prefs.Append($"{Convert.ToString(showBoundingBox)};");
            prefs.Append($"{Convert.ToString(showAllBoundingBoxes)};");

            spline.editorPrefs = prefs.ToString();
        }

        private void Deserialize()
        {
            var prefsStr = spline.editorPrefs;

            if (string.IsNullOrEmpty(prefsStr)) return;

            var prefs = prefsStr.Split(';');

            if (prefs.Length < 10) return;

            directions = JsonUtility.FromJson<ToggleableProp<float>>(prefs[0]);
            normals = JsonUtility.FromJson<ToggleableProp<float>>(prefs[1]);
            acceleration = JsonUtility.FromJson<ToggleableProp<float>>(prefs[2]);
            curvature = JsonUtility.FromJson<ToggleableProp<float>>(prefs[3]);
            curvatureCircle = JsonUtility.FromJson<ToggleableProp<float>>(prefs[4]);
            orientation = JsonUtility.FromJson<ToggleableProp<(float scale, float offset)>>(prefs[5]);
            interstages = JsonUtility.FromJson<ToggleableProp<float>>(prefs[6]);
            atTBoundingBox = JsonUtility.FromJson<ToggleableProp<float>>(prefs[7]);
            showBoundingBox = Convert.ToBoolean(prefs[8]);
            showAllBoundingBoxes = Convert.ToBoolean(prefs[9]);
        }

        private const float handleSize = 0.04f;
        private const float pickSize = 0.06f;
        private const float accDivider = 10f;
        private const float orientationDivider = 100f;
        private const float bezierPointScale = 0.05f;

        private float selectedIndex = -1;

        private void OnSceneGUI()
        {
            spline = target as BezierSpline;
            handleTransform = spline.transform;
            handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;

            DrawPoints();
            DrawBezierSpline();
            if (directions) ShowDirections();
            if (normals) ShowNormals();
            if (acceleration) ShowAcceleration();
            if (curvature) ShowCurvature();
            if (curvatureCircle) ShowCurvatureCircle();
            if (orientation) ShowOrientation();
            if (interstages) DrawInterstages();
            if (showBoundingBox) ShowBoundingBox();
            if (showAllBoundingBoxes) ShowAllBoundingBoxes();
            if (atTBoundingBox) ShowBoundingBoxAtT();

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1) selectedIndex = -1;  
        }

        private void DrawPoints()
        {
            for (int i = 0; i < spline.Vertices.Count - 1; i++)
            {
                var pointA = spline.Vertices[i];
                var pointB = spline.Vertices[i + 1];

                var pa = ShowPoint(i);
                var pb = ShowPoint(i + 1);

                var ca = ShowAfterControll(pointA, i + 0.3f);
                var cb = ShowBeforeControll(pointB, i + 0.6f);

                Handles.color = Color.grey;
                Handles.DrawLine(pa, ca);
                Handles.DrawLine(pb, cb);
            }

            if (spline.Loop)
            {
                var firstPoint = spline.Vertices.FirstOrDefault();
                var lastPoint = spline.Vertices.LastOrDefault();

                var p1ControllBefore = ShowBeforeControll(firstPoint, -0.4f);
                var p2ControllAfter = ShowAfterControll(lastPoint, spline.Vertices.Count - 0.7f);

                Handles.color = Color.grey;
                Handles.DrawLine(handleTransform.TransformPoint(firstPoint.Point), p1ControllBefore);
                Handles.DrawLine(handleTransform.TransformPoint(lastPoint.Point), p2ControllAfter);
            }
        }

        private void ShowDirections()
        {
            Handles.color = Color.green;

            var steps = stepsPerCurve * (spline.Vertices.Count + Convert.ToInt32(spline.Loop));
            for (int i = 0; i < steps; i++)
            {
                var point = spline.GetPoint(i / (float)steps);
                Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directions);
            }
        }

        private void ShowNormals()
        {
            Handles.color = Color.cyan;

            var steps = stepsPerCurve * (spline.Vertices.Count + Convert.ToInt32(spline.Loop));
            for (int i = 0; i < steps; i++)
            {
                var point = spline.GetPoint(i / (float)steps);
                Handles.DrawLine(point, point + spline.GetNormal(i / (float)steps) * normals.Value);
            }
        }

        private void ShowAcceleration()
        {
            Handles.color = Color.red;

            var steps = stepsPerCurve * (spline.Vertices.Count + Convert.ToInt32(spline.Loop));
            for (int i = 0; i < steps; i++)
            {
                var point = spline.GetPoint(i / (float)steps);
                Handles.DrawLine(point, point + spline.GetAcceleration(i / (float)steps) * acceleration / accDivider);
            }
        }

        private void ShowCurvature()
        {
            Handles.color = new Color(0.9f, 0.75f, 0.2f);

            var steps = stepsPerCurve * (spline.Vertices.Count + Convert.ToInt32(spline.Loop));
            for (int i = 0; i < steps; i++)
            {
                var point = spline.GetPoint(i / (float)steps);
                var normal = spline.GetNormal(i / (float)steps);
                Handles.DrawLine(point, point + normal * spline.GetCurvature(i / (float)steps) * curvature);
            }
        }

        private void ShowCurvatureCircle()
        {
            Handles.color = new Color(0.9f, 0.75f, 0.9f);
            Vector3 point = spline.GetPoint(curvatureCircle);
            var normal = spline.GetNormal(curvatureCircle);
            var curvature = 1f / spline.GetCurvature(curvatureCircle);
            var center = point + (normal * curvature);
            var dir = spline.GetDirection(curvatureCircle);

            var circleNormal = Vector3.Cross(normal, dir);
            Handles.DrawWireDisc(center, circleNormal, curvature);

            Handles.color = Color.green;
            float size = HandleUtility.GetHandleSize(point);
            Handles.DrawSolidDisc(point, circleNormal, bezierPointScale * size);
        }

        private void ShowOrientation()
        {
            var steps = stepsPerCurve * (spline.Vertices.Count + Convert.ToInt32(spline.Loop)) * orientation.Value.scale / orientationDivider;
            for (int i = 0; i < steps; i++)
            {
                var opPoint = spline.GetOrientedPoint((i / (float)steps) + orientation.Value.offset % 1);
                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(opPoint.Pos, opPoint.Rot);

                if (EditorGUI.EndChangeCheck())
                {
                    // Should divide by the bezier lenght maybe?
                    var dist = Vector3.Distance(opPoint.Pos, newPos) / 1000f;
                    orientation.Value = (orientation.Value.scale, Mathf.Clamp01(orientation.Value.offset + dist));
                }

                if (expandedPoints)
                {
                    for (int j = mirrorExpandedPoints ? -expandedPointsSteps : 1; j <= expandedPointsSteps; j++)
                    {
                        if (j == 0) continue;
                        var normal = SceneView.currentDrawingSceneView.camera.transform.forward;
                        var pos = opPoint.LocalToWorld((spline.Is2D ? Vector3.up : Vector3.right) * expandedPoints * (j / (float)expandedPointsSteps));
                        float size = HandleUtility.GetHandleSize(pos);

                        Handles.color = Color.red;
                        Handles.DrawSolidDisc(pos, normal, 1.5f * bezierPointScale * size);
                    }
                }
            }
        }

        private void DrawInterstages()
        {
            var listSize = spline.Loop ? spline.Vertices.Count : spline.Vertices.Count - 1;
            var t = interstages == 1 ? 1 : interstages * listSize % 1;
            var (firstVertex, secondVertex) = spline.GetVertexes(interstages);

            var pointA = handleTransform.TransformPoint(firstVertex.Point);
            var pointB = handleTransform.TransformPoint(secondVertex.Point);
            var pointAControllAfter = handleTransform.TransformPoint(firstVertex.ControllPointAfterAbsolute);
            var pointBControllBefore = handleTransform.TransformPoint(secondVertex.ControllPointBeforeAbsolute);

            Handles.color = Color.grey;
            Handles.DrawLine(pointAControllAfter, pointBControllBefore);

            var tPA_PAC = Vector3.Lerp(pointA, pointAControllAfter, t);
            var tPAC_PBC = Vector3.Lerp(pointAControllAfter, pointBControllBefore, t);
            var tPBC_PB = Vector3.Lerp(pointBControllBefore, pointB, t);

            Handles.DrawLine(tPA_PAC, tPAC_PBC);
            Handles.DrawLine(tPAC_PBC, tPBC_PB);

            var tPA_PBC = Vector3.Lerp(tPA_PAC, tPAC_PBC, t);
            var tPAC_PB = Vector3.Lerp(tPAC_PBC, tPBC_PB, t);

            Handles.DrawLine(tPA_PBC, tPAC_PB);

            var normal = SceneView.currentDrawingSceneView.camera.transform.forward;
            var point = spline.GetPoint(interstages);
            float size = HandleUtility.GetHandleSize(point);

            Handles.color = Color.green;
            Handles.DrawSolidDisc(point, normal, bezierPointScale * size);
            Handles.DrawSolidDisc(tPA_PAC, normal, bezierPointScale * size / 2);
            Handles.DrawSolidDisc(tPAC_PBC, normal, bezierPointScale * size / 2);
            Handles.DrawSolidDisc(tPBC_PB, normal, bezierPointScale * size / 2);
            Handles.DrawSolidDisc(tPA_PBC, normal, bezierPointScale * size / 2);
            Handles.DrawSolidDisc(tPAC_PB, normal, bezierPointScale * size / 2);
        }

        private void DrawBezierSpline()
        {
            for (int i = 0; i < spline.Vertices.Count - 1; i++)
            {
                var p0 = handleTransform.TransformPoint(spline.Vertices[i].Point);
                var p1 = handleTransform.TransformPoint(spline.Vertices[i].ControllPointAfterAbsolute);
                var p2 = handleTransform.TransformPoint(spline.Vertices[i + 1].ControllPointBeforeAbsolute);
                var p3 = handleTransform.TransformPoint(spline.Vertices[i + 1].Point);
                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            }

            if (spline.Loop)
            {
                var firstPoint = spline.Vertices.FirstOrDefault();
                var lastPoint = spline.Vertices.LastOrDefault();

                var p0 = handleTransform.TransformPoint(firstPoint.Point);
                var p2 = handleTransform.TransformPoint(lastPoint.ControllPointAfterAbsolute);
                var p1 = handleTransform.TransformPoint(firstPoint.ControllPointBeforeAbsolute);
                var p3 = handleTransform.TransformPoint(lastPoint.Point);
                Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            }
        }

        private void ShowBoundingBox()
        {
            var bounds = spline.GetBoundingBox();

            DrawBoundingBox(bounds);
        }

        private void ShowAllBoundingBoxes()
        {
            var boundingBoxes = spline.GetBoundingBoxes();

            boundingBoxes.ForEach(box => DrawBoundingBox(box));
        }

        private void ShowBoundingBoxAtT()
        {
            var bounds = spline.GetCurveBoundingBox(atTBoundingBox);

            DrawBoundingBox(bounds);
        }

        private void DrawBoundingBox(Bounds bounds)
        {
            Handles.color = Color.green;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }

        private Vector3 ShowPoint(int index)
        {
            if (index >= spline.Vertices.Count) return Vector3.zero;

            Vector3 point = handleTransform.TransformPoint(spline.Vertices[index].Point);
            DoSelectionButton(point, index);

            if (selectedIndex == index)
            {
                DoPointControlls(point, spline.Vertices[index].Rotation, (newPos, quaternion) =>
                {
                    Undo.RecordObject(spline, "Move Point");
                    EditorUtility.SetDirty(spline);
                    spline.Vertices[index].Point = handleTransform.InverseTransformPoint(newPos);
                    spline.Vertices[index].Rotation = quaternion;
                });
            }
            return point;
        }

        private Vector3 ShowAfterControll(BezierVertex point, float index)
        {
            var pointPos = handleTransform.TransformPoint(point.ControllPointAfterAbsolute);

            DoSelectionButton(pointPos, index);

            if (index == selectedIndex)
            {
                DoPointControlls(pointPos, point.Rotation, (newPos, quaternion) =>
                {
                    Undo.RecordObject(spline, "Move Point");
                    EditorUtility.SetDirty(spline);
                    point.ControllPointAfterAbsolute = handleTransform.InverseTransformPoint(newPos);
                    point.Rotation = quaternion;
                });
            }
            return pointPos;
        }

        private Vector3 ShowBeforeControll(BezierVertex point, float index)
        {
            var pointPos = handleTransform.TransformPoint(point.ControllPointBeforeAbsolute);

            DoSelectionButton(pointPos, index);

            if (index == selectedIndex)
            {
                DoPointControlls(pointPos, point.Rotation, (newPos, quaternion) =>
                {
                    Undo.RecordObject(spline, "Move Point");
                    EditorUtility.SetDirty(spline);
                    point.ControllPointBeforeAbsolute = handleTransform.InverseTransformPoint(newPos);
                    point.Rotation = quaternion;
                });
            }
            return pointPos;
        }

        private void DoPointControlls(Vector3 pos, Quaternion quaternion, Action<Vector3, Quaternion> whenChanged)
        {
            EditorGUI.BeginChangeCheck();

            switch (Tools.current)
            {
                case Tool.Move:
                    pos = Handles.DoPositionHandle(pos, handleRotation);
                    break;
                case Tool.Rotate:
                    if (quaternion.x == 0 && quaternion.y == 0 && quaternion.z == 0 && quaternion.w == 0) quaternion = Quaternion.identity;
                    quaternion = Handles.DoRotationHandle(quaternion, pos);
                    break;
                case Tool.Transform:
                    if (quaternion.x == 0 && quaternion.y == 0 && quaternion.z == 0 && quaternion.w == 0) quaternion = Quaternion.identity;
                    Handles.TransformHandle(ref pos, ref quaternion);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                whenChanged?.Invoke(pos, quaternion);
        }


        private void DoSelectionButton(Vector3 position, float index)
        {
            Handles.color = Color.white;
            float size = HandleUtility.GetHandleSize(position);
            if (Handles.Button(position, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
            {
                selectedIndex = index;
                Repaint();
            }
        }

        private void OnEnable()
        {
            verticesProperty = serializedObject.FindProperty("vertices");
            loopProperty = serializedObject.FindProperty("loop");
            is2DProperty = serializedObject.FindProperty("is2D");
            auto3DNormalProperty = serializedObject.FindProperty("auto3DNormal");

            spline = target as BezierSpline;

            Deserialize();
        }

        private void OnDisable()
        {
            Serialize();
        }
    }
}
