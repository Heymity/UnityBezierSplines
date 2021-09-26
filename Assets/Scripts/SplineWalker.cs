using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bezier
{
	public class SplineWalker : MonoBehaviour
	{
		[SerializeField] private BezierSpline spline;
		[SerializeField] private float duration;
		[SerializeField] private bool lookForward;
		[SerializeField] private bool useAcc;
		[SerializeField] private SplineWalkerMode mode;

		private bool goingForward = true;
		private float progress;

		private void Update()
		{
			if (goingForward)
			{
				progress += Time.deltaTime / duration;
				if (progress > 1f)
				{
					if (mode == SplineWalkerMode.Once)
						progress = 1f;					
					else if (mode == SplineWalkerMode.Loop)
						progress -= 1f;
					else
					{
						progress = 2f - progress;
						goingForward = false;
					}
				}
			}
			else
			{
				progress -= Time.deltaTime / duration;
				if (progress < 0f)
				{
					progress = -progress;
					goingForward = true;
				}
			}

			Vector3 position = transform.localPosition;

			if (!useAcc)
			{
				position = spline.GetPoint(progress);
				transform.localPosition = position;
			}
			else
            {
				GetComponent<Rigidbody2D>().AddRelativeForce(spline.GetAcceleration(progress) / 20);
            }

			if (lookForward)
			{
				if (spline.Is2D)
                {
					var dir = spline.GetDirection(progress);
					var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
					transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
				}
				else
					transform.LookAt(position + spline.GetDirection(progress));
			}
		}

		public enum SplineWalkerMode
		{
			Once,
			Loop,
			PingPong
		}
	}
}