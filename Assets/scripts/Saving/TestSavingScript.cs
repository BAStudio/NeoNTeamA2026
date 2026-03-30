using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestSavingScript : MonoBehaviour
{
    [SerializeField] private Button loadButton;
    [SerializeField] private Button saveButton;

    [SerializeField] private TextMeshProUGUI stringOutput;
    [SerializeField] private TextMeshProUGUI intOutput;
    [SerializeField] private TextMeshProUGUI floatOutput;

    private string stringValue;
    private int intValue;
    private float floatValue;

    void Start()
    {
        loadButton.onClick.AddListener(LoadData);
        saveButton.onClick.AddListener(SaveData);
    }

    public void LoadData()
    {
        TestData data = SaveSystem.instance.LoadSection<TestData>("testSave1", "testSaveData");
        if (data != null)
        {
            stringOutput.text = ("string:" + data.text);
            intOutput.text = ("int:" + data.number.ToString());
            floatOutput.text = ("float" + data.floatNumber.ToString());
        }
    }

    public void SaveData() { 
        TestData data = new TestData() { text = stringValue, floatNumber = floatValue, number = intValue};
        SaveSystem.instance.SaveSection("testSave1", "testSaveData", data);
    }

    public void SetStringValue(string value)
    {
        stringValue = value;
    }

    public void SetIntValue(string value)
    {
        int.TryParse(value, out intValue);
    }

    public void SetFloatValue(string value)
    {
        float.TryParse(value, out floatValue);
    }
}
