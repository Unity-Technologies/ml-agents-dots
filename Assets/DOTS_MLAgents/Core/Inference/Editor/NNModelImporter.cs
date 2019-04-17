using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace DOTS_MLAgents.Core.Inference.Editor
{
    /// <summary>
    /// Asset Importer of barracuda models.
    /// </summary>
    [ScriptedImporter(1, new[] {"nn"})]
    public class NNModelImporter : ScriptedImporter {
        private const string IconPath = "Assets/ML-Agents/Resources/NNModelIcon.png";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var model = File.ReadAllBytes(ctx.assetPath);
            var asset = ScriptableObject.CreateInstance<NNModel>();
            asset.Value = model;

            Texture2D texture = (Texture2D)
                AssetDatabase.LoadAssetAtPath(IconPath, typeof(Texture2D));

            ctx.AddObjectToAsset(ctx.assetPath, asset, texture);
            ctx.SetMainObject(asset);
        }
    }
}