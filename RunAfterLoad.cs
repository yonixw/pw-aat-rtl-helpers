using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class RunAfterLoad : MonoBehaviour
{
    /*
    // If "AfterSceneLoad" trick doesn't work, add to :
    public class ReplaceFont : MonoBehaviour
    {
        ...

	    public void Start() { // Start comes after awake
		    RunAfterLoad.OnAfterSceneLoad()
	    }
    */

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void OnAfterSceneLoad()
    {
        GameObject go = new GameObject("_yonixw_first_util");
        go.AddComponent<RunAfterLoad>(); // Add this
        Debug.Log("_yonixw_first_util start!");
    }

    public void LateUpdate()
    {
        messageBoardCtrlYonixwUtils._LateUpdateFull(this);
    }
}

