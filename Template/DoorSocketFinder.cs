using DunGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace YourThunderstoreTeam
{
    internal class DoorSocketFinder : MonoBehaviour
    {

        public Doorway GetCopyOf(Doorway comp, Doorway other)
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp;
        }

        private void Awake()
        {
            var objs = FindObjectsByType<Doorway>(FindObjectsSortMode.None);
            foreach (var door in objs)
            {
                if (door.name == "NormalDoor")
                {
                    //this.gameObject.AddComponent<Doorway>(door);
                    return;
                }
            }
        }
    }
}
