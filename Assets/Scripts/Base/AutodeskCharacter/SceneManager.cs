using System.IO;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public const string url2 = "tcp://localhost:61616";
    public const string url1 = "tcp://137.194.54.80:61615/";

    public const string FILEURL = "Config"; // file stores list of URLs
    public const float DELAY = 0.0F;
    public static bool isThrift = true;

    public static string BAPTopic = "BAP";
    public static string FAPTopic = "FAP";
    public static string AUDIOTopic = "";
    public static string ProducerTopic = "";


    public static float globalTime;
    public static float gretaClock = -1;
    public static long currentFrame = -1;
    public static float globalTimeUnity;
    public static float initUnityClock = -1;

    // change your target postion and zCamera here

    public static Vector3 iTarget = new Vector3(0.0f, 1.40f, 0f);
    public static float xCamera = 0.0f;
    public static float yCamera = 0.0f;
    public static float zCamera = -5.0f;

    public static string[] connectionState = {"Disconnected", "Connected"};
    public static bool isConnectedThrift;
    public int height = Screen.height;
    public int width = Screen.width;

    public bool showPanel;

    // Use this for initialization
    private void Awake()
    {
        showPanel = false;
        initThrift();
    }

    // Update is called once per frame
    private void Update()
    {
        globalTimeUnity = Time.realtimeSinceStartup;
        if (gretaClock > 0)
            currentFrame = (long) (gretaClock / 40);
        else
            globalTime = 0;

        if (Input.GetKey(KeyCode.Tab))
            showPanel = !showPanel;
    }

    private void OnGUI()
    {
        if (showPanel)
        {
            GUI.Label(new Rect(width - 200, 30, 200, 100), "Player Clock: " + timeToFormat(globalTimeUnity));
            GUI.Label(new Rect(width - 200, 50, 200, 100), "Greta Clock: " + timeToFormat(globalTime));
            GUI.Label(new Rect(width - 200, 70, 200, 100), "Frame Number: " + currentFrame);
        }
    }

    public void OnApplicationQuit()
    {
        closeThrift();
    }

    private string readURL(string fileName)
    {
        var asset = Resources.Load(fileName) as TextAsset;
        var text = asset.text;

        using (var reader = new StringReader(text))
        {
            var line = reader.ReadLine();
            while (line.Length == 0 || line[0] == '#')
                line = reader.ReadLine();
            return line;
        }
    }

    private void closeThrift()
    {
        isConnectedThrift = false;
        //...
    }

    private void initThrift()
    {
        //...
    }


    public static string timeToFormat(float time)
    {
        if (time < 0)
            time = 0;
        var timeSec = (long) time;
        var minute = (int) timeSec / 60;
        var sec = (int) timeSec % 60;
        var hour = minute / 60;
        minute = minute % 60;
        var timeMili = (int) ((time - timeSec) * 1000);
        return hour + "h:" + minute + "m:" + sec + "s:" + timeMili;
    }
}