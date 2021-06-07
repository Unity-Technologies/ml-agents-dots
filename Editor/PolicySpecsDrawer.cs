#define CUSTOM_EDITOR
using Unity.Barracuda;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

#if CUSTOM_EDITOR

namespace Unity.AI.MLAgents.Editor
{
    internal static class SpecsPropertyNames
    {
        public const string k_Name = "m_Name";
        public const string k_PolicyProcessorType = "m_PolicyProcessorType";
        public const string k_NumberAgents = "m_NumberAgents";
        public const string k_ObservationShapes = "m_ObservationShapes";
        public const string k_ContinuousActionSize = "m_ContinuousActionSize";
        public const string k_DiscreteActionSize = "m_DiscreteActionSize";
        public const string k_DiscreteActionBranches = "m_DiscreteActionBranches";
        public const string k_Model = "m_Model";
        public const string k_InferenceDevice = "m_InferenceDevice";
    }


    /// <summary>
    /// PropertyDrawer for BrainParameters. Defines how BrainParameters are displayed in the
    /// Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(PolicySpecs))]
    internal class PolicySpecsDrawer : PropertyDrawer
    {
        // The height of a line in the Unity Inspectors
        const float k_LineHeight = 21f;
        const float k_LabelHeight = 18f;
        const float k_WarningBoxHeight = 36f;
        const float k_WarningLineHeight = 39f;

        List<string> m_Warnings = new List<string>();

        float m_TotalHeight = 0f;

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdateWarnings(property);

            var nbLines = 0;
            nbLines += GetHeightObservationShape(property);
            nbLines += GetHeightDiscreteAction(property);
            nbLines += 8; // TODO : COMPUTE
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

            EditorGUI.LabelField(position, "Policy Specs : " + label.text);
            position.y += k_LineHeight;
            EditorGUI.indentLevel++;

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            // Name
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_Name),
                new GUIContent("Name", "The name of the Policy"));
            position.y += k_LineHeight;

            // PolicyProcessorType
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_PolicyProcessorType),
                new GUIContent("Policy Type", "The type of Policy"));
            position.y += k_LineHeight;

            // Number of Agents
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_NumberAgents),
                new GUIContent("Number of Agents", "The maximum number of Agents that can request a Decision in the same step"));
            position.y += k_LineHeight;

            // Observation Shapes
            DrawObservationShape(position, property);
            position.y += k_LineHeight * GetHeightObservationShape(property);

            // Draw Continuous Action Size
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_ContinuousActionSize),
                new GUIContent("Continuous Action Size", "TODO"));
            position.y += k_LineHeight;

            // Draw Discrete Action Size
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionSize),
                new GUIContent("Discrete Action Size", "TODO"));
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

            EditorGUI.EndDisabledGroup();

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
            var actionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionSize);
            var actionSize = actionSizeProperty.intValue;
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

        static int GetHeightDiscreteAction(SerializedProperty property)
        {
            var actionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionSize);
            var actionSize = actionSizeProperty.intValue;
            return actionSize + 1;
        }

        void DrawWarnings(Rect position, SerializedProperty property)
        {
            foreach (string warning in m_Warnings)
            {
                EditorGUI.HelpBox(position, warning, MessageType.Warning);
                position.y += k_WarningLineHeight;
            }
        }

        int GetHeightWarnings(SerializedProperty property)
        {
            return m_Warnings.Count;
        }

        void UpdateWarnings(SerializedProperty property)
        {
            m_Warnings.Clear();

            // Name not empty
            var nameProperty = property.FindPropertyRelative(SpecsPropertyNames.k_Name);
            var name = nameProperty.stringValue;
            if (name == "")
            {
                m_Warnings.Add("Your Policy must have a non-empty name");
            }

            // Max number of agents is not zero
            var nAgentsProperty = property.FindPropertyRelative(SpecsPropertyNames.k_NumberAgents);
            var nAgents = nAgentsProperty.intValue;
            if (nAgents == 0)
            {
                m_Warnings.Add("Your Policy must have a non-zero maximum number of Agents");
            }

            // At least one observation
            var observationShapes = property.FindPropertyRelative(SpecsPropertyNames.k_ObservationShapes);
            if (observationShapes.arraySize == 0)
            {
                m_Warnings.Add("Your Policy must have at least one observation");
            }

            // Action Size is not zero
            var contActionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_ContinuousActionSize);
            var conActionSize = contActionSizeProperty.intValue;
            var discActionSizeProperty = property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionSize);
            var discActionSize = discActionSizeProperty.intValue;
            if (conActionSize + discActionSize == 0)
            {
                m_Warnings.Add("Your Policy must have non-zero action size");
            }
            var actionBranchesProperty = property.FindPropertyRelative(SpecsPropertyNames.k_DiscreteActionBranches);
            for (int i = 0; i < actionBranchesProperty.arraySize; i++)
            {
                if (actionBranchesProperty.GetArrayElementAtIndex(i).intValue <= 1)
                {
                    m_Warnings.Add("Each discrete action must have more than 1 option");
                }
            }

            //Model is not empty
            var modelProperty = property.FindPropertyRelative(SpecsPropertyNames.k_Model);
            var model = (NNModel)modelProperty.objectReferenceValue;
            if (model == null)
            {
                m_Warnings.Add("No model preset (can still train)");
            }
        }
    }
}
#endif
