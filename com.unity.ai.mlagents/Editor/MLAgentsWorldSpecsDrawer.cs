#define CUSTOM_EDITOR
using Barracuda;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

#if CUSTOM_EDITOR

namespace Unity.AI.MLAgents.Editor
{
    internal static class SpecsPropertyNames
    {
        public const string k_Name = "Name";
        public const string k_NumberAgents = "NumberAgents";
        public const string k_ActionType = "ActionType";
        public const string k_ObservationShapes = "ObservationShapes";
        public const string k_ActionSize = "ActionSize";
        public const string k_DiscreteActionBranches = "DiscreteActionBranches";
        public const string k_Model = "Model";
        public const string k_InferenceDevice = "InferenceDevice";
    }


    /// <summary>
    /// PropertyDrawer for BrainParameters. Defines how BrainParameters are displayed in the
    /// Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(MLAgentsWorldSpecs))]
    internal class MLAgentsWorldSpecsDrawer : PropertyDrawer
    {
        // The height of a line in the Unity Inspectors
        const float k_LineHeight = 21f;
        const float k_LabelHeight = 18f;
        const float k_WarningBoxHeight = 36f;
        const float k_WarningLineHeight = 39f;

        static List<string> m_Warnings = new List<string>();

        float m_TotalHeight = 0f;

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateWarnings(property);

            var nbLines = 0;
            nbLines += GetHeightObservationShape(property);
            nbLines += GetHeightDiscreteAction(property);
            nbLines += 7; // TODO : COMPUTE
            m_TotalHeight = k_LineHeight * nbLines + k_WarningLineHeight * GetHeightWarnings(property);

            return m_TotalHeight + 6f;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            position.height = k_LabelHeight;
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.DrawRect(
                new Rect(position.x - 3f, position.y, position.width + 6f, m_TotalHeight),
                new Color(0f, 0f, 0f, 0.1f));

            EditorGUI.LabelField(position, "ML-Agents World Specs : " + label.text);
            position.y += k_LineHeight;
            EditorGUI.indentLevel++;

            // Name
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_Name),
                new GUIContent("World Name", "The name of the World"));
            position.y += k_LineHeight;

            // Number of Agents
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_NumberAgents),
                new GUIContent("Number of Agents", "The maximum number of Agents that can request a Decision in the same step"));
            position.y += k_LineHeight;

            // Observation Shapes
            DrawObservationShape(position, property);
            position.y += k_LineHeight * GetHeightObservationShape(property);

            // Draw Action Type
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_ActionType),
                new GUIContent("Action Type", "The type of Action : Discrete or continuous"));
            position.y += k_LineHeight;

            // Draw Action Size
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_ActionSize),
                new GUIContent("Action Size", "TODO"));
            position.y += k_LineHeight;

            // Draw discrete Action Branches
            DrawDiscreteActionBranches(position, property);
            position.y += k_LineHeight * GetHeightDiscreteAction(property);

            // Draw NNModel and Inference Device
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_Model),
                new GUIContent("Model", "The Model used for inference"));
            position.y += k_LineHeight;
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_InferenceDevice),
                new GUIContent("Inference Device", "TODO"));
            position.y += k_LineHeight;

            // Draw Warnings
            position.height = k_WarningBoxHeight;
            DrawWarnings(position, property);
            position.y += k_LineHeight * GetHeightWarnings(property);
            position.height = k_LineHeight;

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = indent;
        }

        static void DrawObservationShape(Rect position, SerializedProperty property)
        {
            var observationShapes = property.FindPropertyRelative(SpecsPropertyNames.k_ObservationShapes);
            EditorGUI.LabelField(position, "Observation Shapes");
            position.y += k_LineHeight;

            if (GUI.Button(new Rect(position.x, position.y, position.width / 2, position.height), "Add"))
            {
                observationShapes.arraySize++;
            }
            if (GUI.Button(new Rect(position.x + position.width / 2, position.y, position.width / 2, position.height), "Remove"))
            {
                observationShapes.arraySize--;
            }
            position.y += k_LineHeight;

            for (var i = 0; i < observationShapes.arraySize; i++)
            {
                EditorGUI.PropertyField(position,
                    observationShapes.GetArrayElementAtIndex(i),
                    new GUIContent(""));
                position.y += k_LineHeight;
            }
        }

        static int GetHeightObservationShape(SerializedProperty property)
        {
            var observationShapes = property.FindPropertyRelative(SpecsPropertyNames.k_ObservationShapes);
            return (observationShapes.arraySize + 2);
        }

        static void DrawDiscreteActionBranches(Rect position, SerializedProperty property)
        {
            var actionTypeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ActionType);
            var actionType = GetValue<ActionType>(actionTypeProperty);
            var actionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ActionSize);
            var actionSize = GetValue<int>(actionSizeProperty);
            if (actionType == ActionType.DISCRETE)
            {
                EditorGUI.indentLevel++;
                EditorGUI.LabelField(position, "Discrete Action Options");
                position.y += k_LineHeight;

                var actionBranchesProperty = property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionBranches);
                actionBranchesProperty.arraySize = actionSize;

                for (int i = 0; i < actionSize; i++)
                {
                    EditorGUI.PropertyField(position,
                        actionBranchesProperty.GetArrayElementAtIndex(i),
                        new GUIContent("Branch " + i));
                    position.y += k_LineHeight;
                }
                EditorGUI.indentLevel--;
            }
        }

        static int GetHeightDiscreteAction(SerializedProperty property)
        {
            var actionTypeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ActionType);
            var actionType = GetValue<ActionType>(actionTypeProperty);
            var actionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ActionSize);
            var actionSize = GetValue<int>(actionSizeProperty);
            if (actionType == ActionType.DISCRETE)
            {
                return actionSize + 1;
            }
            return 0;
        }

        static void DrawWarnings(Rect position, SerializedProperty property)
        {
            foreach (string warning in m_Warnings)
            {
                EditorGUI.HelpBox(position, warning, MessageType.Warning);
                position.y += k_WarningLineHeight;
            }
        }

        static int GetHeightWarnings(SerializedProperty property)
        {
            return m_Warnings.Count;
        }

        static void UpdateWarnings(SerializedProperty property)
        {
            m_Warnings.Clear();

            // Name not empty
            var nameProperty = property.FindPropertyRelative(SpecsPropertyNames.k_Name);
            var name = GetValue<string>(nameProperty);
            if (name == "")
            {
                m_Warnings.Add("Your World must have a non-empty name");
            }

            // Max number of agents is not zero
            var nAgentsProperty = property.FindPropertyRelative(SpecsPropertyNames.k_NumberAgents);
            var nAgents = GetValue<int>(nAgentsProperty);
            if (nAgents == 0)
            {
                m_Warnings.Add("Your World must have a non-zero maximum number of Agents");
            }

            // At least one observation
            var observationShapes = property.FindPropertyRelative(SpecsPropertyNames.k_ObservationShapes);
            if (observationShapes.arraySize == 0)
            {
                m_Warnings.Add("Your World must have at least one observation");
            }

            // Action Size is not zero
            var actionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ActionSize);
            var actionSize = GetValue<int>(actionSizeProperty);
            if (actionSize == 0)
            {
                m_Warnings.Add("Your World must have non-zero action size");
            }

            //Model is not empty
            var model = GetValue<NNModel>(property.FindPropertyRelative(SpecsPropertyNames.k_Model));
            if (model == null)
            {
                m_Warnings.Add("No model preset (can still train)");
            }
        }

        static T GetValue<T>(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;

            FieldInfo field = null;
            foreach (var path in property.propertyPath.Split('.'))
            {
                var type = obj.GetType();
                field = type.GetField(path);
                obj = field.GetValue(obj);
            }
            return (T)obj;
        }
    }
}
#endif
