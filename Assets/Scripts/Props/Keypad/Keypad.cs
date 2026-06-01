using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Keypad : MonoBehaviour
{
    [SerializeField] private List<GameObject> buttons;
    [SerializeField] private GameObject enter;
    [SerializeField] private GameObject clear;

    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private string correctCode = "1234";

    private int maxInputLength = 4;
    private string currentInput = "";

    private bool inUse = true;

    void Update()
    {
        if (!inUse) return;
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
                        textMesh.color = Color.green;
                    }
                    else
                    {
                        currentInput = "";
                        Debug.Log("IncorrectCode");
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
        }
    }

    private void UpdateDisplay()
    {
        if (textMesh != null)
        {
            textMesh.text = currentInput;
        }
    }
}
