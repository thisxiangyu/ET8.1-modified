using UnityEngine;

namespace ET
{
    [FriendOf(typeof(GlobalComponent))]
    public static partial class GlobalComponentSystem
    {
        [EntitySystem]
        public static void Awake(this GlobalComponent self)
        {
            self.Global = GameObject.Find("/Global").transform;
            self.Unit = GameObject.Find("/Global/Unit").transform;
            self.UI = GameObject.Find("/Global/UI").transform;
            self.GlobalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            self.Camera = self.Global.Find("MainCamera").GetComponent<Camera>();
            self.UICamera = self.Global.Find("UICamera").GetComponent<Camera>();
        }
    }
    
    [ComponentOf(typeof(Scene))]
    public class GlobalComponent: Entity, IAwake
    {
        public Transform Global;
        public Transform Unit { get; set; }
        public Transform UI;
        public Camera Camera { get; set; }
        public Camera UICamera { get; set; }

        public GlobalConfig GlobalConfig { get; set; }
    }
}