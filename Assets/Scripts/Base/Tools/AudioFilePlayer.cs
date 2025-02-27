using audioElements;

namespace tools
{
    public class AudioFilePlayer
    {
        private readonly AudioElementList audioElementList;
        private string idCurrentAudio;
        private bool newAudio;

        public AudioFilePlayer()
        {
            audioElementList = new AudioElementList();
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

        public void playAudioFile(string fileName, long time)
        {
            var audioID = "" + time;
            // Debug.Log ("add audio element:" + audioID + " "+ fileName+ " " + time);
            audioElementList.addAudioElement(new AudioElement(audioID, fileName, time / 40));
        }

        public AudioElement getCurrentAudioElement(long currentTime)
        {
            //Debug.Log ("current frame: "+ apFramesList.getCurrentFrame(currentTime));
            var audioElement = audioElementList.getCurrentAudioElement(currentTime);
            // Debug.Log ("idCurrentaudio " + idCurrentAudio + "; id peeked audio "+audioElement.getId());
            if (audioElement.getId() != idCurrentAudio && audioElement.getName() != "")
            {
                newAudio = true;
                idCurrentAudio = "" + audioElement.getId();
                return audioElement;
            }

            return null;
        }

        public void ClearList()
        {
            audioElementList.Clear();
        }
    }
}