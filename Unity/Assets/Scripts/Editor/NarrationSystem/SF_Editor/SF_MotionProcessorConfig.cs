using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using ET.Client;
using SF基本设置;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class SF_MotionProcessorConfig : ScriptableObject
{
    [Space(10)]
    [ShowInSettingWindow]public ObjectConfigOfNarration SampleBody;
    public GameObject SampleBodyGameObj;
    private const string 泛用人形path = "Assets/Bundles/Narration/NarrationEditor/泛用人形.asset";
    
    [ShowInSettingWindow]public List<AnimationClip> ToBeEdited;
    public AnimatorController MotionProcessorClipController;
    private const string clipControllerPath = "Assets/Bundles/Narration/NarrationEditor/MotionProcessorClipController.controller";

    //如果SampleBody为空，那就按照默认的泛用人形来配置SampleBody
    public void AvoidNullSampleBody()
    {
        // 加载泛用人形的Asset
        ObjectConfigOfNarration 泛用人形 = AssetDatabase.LoadAssetAtPath(泛用人形path, typeof(CharacterConfigOfNarration)) as ObjectConfigOfNarration;
        if (泛用人形 == null)
            Debug.LogWarning("(SF_Editor)当前位置（"+泛用人形path+"）缺失泛用人形配置。");
        //如果SampleBody没有被设置过，那么就采用默认的泛用人形
        if (SampleBody == null)
            SampleBody = 泛用人形;
    }

    //将Config当中的预制体实例化到场景当中并设置为激活
    public GameObject InstantiateAndSelectSamepleBodyGameObject()
    {
        if (SampleBody && SampleBody.GetPrefab())
        {
            GameObject prefab = SampleBody.GetPrefab(); 
            GameObject SampleBodyInstance = Instantiate(prefab); 
                        
            SampleBodyGameObj = SampleBodyInstance;
            
            // 检查是否已经存在Animator组件
            Animator animator = SampleBodyInstance.GetComponent<Animator>();
            // 如果不存在Animator组件，则添加
            if (animator == null) 
                animator = SampleBodyInstance.AddComponent<Animator>();
            
            // 加载ClipController的Asset
            MotionProcessorClipController = AssetDatabase.LoadAssetAtPath(clipControllerPath, typeof(AnimatorController)) as AnimatorController;
            if (MotionProcessorClipController == null)
            {
                Debug.LogError("(SF_Editor)当前位置（"+clipControllerPath+"）缺失MotionProcessorClipController。");
                return null;
            }
            animator.runtimeAnimatorController = this.MotionProcessorClipController;

            SampleBodyInstance.SetActive(true);
            SampleBodyInstance.name = SampleBody.name;
            
            Selection.activeGameObject = SampleBodyGameObj;
            EditorApplication.ExecuteMenuItem("Edit/Lock View to Selected");
            
            //设置图标以使得骨骼可视化
            Transform[] allChilds = SampleBodyGameObj.transform.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChilds)
            {
                EditorGUIUtility.SetIconForObject(child.gameObject, (Texture2D)EditorGUIUtility.IconContent("绿色小圆点").image);
            }
            
            return SampleBodyGameObj;
        }
        Debug.LogWarning(("(SF_Editor)MotionProcessor的SampleBody为空。"));
        return null;
    }

    //在MotionProcessorr启动的时候将MotionProcessorClipController赋予对应对象的Animator。
    //如果Config当中的对象自带Animator那么直接添加，如果不自带Animator就需要附加Animator组件然后再赋予。
    //
    public void AttachClipControllerToTheBody()
    {
        
    }

    //将待编辑的动画文件添加到ClipController当中
    public void PushAllClipsToBeEdited()
    {
        
    }
    
    // 添加状态到ClipController
    private void AddState(string stateName)
    {
        // 获取AnimatorController的根状态机
        AnimatorStateMachine rootStateMachine = MotionProcessorClipController.layers[0].stateMachine;
        // 创建一个新的状态
        AnimatorState newState = rootStateMachine.AddState(stateName);

        //（注意，这个方法当中的状态机类和状态类是来自于UnityEditor命名空间，所以只能在编辑器有效，打包运行时无效）
        //（运行时的状态中动画替换仅能使用AnimatorOverrideController实现）
        newState.motion = ToBeEdited[0]; // 将新的AnimationClip赋给状态
    }

}
