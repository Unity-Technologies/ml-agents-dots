using System;
using System.Reflection;
using UnityEngine;

namespace ECS_MLAgents_v0.Core
{
    public class SensorAttribute : Attribute{ }
    
    public class RewardAttribute : SensorAttribute{ }
    
    // Done could be implemented with a reactive system or a new attribute
    public class DoneAttribute : SensorAttribute{ }


    public class AttributeUtility
    {
        // Note : This is non blittable
        public struct SensorMetadata
        {
            public string Name;
            public int[] ByteDimension;
            public int StartIndex;
            public Type SensorType;
        }
        
        public static void GetSensorMetaData(Type t)
        {
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                Debug.Log(f);
                var attributes = f.GetCustomAttributes<SensorAttribute>();
                foreach (var at in attributes)
                {
                    Debug.Log("\t"+at);
                }
            }
        }
    }
}
