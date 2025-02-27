using audioElements;
using thrift.gen_csharp;
using thrift.services;
using time;

namespace thriftImpl
{
    public class AudioReceiver : Receiver
    {
        private readonly AudioElementList audioElementList;
        private string idCurrentAudio;
        private bool newAudio;
        public TimeController timer;

        public AudioReceiver()
        {
            audioElementList = new AudioElementList();
            timer = new TimeController();
            newAudio = false;
            idCurrentAudio = "default";
        }

        public AudioReceiver(int port) : base(port)
        {
            audioElementList = new AudioElementList();
            timer = new TimeController();
            newAudio = false;
            idCurrentAudio = "default";
        }

        public bool isNewAudio()
        {
            return newAudio;
        }

        public void setNewAudio(bool newAudio_)
        {
            newAudio = newAudio_;
        }

        public override void perform(Message m)
        {
            setCurrentTime(m);
            //BR : retrieve the sample rate
            var s_sampleRate = "";
            float f_sampleRate = 16000;
            float fl_sr;
            m.Properties.TryGetValue("sampleRate", out s_sampleRate);
            //FB : Might be necessary to adjust the cultural information when parsing a number, default culture failed to parse "48000.0" to float recently.
            if (s_sampleRate != null)
            {
                if (float.TryParse(s_sampleRate, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out fl_sr))
                    f_sampleRate = fl_sr;
                else if (float.TryParse(s_sampleRate, out fl_sr))
                    f_sampleRate = fl_sr;

            }
            var sampleRate = (int) f_sampleRate;
            //EB : I need to recover the raw data buffer from the message and to create an AudioElement that contains such a buffer

            if (m.Binary_content.Length > 0)
                audioElementList.addAudioElement(new AudioElement(m.Id, m.String_content, m.Time, m.Binary_content,
                    sampleRate));
            else
                audioElementList.addAudioElement(new AudioElement(m.Id, m.String_content, m.Time));
        }

        public AudioElement getCurrentAudioElement(long currentTime)
        {
            //Debug.Log ("current frame: "+ apFramesList.getCurrentFrame(currentTime));
            //Debug.Log ("DANS LA LIST AUDIO : " + audioElementList.Count());
            var audioElement = audioElementList.getCurrentAudioElement(currentTime);
            //Debug.Log ("idCurrentaudio " + idCurrentAudio + "; id peeked audio "+audioElement.getId());
            if (audioElement != null && audioElement.getId() != idCurrentAudio && audioElement.getName() != "")
            {
                newAudio = true;
                idCurrentAudio = "" + audioElement.getId();
                return audioElement;
            }

            return null;
        }

        public void setCurrentTime(Message m)
        {
            timer.setTimeMillis(m.Time);
        }

        public void ClearList()
        {
            audioElementList.Clear();
        }
    }
}