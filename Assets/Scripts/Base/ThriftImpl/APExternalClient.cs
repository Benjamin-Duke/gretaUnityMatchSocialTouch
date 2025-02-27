using System.Collections.Generic;
using System.Threading;
using animationparameters;
using thrift.gen_csharp;
using thrift.services;
using time;

namespace thriftImpl
{
    public class APExternalClient : ExternalClient
    {
        private readonly APFramesList apFramesList;
        private readonly AnimationParametersFrame lastFrame;
        private readonly int numberOfAP;
        public Thread apExtClientThread;
        public TimeController timer;

        public APExternalClient(int numberOfAP_)
        {
            numberOfAP = numberOfAP_;
            timer = new TimeController();
            lastFrame = new AnimationParametersFrame(numberOfAP, 0);
            apFramesList = new APFramesList(lastFrame);
        }

        public APExternalClient(int numberOfAP_, string host, int port) : base(host, port)
        {
            numberOfAP = numberOfAP_;
            timer = new TimeController();
            lastFrame = new AnimationParametersFrame(numberOfAP, 0);
            apFramesList = new APFramesList(lastFrame);
        }

        public AnimationParametersFrame newAnimParamFrame(int frameNumber)
        {
            return new AnimationParametersFrame(frameNumber);
        }

        public List<AnimationParametersFrame> getGretaAPFrameList(Message m)
        {
            //Debug.Log("Message id: "+m.Id +", type: "+ m.Type +", time: "+ m.Time +", frameNumber: "+ m.APFrameList[0].FrameNumber + ", number of frames: " + m.APFrameList.Count);
            return thriftAPFrameList2gretaAPFrameList(m.APFrameList);
        }

        public override void startConnector()
        {
            base.startConnector();
            // timer.setNotSynchronized();
        }

        public void setCurrentTime(Message m)
        {
            timer.setTimeMillis(m.Time);
        }

        public List<AnimationParametersFrame> thriftAPFrameList2gretaAPFrameList(
            List<ThriftAnimParamFrame> thriftAPframes)
        {
            var gretaAPFrameList = new List<AnimationParametersFrame>(thriftAPframes.Count);
            foreach (var thriftFrame in thriftAPframes)
            {
                var gretaFrame = new AnimationParametersFrame(numberOfAP, thriftFrame.FrameNumber);
                var thriftAPList = thriftFrame.AnimParamList;
                var i = 0;
                foreach (var thriftAP in thriftAPList)
                {
                    gretaFrame.setAnimationParameter(i, thriftAP.Value, thriftAP.Mask);
                    ++i;
                }

                gretaAPFrameList.Add(gretaFrame);
            }

            return gretaAPFrameList;
        }

        public AnimationParametersFrame getCurrentFrame(long currentTime)
        {
            //Debug.Log ("current frame: "+ apFramesList.getCurrentFrame(currentTime));
            var currentAPFrame = apFramesList.getCurrentFrame(currentTime);
            /*if(lastFrame.isEqualTo(currentAPFrame)){
            return null;
            } else {
            lastFrame = currentAPFrame;*/
            return currentAPFrame;
            //}
        }

        public override void perform(Message m)
        {
            //if(!timer.isSynchronized())
            // Debug.Log ("Message "+ m.Id + " received at " + timer.getTimeMillis ());
            setCurrentTime(m);
            apFramesList.addAPFrames(getGretaAPFrameList(m), m.Id);
        }

        public void emptyFrameList()
        {
            apFramesList.emptyFramesList();
        }
    }
}