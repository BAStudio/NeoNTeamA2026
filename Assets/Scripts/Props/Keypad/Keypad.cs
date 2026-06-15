using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Keypad : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private List<GameObject> buttons;
    [SerializeField] private GameObject enter;
    [SerializeField] private GameObject clear;

    [Header("Display")]
    [SerializeField] private TextMeshPro textMesh;

    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask keypadLayer;
    [SerializeField] private UnityEvent OnCorrectCode;

    private string correctCode = "";
    private string currentInput = "";

    private int maxInputLength;

    private bool inUse = true;
    private bool correctCodeEntered = false;

    private void Start()
    {
        ShuffleKeys();
        UpdateDisplay();
    }

    private void Update()
    {
        if (!inUse || correctCodeEntered)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Ray ray = new Ray(
            interactionOrigin.position,
            interactionOrigin.forward
        );

        Debug.DrawRay(
            ray.origin,
            ray.direction * interactDistance,
            Color.white
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, keypadLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsButton(hitObject, enter))
            {
                PressButton(enter);
                CheckCode();
            }
            else if (IsButton(hitObject, clear))
            {
                PressButton(clear);

                currentInput = "";
                UpdateDisplay();
            }
            else
            {
                GameObject matchedButton = buttons.FirstOrDefault(button =>
                    IsButton(hitObject, button));

                if (matchedButton != null)
                {
                    PressButton(matchedButton);

                    if (currentInput.Length < maxInputLength)
                    {
                        string value =
                            matchedButton
                            .GetComponentInChildren<TextMeshPro>()
                            .text;

                        currentInput += value;

                        UpdateDisplay();
                    }
                }
            }
        }
    }

    private bool IsButton(GameObject hitObject, GameObject button)
    {
        return hitObject == button ||
               hitObject.transform.IsChildOf(button.transform);
    }

    private void PressButton(GameObject button)
    {
        Animator animator = button.GetComponentInParent<Animator>();

        if (animator != null)
        {
            animator.SetTrigger("press");
        }
    }

    private void CheckCode()
    {
        if (currentInput == correctCode)
        {
            Debug.Log("Correct Code");

            correctCodeEntered = true;

            if (textMesh != null)
            {
                textMesh.color = Color.green;
            }

            OnCorrectCode?.Invoke();
        }
        else
        {
            Debug.Log("Incorrect Code");

            currentInput = "";

            ShuffleKeys();

            UpdateDisplay();
        }
    }

    private void ShuffleKeys()
    {
        buttons = buttons
            .OrderBy(x => Random.value)
            .ToList();

        for (int i = 0; i < buttons.Count; i++)
        {
            TextMeshPro label =
                buttons[i].GetComponentInChildren<TextMeshPro>();

            if (label != null)
            {
                label.text = i.ToString();
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

    public void SetCode(string newCode)
    {
        correctCode = newCode;
        maxInputLength = correctCode.Length;
    }

    public bool IsCorrect()
    {
        return correctCodeEntered;
    }

    public void ResetKeypad()
    {
        currentInput = "";
        correctCodeEntered = false;

        if (textMesh != null)
        {
            textMesh.color = Color.white;
        }

        ShuffleKeys();
        UpdateDisplay();
    }

    public void UseKeypad(bool state)
    {
        inUse = state;
    }
}