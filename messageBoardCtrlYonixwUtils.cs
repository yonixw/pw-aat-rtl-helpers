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

// Todo:

// Get all AAT tags and their meaning?

// Print all files as they are loaded (where to inject?...)
    // Load in any scene:
        // https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html

// Simple 1..0..1 for alignment

// Color need tags reverse for RTL ... but there are 2 tags - open and not open...

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
        Process clipboardExecutable = new Process(); 
        clipboardExecutable.StartInfo = new ProcessStartInfo // Creates the process
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
            FileName = @"clip", 
        };
        clipboardExecutable.Start();

        clipboardExecutable.StandardInput.WriteLine(value); // CLIP uses STDIN as input.
        // When we are done writing all the string, close it so clip doesn't wait and get stuck
        clipboardExecutable.StandardInput.Close(); 
        
         clipboardExecutable.WaitForExit();
         
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
                 if (_simpleReplace.ContainsKey(_TXT)) {
                    _TXT = _simpleReplace[_TXT];
                }
                
                text.alignment = TextAnchor.MiddleRight; // Todo, based on '##0' or based on Name?
                if       (_TXT.Contains(" #1# ")) {
                    _TXT = _TXT.Replace(" #1# ","");
                    text.alignment = TextAnchor.UpperLeft;
                } else if (_TXT.Contains(" #2# ")) {
                    _TXT = _TXT.Replace(" #2# ","");
                    text.alignment = TextAnchor.UpperCenter;
                } else if (_TXT.Contains(" #3# ")) {
                    _TXT = _TXT.Replace(" #3# ","");
                    text.alignment = TextAnchor.UpperRight;
                } else if (_TXT.Contains(" #4# ")) {
                    _TXT = _TXT.Replace(" #4# ","");
                    text.alignment = TextAnchor.MiddleLeft;
                } else if (_TXT.Contains(" #5# ")) {
                    _TXT = _TXT.Replace(" #5# ","");
                    text.alignment = TextAnchor.MiddleCenter;
                } else if (_TXT.Contains(" #6# ")) {
                    _TXT = _TXT.Replace(" #6# ","");
                    text.alignment = TextAnchor.MiddleRight;
                } else if (_TXT.Contains(" #7# ")) {
                    _TXT = _TXT.Replace(" #7# ","");
                    text.alignment = TextAnchor.LowerLeft;
                } else if (_TXT.Contains(" #8# ")) {
                    _TXT = _TXT.Replace(" #8# ","");
                    text.alignment = TextAnchor.LowerCenter;
                } else if (_TXT.Contains(" #9# ")) {
                    _TXT = _TXT.Replace(" #9# ","");
                    text.alignment = TextAnchor.LowerRight;
                }

                text.text = 
                        _rtl_flag + 
                        fullregreplace(FixRTL(_TXT));
            }
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
            new XWConfig.XWReplace() { regex = "\\(", replace = ")" } 
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
