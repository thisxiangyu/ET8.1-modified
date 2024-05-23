using UnityEditor;
using UnityEngine;
using ET.Client;
using System;

public class StringsFlowAssetsPostprocessor : AssetPostprocessor
{
    private const string StringsFlowTag = "StringsFlowAsset";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                               string[] movedAssets, string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (importedAssets[i].EndsWith(".asset"))
            {
                //UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(importedAssets[i]);
                //if (asset is StringOfNarration)
                //{
                //    AssetDatabase.SetLabels(asset, new[] { StringsFlowTag });    //给SF资产自动打上StringsFlowTag。
                //}
            }
        }
    } 
}

public class StringsFlowModificationProcessor : AssetModificationProcessor
{
    // 在资源即将被创建时调用
    public static void OnWillCreateAsset(string path)
    {
        //if (path.EndsWith(".asset"))
        //{
        //    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        //    if (asset is StringOfNarration)
        //    {
        //        Debug.Log("(SF_Editor)Create Asset :" + path);
        //    }
        //}
    }

    // 在资源即将被删除时调用
    public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
    {
        // 检查文件是否存在，如果存在则表示删除操作
        if (System.IO.File.Exists(path))
        {
            if (path.EndsWith(".asset"))
            {
                Debug.Log("(SF_Editor)DeletedAsset :" + path);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset is ConfigOfNarration)
                {
                    ((ConfigOfNarration)asset).DeleteAllRuntimeAssets(); //删除的时候把附属中的分化体也删除
                }
            }
        }
        // 返回删除成功
        return AssetDeleteResult.DidNotDelete;
    }  
}