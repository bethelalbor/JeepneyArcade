using System;
using System.Collections.Generic;
using UnityEngine;

namespace JeepneyArcade.EditorTools
{
    // Legacy serialized types retained for existing profile assets only.
    // The one-click generator does not use profiles or approval records.
    public enum CityColliderShape { Box, PoleBox, PoleCapsule }

    [Serializable]
    public sealed class CityColliderEntry
    {
        public string sourceId;
        public string colliderId;
        public string path;
        public string note;
        public CityColliderShape shape;
        public Vector3 center;
        public Vector3 size;
        public bool approved;
        public bool manual;
    }

    public sealed class CityColliderProfile : ScriptableObject
    {
        public List<CityColliderEntry> entries = new List<CityColliderEntry>();
    }
}
