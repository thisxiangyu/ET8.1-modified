using ET.Client;
using UnityEngine;
//using UnityEngine.Rendering.Universal;

namespace ET.Client
{
    public static class MainCameraInitHelperForLoader
    {
        /// <summary>
        /// 寻找设置场景主相机(这个函数规定场景的主相机命名必须为MainCamera或者Main Camera且必须放在场景的第一个游戏对象的子级)；
        /// </summary>
        /// <param name="UnityScene"></param>
        /// <returns></returns>
        public static Camera FindMainCameraForScene(UnityEngine.SceneManagement.Scene UnityScene)
        {
            GameObject[] objects = UnityScene.GetRootGameObjects();
            Transform mainCameraTransform = objects[0].transform.Find("MainCamera");
            if (mainCameraTransform == null)
            {
                mainCameraTransform = objects[0].transform.Find("Main Camera");
                if (mainCameraTransform == null)
                {
                    Debug.LogError("在场景中的根物体上没有找到名为MainCamera或Main Camera的对象，无法初始化场景" + UnityScene.name + "的主相机");
                    return null;
                }
            }
            Camera mainCamera = mainCameraTransform.GetComponent<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("未找到场景中的主相机物体的Camera组件，请检查场景" + UnityScene.name);
                return null;
            }
            return mainCamera;
        }

        /// <summary>
        /// URP在这里这个来设置UICamera叠加在MainCamera之上
        /// </summary>
        /// <param name="MainCamera">主相机</param>
        /// <param name="UICamera">UI相机</param>
        public static void SetUICameraForCamera(Camera MainCamera, Camera UICamera)
        {
            //var data = MainCamera.GetUniversalAdditionalCameraData(); //URP可以通过这个方法来访问和修改相机的后处理效果、自定义渲染器特性、剪辑平面设置、屏幕空间反射等
            //if (!data.cameraStack.Contains(UICamera))
            //    data.cameraStack.Add(UICamera);
        }
    }
}
