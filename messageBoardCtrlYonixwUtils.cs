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


/*
using UnityEngine.SceneManagement;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
*/

// Todo:
// Allow refresh line with new regex (need to save cache)
    // Refresh
// Allow custom row enter? tags included?
// Allow show meta (position?) of labels
// Show escaped tags to see whats rendering
    //  Fix center by '#1'...?
// "Add pause" to each line -> Pause by console but sound keep going

// Evidence bag? RTL? Text find... and parent chain...

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
    private static extern bool AllocConsole();
    
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
    
    public static void _LateUpdate(List<Text> line_list_)
    {
        if (_config == null)
        {
            _config = messageBoardCtrlYonixwUtils.getConfig(messageBoardCtrlYonixwUtils.getConfigLines());
        }
        if (Input.GetKeyUp(KeyCode.F4))
        {
            AllocConsole();
            _stdOutWriter = new StreamWriter(Console.OpenStandardOutput());
            _stdOutWriter.AutoFlush = true;
            l("[Console Start] " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F5))
        {
            _config = getConfig(getConfigLines());
            incRtlFlag();
            l("[F5] " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F6))
        {
            //l("[F6 Input] " + Time.deltaTime);
            //string line = rl();
            //l("[F6 Echo] " + line);
            l("[F6] Lines:");
            l("[0]: " + line_list_[0].text);
            l("[1]: " + line_list_[1].text);
            l("[2]: " + line_list_[2].text);
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
