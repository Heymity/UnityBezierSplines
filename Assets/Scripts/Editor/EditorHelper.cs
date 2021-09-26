using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Bezier.Editor
{
    public class ToggleableProp<T1>
    {
        [SerializeField] private bool active;
        [SerializeField] private T1 value;

        public T1 Value { get => value; set => this.value = value; }
        public bool Active { get => active; set => active = value; }

        public void DoEditor(string toggleLabel, Action drawer)
        {
            active = EditorGUILayout.Toggle(toggleLabel, active);
            EditorGUI.BeginDisabledGroup(!active);
            drawer?.Invoke();
            EditorGUI.EndDisabledGroup();
        }

        public ToggleableProp(bool active, T1 value = default)
        {
            Active = active;
            Value = value;
        }

        public ToggleableProp() : this(false) { }

        public static implicit operator bool(ToggleableProp<T1> a) => a.Active;
        public static implicit operator T1(ToggleableProp<T1> a) => a.Value;
        public static implicit operator ToggleableProp<T1>(bool a) => new ToggleableProp<T1>(a);
        public static implicit operator ToggleableProp<T1>(T1 a) => new ToggleableProp<T1>(false, a);
        public static implicit operator ToggleableProp<T1>((bool active, T1 value) a) => new ToggleableProp<T1>(a.active, a.value);
    }
}