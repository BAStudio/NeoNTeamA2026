using System;
using TMPro;
using UnityEngine;

public class NotePad : MonoBehaviour
{

    [SerializeField] private GameObject _notePad;

    [SerializeField] private TextMeshPro _noteText; // Assign in Inspector

    [SerializeField] private float _notePadMoveSpeed = 2f;

    [SerializeField] private float _notePadMaxHeight = 1.5f;
    [SerializeField] private float _notePadMinHeight = 0.5f;

    private bool _isActive = false;
    private bool _isMoving = false;

    private float _notePadTargetHeight;

    private string _notePadText = "";

    public static event System.Action StopMovement;
    public static event System.Action ContinueMovement;



    private void Start()
    {
        _notePadTargetHeight = _notePadMinHeight;
        _notePad.SetActive(false);
    }

    void Update()
    {
        // Toggle clipboard with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!_isActive)
            {
                _notePad.SetActive(true);
                _notePadTargetHeight = _notePadMaxHeight;
                _isActive = true;
                _isMoving = true;

                StopMovement?.Invoke();
            }
            else
            {
                _notePadTargetHeight = _notePadMinHeight;
                _isActive = false;
                _isMoving = true;

                ContinueMovement?.Invoke();
            }

        }

        // Lerp position every frame if moving
        if (_isMoving)
        {
            Vector3 pos = _notePad.transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, _notePadTargetHeight, Time.deltaTime * _notePadMoveSpeed);
            _notePad.transform.localPosition = pos;

            // Check if lerp is close enough to target
            if (Mathf.Abs(pos.y - _notePadTargetHeight) < 0.01f)
            {
                _notePad.transform.localPosition = new Vector3(pos.x, _notePadTargetHeight, pos.z);
                _isMoving = false;

                // If deactivating, hide notepad
                if (!_isActive)
                    _notePad.SetActive(false);
            }
        }

        // Typing when clipboard is active and not moving down
        if (_isActive && !_isMoving)
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\b' && _notePadText.Length > 0)
                    _notePadText = _notePadText.Substring(0, _notePadText.Length - 1);
                else if (c == '\n' || c == '\r')
                    ; // Do nothing on Enter
                else
                    _notePadText += c;
            }
            _noteText.text = _notePadText;
        }
    }

    private void NotePadStopMovement()
    {
        throw new NotImplementedException();
    }
}