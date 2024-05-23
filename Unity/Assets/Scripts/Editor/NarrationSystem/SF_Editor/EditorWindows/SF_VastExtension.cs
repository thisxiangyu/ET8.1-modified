
using ET;
using ET.Clent;
using ET.Client;
using SF_Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public partial class SF_Vast
{
    public static bool isAnimationClipEditMode = false;
    private static SF_MotionProcessor SF_MotionProcessorWindow;
    public static UnityEngine.SceneManagement.Scene ActDirectionRoomRef;
    private const string ActDirectionRoomPath = "Assets/Bundles/Narration/NarrationEditor/ActDirectionRoom.unity";
    
    //绘制工具按钮，根据不同的选中物体提供不同的编辑工具
    private void DrawExtensionToolsBtn()
    {
        if (selectedObject is AnimationClip)
        {        
            Texture2D motionProcessorIcon = EditorGUIUtility.IconContent("绿色同心圆").image as Texture2D;
            if (GUILayout.Button(new GUIContent(" Motion Processor", motionProcessorIcon),  EditorStyles.toolbarButton, GUILayout.Width(190), GUILayout.Height(25)))
            {
                InitAnimationClipEditMode();
            }
        }
    }

    #region MotionProcessor相关

    [MenuItem("Assets/Import To Motion Processor...",false,2)]
    private static void ImportToMotionProcessor()
    {
        if(selectedObject!=null&&selectedObject is AnimationClip)
         InitAnimationClipEditMode();
        else
        {
            // 如果未选中 AnimationClip，弹出警告窗口
            EditorUtility.DisplayDialog("无效", "未选中 AnimationClip，只有 AnimationClip 可以这样操作。", "确定");
        }
    }

    public static void InitAnimationClipEditMode()
    {
        if (!isAnimationClipEditMode)
        {
            //加载动作编辑室场景
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ActDirectionRoomPath);
            if (scene != null)
                ActDirectionRoomRef = EditorSceneManager.OpenScene(ActDirectionRoomPath, OpenSceneMode.Additive);
            
            // 尝试获取 SF_MotionProcessor 窗口实例
            if (SF_MotionProcessorWindow == null)
            {        // 在Hierarchy旁边打开一个SF_MotionProcessor窗口
                SF_MotionProcessorWindow =CreateWindow<SF_MotionProcessor>(typeof(SF_Conductor));
                SF_MotionProcessorWindow.titleContent = UpdateWindowTitle.UpdateAs(" MotionProcessor", "绿色同心圆", "MotionProcessor");
            }
            SF_MotionProcessorWindow.Focus();
            
            //延迟一帧执行以下逻辑
            EditorApplication.delayCall += () =>
            {
                SF_MotionProcessorWindow.config.InstantiateAndSelectSamepleBodyGameObject().transform.parent = SF_Vast.ActDirectionRoomRef.GetRootGameObjects()[0].transform;
                EditorApplication.delayCall = null;
            };
            
            //将选择的AnimationClip导入到MotionProcessor的列表当中
            bool existed = false;
            AnimationClip selectedAnimationClip = selectedObject as AnimationClip;
            foreach (AnimationClip animationClip in SF_MotionProcessorWindow.config.ToBeEdited)
            {
                if (animationClip.name == selectedAnimationClip.name)
                    existed = true;
            }
            if(!existed)
                SF_MotionProcessorWindow.config.ToBeEdited.Insert(0,selectedAnimationClip);
            else
            {
                Debug.Log("(SF_Editor)MotionProcessor当中已经存在名为"+selectedAnimationClip.name+"的动画。");
            }
            
            //聚焦场景窗口
            SceneView sceneView = GetWindow<SceneView>();
            if (sceneView != null)
                sceneView.Focus();
            
            //切换窗口布局
            EditorApplication.ExecuteMenuItem("Window/Layouts/SF_AnimationClipsEditMode");
            
            //将SF_Editor卸载
            SceneManager.UnloadSceneAsync(SF_MenuItems.SF_EditorRef);
            isAnimationClipEditMode = true;   
        }
        else
        {
            EditorUtility.DisplayDialog("无效", "Motion Processor此前已开启了。无法重复启动。", "确定");
        }
    }
    
    public static void ReturnSFEditor()
    {
        //把场景切换回SF_Editor
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SF_MenuItems.editorPath);
        if (scene != null)
            EditorSceneManager.OpenScene(SF_MenuItems.editorPath, OpenSceneMode.Additive);
        SF_MenuItems.CheckIfSFEditorSceneIsOpen();
        SceneManager.UnloadSceneAsync(ActDirectionRoomRef);
        
        //切换窗口布局
        EditorApplication.ExecuteMenuItem("Window/Layouts/SF_Default");
        
        isAnimationClipEditMode = false;  
    }

    #endregion

#if 以字符串紧随标记符读写
    //编译EnergyOfSpeak当中的语句
    public static void CompileSentences(EnergyOfSpeak energyOfSpeak)
    {
        energyOfSpeak.CompiledSentences = new CompiledSentence[energyOfSpeak.Sentences.Count];
        for (int i = 0; i < energyOfSpeak.Sentences.Count; i++)
        {
            Sentence thisSentence = energyOfSpeak.Sentences[i];
            //注,这里的标记编译规则设定如下:
            //
            //([ 符号用来作为标记的开始,大写字母代表了列表名, i代表在对应列表中的index, n表示数值
            //由于我们的解析规则是从左向右解析,所以出于最佳考虑,对于+和-不需要双括号,只要左括号就行了,只有带数字的需要双括号,避免无法获取到正确的数字)
            //
            //DurationWeightStatement : [n] => Wn
            //ActState: [in][out] => Ai+n (i代表开始的索引，n代表in和out之间间隔多少个字符 )
            //Resultant: [in] => Ri
            //MusicTrack: [in] => M
            //SoundEffects: [in] => Si
            //Shoot: [in] => Oi

            //将原句切分成string数组
            List<string> wordsOneByOne = new();
            foreach (char oneWord in thisSentence.Words)
            {
                wordsOneByOne.Add(oneWord.ToString());
            }

            #region 编译DurationWeightStatement
            {
                string statement = thisSentence.DurationWeightStatement;
                //检测关键字声明是否为空
                if (statement != null && statement.Length != 0)
                {
                    //检测statementWords是否严格按照在原句的基础上添加标记
                    string pattern = @"\[\d+(\.\d+)?\]";
                    string clearString = Regex.Replace(statement, pattern, ""); // 使用正则表达式匹配并替换所有的 [n] 形式的子字符串
                    if (clearString != thisSentence.Words)
                    {
                        Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的DurationWeightStatement未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                        if(UnityEditor.EditorApplication.isPlaying) 
                            UnityEditor.EditorApplication.isPlaying= false;
                        return;
                    }

                    compileOneByOne:
                    // 使用正则表达式获取所有标记关键字的index
                    Match theFirstMatch = Regex.Match(statement, pattern);
                    if (theFirstMatch.Success)
                    {
                        // 找到第一个匹配项的位置索引
                        int index = theFirstMatch.Index;
                        // 分割输入字符串，并将第一个匹配项删除
                        statement = statement.Substring(0, index) +
                                statement.Substring(index + theFirstMatch.Length);
                        int keyWordIndex = theFirstMatch.Index - 1;
                        wordsOneByOne[keyWordIndex] += theFirstMatch.Value.Replace("[", "W").Replace("]", "");
                        if (statement != thisSentence.Words)
                            goto compileOneByOne;   
                    }
                }
            }
            

            #endregion
            #region 编译ActState
            for (int a = thisSentence.ActDirection.ActState.Count - 1; a >= 0; a--)
            {
                string statement = thisSentence.ActDirection.ActState[a].KeywordsStatement;
                //检测关键字声明是否为空
                if (statement != null && statement.Length != 0)
                {
                    //检测statementWords是否严格按照在原句的基础上添加标记
                    if (statement.Replace("[in]", "").Replace("[out]", "") != thisSentence.Words)
                    {
                        Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的ActDirection内部ActState第" + a + "条设计未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                        if(UnityEditor.EditorApplication.isPlaying) 
                            UnityEditor.EditorApplication.isPlaying= false;
                        return;
                    }

                    //编译in关键字
                    int inIndex = statement.IndexOf("[in]");
                    if(inIndex>0)
                        wordsOneByOne[inIndex - 1] += "A" + a;
                    else
                    {
                        thisSentence.ActDirection.ActState.RemoveAt(a);
                        continue;
                    }

                    statement = statement.Replace("[in]", "");

                    //编译out关键字
                    int outIndex = statement.IndexOf("[out]");
                    int n = outIndex - inIndex; //in和out之间间隔多少个字符
                    if(outIndex>0)
                        wordsOneByOne[inIndex - 1] += "+" + n;
                }
                else
                {
                    thisSentence.ActDirection.ActState.RemoveAt(a);
                }
            }
            #endregion
            #region 编译Resultant
            for (int r = thisSentence.ActDirection.Resultant.Count - 1; r >= 0; r--)
            {
                string statement = thisSentence.ActDirection.Resultant[r].KeywordStatement;
                //检测关键字声明是否为空
                int inIndex = statement.IndexOf("[in]");
                if (statement != null && statement.Length != 0&&inIndex>0)
                {
                    //检测statementWords是否严格按照在原句的基础上添加标记
                    if (statement.Replace("[in]", "") != thisSentence.Words)
                    {
                        Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的ActDirection内部Resultant第" + r + "条设计未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                        if(UnityEditor.EditorApplication.isPlaying) 
                            UnityEditor.EditorApplication.isPlaying= false;
                        return;
                    }
                    wordsOneByOne[inIndex - 1] += "R" + r;
                }
                else
                {
                    thisSentence.ActDirection.Resultant.RemoveAt(r);
                }
            }

#endregion
            #region 编译MusicTrack
            {
                string statement = thisSentence.MusicTrack.KeywordStatement;
                //检测关键字声明是否为空
                int inIndex = statement.IndexOf("[in]");
                if (statement != null && statement.Length != 0)
                {
                    if (inIndex > 0)
                    {
                        if (thisSentence.MusicTrack.music == null)
                        {
                            Debug.LogError("(SF_Editor)检测到Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的MusicTrack未设置任何音乐。");
                            if(UnityEditor.EditorApplication.isPlaying) 
                                UnityEditor.EditorApplication.isPlaying= false;
                            return;
                        }
                        //检测statementWords是否严格按照在原句的基础上添加标记
                        if (statement.Replace("[in]", "") != thisSentence.Words)
                        {
                            Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的MusicTrack关键字未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                            if(UnityEditor.EditorApplication.isPlaying) 
                                UnityEditor.EditorApplication.isPlaying= false;
                            return;
                        }
                        wordsOneByOne[inIndex - 1] += "M";   
                    }
                    else if (inIndex ==0)
                    {
                        Debug.LogError("(SF_Editor)检测到Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的MusicTrack声明的关键字位于首个字符，这是不合法的。");
                    }
                }
            }
            #endregion
            #region 编译SoundEffects
            for (int s = thisSentence.Performances.SoundEffects.Count - 1; s >= 0; s--)
            {
                string statement = thisSentence.Performances.SoundEffects[s].KeywordStatement;
                //检测关键字声明是否为空
                int inIndex = statement.IndexOf("[in]");
                if (statement != null && statement.Length != 0 && inIndex>0)
                {
                    //检测statementWords是否严格按照在原句的基础上添加标记
                    if (statement.Replace("[in]", "") != thisSentence.Words||inIndex<=0)
                    {
                        Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + (i + 1) + "条Sentence[" + thisSentence.Words + "]的Performances内部SoundEffects第" + s + "条设计未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                        if(UnityEditor.EditorApplication.isPlaying) 
                            UnityEditor.EditorApplication.isPlaying= false;
                        return;
                    }
                    wordsOneByOne[inIndex - 1] += "S" + s;
                }
                else
                {
                    thisSentence.Performances.SoundEffects.RemoveAt(s);
                }
            }
            #endregion
            #region 编译Shoot
            for (int o = thisSentence.Performances.Shoot.Count - 1; o >= 0; o--)
            {
                string statement = thisSentence.Performances.Shoot[o].KeywordStatement;
                //检测关键字声明是否为空
                int inIndex = statement.IndexOf("[in]");
                if (statement != null && statement.Length != 0 && inIndex>0)
                {
                    //检测statementWords是否严格按照在原句的基础上添加标记
                    if (statement.Replace("[in]", "") != thisSentence.Words)
                    {
                        Debug.LogError("(SF_Editor)Energy[" + energyOfSpeak.name + "]的第" + o + "条Sentence[" + thisSentence.Words + "]的Performances内部Shoot第" + o + "条设计未严格按照[" + thisSentence.Words + "]进行声明，解析停止。请检查KeywordsStatement的正确性。");
                        if(UnityEditor.EditorApplication.isPlaying) 
                            UnityEditor.EditorApplication.isPlaying= false;
                        return;
                    }
                    wordsOneByOne[inIndex - 1] += "O" + o;
                }
                else
                {
                    thisSentence.Performances.Shoot.RemoveAt(o);
                }
            }
            #endregion
            ///整合所有编译语句，编译完毕
            energyOfSpeak.CompiledSentences[i] = new CompiledSentence(wordsInput: wordsOneByOne.ToArray());
        }
    }

    public static void CompileAllEnergyOfSpeak() {
        string[] guids = AssetDatabase.FindAssets("t:EnergyOfSpeak"); // 查找所有类型为EnergyOfSpeak的资产
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EnergyOfSpeak energyOfSpeak = AssetDatabase.LoadAssetAtPath<EnergyOfSpeak>(assetPath);
            if (energyOfSpeak != null)
            {
                Debug.Log("(SF_Editor)开始编译EnergyOfSpeak : " + assetPath);
                CompileSentences(energyOfSpeak);
                EditorUtility.SetDirty(energyOfSpeak);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("(SF_Editor)所有EnergyOfSpeak编译完成。");
    }

    //在编辑器当中按照原句同步声明
    public static void SynchronizeStatementsToWords(EnergyOfSpeak energyOfSpeak,int sentenceIndex = -1)
    {
        Undo.RecordObject(energyOfSpeak,"EnergyOfSpeak刷新声明");
        
        for (int i=0;i< energyOfSpeak.Sentences.Count;i++) 
        {
            //sentenceIndex为-1则全部同步，否则就是单句同步，直接跳过不需要同步的sentence
            if(sentenceIndex != -1 && i!=sentenceIndex)
                continue;
            
            Sentence thisSentence = energyOfSpeak.Sentences[i];

            #region 同步DurationWeightStatement
            {
                List<string> durationWeightWordList = new List<string>();
                string durationWeightPattern =  @"\[\d+(\.\d+)?\]";
                MatchCollection matches = Regex.Matches(thisSentence.DurationWeightStatement, durationWeightPattern);
                //检测捕获内容的数量
                if (matches.Count>0 )
                {
                    //开始拆分
                    int lastIndex = 0;
                    for(int matchIndex = 0;matchIndex<matches.Count;matchIndex++)
                    {
                        Match match = matches[matchIndex];
                        if(match.Index - lastIndex - 1>=0)
                        {
                            string substring = thisSentence.DurationWeightStatement.Substring(lastIndex, match.Index - lastIndex - 1);
                            durationWeightWordList.Add(thisSentence.DurationWeightStatement[match.Index - 1] + match.Value);
                            lastIndex = match.Index + match.Length;
                        }
                        else //这种情况说明两个权重连在一起了,只保留一个权重
                        {
                            durationWeightWordList[durationWeightWordList.Count - 1] += match.Value;
                            lastIndex = match.Index + match.Length;                            
                        }
                    }
                    //拆分完毕

                    //生成新的声明
                    string NewDurationWeightStatement = thisSentence.Words;
                    int keywordIndexMark=-1;
                    foreach (string durationWeightWords  in durationWeightWordList)
                    {
                        char keyWord = durationWeightWords[0];
                        //这个算法很难解释，不注释了，直接看代码理解吧......
                        //反正，它的目的是为了让新的声明尽量保持原声明已有的关键字
                        for (int k = 0; k < NewDurationWeightStatement.Length; k++)
                        {
                            if (NewDurationWeightStatement[k] == keyWord)
                            {
                                if (keywordIndexMark == -1)
                                    keywordIndexMark = k;
                                else if( k<keywordIndexMark)
                                    continue;
                                NewDurationWeightStatement = NewDurationWeightStatement.Insert(k+1,durationWeightWords.Replace(keyWord.ToString(),""));
                                break;
                            }
                        }
                    }
                    thisSentence.DurationWeightStatement = NewDurationWeightStatement;
                }
                else
                    thisSentence.DurationWeightStatement = thisSentence.Words;
            }
            #endregion
            #region 同步ActState
            for(int a = 0; a< thisSentence.ActDirection.ActState.Count;a++)
            {
                int inWordIndex = -1;
                int outWordIndex = -1;
                List<string> list = new List<string>();
                // 使用正则表达式匹配[in]或者[out]，然后将其作为整体加入到拆分后的列表中
                string actStatePattern = @"\[(in|out)\]";
                string ActStateStatement = thisSentence.ActDirection.ActState[a].KeywordsStatement;
                MatchCollection matches = Regex.Matches(ActStateStatement, actStatePattern);
                //检测捕获内容的合法性
                if (matches.Count>0 && matches[0].Value == "[in]")
                {
                    inWordIndex = matches[0].Index - 1;
                    if (matches[0].Index == 0)
                    {
                        Debug.LogError("(SF_Editor)刷新失败，检测到“"+thisSentence.Words+"”当中ActState的第"+(a+1)+"条声明[in]标记在首字符前的位置，这是不合法的，请修正再继续。");
                        return;
                    }
                    if(matches.Count>1&&matches[1].Value!="[out]")
                    {
                        Debug.LogError("(SF_Editor)刷新失败，检测到“"+thisSentence.Words+"”当中ActState的第"+(a+1)+"条声明存在重复的[in]，请修正再继续。");
                        return;
                    }
                    //开始拆分
                    int lastIndex = 0;
                    for(int matchIndex = 0;matchIndex<2 && matchIndex<matches.Count;matchIndex++)
                    {
                        Match match = matches[matchIndex];
                        if(match.Index - lastIndex - 1>=0)
                        {
                            string substring = ActStateStatement.Substring(lastIndex, match.Index - lastIndex - 1);
                            foreach (char Char in substring)
                                list.Add(Char.ToString());
                            list.Add(ActStateStatement[match.Index - 1] + match.Value);
                            if(match.Value=="[out]")
                                outWordIndex = list.Count-1;
                            lastIndex = match.Index + match.Length;
                        }
                        else //这种情况说明[in]和[out]连在一起了,需要将[out]写到[in]的后面
                        {
                            list[list.Count - 1] += "[out]";
                            outWordIndex = inWordIndex;
                            lastIndex = match.Index + match.Length;                            
                        }
                    }
                    if (lastIndex < ActStateStatement.Length)
                    {
                        string substring = ActStateStatement.Substring(lastIndex);
                        foreach (char Char in substring)
                            list.Add(Char.ToString());
                    }
                    //拆分完毕
            
                    //比对拆分后的列表与Words的差异，按以下三个规则进行同步：
                    //1.如果[in]和[out]所在的字符同时都依然存在，且二者前后顺序也没有发生改变，大概率是进行了缩句或者扩句
                    //2.如果[in]和[out]所在的字符任何一个已经不存在而另一个存在，很可能是删掉了一部分句子成分，那就让依然存在的字符保持不变，另找一个新的字符，尽量保持中间间隔字符数量一致
                    //3.如果[in]和[out]都已经不在，那么最可能是整句话都重写了，那就只需要尽量保持中间间隔字符数量一致就可以了
                    //举例来说，
                    //有这样一个已有声明：你好[in]呀你好呀[out]哈哈
                    // 要进行同步，可能会有不同情况如下所示
                    //（左边是目标Words内容，右边是进行同步后的）
                    //情况1 ：你好呀哈哈哈你好呀 => 你好[in]呀哈哈哈你好呀[out]
                    //情况2 ：很好，很好 => 很好[in]，很好
                    //情况3 ：等你呀,赶快走吧 => 等你[in]呀,赶快走[out]吧
            
                    if (outWordIndex != -1)//存在out
                    {
                        //获取所有匹配的in字符和out字符
                        string words = thisSentence.Words;
                        char targetCharOfIn = list[inWordIndex][0];
                        char targetCharOfOut = list[outWordIndex][0];
                        // 使用正则表达式匹配目标字符
                        MatchCollection matchesOfIn = Regex.Matches(words, Regex.Escape(targetCharOfIn.ToString()));
                        MatchCollection matchesOfOut = Regex.Matches(words, Regex.Escape(targetCharOfOut.ToString()));
                
                        //情况1:二者皆存在
                        if ( thisSentence.Words.Contains(list[inWordIndex][0]) && thisSentence.Words.Contains(list[outWordIndex][0]))
                        {
                            //两两组成一对，计算两个字符之间的相对距离
                            //找出相对距离的值最接近于声明语句的，作为胜选的一对
                            //元组第一个int表示in字符，第二个表示out字符，第三个表示它们之间的间隔，第四个表示in的关键字的索引偏移量
                            (int,int,int,int) evaluationData = new (0,0,int.MaxValue,int.MaxValue);
                            foreach (Match matchOfIn in matchesOfIn) {
                                foreach (Match matchOfOut in matchesOfOut)
                                {
                                    //计算间隔（距离多少个字）
                                    int distance = matchOfOut.Index - matchOfIn.Index;
                                    //剔除距离为负数的（因为in必然要在out之前，不可能反过来）
                                    if (distance >= 0)
                                    {
                                        //获取声明中两个字的间隔
                                        int distanceInStatementWords = outWordIndex - inWordIndex;
                                        //找出最接近distanceInStatementWords的距离值
                                        int 间隔差值的绝对值 = Math.Abs(distanceInStatementWords - distance);
                                        int in关键字索引的偏移 = Math.Abs(matchOfIn.Index - inWordIndex);
                                        //先比较间隔差值的绝对值，如果有必要再比较in关键字索引的偏移
                                        if (间隔差值的绝对值 < evaluationData.Item3)
                                            evaluationData = new(matchOfIn.Index,matchOfOut.Index,间隔差值的绝对值,in关键字索引的偏移);
                                        else if (间隔差值的绝对值 == evaluationData.Item3)
                                        {
                                            if(in关键字索引的偏移<evaluationData.Item4)
                                                evaluationData = new(matchOfIn.Index,matchOfOut.Index,间隔差值的绝对值,in关键字索引的偏移);
                                        }
                                    }
                                }
                            }
                            //如果存在合适的一对
                            if (evaluationData.Item3 != int.MaxValue)
                            {
                                string NewStatement = words.Insert(evaluationData.Item2+1,"[out]").Insert(evaluationData.Item1+1,"[in]");
                                thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                            }
                            else //如果不存在，转换in和out位置，然后按照位置比例匹配in和out
                            {
                                float in位置占比 = (float)(inWordIndex+1)/(list.Count-1);
                                float out位置占比= (float)(outWordIndex+1)/(list.Count-1);
                                if (in位置占比 > 1)
                                    in位置占比 = 1;
                                if (out位置占比 > 1)
                                    out位置占比 = 1;
                                string NewStatement = thisSentence.Words.Insert(Mathf.CeilToInt(out位置占比*(thisSentence.Words.Length-1)+1),"[out]").Insert(Mathf.CeilToInt(in位置占比*(thisSentence.Words.Length-1)+1),"[in]");
                                thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                            }
                        }
                        //情况2:二者存其一
                        else if (thisSentence.Words.Contains(list[inWordIndex][0]) || thisSentence.Words.Contains(list[outWordIndex][0]))
                        {
                            //在所有潜在关键字当中选出最符合的一个
                            int distanceOfIn = inWordIndex;
                            int distanceOfOut = list.Count - outWordIndex;
                            int distanceBetweenInAndOut = Math.Abs(outWordIndex - inWordIndex);
                            //如果存在的是in关键字
                            if (matchesOfIn.Count != 0)
                            {
                                int indexOfNewWordOfIn = int.MaxValue;
                                foreach (Match match in matchesOfIn)
                                {
                                    if (Math.Abs(distanceOfIn - match.Index) < indexOfNewWordOfIn)
                                        indexOfNewWordOfIn = match.Index;
                                }

                                string NewStatement = default;
                                if(indexOfNewWordOfIn+1+distanceBetweenInAndOut<thisSentence.Words.Length)
                                    NewStatement = thisSentence.Words.Insert(indexOfNewWordOfIn+1+distanceBetweenInAndOut,"[out]").Insert(indexOfNewWordOfIn+1,"[in]");
                                else
                                    NewStatement = thisSentence.Words.Insert(thisSentence.Words.Length,"[out]").Insert(indexOfNewWordOfIn+1,"[in]");
                                thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                            }
                            //如果存在的是out关键字
                            else
                            {   int indexOfNewWordOfOut = int.MaxValue;
                                foreach (Match match in matchesOfOut)
                                {
                                    int indexCount = default;
                                    if (thisSentence.Words.Length - distanceOfOut > 1)
                                        indexCount = thisSentence.Words.Length - distanceOfOut;
                                    else
                                        indexCount = 1;
                                    if (Math.Abs(match.Index - indexCount) < indexOfNewWordOfOut)
                                        indexOfNewWordOfOut = match.Index;
                                }
                                string NewStatement = default;
                                if(indexOfNewWordOfOut+1-distanceBetweenInAndOut>1)
                                    NewStatement = thisSentence.Words.Insert(indexOfNewWordOfOut+1,"[out]").Insert(indexOfNewWordOfOut+1-distanceBetweenInAndOut,"[in]");
                                else
                                    NewStatement = thisSentence.Words.Insert(indexOfNewWordOfOut+1,"[out]").Insert(1,"[in]");
                                thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                            }
                        }
                        else //情况3:二者皆不存在
                        {
                            //按照位置比例匹配in和out
                            float in位置占比 = (inWordIndex+1)/(float)(list.Count-1);
                            float out位置占比= (outWordIndex+1)/(float)(list.Count-1);
                            if (in位置占比 > 1)
                                in位置占比 = 1;
                            if (out位置占比 > 1)
                                out位置占比 = 1;
                            string NewStatement = thisSentence.Words.Insert(Mathf.CeilToInt(out位置占比*(thisSentence.Words.Length-1)+1),"[out]").Insert(Mathf.CeilToInt(in位置占比*(thisSentence.Words.Length-1)+1),"[in]");
                            thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                        }
                    }
                    else //不存在out，仅需匹配in
                    {
                        float in位置占比 = inWordIndex/(float)(list.Count-1);
                        if (in位置占比 > 1)
                            in位置占比 = 1;
                        string NewStatement = thisSentence.Words.Insert(Mathf.CeilToInt(in位置占比*(thisSentence.Words.Length-1)+1),"[in]");
                        thisSentence.ActDirection.ActState[a].KeywordsStatement = NewStatement;
                    }
                }
                else //不存在in或者没有匹配的关键字，直接重置
                    thisSentence.ActDirection.ActState[a].KeywordsStatement = thisSentence.Words;
            }
            #endregion
            #region 同步Resultant
            for (int r = 0; r < thisSentence.ActDirection.Resultant.Count; r++)
            {
                string NewStatement = SynchronizeKeyWordOfIn(thisSentence.ActDirection.Resultant[r].KeywordStatement,thisSentence.Words);
                thisSentence.ActDirection.Resultant[r].KeywordStatement = NewStatement;
            }
            #endregion
            #region 同步MusicTrack
            thisSentence.MusicTrack.KeywordStatement =SynchronizeKeyWordOfIn(thisSentence.MusicTrack.KeywordStatement,thisSentence.Words);
            #endregion
            #region 同步SoundEffects
            for (int s = 0; s < thisSentence.Performances.SoundEffects.Count; s++)
            {
                string NewStatement = SynchronizeKeyWordOfIn(thisSentence.Performances.SoundEffects[s].KeywordStatement,thisSentence.Words);
                thisSentence.Performances.SoundEffects[s].KeywordStatement = NewStatement;
            }
            #endregion
            #region 同步Shoot
            for (int o = 0; o < thisSentence.Performances.Shoot.Count; o++)
            {
                string NewStatement = SynchronizeKeyWordOfIn(thisSentence.Performances.Shoot[o].KeywordStatement,thisSentence.Words);
                thisSentence.Performances.Shoot[o].KeywordStatement = NewStatement;
            }
            #endregion
        }

        EditorUtility.SetDirty(selectedObject);
        AssetDatabase.Refresh();
    }

    private static string SynchronizeKeyWordOfIn(string statement,string Words)
    {
        string PatternOfIn = @"\[(in)\]";
        Match match = Regex.Match(statement, PatternOfIn);
        if (match.Success && match.Index != 0)
        {
            string cleanedStatement = statement.Remove(match.Index, 4);
            float in位置占比 = (match.Index - 1) / (float)(cleanedStatement.Length - 1);
            if (in位置占比 > 1)
                in位置占比 = 1;
            string NewStatement = Words.Insert(Mathf.CeilToInt(in位置占比 * (Words.Length - 1) + 1), "[in]");
            return NewStatement;
        }
        return Words;
    }
#elif 以索引记载读写

#endif
}
