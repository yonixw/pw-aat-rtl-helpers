using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using System.Xml.Serialization;

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

// Todo :

// Get all AAT tags and their meaning?

// Print all files as they are loaded (where to inject?...)
    // Load in any scene:
        // https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html

// Color need tags reverse for RTL ... but there are 2 tags - open and not open...

// TODO round2: 
//      Fast way to encode from txt to file... (need c# cli for encoding software..)

/*

public partial class decryptionCtrl : MonoBehaviour
{
	public byte[] load(string in_path)
	{
		messageBoardCtrlYonixwUtils.l("[L] Loading file: " + in_path);

..

public partial class messageBoardCtrl : MonoBehaviour
{
    public void LateUpdate()
    {
        messageBoardCtrlYonixwUtils._LateUpdate(this.line_list_);
    }
    
    public void load()
    {
        ....
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
    public static string _rtl_flag = "<size=1></size><size=1></size>";
    
    public static void incRtlFlag () {
            _rtl_i++;
            _rtl_flag = "<size=1></size><size=" + _rtl_i + "></size>";
    }
    
    public static List<Text> _txt_cache = new List<Text>();
    public static bool _reuse_cache = false;
    
    public static XWConfig _config = null;
    
    public class XWConfig {
        
        public class XWReplace {
            public string regex;
            public string replace;
        }
        
        public List<XWReplace> replaces = new List<XWReplace>();
        
        // Assuming no new Gameobject are added,
        // so only need to find them once, using filter
        public List<string> unityUINameRegex = new List<string>();
        
        public class XWSimpleReplace {
            public string exact;
            public string replace;
        }
        
        public List<XWSimpleReplace> simpleTranslate = new List<XWSimpleReplace>();

        public bool openDebugConsole = false;

        public List<string> debugLines = new List<string>();
    }
    
    private static Dictionary<string, string> _simpleReplace = 
            new Dictionary<string, string>();
    
    private static StreamWriter _consoleStdOutWriter;

    public static void l(string l) {
        if (_consoleStdOutWriter != null) // Console was opened
            _consoleStdOutWriter.WriteLine(l);
    }
    
    public static string rl(string _def="") {
        if (_consoleStdOutWriter != null) // Console was opened
            return new StreamReader(Console.OpenStandardInput()).ReadLine();
        return _def;
    }
    
    public static string objPath(Transform t) {
        string path = "/" + t.name; // "/" is the start path
        Transform parent = t.parent;
        while (parent != null) {
            path = "/" + parent.name  + path;
            parent = parent.parent;
        }
        return path;
    }
    
    public static string toXML<T>(T s) {
       XmlSerializer serializer =
            new XmlSerializer(typeof(T));
        using (StringWriter textWriter = new StringWriter())
        {
            serializer.Serialize(textWriter, s);
            return textWriter.ToString();
        }
    }
    
    public static T fromXML<T>(string s)
    {
        XmlSerializer xmlSerializer = 
            new XmlSerializer(typeof(T));
        using (StringReader textReader = new StringReader(s))
        {
            return (T)xmlSerializer.Deserialize(textReader);
        }
    }
    
    public static void SetClipboard(string value)
    {
        Process clipProc = new Process(); 
        clipProc.StartInfo = new ProcessStartInfo // Creates the process
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            FileName = @"clip", 
        };
        clipProc.Start();

        // UTF16 that clip wants
        //      For specifiec stuff Encoding.GetEncoding(1255);
        System.Text.Encoding txtEncoding =  System.Text.Encoding.Unicode;
        StreamWriter streamWriter = new StreamWriter(clipProc.StandardInput.BaseStream, txtEncoding);
        streamWriter.WriteLine(value);
        // When we are done writing all the string, close it so clip doesn't wait and get stuck
        streamWriter.Close();

        // -- Old english only way --
        //clipProc.StandardInput.WriteLine(value); // CLIP uses STDIN as input.
        // When we are done writing all the string, close it so clip doesn't wait and get stuck
        //clipProc.StandardInput.Close(); 
        // -------------------------
        
        clipProc.WaitForExit();
         
        return;
    }

    public static bool _console_open = false;
    public static void OpenConsole() {
        if (_console_open) return;
        AllocConsole();
        _consoleStdOutWriter = new StreamWriter(Console.OpenStandardOutput());
        _consoleStdOutWriter.AutoFlush = true;
        _console_open = true;
    }
    
    public static void _LateUpdate(List<Text> line_list_)
    {
        if (_config == null)
        {
            reloadConfig();
        }
        
        if (Input.GetKeyUp(KeyCode.F4))
        {
            OpenConsole();
            l("[F4] Console Start! " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F5))
        {
            // To quick debug of configs, we tell to use 
            // latest original text 
            _reuse_cache = true; 
            
            reloadConfig();
            incRtlFlag();
            l("[F5] Refreshed config! " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F6))
        {
            l("[F6] Waiting, Enter option, [H] Help, [empty] to continure");
            string option = rl();
            l("[F6] Got option: " + option);
            
            if (option == "H") {
                l("[H] Help, all options:");
                l("[0] Clear console");
                l("[1] dump 3 dialog lines");
                l("[2] print dump all UnityEngine.UI.Text in scene");
                l("[3] clipboard  dump all UnityEngine.UI.Text in scene");
                l("[4] Save example config");
                l("[5] print dump all monitored UI.Text from config");
                l("[6] clipboard dump all monitored UI.Text from config");
            }
            if (option == "0") {
                Console.Clear(); 
            }
            if (option == "1") {
                l("[line 0]: " + line_list_[0].text);
                l("[line 1]: " + line_list_[1].text);
                l("[line 2]: " + line_list_[2].text);
            }
            if (option == "2" || option == "3") {
                UnityEngine.UI.Text[] allTxts = 
                    Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
                    //GameObject.FindObjectsOfType<UnityEngine.UI.Text>();
                    //Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
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
            if (option == "4") {
                saveExampleConfig();
                l("Saved!");
            }
            if (option == "5" || option == "6") {
                string result = "";
                for (int i=0;i<_txt_cache.Count;i++) 
                {
                    Text _t = _txt_cache[i];
                    result += "(" + i + ")" + "\n";
                    i++;
                    
                    result += "[Path] " + objPath(_t.transform) + "\n";
                    result += "[Text] " + _t.text + "\n";
                    
                }

                if (option == "5") {
                    l(result);
                }
                if (option == "6") {
                    l("Copied to clipboard");
                    SetClipboard(result);
                }
            }
            
        }
        
        // Update all
        for (int i=0;i<_txt_cache.Count;i++) 
        {
            // Becaue we monitor and change the 
            // Same text that is being changed by the game from
            // other scripts,
            // Our only way to detect change is if the rtl_flag
            // is removed (or the text is empty). 
            
            Text text = _txt_cache[i];
            string _TXT = text.text;

            bool noChange = 
                string.IsNullOrEmpty(_TXT) ||
                _TXT.StartsWith(_rtl_flag);

            
            if (!noChange)
            {
                if (_config.debugLines.Count > 0) {
                    int _dbg_i = line_list_.IndexOf(text);
                    if (_dbg_i>-1 && _dbg_i < _config.debugLines.Count) {
                        _TXT = _config.debugLines[_dbg_i];
                    }
                }
                
                 if (_simpleReplace.ContainsKey(_TXT)) {
                    _TXT = _simpleReplace[_TXT];
                }
                
                const string prefixAlign = " ף";
                const string postfixAlign = "ף ";
                if (_TXT.IndexOf(prefixAlign) > -1) {
                    string alignCode = findCustomCode(_TXT,prefixAlign,postfixAlign);

                    if (alignCode != "") {
                        _TXT = _TXT
                            .Replace(prefixAlign + alignCode + postfixAlign,"");
                        if (uiAlignEnum.ContainsKey(alignCode)) {
                            text.alignment = uiAlignEnum[alignCode];
                        }
                    }
                    else {
                        // Hide the text until the typing effect will finish the full custom code "ף{num}ף"
                        _TXT = _TXT
                            .Replace(prefixAlign,"<size=0>") + "</size>";
                    }
                }
                else {
                    text.alignment = TextAnchor.MiddleRight; // Allow exception by Name?
                }

                text.text = 
                        _rtl_flag + 
                        fullregreplace(FixRTL(_TXT));
            }
        }
        
    }

    public static Dictionary<string, TextAnchor> uiAlignEnum = new Dictionary<string, TextAnchor>() {
        {"1",TextAnchor.UpperLeft},
        {"2",TextAnchor.UpperCenter},
        {"3",TextAnchor.UpperRight},
        {"4",TextAnchor.MiddleLeft},
        {"5",TextAnchor.MiddleCenter},
        {"6",TextAnchor.MiddleRight}, // Default in our code
        {"7",TextAnchor.LowerLeft},
        {"8",TextAnchor.LowerCenter},
        {"9",TextAnchor.LowerRight}
    };

    public static string findCustomCode(string txt, string prefix, string postfix) {
        int start = txt.IndexOf(prefix);
        if (start == -1) return "";

        int last = txt.IndexOf(postfix,start+1);
        if (start > -1 && last > -1 && last > start +  +1) {
            start += prefix.Length;
            return txt.Substring(start, last - start );
        }
        else {
            return ""; // not found
        }
    }

    // -------- Smart RTL reverse without tag reverse

    // "22:22", "01/02/23", "1,000.55", "1,000", "44.55",
    public static Regex numberWords = new Regex("^\\d[/\\d:\\.\\,\\\\]*\\d+$");

    public static string WordReverser(System.Text.RegularExpressions.Match match)
    {
      if (match.Value.Length > 0 ) {
        string val = match.Value;
        
        bool hasEndTagPrev = val.StartsWith(">");
        if (hasEndTagPrev) val = val.Substring(1);
        
        bool hasStartTagNxt = val.EndsWith("<");
        if (hasStartTagNxt) val = val.Substring(0, val.Length - 1);

        string[] words = val.Split(' ');
        for (int i=0;i<words.Length;i++) {
            if (!numberWords.Match(words[i]).Success) {
                continue; // Skip no number words, so it will double reverse
            }
            char[] _txt = words[i].ToCharArray();
            Array.Reverse(_txt);
            words[i] =  new string(_txt);
        }
        
        string result = string.Join(" ", words);
        char[] _txt2 = result.ToCharArray();
        Array.Reverse(_txt2);
        result = new string(_txt2);
        
        if (hasEndTagPrev) result = ">" + result;
        if (hasStartTagNxt) result =  result + "<";
        
        return result;
      }
      return "[" + match.Value + "]";
    }

    public static System.Text.RegularExpressions.MatchEvaluator insideTagEvaluator = 
            new System.Text.RegularExpressions.MatchEvaluator(WordReverser);
    public static string insideTagPattern = @"(^|>)([^><]+)(<|$)";   
    public static string FixRTL(string data) {
        return System.Text.RegularExpressions.Regex.Replace(
            data, insideTagPattern, insideTagEvaluator
        );
    }
    
    // -------- Debug and replace for RTL
    
    public static string getConfPath() {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string txtPath = Path.Combine(assemblyFolder,"PW_AAT_RTL_MOD.xml");
        return txtPath;
    }
    
    public static string getConfigString() {
        string contents = File.ReadAllText(getConfPath());
        if ( String.IsNullOrEmpty(contents) ) {
            contents = "";
        }
        return contents;
    }


    public static void resetConfigLocal() {
        _txt_cache = 
            new List<Text>(_config.unityUINameRegex.Count);
            
        UnityEngine.UI.Text[] allTxts = 
                Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
        List<string> allTxtsFullPath = 
                    new List<string>();
        
        foreach (Text t in allTxts) {
            allTxtsFullPath.Add(objPath(t.transform));
        }

        l("Start add UI Text");
        for (int j=0;j<_config.unityUINameRegex.Count;j++) {
            l("[*] Filter: " + _config.unityUINameRegex[j].ToLower() );
            for (int i=0;i<allTxtsFullPath.Count;i++) {
                if (
                    allTxtsFullPath[i].ToLower().Contains(_config.unityUINameRegex[j].ToLower())
                    //new Regex().Match().Success
                    ) {
                    
                    _txt_cache.Add(allTxts[i]);
                    l("[***] Added: " + allTxtsFullPath[i].ToLower() );
                    l("[***] Start Alignment: " + allTxts[i].alignment.ToString() );
                }
            }
        }
        
        // Simple (exact) replace dict populate
        _simpleReplace = new Dictionary<string, string>();
        for (int i=0;i<_config.simpleTranslate.Count;i++) {
            _simpleReplace.Add(
                _config.simpleTranslate[i].exact,
                _config.simpleTranslate[i].replace);
        }
    }
    
    public static void reloadConfig() {        
        XWConfig config = new XWConfig();
        try {
            string fileContent = getConfigString();
            config = fromXML<XWConfig>(fileContent);
        }
        catch (Exception ex) {
            l("Can't open file, empty config");
        }
        _config = config;

        if (_config.openDebugConsole) {
            OpenConsole();
            l("Console open because config, F5-Refresh, F6-Actions");
        }
        
        resetConfigLocal();
    }
    
    public static void saveExampleConfig() {
        XWConfig c = new XWConfig();
        c.replaces.Add(
            new XWConfig.XWReplace() { regex = "(", replace = ")" } 
        );
        c.unityUINameRegex.Add("line");
        c.simpleTranslate.Add(
            new XWConfig.XWSimpleReplace() { exact = "Back", replace = "חזור" } 
        );
        c.openDebugConsole = true;
        
        File.WriteAllText(getConfPath(),toXML(c));
    }
    
    public static string regreplace(string txt, string from, string to) {
        //return Regex.Replace(txt, from, Regex.Unescape(to));
        return txt.Replace(from,to);
    }

    public static string fullregreplace(string txt) {
        foreach (XWConfig.XWReplace pair in _config.replaces) {
            txt = regreplace(txt,pair.regex,pair.replace);
        }
        return txt;
    }

}
