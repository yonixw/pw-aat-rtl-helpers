using System;
using System.Collections.Generic;

using UnityEngine;
//using UnityEngine.UI;


namespace UnityEngine.UI
{
    public static class YonixwUnityUITools
    {
        // Remember to Add to: UnityEngine.UI.Text:
        //
        //  YonixwUnityUITools.addMe(YonixwUnityUITools.asText(this));
        //
        //      (MonoBehaviour) protected override void Text.OnEnable()
        //      (MonoBehaviour) protected override void Text.OnTransformParentChanged()

        public static List<Text> Added = new List<Text>();
        public static List<string> FullNames = new List<string>();

        public static void addMe(Text t)
        {

            string fullpath = objPath(t.transform);
            Added.Add(t);
            FullNames.Add(fullpath);
        }

        public static void clear()
        {
            Added = new List<Text>();
            FullNames = new List<string>();
        }

        public static Text asText(object o)
        {
            return (Text)o;
        }

        public static string objPath(Transform t)
        {
            string path = "/" + t.name; // "/" is the start path
            Transform parent = t.parent;
            while (parent != null)
            {
                path = "/" + parent.name + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
