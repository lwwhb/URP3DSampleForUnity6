using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class QualityLevelToggle : MonoBehaviour
{
    [SerializeField] private int m_Mask;

    [SerializeField] private string[] m_ValidLevels;

    //[SerializeField] private string[] validQualityLevelNames;

    private void Start()
    {
        UpdateActiveState();
    }

    private void UpdateActiveState()
    {
        string currentQualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()];
        gameObject.SetActive(m_ValidLevels.Contains(currentQualityLevel));
        
        //PrintValidLevels();
    }

    private void PrintValidLevels()
    {
        StringBuilder sb = new StringBuilder("Valid Levels: ");
        foreach (var validLevel in m_ValidLevels)
        {
            sb.Append(validLevel);
            sb.Append(", ");
        }
        
        Debug.Log(sb.ToString());
    }
}