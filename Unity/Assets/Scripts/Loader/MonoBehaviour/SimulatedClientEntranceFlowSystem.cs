using ET;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

//请按照页面运行流程顺序从小到大(即从上到下)注册，从Init分界线开始需要在PathsDictRegistrationTool当中写方法注册。
public enum KeyUIPage 
{
    /*-------------------Init之前的页面-------------------*/
    start,
    SimulatedBrandInterpretationVideoPage,
    SimulatedBrandInterpretationImageAndTextPage,
    LoadingPage,

    /*-------------------Init之后的页面-------------------*/
    LoginPage,
    GameEntrancePage,
    HallPage,
    ChapterAndSectionPage,
    CompetitionPage,
    ChapterAndSectionSettlementPage,
    CompetitionSettlementPage,
}
#if UNITY_EDITOR
public static class PathsDictRegistrationTool
{
    public static void RegisterAllPagePaths()
    {
        string LoginPagePath = "/"+KeyUIPage.LoginPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.LoginPage, LoginPagePath);
        string GameEntrancePath = LoginPagePath+"/"+KeyUIPage.GameEntrancePage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.GameEntrancePage, GameEntrancePath);
        string HallPagePath = GameEntrancePath + "/"+KeyUIPage.HallPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.HallPage, HallPagePath);
        string ChapterAndSectionPagePath = HallPagePath+"/"+KeyUIPage.ChapterAndSectionPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.ChapterAndSectionPage, ChapterAndSectionPagePath);
        string CompetitionPagePath = HallPagePath+"/"+KeyUIPage.CompetitionPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.CompetitionPage, CompetitionPagePath);
        string ChapterAndSectionSettlementPagePath = ChapterAndSectionPagePath+"/"+KeyUIPage.ChapterAndSectionSettlementPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.ChapterAndSectionSettlementPage, ChapterAndSectionSettlementPagePath);
        string CompetitionSettlementPagePath = CompetitionPagePath+"/"+KeyUIPage.CompetitionSettlementPage;
        SimulatedClientEntranceFlowSystem.PathsDict.Add(KeyUIPage.CompetitionSettlementPage, CompetitionSettlementPagePath);
    }
    public static void UnregisterLoginPage()
    {SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.LoginPage);}
    public static void UnregisterHallPage()
    {SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.HallPage);}
    public static void UnregisterChapterAndSectionPage()
    {SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.ChapterAndSectionPage); }
    public static void UnregisterCompetitionPage()
    { SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.CompetitionPage);}
    public static void UnregisterChapterAndSectionSettlementPage()
    {SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.ChapterAndSectionSettlementPage);}
    public static void UnregisterCompetitionSettlementPage()
    {SimulatedClientEntranceFlowSystem.PathsDict.Remove(KeyUIPage.CompetitionSettlementPage);}
}

public static class EditorSkipPageTool
{
    //调用这个方法判断是否要跳过当前页面
    public static bool ShallSkipThisPage(KeyUIPage CurrentPage)
    {
        if (SimulatedClientEntranceFlowSystem.StaticStartPoint != KeyUIPage.start)
        {
            if ((int)SimulatedClientEntranceFlowSystem.StaticStartPoint > (int)CurrentPage)
                return true;
            return false;
        }
        return false;
    }
    public static bool ShallSkipThisPage(string NextPagePath)
    {
        if (SimulatedClientEntranceFlowSystem.StaticStartPoint != KeyUIPage.start && SimulatedClientEntranceFlowSystem.PathsDict.ContainsKey(SimulatedClientEntranceFlowSystem.StaticStartPoint))
        {
            string StartPointPath = SimulatedClientEntranceFlowSystem.PathsDict[SimulatedClientEntranceFlowSystem.StaticStartPoint];
            if (StartPointPath.Contains(NextPagePath))
            {
                SimulatedClientEntranceFlowSystem.CurrentPagePath = NextPagePath; //更新CurrentPagePath到下一个Page
                return true;
            }
            return false;
        }
        return false;
    }

}
#endif

/*这个组件处理启动前的品牌演绎和资源加载读条；对于编辑器开发模式，可以控制从哪一页UI开始*/
/*通常的进入流程：音视频播放完毕 => 显示图片内容 => 显示加载读条界面 => ET框架初始化开启 => 显示登录界面*/
/*因此本系统遵循先视频后图文的逻辑，使用的时候需要注意什么时候把ET框架开启*/
public class SimulatedClientEntranceFlowSystem : MonoBehaviour
{
    [Header("BasicConfig")]
    public Init InitRef;
#if UNITY_EDITOR
    public KeyUIPage AutoSkipTo;//设置直接从某个UI页面进入
    public static Dictionary<KeyUIPage, string> PathsDict = new();
#endif
    public static KeyUIPage StaticStartPoint = KeyUIPage.start;
    public static string CurrentPagePath = "";
    public bool CanAppRunInBackground = true;//设置是否可后台运行(如果是true,窗口未激活也不会中断声音和程序运行)
    public bool HideCursor = true;
    public RectTransform EntranceLayerOverlayOn; //置顶的层级
    public RectTransform EntranceLayerBottomingOn; //置底的层级

    [Header("EntryFlowReferences")]
    public GameObject PrefabOfBrandInterpretation;

    private RectTransform BrandInterpretationObj;
    private ReferenceCollector BrandInterpretationRC;
    private VideoPlayer EntryVideoPlayer;
    private int VideoEarlyEndFrame = 0;
    private GameObject EntranceImageAndTextDisplay;
    private GameObject BasicStatement;
    private GameObject WarningPage;
    private GameObject AgreementsPage;

    [Header("EntryFlowControl")]
    public bool Has_the_initial_user_agreement_been_signed = false; //初始用户协议是否已经签订
    public bool Have_the_necessary_user_permissions_been_enabled = false; //必要用户权限是否已经开启
    public static bool Automatic_Login_Successful = false; //自动登录是否成功

    private  event Action OnAllEntryVideosEnd;
    private event Action OnAllImageAndTextDisplayFinished;

    [SerializeField]
    private UnityEvent OnStartedPlaying = new UnityEvent();
    private void OnStarted_DisplayCurrentVideo(VideoPlayer source)
    {
        DisplayCurrentVideo();
        OnStartedPlaying.Invoke();
    }


    private void Awake()
    {
        if (InitRef.usingSimulatedClientEntranceFlowSystem) {

#if UNITY_EDITOR
            StaticStartPoint = AutoSkipTo;
            PathsDictRegistrationTool.RegisterAllPagePaths();
#endif
            //上面的代码意味着,如果不是编辑器模式,那么StaticStartPoint始终会是KeyUIPage.start的初始值——即进入程序最开头
            Application.runInBackground = CanAppRunInBackground;
            InitRef.OnInitStart += InstantiateBrandInterpretationObject;
            OnAllImageAndTextDisplayFinished += () => Destroy(BrandInterpretationObj.gameObject); //销毁品牌演绎对象
            OnAllImageAndTextDisplayFinished += () => Destroy(this); //销毁自己
        }
    }

    private void Start()
    {
        this.StartCoroutine(DelayStart());//之所以要延迟：进入流程必须在Init的Start之后才能进行
    }
    
    IEnumerator DelayStart()
    {
        yield return null;
        if (InitRef.usingSimulatedClientEntranceFlowSystem)
        {
            BrandInterpretationRC = BrandInterpretationObj.GetComponent<ReferenceCollector>();
            EntryVideoPlayer = BrandInterpretationRC.Get<GameObject>("VideoPlayer").GetComponent<VideoPlayer>();
            HideCurrentVideo();
            EntryVideoPlayer.started += OnStarted_DisplayCurrentVideo;
            EntranceImageAndTextDisplay = BrandInterpretationRC.Get<GameObject>("EntranceImageAndText");
            BasicStatement = EntranceImageAndTextDisplay.transform.Find("BasicStatement").gameObject;
            WarningPage = EntranceImageAndTextDisplay.transform.Find("WarningPage").gameObject;
            AgreementsPage = EntranceImageAndTextDisplay.transform.Find("AgreementsPage").gameObject;
            EntranceImageAndTextDisplay.SetActive(false);
        }
    }

    private void Update()
    {
        //如果视频播放完毕
        if (EntryVideoPlayer != null && EntryVideoPlayer.frame == (long)EntryVideoPlayer.frameCount - 1 - VideoEarlyEndFrame)
        {
            ProcessEntryVideosAllEnd(EntryVideoPlayer);
        }
    }


    //将品牌演绎的预制体实例化
    private void InstantiateBrandInterpretationObject() {
        BrandInterpretationObj = Instantiate(PrefabOfBrandInterpretation, EntranceLayerOverlayOn).GetComponent<RectTransform>();

        VideoPlayer player= BrandInterpretationObj.Find("VideoPlayer").GetComponent<VideoPlayer>();

        KeyUIPage CurrentPage = KeyUIPage.SimulatedBrandInterpretationVideoPage;

#if UNITY_EDITOR
        if (EditorSkipPageTool.ShallSkipThisPage(CurrentPage))
            player.frame = (long)player.frameCount - 1 - VideoEarlyEndFrame;
#endif
    }

    //隐藏或显示视频
    Color transparent = new Color(1,1,1,0);
    private void HideCurrentVideo() {
        BrandInterpretationObj.Find("VideoPlayer").GetComponent<RawImage>().color = transparent;
    }
    private void DisplayCurrentVideo()
    {
        BrandInterpretationObj.Find("VideoPlayer").GetComponent<RawImage>().color = Color.white;
    }

    //开启ET框架的初始化流程
    private void ET_Start()
    {
        InitRef.StartAsync().Coroutine();
    }

    //当视频播放阶段完全结束后
    private void ProcessEntryVideosAllEnd(VideoPlayer EntryVideoPlayer) {
        EntryVideoPlayer.Stop();
        EntryVideoPlayer.gameObject.SetActive(false);
        EntryVideoPlayer = null;

        if (OnAllEntryVideosEnd != null)
            OnAllEntryVideosEnd();

        StartCoroutine(ProcessEntranceImageAndTextDisplay(1f , 1f, 2.95f));
    }

    /// <summary>
    /// 处理图文显示流程
    /// </summary>
    /// <param name="IntervalTime">间隔时间</param>
    /// <param name="ET_Start_Time">ET框架初始化启动的时间</param>
    /// <param name="DurationOfTheLastOne">最后一个元素的持续时间</param>
    /// <returns></returns>
    private IEnumerator ProcessEntranceImageAndTextDisplay(float IntervalTime, float ET_Start_Time, float DurationOfTheLastOne) {
        EntranceImageAndTextDisplay.SetActive(true);

        BasicStatement.SetActive(false);
        WarningPage.SetActive(false);
        AgreementsPage.SetActive(false);

        KeyUIPage CurrentPage = KeyUIPage.SimulatedBrandInterpretationImageAndTextPage;
        bool ShallSkip = false;
#if UNITY_EDITOR
        ShallSkip = EditorSkipPageTool.ShallSkipThisPage(CurrentPage);
#endif
        if (ShallSkip)
        {
            //是否跳过Loading(Loading由Init控制，不能避免加载，但是可以隐藏显示)
            this.InitRef.displayLoadingPage = !EditorSkipPageTool.ShallSkipThisPage(KeyUIPage.LoadingPage);
            ET_Start(); //直接开启ET框架的初始化流程
        }
        else
        {
            //如果没有签订
            if (!Has_the_initial_user_agreement_been_signed && !Have_the_necessary_user_permissions_been_enabled)
            {
                AgreementsPage.SetActive(true);
                Button acceptButton = AgreementsPage.transform.Find("Accept").GetComponent<Button>();
                Button refuseButton = AgreementsPage.transform.Find("Refuse").GetComponent<Button>();
                acceptButton.onClick.AddListener(SignAgreements);
                refuseButton.onClick.AddListener(RefuseToSignAgreements);
                do
                {
                    yield return null; //一直等待直到签订
                } while (!Has_the_initial_user_agreement_been_signed && !Have_the_necessary_user_permissions_been_enabled);
                AgreementsPage.SetActive(false);
            }

            WarningPage.SetActive(true);
            yield return new WaitForSeconds(IntervalTime);
            BasicStatement.SetActive(true);
            yield return new WaitForSeconds(ET_Start_Time);
            this.InitRef.displayLoadingPage = !EditorSkipPageTool.ShallSkipThisPage(KeyUIPage.LoadingPage);
            ET_Start(); //开启ET框架的初始化流程
            yield return new WaitForSeconds(DurationOfTheLastOne);


            BasicStatement.SetActive(false);
            WarningPage.SetActive(false);
        }

        //图文呈现结束
        EntranceImageAndTextDisplay.SetActive(false);
        if (OnAllImageAndTextDisplayFinished!= null)
            OnAllImageAndTextDisplayFinished();

        Cursor.visible = HideCursor;
    }

    //签订一揽子协议
    private void SignAgreements() {

        Has_the_initial_user_agreement_been_signed = true;
        Have_the_necessary_user_permissions_been_enabled = true;
    }

    //拒绝签订(会直接退出游戏)
    private void RefuseToSignAgreements()
    {
        Has_the_initial_user_agreement_been_signed = false;
        Have_the_necessary_user_permissions_been_enabled = false;
        Init.QuitApplication();
    }
}



