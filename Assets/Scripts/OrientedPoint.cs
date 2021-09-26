using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bezier
{
    public struct OrientedPoint
    {
        private Vector3 pos;
        private Quaternion rot;

        public Vector3 Pos { get => pos; set => pos = value; }
        public Quaternion Rot { get => rot; set => rot = value; }
        
        public OrientedPoint(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }

        public OrientedPoint(Vector3 pos, Vector3 forward)
        {
            this.pos = pos;
            this.rot = Quaternion.LookRotation(forward);
        }

        public Vector3 LocalToWorld(Vector3 localSpace) => pos + rot * localSpace;     
    }
}