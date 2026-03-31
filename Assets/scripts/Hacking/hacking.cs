using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class hacking : MonoBehaviour
{
    List<char> codeThatNeedsToBehacked = new List<char>();
    List<char> userInput = new List<char>();

    [SerializeField] List<TextMeshProUGUI> textMeshProUGUIs = new List<TextMeshProUGUI>();

    char currentNeededCharacter;

    int currentCharacterIndex = 0;
    int finalCharacterIndex;

    bool buttonPress;
    bool codeCracked = true;

    string prefKey;
    char prefChar;

    void Update()
    {
        CheckCurrentInput();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ResetCode();
        }
    }

    void ResetCode()
    {
        codeCracked = true;
        codeThatNeedsToBehacked.Clear();
        finalCharacterIndex = 0;
        currentCharacterIndex = 0;
        for (int i = 0; i < textMeshProUGUIs.Count; i++)
        {
            char currentCharacter = (char)('a' + Random.Range(0, 26));
            while (currentCharacter == prefChar)
            {
                currentCharacter = (char)('a' + Random.Range(0, 26));
            }
            prefChar = currentCharacter;
            codeThatNeedsToBehacked.Add(currentCharacter);
            textMeshProUGUIs[i].text = currentCharacter.ToString().ToUpper();
            textMeshProUGUIs[i].color = Color.red;
        }
        finalCharacterIndex = codeThatNeedsToBehacked.Count;
        currentNeededCharacter = codeThatNeedsToBehacked[0];
        codeThatNeedsToBehacked.Remove(currentNeededCharacter);
        codeCracked = false;
    }

    void CheckCurrentInput()
    {
        InputSystem.onAnyButtonPress.Call(currentAction =>
        {
            if (codeCracked) return;

            buttonPress = true;
            if (buttonPress)
            {
                if (currentAction.name.ToCharArray()[0] == currentNeededCharacter)
                {
                    textMeshProUGUIs[currentCharacterIndex].color = Color.green;

                    currentCharacterIndex++;

                    if (currentCharacterIndex >= finalCharacterIndex)
                    {
                        codeCracked = true;
                        Debug.Log("you win");
                        buttonPress = false;
                    }
                    else
                    {
                        currentNeededCharacter = codeThatNeedsToBehacked[0];
                        codeThatNeedsToBehacked.Remove(currentNeededCharacter);
                        buttonPress = false;
                    }
                }
            }
        });
    }
   
}
