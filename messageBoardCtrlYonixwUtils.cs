using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics; // Win only


/*
using UnityEngine.SceneManagement;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
*/

// Todo:
// Allow refresh line with new regex (need to save cache)
    // Refresh
// Allow show meta (position?) of labels
// Allow custom row enter? tags included?
        //  Fix center by '##1'...?
// Print all files as they are loaded (where to inject?...)
    // Load in any scene:
        // https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html

// Complex Config XML:
    // https://learn.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer?view=net-7.0&redirectedfrom=MSDN

/*
public partial class messageBoardCtrl : MonoBehaviour
{
    public void LateUpdate()
    {
        messageBoardCtrlYonixwUtils._LateUpdate(this.line_list_);
    }
    
    public void load()
    {
        // ....
        foreach (Text text in this.line_list_)
            {
                mainCtrl.instance.addText(text);
                text.alignment = TextAnchor.MiddleRight;
            }
    }
}
*/

public static class messageBoardCtrlYonixwUtils  {

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole(); // Win Only
    
    public static int _rtl_i = 1;
    public static string _rtl_flag = "<size=1></size>";
    
    public static List<string[]> _config = null;
    
    private static StreamWriter _stdOutWriter;

    public static void incRtlFlag () {
            _rtl_i++;
            _rtl_flag = "<size=" + _rtl_i + "></size>";
    }
    
    public static void l(string l) {
        if (_stdOutWriter != null) // Console was opened
            _stdOutWriter.WriteLine(l);
    }
    
    public static string rl(string _def="") {
        if (_stdOutWriter != null) // Console was opened
            return new StreamReader(Console.OpenStandardInput()).ReadLine();
        return _def;
    }
    
    public static string objPath(Transform t) {
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null) {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
    
    public static void SetClipboard(string value)
    {
        Process clipboardExecutable = new Process(); 
        clipboardExecutable.StartInfo = new ProcessStartInfo // Creates the process
        {
            RedirectStandardInput = true,
            FileName = @"clip", 
        };
        clipboardExecutable.Start();

        clipboardExecutable.StandardInput.Write(value); // CLIP uses STDIN as input.
        // When we are done writing all the string, close it so clip doesn't wait and get stuck
        clipboardExecutable.StandardInput.Close(); 

        return;
    }
    
    public static void _LateUpdate(List<Text> line_list_)
    {
        if (_config == null)
        {
            _config = messageBoardCtrlYonixwUtils.getConfig(
                    messageBoardCtrlYonixwUtils.getConfigLines());
        }
        if (Input.GetKeyUp(KeyCode.F4))
        {
            AllocConsole();
            _stdOutWriter = new StreamWriter(Console.OpenStandardOutput());
            _stdOutWriter.AutoFlush = true;
            l("[F4] Console Start! " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F5))
        {
            _config = getConfig(getConfigLines());
            incRtlFlag();
            l("[F5] Refresh config! " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F6))
        {
            l("[F6] Waiting, Enter option, [0] Help, [empty] to continure");
            string option = rl();
            l("[F6] Got option: " + option);
            
            if (option == "0") {
                l("[0] Help, all options:");
                l("[1] dump 3 dialog lines");
                l("[2] print dump all (active) UnityEngine.UI.Text");
                l("[3] copy  dump all (active) UnityEngine.UI.Text");
            }
            if (option == "1") {
                l("[0]: " + line_list_[0].text);
                l("[1]: " + line_list_[1].text);
                l("[2]: " + line_list_[2].text);
            }
            if (option == "2" || option == "3") {
                UnityEngine.UI.Text[] allTxts = 
                    GameObject.FindObjectsOfType<UnityEngine.UI.Text>();
                // including inactive: Resources.FindObjectsOfTypeAll<BaseClass>();
                string result = "";
                int i = 0;
                foreach(UnityEngine.UI.Text _t in allTxts) {
                    result += "(" + i + ")" + "\n";
                    i++;
                    
                    result += "[Path] " + objPath(_t.transform) + "\n";
                    result += "[Text] " + _t.text + "\n";
                }
                
                if (option == "2") {
                    l(result);
                }
                if (option == "3") {
                    l("Copied to clipboard");
                    SetClipboard(result);
                }
            }            
        }
        
        foreach (Text text in line_list_)
        {
            if (!string.IsNullOrEmpty(text.text) && !text.text.StartsWith(_rtl_flag))
            {
                text.text = _rtl_flag + fullregreplace(FixRTL(text.text), _config);
            }
        }
        
    }

    // -------- Smart RTL reverse without tag reverse

    public static string WordReverser(System.Text.RegularExpressions.Match match)
    {
      if (match.Value.Length > 0 ) {
        string val = match.Value;
        
        bool hasEndTagPrev = val.StartsWith(">");
        if (hasEndTagPrev) val = val.Substring(1);
        
        bool hasStartTagNxt = val.EndsWith("<");
        if (hasStartTagNxt) val = val.Substring(0, val.Length - 1);

        char[] _txt = val.ToCharArray();
        Array.Reverse(_txt);
        string result = new string(_txt);
        
        if (hasEndTagPrev) result = ">" + result;
        if (hasStartTagNxt) result =  result + "<";
        
        return result;
      }
      return "[" + match.Value + "]";
    }
    
    public static string FixRTL(string data) {
       string pattern = @"(^|>)([^><]+)(<|$)";   
       System.Text.RegularExpressions.MatchEvaluator evaluator = 
            new System.Text.RegularExpressions.MatchEvaluator(WordReverser);
        return System.Text.RegularExpressions.Regex.Replace(
            data, pattern, evaluator
        );
    }
    
    // -------- Debug and replace for RTL
    
    public static string[] getConfigLines() {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string txtPath = Path.Combine(assemblyFolder,"HE.Mod.Replace.txt");
        string contents = File.ReadAllText(txtPath);
        if ( String.IsNullOrEmpty(contents) ) {
            contents = "";
        }
        return contents.Split('\n');
    }

    public static List<string[]> getConfig(string[] lines) {
        List<string[]> items = new List<string[]>();
        foreach(string _line in lines) {
            string line = _line.Trim();
            int firstIndx = line.IndexOf("=>");
            if (firstIndx > -1) {
                string from = line.Substring(0,firstIndx);
                string to = line.Substring(firstIndx+2);
                items.Add(new string[] {from, to});
            }
        }
        return items;
    }
    
    public static string regreplace(string txt, string from, string to) {
        return Regex.Replace(txt, from, Regex.Unescape(to));
    }

    public static string fullregreplace(string txt, List<string[]> conf) {
        foreach (string[] pair in conf) {
            txt = regreplace(txt,pair[0],pair[1]);
        }
        return txt;
    }

}
