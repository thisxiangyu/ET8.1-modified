using ET.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SF_编辑器变量批量转换工具: ScriptableObject
{
    List<ScriptableObject> ScriptableObject;

    public static void 开始转换()
    {
        Debug.Log("(SF_编辑器变量批量转换工具)已激活");
        转换全部SpeakOfEergy的Words到DurationStatement();
    }

    /// _______________________________在下面写要转换的内容_________________________________


    static void 转换数组部分() {
        //(ScriptableObject as EnergyOfSpeak).Sentences.Words
    }

    static void 转换全部SpeakOfEergy的Words到DurationStatement()
    {
        Debug.Log("(SF_编辑器变量批量转换工具)即将转换全部SpeakOfEergy的Words到DurationStatement.");
        //(ScriptableObject as EnergyOfSpeak).Sentences.Words
    }
}
