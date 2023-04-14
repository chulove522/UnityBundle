using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;
using System.Linq;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEngine.Networking;


public class AssetBundleManager : MonoBehaviour {

    public Text ThemeName;
    public Text BundleState;
    public Text StreamingPath;
    public Text ErrorMsg;
    public Text LogMsg;
    public Image TestImage;

    public Transform loadhere;

    private static AssetBundleManager Instance;

    public enum BundleType {
        Others = 0,
        Font,           // .ttf
        Shader,         // .shader
        Material,       // .mat
        Fbx,            //fbx model
        Prefab,         // .prefab
        UI,            // picbot所使用的不同UI包
    }

    /// <summary>
    /// 連接所需資料  下載路徑
    /// </summary>
    /// //unity_bundle / 123123
    /// ftp unity_bundle@ubuntu@18.180.240.3
    public string serverUrl = "18.180.240.3";
    string localStreamingPath;
    public string storagePath;
    string GlobalConstAB_VERSION_FILE = "ABVersion.json";

    // ssh -i C:\Users\chulo\UnityProject\FunkAR.pem ubuntu@18.180.240.3


    public static bool isLocal = false;

    static bool isInit = false;

    public Dictionary<string, AssetBundle> loadedBundle;

    public delegate void LoadHandler(AssetBundle stageObject);

    public List<ABVersionData> _localVersionData;
    public List<ABVersionData> _serverVersionData;

    private int ftpConnectPort = 21;


    //確認兩側版本以及最後更新狀態
    [Serializable]
    public class ABVersionData
    {
        public string Name;
        public string Version;
        public int Datetime;  //304111220 = 2023年 四月11日12點20分
    }

    public void Awake()
    {
        Instance = this.GetComponent<AssetBundleManager>();
        DontDestroyOnLoad(gameObject);
        Init();
    }



    public void LoadJson(string ABcheckPath) {

        using (StreamReader r = new StreamReader(ABcheckPath)) {

            string ABjson = r.ReadToEnd();

            Debug.Log(ABjson);

            List<ABVersionData> abList = JsonConvert.DeserializeObject<List<ABVersionData>>(ABjson);

            /*foreach (var item in abList) {
                Debug.Log("Name:" + item.Name + " Ver:" + item.Version +" Last Updated:"+ item.Datetime);
            }*/

        }

    }


    public void Init()
    {
        if (loadedBundle == null)
            loadedBundle = new Dictionary<string, AssetBundle>();

        storagePath = Application.streamingAssetsPath;

        string ABcheckPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles/");
        ABcheckPath += GlobalConstAB_VERSION_FILE;

        localStreamingPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles/Windows");

        ShowOnScreen(localStreamingPath , StreamingPath);


        ShowOnScreen(ABcheckPath, LogMsg);



        if (File.Exists(ABcheckPath))
        {
            ShowOnScreen("found AB check json", LogMsg);
            //本地Json必讀
            LoadJson(ABcheckPath);

            //測試 下載一張圖
            DownloadWithFTP(serverUrl, "unity_bundle" , "123123" , "ftp/test.png", storagePath);            

           isInit = true;  
        }
        else
        {
            ShowOnScreen("漏了ABVersion File"+ ABcheckPath, ErrorMsg);
            isInit = false;
            Application.Quit();
        }
        //聯網
        if (Application.internetReachability == NetworkReachability.NotReachable || isLocal == true) {
            isLocal = true;
            Debug.Log("不聯網索取AB");
        }
        else {
            isLocal = false;
        }



    }
    //ftp://unity_bundle@18.180.240.3/home/unity_bundle/ftp/test.jpg
    //18.180.240.3
    private void DownloadWithFTP(string ftpHost, string ftpUsername, string ftpPassword, string ftpFilePath, string localFilePath)
    {
        if (isLocal)
            return;
        else {

            // Set up the credentials for the FTP client
            NetworkCredential credentials = new NetworkCredential(ftpUsername, ftpPassword);

            // Set up the FTP request
            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpHost}/home/unity_bundle/{ftpFilePath}");

            //request.Method = WebRequestMethods.Ftp.DownloadFile;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{ftpHost}:21/");
            request.Credentials = credentials;
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            // Get the list of files in the root directory

            print("response :" + request.GetResponse());


            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse()) {

                
                        using (Stream responseStream = response.GetResponseStream()) {
                            using (StreamReader reader = new StreamReader(responseStream)) {
                                string line;
                                while ((line = reader.ReadLine()) != null) {
                                    Console.WriteLine(line);
                                }
                            }
                        }
                    }

            /*
            try {
                // Connect to the FTP server and download the file
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream ftpStream = response.GetResponseStream())
                using (Stream fileStream = new FileStream(localFilePath, FileMode.Create)) {
                    ftpStream.CopyTo(fileStream);
                }
            }
            catch (Exception e) {
                print(e.InnerException);
                throw;
            }*/

        } 
    }
    
    

    /*
    bool VersionCompare(string bundleName)
    {
        bundleName = bundleName.ToLower();
        if (_localVersionData== null || _localVersionData.Count == 0)
        { return false; }
        if (_serverVersionData == null || _serverVersionData.Count == 0)
        { return false; }

        var cacheLocalBundle = _localVersionData.Where(s => s.Name == bundleName);
        var cacheServerBundle = _serverVersionData.Where(s => s.Name == bundleName);

        if(cacheServerBundle == null)
        {
            Debug.LogError("Get Bundle Data Is None From Server. ,Bundle Name : " + bundleName);
            return false;
        }

        if (cacheLocalBundle == null)
        {
            Debug.LogError("Get Bundle Data Is None From Local. ,Bundle Name : " + bundleName);
            return false;
        }

        if (cacheLocalBundle.Count() == 0 || cacheServerBundle.Count() == 0)
        {
            return false;
        }

        return true;
    }*/

    /// <summary>
    /// 从内存中异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadFromMemory() {
        string cubeAbPath = "AssetBundles/wall/cubewall.unity";
        //读取字节流
        byte[] abBytes = File.ReadAllBytes(cubeAbPath);
        AssetBundleCreateRequest abRequest = AssetBundle.LoadFromMemoryAsync(abBytes);
        yield return abRequest;
        AssetBundle ab = abRequest.assetBundle;
        GameObject obj = ab.LoadAsset<GameObject>("cubewall");

        Instantiate(obj);


    }
    /// <summary>
    /// 从本地异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncCreateFromLocal(string bundlename) {
        bundlename = bundlename.Replace(" ","").ToLower();
        string cubeAbPath = "AssetBundles/StandaloneWindows/" + bundlename;
        AssetBundleCreateRequest abRequest = AssetBundle.LoadFromFileAsync(cubeAbPath);
        yield return abRequest;
        AssetBundle ab = abRequest.assetBundle;
        GameObject obj = ab.LoadAsset<GameObject>("cubewall");

        Instantiate(obj);
    }
    //https://blog.csdn.net/yunjianxi0000/article/details/96283609
    /// <summary>
    /// 通过 UnityWebRequest 加载
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadFromWebRequest(string bundleName) {
        bundleName = "2020_newyear";
        string url = "" + bundleName;
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return request.SendWebRequest();

        
        AssetBundle ab = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
        GameObject obj = ab.LoadAsset<GameObject>("2020_newyear");

        Instantiate(obj);
        obj.transform.SetParent(loadhere);
        obj.transform.localPosition = Vector3.zero;
    }

    /*
    public IEnumerator Load (string bundleName)
    {
        bundleName = bundleName.Replace(" ", "");//去除空白字元

        if (loadedBundle == null)
            loadedBundle = new Dictionary<string, AssetBundle>();

        AssetBundle ab;

        if (loadedBundle.ContainsKey(bundleName) && loadedBundle[bundleName] != null)
        {
            ab = loadedBundle[bundleName];
            yield break;
        }

        byte[] bundleData = new byte[0];
        var filePath = Path.Combine(storagePath, bundleName);
        
        //聯網
        //if (Application.internetReachability == NetworkReachability.NotReachable) {
            isLocal = true;
        //    Debug.Log("Network Not Reachable");
        //}
        
        if (!isLocal)
        {
            yield return StartCoroutine(GetServerBundleVersion());
        }


        //沒有檔案的狀況
        if (!File.Exists(filePath))
        {
            //從伺服器上下載
            if (!isLocal)
            {
                yield return StartCoroutine(LoadFromWebRequest(bundleName));
                //更新目前bundle最後下載的版本
                //UpdateLocalData(bundleName);
                bundleData = File.ReadAllBytes(filePath);
               
            }
            else
            {
                //從streaming路徑取得
                var localFilePath = Path.Combine(localStreamingPath, bundleName);
                if (File.Exists(localFilePath))
                {
                    // Debug.Log("Get Bundle From: " + localFilePath);
                    bundleData = File.ReadAllBytes(localFilePath);
                }
                else
                {
                    Debug.LogError("Local File is not Exists." + localFilePath);
                    yield break;
                }
            }
        }
        else
        {
            if (!isLocal)
            {
                bool versionCheck = VersionCompare(bundleName);
                //檢查版本,開始下載
                if (!versionCheck)
                {
                    yield return StartCoroutine(DownloadBundleFromServer(bundleName));
                    //SetLocalData(bundleName);
                        //UpdateLocalData(bundleName);
                        bundleData = File.ReadAllBytes(filePath);
                }

                bundleData = File.ReadAllBytes(filePath);
            }
            else
            {
                var localFilePath = Path.Combine(localStreamingPath, bundleName);
                if (File.Exists(localFilePath))
                {
                    bundleData = File.ReadAllBytes(localFilePath);
                }
                else
                {
                    Debug.LogError("Local File is not Exists." + localFilePath);
                    yield break;
                }
            }
            
        }

        AssetBundleCreateRequest createRequest;
        if (bundleData != null && bundleData.Length != 0)
        {
            createRequest = AssetBundle.LoadFromMemoryAsync(bundleData);
        }
        else
        {
            yield break;
        }
        
        yield return createRequest;

        if(createRequest.assetBundle != null)
        {
            ab = createRequest.assetBundle;
            loadedBundle[bundleName] = ab;
        }
        else
        {
            Debug.LogError("Bundle Load Fail : "+bundleName);
        }
    }*/
    /*
    public IEnumerator DownloadBundleFromServer(string bundleName)
    {
        var downloadPath = serverPath + bundleName;
        Debug.LogWarning("Start Download : " + downloadPath);
        WWW www;
        www = new WWW(downloadPath);

        yield return www;

        if (!string.IsNullOrEmpty(www.error) || www.bytes.Length == 0)
        {
            Debug.LogError("Connection Fail. " + www.error);
            isLocal = true;
            yield break;
        }
        var downloadSavePath = Path.Combine(storagePath, bundleName);
        File.WriteAllBytes(downloadSavePath, www.bytes);
        
    }
    */

    void ShowOnScreen(string s , Text t) {
        t.text = s;
    }


    /*
    //複寫資訊
    public void WriteVersionInfo(string path,string content)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning(string.Format("Not Found File : {0} , Creat A New Version.", path));
            StreamWriter _crcFile = File.CreateText(path);
            _crcFile.Write(content);
            _crcFile.Close();
        }
        else
        {
            File.WriteAllText(path, content);
        }
    }*/

}
