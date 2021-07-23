using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KaizerWaldCode.ECSEditor
{
    [CustomEditor(typeof(EditorSettings))]
    public class MapGeneratorEditor : Editor
    {
        //[SerializeField] private KaizerWaldCode.Data.Conversion.ConversionMapSettings convertedSetting;

        public override void OnInspectorGUI()
        {

            EditorSettings mapGen = (EditorSettings)target;
    
            if (DrawDefaultInspector())
            {
                if (mapGen.AutoUpdate)
                {
                    mapGen.ProcessChanges();
                }
            }
    
            if (GUILayout.Button("Generate"))
            {
                mapGen.ProcessChanges();
            }
            
        }
    }
}
