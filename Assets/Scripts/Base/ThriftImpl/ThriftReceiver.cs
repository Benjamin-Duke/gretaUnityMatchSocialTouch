using animationparameters;
using thrift;
using thrift.gen_csharp;
using UnityEngine;

public class ThriftReceiver : MonoBehaviour
{
    private InstantiationAPReceiver apReceiver;

    // Use this for initialization
    private void Start()
    {
        Debug.Log("Start");
        apReceiver = new InstantiationAPReceiver(69, 9090);
        apReceiver.startConnection();
        Debug.Log("Receiver started");
    }

    // Update is called once per frame
    private void Update()
    {
        if (apReceiver.timer.isSynchronized())
        {
            var currentAPFrame = apReceiver.getCurrentFrame(apReceiver.timer.getTimeMillis() / 40);

            if (currentAPFrame != null)
                Debug.Log(
                    apReceiver.timer.getTimeMillis() / 40 + " " + currentAPFrame.AnimationParametersFrame2String());
        }
    }

    private void OnApplicationQuit()
    {
        apReceiver.stopConnector();
    }
}

public class InstantiationAPReceiver : APReceiver
{
    private readonly APFramesList apFramesList;
    private bool firstMessageReceived;

    public InstantiationAPReceiver(int numOfAP, int port) : base(numOfAP, port)
    {
        apFramesList = new APFramesList(new AnimationParametersFrame(69, 0));
    }

    public override void perform(Message m)
    {
        apFramesList.addAPFrames(getGretaAPFrameList(m), m.Id);
    }

    public AnimationParametersFrame getCurrentFrame(long currentTime)
    {
        //Debug.Log ("current frame: "+ apFramesList.getCurrentFrame(currentTime));
        return apFramesList.getCurrentFrame(currentTime);
    }
}