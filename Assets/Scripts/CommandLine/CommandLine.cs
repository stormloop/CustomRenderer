using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommandLine : MonoBehaviour
{
    public static CommandLine INSTANCE;

    GameObject Output;

    List<string> commandHistory = new List<string>();
    string incompleteCommand;
    int index;

    bool active;

    private void Awake()
    {
        INSTANCE = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Output = GameObject.Find("Output");
        CloseCommandLine();
    }

    // Update is called once per frame
    void Update()
    {
        if (
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && active
            && !GetComponentInChildren<TMP_InputField>().text.Equals("")
        )
            ExecuteCommand();
        if (Input.GetKeyDown(KeyCode.Escape) && active)
            CloseCommandLine();
        if (Input.GetKeyDown(KeyCode.Backslash) && !active)
            OpenCommandLine();
        if (Input.GetKeyDown(KeyCode.UpArrow) && active)
        {
            if (index == commandHistory.Count)
                incompleteCommand = GetComponentInChildren<TMP_InputField>().text;
            index = Mathf.Max(0, index - 1);
            if (commandHistory.Count == 0)
            {
                GetComponentInChildren<TMP_InputField>().text = incompleteCommand;
                return;
            }
            GetComponentInChildren<TMP_InputField>().text = commandHistory[index];
            GetComponentInChildren<TMP_InputField>().MoveToEndOfLine(false, false);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && active)
        {
            index = Mathf.Min(commandHistory.Count, index + 1);
            if (index == commandHistory.Count)
            {
                GetComponentInChildren<TMP_InputField>().text = incompleteCommand;
                return;
            }
            GetComponentInChildren<TMP_InputField>().text = commandHistory[index];
        }

        if (!active && Output.activeSelf)
        {
            Output.GetComponent<Image>().color -= new Color(0, 0, 0, Time.deltaTime * 64 / 255 / 5);
            Output.GetComponentInChildren<TMP_Text>().color -= new Color(
                0,
                0,
                0,
                Time.deltaTime / 5
            );
            if (Output.GetComponent<Image>().color.a <= 0)
                Output.SetActive(false);
        }
    }

    public void OpenCommandLine()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }
        active = true;
        Output.GetComponent<Image>().color = Color.white / 255 * 64;
        Output.GetComponentInChildren<TMP_Text>().color = Color.white;
        GetComponentInChildren<TMP_InputField>().text = "";
        GetComponentInChildren<TMP_InputField>().Select();
    }

    public void CloseCommandLine()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (!transform.GetChild(i).gameObject.Equals(Output))
                transform.GetChild(i).gameObject.SetActive(false);
        }
        active = false;
        if (!EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ExecuteCommand()
    {
        string command = GetComponentInChildren<TMP_InputField>().text;
        CloseCommandLine();
        if (commandHistory.Count <= 1 || commandHistory[commandHistory.Count - 1] != command)
            commandHistory.Add(command);
        index = commandHistory.Count;
        Commands.ExecuteCommand(command);
    }

    public void LogError(string text)
    {
        GameObject.Find("Output").GetComponentInChildren<TMP_Text>().text +=
            "\n<line-indent=0><color=\"white\">>"
            + commandHistory[commandHistory.Count - 1]
            + "</line-indent>\n<color=\"red\"><line-indent=5>"
            + text
            + "</line-indent>";
    }

    public void LogText(string text)
    {
        GameObject.Find("Output").GetComponentInChildren<TMP_Text>().text +=
            "\n<line-indent=0><color=\"white\">>"
            + commandHistory[commandHistory.Count - 1]
            + "</line-indent>\n<line-indent=5>"
            + text
            + "</line-indent>";
    }

    public void ClearOutput()
    {
        GameObject.Find("Output").GetComponentInChildren<TMP_Text>().text = "";
    }
}
