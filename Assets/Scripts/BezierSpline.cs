using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bezier
{
    public class BezierSpline : MonoBehaviour
    {
        [SerializeField] private List<BezierVertex> vertices;
        [SerializeField] private bool loop;
        [SerializeField] private bool is2D;
        [SerializeField] private bool auto3DNormal;

#if UNITY_EDITOR
        public string editorPrefs;
#endif

        public List<BezierVertex> Vertices { get => vertices; set => vertices = value; }
        public bool Loop
        {
            get => loop;
            set
            {
                if (loop != value)               
                    loop = value;                
            }
        }
        public bool Is2D => is2D;
        public int CurveCount => Loop ? Vertices.Count : Vertices.Count - 1;
        public bool Auto3DNormal { get => auto3DNormal; set => auto3DNormal = value; }

        public (BezierVertex firstVertex, BezierVertex secondVertex) GetVertexes(float t)
        {
            var index = (int)(t * CurveCount);

            var firstVertex = vertices.FirstOrDefault();
            var secondVertex = vertices.FirstOrDefault();

            if (index >= Vertices.Count - 1)
            {
                if (Loop)
                {
                    firstVertex = vertices.LastOrDefault();
                    secondVertex = vertices.FirstOrDefault();
                }
                else
                {
                    firstVertex = vertices[index - 1];
                    secondVertex = vertices[index];
                }
            }
            else if (index < Vertices.Count - 1)
            {
                firstVertex = vertices[index];
                secondVertex = vertices[index + 1];
            }

            return (firstVertex, secondVertex);
        }

        public Vector3 GetPoint(float t)
        {
            t = Mathf.Clamp01(t);

            var (firstVertex, secondVertex) = GetVertexes(t);

            return transform.TransformPoint(
            CubicBezier.GetPoint(
                firstVertex.Point,
                firstVertex.ControllPointAfterAbsolute,
                secondVertex.ControllPointBeforeAbsolute,
                secondVertex.Point, 
                t == 1 ? 1 : t * CurveCount % 1
            ));
        }

        public Vector3 GetVelocity(float t)
        {
            t = Mathf.Clamp01(t);

            var (firstVertex, secondVertex) = GetVertexes(t);

            return transform.TransformPoint(
            CubicBezier.GetFirstDerivative(
                firstVertex.Point,
                firstVertex.ControllPointAfterAbsolute,
                secondVertex.ControllPointBeforeAbsolute,
                secondVertex.Point,
                t == 1 ? 1 : t * CurveCount % 1
            ));
        }

        public Vector3 GetDirection(float t) => GetVelocity(t).normalized;
        public Vector3 GetTangent(float t) => GetVelocity(t).normalized;
        
        public Vector3 GetNormal(float t, Vector3? up = null)
        {
            var dir = GetDirection(t);

            if (Is2D) return new Vector3(-dir.y, dir.x, 0);
            else
            {
                /// The problem here is that the normal of a 3D vector is actualy a plane, and not a vector. So we need to define which vector is the right one for the normal. The actual best way I think to do this is to use the second solution but store the up vector individually in each bezier vertex, so consisten normals can exist, and free controll of them is possible.
                if (Auto3DNormal)
                {
                    /// https://pomax.github.io/bezierinfo/#pointvectors3d
                    var b = (dir + GetAcceleration(t)).normalized;
                    var r = Vector3.Cross(dir, b).normalized;
                    var normal = Vector3.Cross(r, dir).normalized;
                    return normal;
                }
                else
                {
                    // https://docs.google.com/presentation/d/10XjxscVrm5LprOmG-VB2DltVyQ_QygD26N6XC2iap2A/edit#slide=id.gdd21cdb12_0_26
                    if (up == null) up = Vector3.up;

                    var size = Loop ? Vertices.Count : Vertices.Count - 1;
                    var (firstVertex, secondVertex) = GetVertexes(t);
                    up = Vector3.Lerp(firstVertex.Up, secondVertex.Up, t == 1 ? 1 : t * size % 1).normalized;

                    //return Quaternion.LookRotation(dir, up.GetValueOrDefault()) * Vector3.up;

                    var binormal = Vector3.Cross(dir, up.GetValueOrDefault()).normalized;
                    var normal = Vector3.Cross(binormal, dir).normalized;
                    return normal;
                }
            }
        }

        public Quaternion GetOrientation(float t, Vector3? up = null)
        {
            var dir = GetDirection(t);
            var normal = GetNormal(t, up);
            return Quaternion.LookRotation(dir, normal);
        }

        public OrientedPoint GetOrientedPoint(float t) => new OrientedPoint(GetPoint(t), GetOrientation(t));
        
        public Vector3 GetAcceleration(float t)
        {
            t = Mathf.Clamp01(t);

            var (firstVertex, secondVertex) = GetVertexes(t);

            return transform.TransformPoint(
            CubicBezier.GetSecondDerivative(
                firstVertex.Point,
                firstVertex.ControllPointAfterAbsolute,
                secondVertex.ControllPointBeforeAbsolute,
                secondVertex.Point,
                t == 1 ? 1 : t * CurveCount % 1
            ));
        }

        public float GetCurvature(float t)
        {
            t = Mathf.Clamp01(t);

            var (firstVertex, secondVertex) = GetVertexes(t);

            return CubicBezier.GetCurvature(
                firstVertex.Point,
                firstVertex.ControllPointAfterAbsolute,
                secondVertex.ControllPointBeforeAbsolute,
                secondVertex.Point,
                t == 1 ? 1 : t * CurveCount % 1
            );
        }

        public Bounds GetCurveBoundingBox(float t)
        {
            t = Mathf.Clamp01(t);

            var (firstVertex, secondVertex) = GetVertexes(t);

            return CubicBezier.GetBoundingBox(
                firstVertex.Point,
                firstVertex.ControllPointAfterAbsolute,
                secondVertex.ControllPointBeforeAbsolute,
                secondVertex.Point
            );
        }

        public Bounds GetCurveBoundingBox(int curveIndex) => GetCurveBoundingBox((float)curveIndex / CurveCount);   

        public Bounds GetBoundingBox()
        {
            var xList = new List<float>();
            var yList = new List<float>();
            var zList = new List<float>();

            for (int i = 0; i <= CurveCount; i++)
            {
                var curveBound = GetCurveBoundingBox(i);

                var max = curveBound.max;
                var min = curveBound.min;

                xList.Add(max.x);
                xList.Add(min.x);
                yList.Add(max.y);
                yList.Add(min.y);
                zList.Add(max.z);
                zList.Add(min.z);
            }

            var maxX = xList.Max();
            var minX = xList.Min();
            var maxY = yList.Max();
            var minY = yList.Min();
            var maxZ = zList.Max();
            var minZ = zList.Min();

            var center = new Vector3((maxX - minX) / 2 + minX, (maxY - minY) / 2 + minY, (maxZ - minZ) / 2 + minZ);
            var size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);

            return new Bounds(center, size);
        }

        public List<Bounds> GetBoundingBoxes()
        {
            var bounds = new List<Bounds>();

            for (int i = 0; i <= CurveCount; i++)
                bounds.Add(GetCurveBoundingBox(i));

            return bounds;
        }

        public float GetAproximatedLenght(int stepsPerCurve = -1)
        {
            int steps;
            if (stepsPerCurve <= 0)
                steps = 32 * CurveCount;
            else
                steps = stepsPerCurve * CurveCount;

            var dist = 0f;
            var prevPoint = GetPoint(0f);
            for (int i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var point = GetPoint(t);

                dist += Vector3.Distance(prevPoint, point);

                prevPoint = point;
            }

            return dist;
        }

        public float GetAproximatedLenght(float t, int stepsPerCurve = -1) => t * GetAproximatedLenght(stepsPerCurve);

        public Vector3 GetPointByDistance(float t) => GetPoint(t / GetAproximatedLenght());
    }

    [System.Serializable]
    public class BezierVertex
    {
        [SerializeField] private Vector3 point;
        [SerializeField] private Vector3 controllPointAfter;
        [SerializeField] private Vector3 controllPointBefore;

        [SerializeField] private Quaternion rotation = Quaternion.identity;

        [SerializeField] private LockMode lockMode;

        public Vector3 Point { get => point; set => point = value; }
        public Vector3 ControllPointAfter 
        { 
            get => ControllsLockMode == LockMode.BeforeSymetric ? -ControllPointBefore : controllPointAfter;
            set
            {
                if (ControllsLockMode == LockMode.BeforeSymetric)
                    ControllPointBefore = -value;
                else
                    controllPointAfter = value;
            }
        }
        public Vector3 ControllPointBefore 
        { 
            get => ControllsLockMode == LockMode.AfterSymetric ? -ControllPointAfter : controllPointBefore;
            set
            {
                if (ControllsLockMode == LockMode.AfterSymetric)
                    ControllPointAfter = -value;
                else
                    controllPointBefore = value;
            }
        }
        public Vector3 ControllPointAfterAbsolute { get => Point + ControllPointAfter; set => ControllPointAfter = value - Point; }
        public Vector3 ControllPointBeforeAbsolute { get => Point + ControllPointBefore; set => ControllPointBefore = value - Point; }

        public LockMode ControllsLockMode { get => lockMode; set => lockMode = value; }
        public Quaternion Rotation { get => rotation; set => rotation = value; }
        public Vector3 Up 
        { 
            get => rotation * Vector3.up;
            set
            {
                rotation = Quaternion.FromToRotation(Vector3.up, value);
            }
        }

        public BezierVertex(Vector3 point, Vector3 controllPointBefore, Vector3 controllPointAfter)
        {
            this.point = point;
            this.controllPointAfter = controllPointAfter;
            this.controllPointBefore = controllPointBefore;
            this.lockMode = LockMode.None;
        }

        public enum LockMode
        {
            None,
            BeforeSymetric,
            AfterSymetric
        }
    }
}