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

// Get all tags and their meaning?

// Allow refresh line with new regex (need to save cache)
    // Refresh
// Allow show meta (position?) of labels

// Fix center by '##1'...?
// Print all files as they are loaded (where to inject?...)
    // Load in any scene:
        // https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
        
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
    
    public static void incRtlFlag () {
            _rtl_i++;
            _rtl_flag = "<size=" + _rtl_i + "></size>";
            _rtl_txt_cache = (new string[_rtl_txt_cache.Length]).ToList();
    }
    
    public static List<string> _source_txt_cache = new List<string>();
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
    }
    
    private Dictionary<string, string> _simpleReplace = 
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
    
    
    
    public static void _LateUpdate(List<Text> line_list_)
    {
        if (_config == null)
        {
            reloadConfig();
        }
        
        if (Input.GetKeyUp(KeyCode.F4))
        {
            AllocConsole();
            _consoleStdOutWriter = new StreamWriter(Console.OpenStandardOutput());
            _consoleStdOutWriter.AutoFlush = true;
            l("[F4] Console Start! " + Time.deltaTime);
        }
        if (Input.GetKeyUp(KeyCode.F5))
        {
            // To quick debug of configs, we tell to use 
            // latest original text 
            _reuse_cache = true; 
            
            reloadConfig();
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
                l("[2] print dump all UnityEngine.UI.Text");
                l("[3] copy  dump all UnityEngine.UI.Text");
                l("[4] Save example config");
            }
            if (option == "1") {
                l("[0]: " + line_list_[0].text);
                l("[1]: " + line_list_[1].text);
                l("[2]: " + line_list_[2].text);
            }
            if (option == "2" || option == "3") {
                UnityEngine.UI.Text[] allTxts = 
                    Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
                // active only GameObject.FindObjectsOfType<UnityEngine.UI.Text>();
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
            
        }
        
        // Update all
        for (int i=0;i<_txt_cache.Length;i++) {
        {
            // Becaue we monitor and change the 
            // Same text that is being changed by the game from
            // other scripts,
            // Our only way to detect change is if the rtl_flag
            // is removed (or the text is empty). 
            
            Text text = _txt_cache[i];
            bool noChange = 
                string.IsNullOrEmpty(text.text) ||
                text.text.StartsWith(_rtl_flag);
            
            
            if (!noChange)
            {
                if (_reuse_cache) {
                    _source_txt_cache[i] = text.text;
                    _reuse_cache = false;
                }
                
                if (_config.simpleTranslate.ContainsKey(_source_txt_cache[i])) {
                    text.text = 
                        _rtl_flag + 
                        fullregreplace(FixRTL(
                            _config.simpleTranslate[_source_txt_cache[i]]
                        ));
                }
                else {
                    text.text = 
                        _rtl_flag + 
                        fullregreplace(FixRTL(_source_txt_cache[i]));
                }
                
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
            new List<Text>(_config.unityUINameRegex.Length);
        _source_txt_cache = 
            new List<string>(_config.unityUINameRegex.Length);
            
        UnityEngine.UI.Text[] allTxts = 
                    Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
        List<string> allTxtsFullPath = 
                    new Liste<string>();
        
        foreach (Text t in allTxts) {
            allTxtsFullPath.Add(objPath(t));
        }
        for (int i=0;i<allTxtsFullPath.Length;i++) {
            for (int j=0;j<_config.unityUINameRegex.Length;j++) {
                if (
                    new Regex(_config.unityUINameRegex[j])
                    .Match(allTxtsFullPath[i])
                    .Success
                    ) {
                    _txt_cache.Add(allTxts[i]);
                    _txt_cache.Add(allTxts[i].text);
                }
            }
        }
        
        // Simple (exact) replace dict populate
        _simpleReplace = new Dictionary<string, string>();
        for (int i=0;i<_config.simpleTranslate.Length;i++) {
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
        
        resetConfigLocal()
    }
    
    public static void saveExampleConfig() {
        XWConfig c = new XWConfig();
        c.replaces.Add(
            new XWConfig.XWReplace() { regex = "\\(", replace = ")" } 
        );
        c.unityUINameRegex.Add("line\\d\\d");
        c.simpleTranslate.Add(
            new XWConfig.XWReplace() { regex = "Back", replace = "חזור" } 
        );
        
        File.WriteAllText(getConfPath(),toXML(c));
    }
    
    public static string regreplace(string txt, string from, string to) {
        return Regex.Replace(txt, from, Regex.Unescape(to));
    }

    public static string fullregreplace(string txt) {
        foreach (XWConfig.XWReplace pair in _config.replaces) {
            txt = regreplace(txt,pair.regex,pair.replace);
        }
        return txt;
    }

}
