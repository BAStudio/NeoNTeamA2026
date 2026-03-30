using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class hacking : MonoBehaviour
{
    List<char> codeThatNeedsToBehacked = new List<char>();
    List<char> userInput = new List<char>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        for(int i = 0; i < 5; i++)
        {
            codeThatNeedsToBehacked.Add((char) ('A' + Random.Range(0, 26)));
        }

    }

    // Update is called once per frame
    void Update()
    {

          

        
    }
    private void OnGUI()
    {
        Event e = Event.current;
        if (e.isKey)
        {

            userInput.Add(((char)e.keyCode));
            for(int i = 0; i < userInput.Count; i++)
            {
                Debug.Log(userInput[i]);
            }
        }
    }
}
