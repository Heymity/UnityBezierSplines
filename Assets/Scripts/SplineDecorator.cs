using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bezier
{
	public class SplineDecorator : MonoBehaviour
	{
		[SerializeField] private BezierSpline spline;
		[SerializeField] private int frequency;
		[SerializeField] private bool lookForward;
		[SerializeField] private Transform[] items;

		private void Awake()
		{
			if (frequency <= 0 || items == null || items.Length == 0)
				return;

			float stepSize = frequency * items.Length;
			if (spline.Loop || stepSize == 1)
				stepSize = 1f / stepSize;
			else
				stepSize = 1f / (stepSize - 1);

			for (int p = 0, f = 0; f < frequency; f++)
			{
				for (int i = 0; i < items.Length; i++, p++)
				{
					Transform item = Instantiate(items[i]);
					Vector3 position = spline.GetPoint(p * stepSize);
					item.transform.localPosition = position;
					if (lookForward)
					{
						if (spline.Is2D)
						{
							var dir = spline.GetDirection(p * stepSize);
							var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
							item.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
						}
						else
							item.transform.LookAt(position + spline.GetDirection(p * stepSize));
					}
					item.transform.parent = transform;
				}
			}
		}
	}
}