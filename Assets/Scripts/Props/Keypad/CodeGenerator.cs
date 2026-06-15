using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CodeGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Keypad keypad;

    [Header("Hint Displays")]
    [SerializeField] private List<TextMeshPro> displays;

    [Header("Settings")]
    [SerializeField] private int codeLength = 4;

    private string generatedCode;

    private void Start()
    {
        GenerateCode();
    }

    public void GenerateCode()
    {
        generatedCode = "";

        for (int i = 0; i < codeLength; i++)
        {
            generatedCode += Random.Range(0, 10).ToString();
        }

        SendCodeToKeypad();
        UpdateDisplays();

        Debug.Log("Generated Code: " + generatedCode);
    }

    private void SendCodeToKeypad()
    {
        keypad.SetCode(generatedCode);
    }

    private void UpdateDisplays()
    {
        for (int i = 0; i < generatedCode.Length; i++)
        {
            char[] chars = { '*', '*', '*', '*' };

            chars[i] = generatedCode[i];

            displays[i].text = new string(chars);
        }
    }
}