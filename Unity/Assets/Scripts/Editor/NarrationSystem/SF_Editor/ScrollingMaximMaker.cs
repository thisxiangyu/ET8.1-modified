using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScrollingMaximMaker : MonoBehaviour
{
    public static string currentMaxim = "“不积跬步无以至千里，不积小流无以成江海。”  —— 荀子";
    private static int currentIndex = 0;
    
    public static float lastUpdateTime;
    private static float interval = 16f; // 间隔时间

    private static readonly List<string> list = new ()
    {
        "“开头的第一段必须已经拥有一切。”  —— 加西亚·马尔克斯",
        "“写作的第一步，显然是选择一个主题。”  —— 阿契尔《剧作法》",
        "“真正的主题不是一个词，而是一个清晰、连贯的句子。” —— 罗伯特·麦基《故事》",
        "“灵感不在的时候需要技巧来补偿。”  —— 加西亚·马尔克斯",
        "“人物弧光（Character Arc），很少会遵循一条平滑的轨迹，而更可能在动态的曲折中前行。”  —— 罗伯特·麦基《人物》",
        "“在故事开头出现过的物品一定要在后来用到。” —— 契诃夫",
        "“能删则删，删了再删。”  —— 毛姆",
        "“生活就是与心中魔鬼搏斗，写作就是对自己进行审判。”  —— 易卜生",
        "“戏剧的本质是'激变'（Crisis）。剧作家所处理的是剧烈的变化。”  —— 阿契尔《剧作法》",
        "“Show，don`t tell.”（展示给观众，而不是告诉观众）",
        "“若无'冲突'，故事中的一切都无法向前发展。  —— 罗伯特·麦基《故事》”",
        "“故事是由五个部分组成的设计：激励事件、进展纠葛、危机、高潮、结局。”  —— 罗伯特·麦基《故事》",
        "“我写作的开始五花八门，有主题先行的，也有时候是一个细节、一段对话或者一个意象打动了我。”  —— 余华",
        "“所有的主义都是现实主义。”  —— 贾樟柯",
        "“神话乃众人的梦，梦乃众人的神话。”  —— 约瑟夫·坎贝尔《千面英雄》",
        "“启程 - 启蒙 - 归来，是神话中英雄历程的标准道路。”  —— 约瑟夫·坎贝尔《千面英雄》",
        "“舞台前沿是一道‘第四堵墙’，它对观众是透明的，对演员来说是不透明的。”  —— 让·柔琏",
        "“The look，the hook，and the book.”  —— 贾斯汀·怀亚特对美国早期'高概念电影'的定义",
        "“'麦格芬'是一种旁敲侧击，一种诡计，一种手段，一种gimmick。” —— 希区柯克解释故事中的'麦格芬'",
        "“互动叙事与小说或电影不同，互动叙事的核心是玩家，而不是创作者。”  —— Chris Crawford",
    };
    
    public static void DoScrolling()
    {
        float currentTime = (float)EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime >= interval)
        {
            // 每隔 interval 秒执行一次逻辑
            if (currentIndex != list.Count - 1)
                currentIndex += 1;
            else
                currentIndex = 0;
            currentMaxim = list[currentIndex];
            lastUpdateTime = currentTime;
        }
    }
}
