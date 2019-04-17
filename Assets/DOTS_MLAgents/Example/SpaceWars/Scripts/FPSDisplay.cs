using System.Diagnostics.Tracing;
using UnityEngine;

namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    public class FPSDisplay : MonoBehaviour
    {
        float deltaTime = 0.0f;
        private float realTimeRatio = 1;
        int counter = 0;
 
        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            realTimeRatio += (Time.deltaTime / Time.unscaledDeltaTime - realTimeRatio) * 0.1f;
            counter++;
        }
 
        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;
 
            GUIStyle style = new GUIStyle();
 
            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color (1.0f, 1.0f, 0.5f, 1.0f);
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format(" {0:0.0} ms ({1:0.} fps)\n" +
                                        " # Ships : {2}\n" +
                                        " deltaTime {3}\n" +
                                        " unscaled deltaTime {4}\n" +
                                        " Time Speed {5}\n", 
                msec, fps, Globals.NumberShips, Time.deltaTime, Time.unscaledDeltaTime, realTimeRatio);
//            if (counter %100 == 0)
//                Debug.Log(text);
            GUI.Label(rect, text, style);
        }
    }
}