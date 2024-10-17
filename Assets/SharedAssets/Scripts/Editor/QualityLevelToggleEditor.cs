using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;



[CustomEditor(typeof(QualityLevelToggle))]
public class QualityLevelToggleEditor : Editor
{
    private const string k_Label = "Enable on QualityLevel";

    public override VisualElement CreateInspectorGUI()
    {
        var qualityLevelNames = QualitySettings.names;
        var maskField = new MaskField(k_Label, qualityLevelNames.ToList(), -1);
        maskField.bindingPath = "m_Mask";

        SerializedProperty maskProperty = serializedObject.FindProperty("m_Mask");
        maskField.TrackPropertyValue(maskProperty, SetValidLevels);
        maskField.BindProperty(maskProperty);
        
        VisualElement myInspector = new VisualElement();
        
        myInspector.Add(maskField);

        return myInspector;
    }
    
    
    void SetValidLevels(SerializedProperty maskProperty)
    {
        var levels = QualityLevelMaskToString(maskProperty.intValue);
        var array = maskProperty.serializedObject.FindProperty("m_ValidLevels");
        array.arraySize = levels.Count;

        for (var i = 0; i < levels.Count; i++)
        {
            var element = array.GetArrayElementAtIndex(i);
            element.stringValue = levels[i];
        }
        
        maskProperty.serializedObject.ApplyModifiedProperties();
    }
    
    List<string> QualityLevelMaskToString(int mask)
    {
        List<string> levels = new List<string>();
        for (int i = 0; i < QualitySettings.count; i++)
        {
            if ((mask & (1 << i)) > 0)
            {
                levels.Add(QualitySettings.names[i]);
            }
        }
        return levels;
    }
}
