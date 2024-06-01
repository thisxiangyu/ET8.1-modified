using UnityEngine;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    public class GoOnTheRoad : MonoBehaviour
    {
        public Init InitRef;
        public bool avoidRoadScene = false;

        [Header("GlobalMainCamera will be deactivated, use RoadScene cameras")]
        public Camera GlobalMainCameraRef;
        public Camera GlobalUICameraRef;

        [HideInInspector]public UnityEngine.SceneManagement.Scene RoadScene;

        //单例
        private static GoOnTheRoad instance;
        public static GoOnTheRoad Instance
        {
            get
            {
                return instance;
            }
        }

        void Awake()
        {
            instance = this;

            if(!avoidRoadScene)
            {
                InitRef.OnStartAsyncBegin += LoadRoadScene;
                InitRef.OnStartAsyncFinish += SetRoadSceneAsActiveScene;
                InitRef.OnStartAsyncFinish += ()=>Destroy(this);
            }
        }

        /*第一个场景是不需要异步加载的，因为要等Road加载完，主线程才应该往后推*/
        /*为了节省初始包体大小，在启动界面的Road场景和环境元素仅包括一小部分，别的所有元素要等资源下载、点击进入游戏之后再加载*/
        public void LoadRoadScene() {
            SceneManager.LoadScene("Road", LoadSceneMode.Single);
            RoadScene = SceneManager.GetSceneByName("Road");
        }

        public void SetRoadSceneAsActiveScene() {
            GlobalMainCameraRef.enabled = false;

            //切换相机
            Camera RoadSceneMainCamera = MainCameraInitHelperForLoader.FindMainCameraForScene(RoadScene);
            MainCameraInitHelperForLoader.SetUICameraForCamera(RoadSceneMainCamera, GlobalUICameraRef);

            SceneManager.SetActiveScene(RoadScene);
        }

        public void LoadLandscape()
        {

        }

        public void UnloadLandscape()
        {

        }

        public void LoadEnvironment()
        {

        }

        public void UnloadEnvironment()
        {

        }
    }
}
