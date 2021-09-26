using UnityEngine;

public class Line : MonoBehaviour
{
	[SerializeField] private Vector3 p0;
	[SerializeField] private Vector3 p1;

    public Vector3 PointA { get => p0; set => p0 = value; }
    public Vector3 PointB { get => p1; set => p1 = value; }
}