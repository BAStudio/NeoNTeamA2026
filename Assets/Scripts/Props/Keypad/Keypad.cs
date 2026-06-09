using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


public class Keypad : MonoBehaviour
{
    [SerializeField] private List<GameObject> buttons;
    [SerializeField] private GameObject enter;
    [SerializeField] private GameObject clear;

    [SerializeField] private TextMeshPro textMesh;
    public string correctCode = "1234";

    [SerializeField] private UnityEvent OpenDoor;

    private int maxInputLength;
    private string currentInput = "";

    private bool inUse = true;
    private bool correctCodeEntered = false;


    private void Start()
    {
        maxInputLength = correctCode.Length;
        ShuffelKeys();
    }

    private void ShuffelKeys()
    {
        buttons = buttons.OrderBy(x => UnityEngine.Random.value).ToList();
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].GetComponentInChildren<TextMeshPro>().text = i.ToString();
        }
    }

    void Update()
    {
        if (!inUse || correctCodeEntered) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Debug.DrawRay(ray.origin, ray.direction * 20, Color.white);
            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.transform.gameObject;

                if (hitObject == enter)
                {
                    hitObject.GetComponent<Animator>().SetTrigger("press");
                    if (currentInput == correctCode)
                    {
                        Debug.Log("CorrectCode");
                        correctCodeEntered = true;
                        textMesh.color = Color.green;
                    }
                    else
                    {
                        currentInput = "";
                        Debug.Log("IncorrectCode");
                        ShuffelKeys();
                        UpdateDisplay();
                    }
                }
                else if (hitObject == clear)
                {
                    hitObject.GetComponent<Animator>().SetTrigger("press");
                    currentInput = "";
                    UpdateDisplay();
                }
                else
                {
                    int buttonIndex = buttons.IndexOf(hitObject);
                    if (buttonIndex != -1)
                    {
                        hitObject.GetComponent<Animator>().SetTrigger("press");
                        if (currentInput.Length < maxInputLength)
                        {
                            currentInput += buttonIndex.ToString();
                            UpdateDisplay();
                        }
                    }
                }
            }

            if (correctCodeEntered)
            {
                OpenDoor?.Invoke();
            }
        }
    }

    private void UpdateDisplay()
    {
        if (textMesh != null)
        {
            textMesh.text = currentInput;
        }
    }

    public void UseKeypad()
    {
        inUse = true;
    }
}
