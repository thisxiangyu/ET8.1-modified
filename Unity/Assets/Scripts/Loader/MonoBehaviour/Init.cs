using System;
using CommandLine;
using UnityEngine;

namespace ET
{
	/*这个脚本作为全局启动脚本，挂在全局对象(通常叫Global)上*/
	public class Init: MonoBehaviour//继承了MonoBehaviour类，由Unity管理生命周期
    {
        public GameObject PrefabOfInitializePage;
        [HideInInspector] public GameObject InitializePage;
        public bool usingSimulatedClientEntranceFlowSystem = true; //是否要使用模拟的客户端运行流程
        [HideInInspector] public bool isOnStartAsync = false;
        [HideInInspector] public bool isStartAsyncFinished = false;
        private bool goUpdate = false;

        public event Action OnInitStart;
        public event Action OnStartAsyncBegin;
        public event Action OnStartAsyncFinish;


        //单例
        private static Init instance;
        public static Init Instance
        {
            get
            {
                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
		{
            //生成一个DontDestroyOnLoad场景，确保在切换场景的时候本全局对象不被销毁
            DontDestroyOnLoad(this.gameObject);

            OnInitStart?.Invoke();

            if (!usingSimulatedClientEntranceFlowSystem)
            {
                StartAsync().Coroutine();
                goUpdate = true;
            }
		}

        private void Update()
        {
            if (goUpdate)
            {
                TimeInfo.Instance.Update();
                FiberManager.Instance.Update();
            }
        }

        private void LateUpdate()
        {
            if (goUpdate)
                FiberManager.Instance.LateUpdate();
        }

        private void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }

        //启动函数
        public async ETTask StartAsync()
		{
            //启动加载页面(进度条)
            InitializePage = LoadLoadingPage();
            if (InitializePage == null)
            {
                Debug.LogError("缺少InitializePage，请检查。");
                QuitApplication();
                return;
            }

            //流程开始
            OnStartAsyncBegin?.Invoke();
            isOnStartAsync = true;

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				Log.Error(e.ExceptionObject.ToString());
			};

			// 命令行参数
			string[] args = "".Split(" ");
			Parser.Default.ParseArguments<Options>(args)
				.WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
				.WithParsed((o)=>World.Instance.AddSingleton(o));

			Options.Instance.StartConfig = $"StartConfig/Localhost";	//启动配置
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger(); //在World中添加一个Logger单例
            ETTask.ExceptionHandler += Log.Error; 
            World.Instance.AddSingleton<TimeInfo>();//在World中添加一个TimeInfo单例
            World.Instance.AddSingleton<FiberManager>();//在World中添加一个FiberManager单例

            goUpdate = true;

            //在World中添加一个ResourcesComponent单例(用来启动YooAssets)
            await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync("DefaultPackage", true);
            //在World中添加一个CodeLoader单例
            CodeLoader codeLoader = World.Instance.AddSingleton<CodeLoader>();
            //加载动态链接库(Model.dll和mscorlib.dll)
            await codeLoader.DownloadAsync();
            //在Start()当中会将Model动态链接库读取成二进制数据，然后加载热更代码，最后运行StaticMethod
            codeLoader.Start();

            OnStartAsyncFinish?.Invoke();
            isStartAsyncFinished = true;
        }

        //加载LoadingPage到Overlay上
        [HideInInspector]public bool displayLoadingPage=true;
        private GameObject LoadLoadingPage()
        {
            if(PrefabOfInitializePage == null)
                return null;

            Transform UI = transform.Find("UI");
            if (UI == null)
                return null;

            Transform Overlay = UI.Find("Overlay");
            if (Overlay == null)
                return null;

            GameObject LoadingPage = Instantiate(PrefabOfInitializePage, Overlay);
            LoadingPage.SetActive(displayLoadingPage);

            return LoadingPage;
        }

        //推动进度条
        public void DriveInitializeProgressBar(float Progress = 0f)
        {

        }

        //初始化完成
        public void Completed()
        {
            InitializePage.SetActive(false);
            Destroy(InitializePage);
            PrefabOfInitializePage = null;
            OnInitStart = null;
            OnStartAsyncBegin = null;
            OnStartAsyncFinish = null;
        }

        //退出程序
        public static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
}