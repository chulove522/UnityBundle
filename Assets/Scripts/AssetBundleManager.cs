using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Linq;
using System.Text;

public class AssetBundleManager : MonoBehaviour {

    public Text ThemeNameMsg;
    public Text BundleStateMsg;
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
    ///     //18.180.240.3
    /// </summary>
    string serverUrl = "18.180.240.3";
    string localStreamingPath;
    string storagePath;
    string GlobalConstAB_VERSION_FILE = "ABVersion.json";
    string GlobalConstAnnouncement = "活動公告.txt";
    string TestImageFile;
    string rootPath;
    string bundlePath;
    string annnouncementPath;

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

            foreach (var item in abList) {
                ShowOnScreen("Name:" + item.Name +"\n" , ThemeNameMsg);
                ShowOnScreen("Ver:" + item.Version + " Last Updated:" + item.Datetime + "\n" , BundleStateMsg);
            }

        }

    }


    public void Init()
    {

        ClearAllText();

        if (loadedBundle == null)
            loadedBundle = new Dictionary<string, AssetBundle>();

        storagePath = Application.streamingAssetsPath;

        string ABcheckPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles/");
        ABcheckPath += GlobalConstAB_VERSION_FILE;

        localStreamingPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles/Windows");

        TestImageFile = Application.dataPath + "/TestImageFile";

        rootPath = "/home/unity_bundle_sftp_user/";
        bundlePath = "/home/unity_bundle_sftp_user/bundles/";
        annnouncementPath = "/home/unity_bundle_sftp_user/announcement/";

        ShowOnScreen(localStreamingPath , StreamingPath);


        ShowOnScreen("ABcheckPath:" + ABcheckPath, LogMsg);



        if (System.IO.File.Exists(ABcheckPath))
        {
            ShowOnScreen("------found AB check json------", LogMsg);
            //本地Json必讀
            LoadJson(ABcheckPath);

            
            //測試 下載一張圖    
            DownloadWithsFTP(serverUrl, "unity_bundle_sftp_user");

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

   
    private void DownloadWithsFTP(string ftpHost, string ftpUsername) {
        if (!isLocal) {
            var privateKey = new PrivateKeyFile(@"C:\Users\chulo\文件\unitybundles\meta_dev_armand_unity_sftp.pem");

            using (var sftp = new SftpClient(ftpHost, ftpUsername, new[] { privateKey})) {
                sftp.Connect();

                


                Dir(sftp, bundlePath);
                ShowOnScreen("------Create folder------", LogMsg);

                var rndName = DateTime.Now.ToString("yyyyMMddHHmm");
                rndName = rndName.Substring(2, 10);

                sftp.CreateDirectory($"{rndName}");


                Dir(sftp, bundlePath);

                ShowOnScreen("------Upload file------", LogMsg);
                /*
                using (var ms = new MemoryStream()) {
                    var buff = Encoding.UTF8.GetBytes("Hello, World!");
                    ms.Write(buff, 0, buff.Length);
                    ms.Position = 0;
                    //sftp.UploadFile(ms, $"/{rndName}A/test.txt");
                    sftp.UploadFile(ms, annnouncementPath + $"test.txt");
                }*/
                string image01Path = TestImageFile + "/test01.jpg";
                using (var fileStream = new FileStream(image01Path, FileMode.Open)) {
                    sftp.UploadFile(fileStream, annnouncementPath + Path.GetFileName(image01Path));
                }


                Dir(sftp, bundlePath + $"{rndName}");

                using (var announcementDocu = new FileStream("D:\\announcementDocu.txt", FileMode.Create)) {
                    //sftp.DownloadFile( rootPath + $"{rndName}/test.txt", file);
                    sftp.DownloadFile( annnouncementPath + GlobalConstAnnouncement, announcementDocu);
                   
                }

                ShowOnScreen("------Downloaded content=" + System.IO.File.ReadAllText("D:\\announcementDocu.txt") + "------", LogMsg);

                /*
                WriteTitle("Move file");

                sftp.CreateDirectory($"/{rndName}B");
                sftp.RenameFile($"/{rndName}A/test.txt", $"/{rndName}B/test.txt");

                Dir($"/{rndName}A");
                Dir($"/{rndName}B");
                */
                /*
                WriteTitle("Delete file");

                sftp.DeleteFile($"/{rndName}B/test.txt");

                Dir($"/{rndName}B");

                sftp.DeleteDirectory($"/{rndName}A");
                sftp.DeleteDirectory($"/{rndName}B");
                */
            }

            Console.Read();

        }
    }

    private void Dir(SftpClient sftp, string path) {

        sftp.ChangeDirectory(path);

        Action<SftpFile> ShowDirOrFile = (item) => {
            if (item.IsDirectory)
                ShowOnScreen($"[{item.Name}]", LogMsg);
            //else
                 //ShowOnScreen($"{item.Name:-32} {item.Length,8:N0} bytes", LogMsg);
        };

        var list = sftp.ListDirectory(path)
                        //忽略 . 及 .. 目錄
                        .Where(o => !".,..".Split(',').Contains(o.Name))
                        .ToList();
        if (list.Any())
            list.ForEach(ShowDirOrFile);
        else {
            ShowOnScreen("No files.", LogMsg);
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
        byte[] abBytes = System.IO.File.ReadAllBytes(cubeAbPath);
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
        t.text += s;
        t.text += "\n";
    }

    void ClearAllText() {
        ThemeNameMsg.text = "";
        BundleStateMsg.text = "";
        LogMsg.text = "";
    }

    private void DownloadWithFTP(string ftpHost, string ftpUsername, string ftpPassword, string ftpFilePath, string savePath) {
        if (isLocal)
            return;
        else {
            /*
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(ftpHost));
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = true;
            // Set up the credentials for the FTP client
            NetworkCredential credentials = new NetworkCredential(ftpUsername, ftpPassword);

            // Set up the FTP request
            request.Credentials = credentials;

            request.Method = WebRequestMethods.Ftp.ListDirectory;
            if (request.GetResponse() != null) {
                print("response :" + request.GetResponse());
            }

            request.Method = WebRequestMethods.Ftp.DownloadFile;
            // Get the list of files in the root directory

            if (request.GetResponse()!= null) {

                print("response :" + request.GetResponse());

                downloadAndSave(request.GetResponse(), savePath);
            }*/
        }

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
