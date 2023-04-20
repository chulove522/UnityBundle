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

    public Text BundleStateMsg;   //show version and date
    public Text BundleExistsMsg;  //show loaded
    public Text ABVersionPath;
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
    private string serverUrl = "18.180.240.3";
    private string AbPath = "AssetBundles/StandaloneWindows/";
    private string storagePath = "Assets/Download/";
    private string uploadPath = "Assets/Upload/";
    private string GlobalConstAB_VERSION_FILE = "ABVersion.json";
    private string GlobalConstAnnouncement = "活動公告.txt";

    /// <summary>
    ///    寫在init中
    /// 
    ///    serverRootPath = "/home/unity_bundle_sftp_user/";
    ///    ServerBundlePath = "/home/unity_bundle_sftp_user/bundles/";
    ///    annnouncementPath = "/home/unity_bundle_sftp_user/announcement/";
    ///    
    /// 
    /// </summary>
    string serverRootPath;
    string ServerBundlePath;
    string annnouncementPath;
    public List<ABVersionData> LocalVersionData;
    public List<ABVersionData> ServerVersionData;

    // ssh -i C:\Users\chulo\UnityProject\FunkAR.pem ubuntu@18.180.240.3


    public static bool isLocal = false;

    static bool isInit = false;

    public delegate void LoadHandler(AssetBundle stageObject);


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
        LocalVersionData = new List<ABVersionData> ();
        if (!isInit)
            Init();

    }


    //show 自己的ab list
    public void LoadJson(string ABcheckPath) {

        using (StreamReader r = new StreamReader(ABcheckPath)) {

            string ABjson = r.ReadToEnd();

            Debug.Log(ABjson);

            LocalVersionData = JsonConvert.DeserializeObject<List<ABVersionData>>(ABjson);

            foreach (var item in LocalVersionData) {
                ShowOnScreen("Name:" + item.Name + "\n" + "Ver:" + item.Version + " Last Updated:" + item.Datetime + "\n--------\n", BundleStateMsg);
            }

        }

    }


    public void Init()
    {

        ClearAllText();
        string ABcheckPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles/");
        ABcheckPath += GlobalConstAB_VERSION_FILE;

        serverRootPath = "/home/unity_bundle_sftp_user/";
        ServerBundlePath = "/home/unity_bundle_sftp_user/bundles/";
        annnouncementPath = "/home/unity_bundle_sftp_user/announcement/";

        ShowOnScreen(ABcheckPath, ABVersionPath);


        if (System.IO.File.Exists(ABcheckPath))
        {
            ShowOnScreen("------found AB check json------\n", LogMsg);
            //本地Json必讀
            LoadJson(ABcheckPath);


            //測試 下載一張圖    
            DownloadWithSFTP(serverUrl, "unity_bundle_sftp_user");


            //測試 載入bundle使用
            StartCoroutine( AsyncCreateFromLocal("2020_cosmo","Cube01",3));
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

    private int dateToInt() {
        string date = DateTime.Now.ToString("yyyyMMddHHmm");
        date = date.Substring(3, 10);
        int dateToNumber = 0;
        for (int i = 0; i < 9; i++) {
            dateToNumber += date[i] * (int)(Math.Pow(10, i));
        }
        print("dateToNumber" + dateToNumber);
        return dateToNumber;
    }

    private string dateToString() {
        string date = DateTime.Now.ToString("yyyyMMddHHmm");
        date = date.Substring(3, 10);
        return date;
    }

   
    private void DownloadWithSFTP(string ftpHost, string ftpUsername) {
        if (!isLocal) {
            var privateKey = new PrivateKeyFile(@"C:\Users\chulo\文件\unitybundles\meta_dev_armand_unity_sftp.pem");

            using (var sftp = new SftpClient(ftpHost, ftpUsername, new[] { privateKey})) {
                sftp.Connect();

                


                Dir(sftp, ServerBundlePath);
                ShowOnScreen("------Create folder------", LogMsg);

                var rndName = dateToString();


                sftp.CreateDirectory($"{rndName}");


                Dir(sftp, ServerBundlePath);

                ShowOnScreen("------Upload file------", LogMsg);
                /*
                    using (var ms = new MemoryStream()) {
                        var buff = Encoding.UTF8.GetBytes("Hello, World!");
                        ms.Write(buff, 0, buff.Length);
                        ms.Position = 0;
                        //sftp.UploadFile(ms, $"/{rndName}A/test.txt");
                        sftp.UploadFile(ms, annnouncementPath + $"test.txt");
                    }*/

                string image01Path = uploadPath + "test01.jpg";
                using (var fileStream = new FileStream(image01Path, FileMode.Open)) {
                    sftp.UploadFile(fileStream, annnouncementPath + Path.GetFileName(image01Path));
                }


                Dir(sftp, ServerBundlePath + $"{rndName}");

                using (var announcementDocu = new FileStream("D:\\announcementDocu.txt", FileMode.Create)) {
                    //sftp.DownloadFile( rootPath + $"{rndName}/test.txt", file);
                    sftp.DownloadFile( annnouncementPath + GlobalConstAnnouncement, announcementDocu);
                   
                }

                ShowOnScreen("------Downloaded content------\n" + System.IO.File.ReadAllText("D:\\announcementDocu.txt") + "\n------------\n", LogMsg);

                
                ShowOnScreen("------Move file------\n" + System.IO.File.ReadAllText("D:\\announcementDocu.txt") + "\n------------\n", LogMsg);

                sftp.CreateDirectory($"/{rndName}_Move");
                sftp.RenameFile($"/{rndName}/test.txt", $"/{rndName}_Move/test.txt");

                Dir(sftp , $"/{rndName}");
                Dir(sftp , $"/{rndName}_Move");
                
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
    IEnumerator AsyncCreateFromLocal(string bundlename , string objectname, int count) {
        //changeABPath
        string bundlePath = AbPath + bundlename;
        bundlename = bundlename.Replace(" ","").ToLower();
        
        string manifestpath = "AssetBundles/StandaloneWindows/StandaloneWindows"; //請注意有連續兩個Standalone Windows
        
        AssetBundleCreateRequest abRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return abRequest;
        AssetBundle ab = abRequest.assetBundle;



        //加载的Manifest文件是主的Manifest文件而不是每个AB包的Manifest文件，因为从主Manifest可以访问到所有资源的依赖资源   
        AssetBundle ABmanifest = AssetBundle.LoadFromFile(manifestpath);
        //获取AssetBundle文件的AssetBundleManifest的信息
        AssetBundleManifest manifest = ABmanifest.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        string[] dependencies = manifest.GetAllDependencies(bundlename);


        if (manifest == null) {
            ShowOnScreen("manifest null!" , ErrorMsg);
        }
        else {
            //載入依賴bundles
            foreach (string dependency in dependencies) {
                string dependPath = "AssetBundles/StandaloneWindows/" + dependency;
                AssetBundle.LoadFromFile(dependPath);
                print("載入依賴bundles:" + dependency + " from:" + dependPath);
                print("Path.Combine(AbPath, dependency) = " + Path.Combine(AbPath, dependency));
            }
            //拿自己的ABjson 與本地的bundle名稱比對 確認沒少
            List<string> abListFromJson =  new List<string>();
            foreach (var item in LocalVersionData) {
                abListFromJson.Add(item.Name.Replace(" ", "").ToLower());
            }
            List<string> allABIhave = new List<string>();
            allABIhave = manifest.GetAllAssetBundles().ToList();

            foreach (var ab_item in allABIhave) {  //實際上有的
                if (abListFromJson.Contains(ab_item)) {
                    ShowOnScreen("本地有bundle: " + ab_item + "\n--------\n", BundleExistsMsg);
                }
                else {
                    ShowOnScreen("請在ABVersion中加入此項目: " + ab_item +"\n--------\n", BundleExistsMsg);
                }

            }

            foreach (string json_item in abListFromJson) {
                if (!allABIhave.Contains(json_item))
                    ShowOnScreen("本地端bundle遺失:" + json_item + "\n--------\n", BundleExistsMsg);
            }

        }

        ////----------------

        GameObject[] objS = ab.LoadAllAssets<GameObject>();

        //生成的次數
       foreach (GameObject obj in objS) {
            Instantiate(obj);
            obj.transform.SetParent(loadhere);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            print("生成:" + obj.name);
        }


    /*
    //一個一個!!
    try {
        GameObject obj = ab.LoadAsset<GameObject>(objectname);
        //生成的次數
        for (int i = 0; i < count; i++) {
            Instantiate(obj);
            obj.transform.SetParent(loadhere);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;       }

        }
    catch (Exception e ) {
        ShowOnScreen("the objectname you want from this bundle is probably wrong" , ErrorMsg);
        ShowOnScreen(e.Message, ErrorMsg);
        throw;
    }
    */


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
    

    void ShowOnScreen(string s , Text t) {
        t.text += s;
        t.text += "\n";
    }

    void ClearAllText() {
        BundleStateMsg.text = "";
        LogMsg.text = "";
        BundleExistsMsg.text = "";
    }

    #region FTPDownload
    /*
    private void DownloadWithFTP(string ftpHost, string ftpUsername, string ftpPassword, string ftpFilePath, string savePath) {
        if (isLocal)
            return;
        else {
            
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
            }
        }

    }
    */
    #endregion
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

}
