using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace animationparameters
{
    public class APFramesList
    {
        private readonly List<AnimationParametersFrame> apFramesList;
        private readonly int numAPs;

        public APFramesList(int apFrameLength)
        {
            apFramesList = new List<AnimationParametersFrame>();
            numAPs = apFrameLength;
        }

        public APFramesList(AnimationParametersFrame firstAPFrame)
        {
            apFramesList = new List<AnimationParametersFrame>();
            numAPs = firstAPFrame.APVector.Count;
            apFramesList.Add(firstAPFrame);
            firstAPFrame.setFrameNumber(0);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void addFrame(AnimationParametersFrame apFrame)
        {
            // Debug.Log("addFrame number: " + apFrame.getFrameNumber());
            var framesListLenght = apFramesList.Count;
            var numberOfLastFrame = apFramesList[framesListLenght - 1].getFrameNumber();
            if (numberOfLastFrame >= apFrame.getFrameNumber())
                for (var i = framesListLenght - 1; i >= 0; i--)
                {
                    if (apFrame.getFrameNumber() > apFramesList[i].getFrameNumber())
                    {
                        apFramesList.Insert(i + 1, apFrame);
                        break;
                    }

                    if (apFrame.getFrameNumber() == apFramesList[i].getFrameNumber())
                    {
                        for (var j = 0; j < apFrame.size(); j++)
                        {
                            var ap = apFrame.getAnimationParametersList()[j];

                            if (ap.getMask())
                            {
                                apFramesList[i].setValue(j, ap.getValue());
                                apFramesList[i].setMask(j, true);
                            }
                        }

                        //apFramesList [i] = apFrame;
                        break;
                    }
                }
            else
                apFramesList.Add(apFrame);
        }

        public void addAPFrames(List<AnimationParametersFrame> apFrames, string id)
        {
            foreach (var apFrame in apFrames) addFrame(apFrame);
        }

        public void addAPFramesFromFile(string fileName, long firstFrameNumber)
        {
            StringReader apDataReader = null;
            // apData is a string containing the whole file. To be read line-by-line
            //Debug.Log ("firstAPFrame number " + firstFrameNumber);
            var apData = (TextAsset) Resources.Load(fileName, typeof(TextAsset));
            long firstFileFrameNum = 0;
            var firstFrame = true;
            if (apData == null)
            {
                Debug.Log(fileName + " not found");
            }
            else
            {
                apDataReader = new StringReader(apData.text);
                if (apDataReader == null)
                {
                    Debug.Log(fileName + " not readable");
                }
                else
                {
                    var readLine = "";
                    // Read each line from the file
                    // Debug.Log ("APFrameList APnum " +numAPs + " at time " + DateTime.Now.Ticks/10000);

                    while ((readLine = apDataReader.ReadLine()) != null)
                    {
                        var firstLine = readLine;
                        var secondLine = apDataReader.ReadLine();

                        var firstLineTab = firstLine.Split(' ');
                        var secondLineTab = secondLine.Split(' ');
                        /* // Debug of line content
                        String firstLineTabStr = "";
                        for(int i=0; i<firstLineTab.Length;i++){
                        firstLineTabStr += firstLineTab[i];
                        }
                        firstLineTabStr += "End";
                        Debug.Log ("FirstLineTab "+ firstLineTabStr);*/
                        // Debug.Log ("first line length "+ firstLineTab.Length);
                        // Debug.Log ("second line length "+ secondLineTab.Length);
                        var firstLineIter = 0;
                        var secondLineIter = 0;
                        // frameNum
                        if (firstFrame)
                        {
                            firstFileFrameNum = long.Parse(secondLineTab[secondLineIter]);
                            firstFrame = false;
                        }

                        var frameNum = long.Parse(secondLineTab[secondLineIter]) - firstFileFrameNum;
                        secondLineIter++;

                        var apnr = 1;

                        var frame = new AnimationParametersFrame(numAPs);
                        frame.setFrameNumber(firstFrameNumber + frameNum);

                        while (firstLineIter < numAPs - 1)
                        {
                            // Debug.Log (firstLineTab[firstLineIter]);
                            var mask = int.Parse(firstLineTab[firstLineIter]);
                            firstLineIter++;

                            if (mask == 1)
                            {
                                var apValue = int.Parse(secondLineTab[secondLineIter]);
                                secondLineIter++;
                                frame.setAnimationParameter(apnr, apValue);
                            } //end more tokens
                            else
                            {
                                frame.setAnimationParameter(apnr, 0);
                            }

                            apnr++;
                        }

                        addFrame(frame);
                    }

                    // Debug.Log (fileName + " loaded at time " + DateTime.Now.Ticks/10000);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void emptyFramesList()
        {
            var firstAPFrame = new AnimationParametersFrame(peek());
            apFramesList.Clear();
            apFramesList.Add(firstAPFrame);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void updateFrames(long currentFrameNumber)
        {
            var currentFrame = currentFrameNumber;
            var firstAPFrame = peek();
            for (var j = 0; j < apFramesList.Count; j++)
            {
                var apFrame = apFramesList[j];
                if (apFrame.getFrameNumber() > currentFrame) break;
                if (apFrame != firstAPFrame)
                {
                    for (var i = 0; i < apFrame.size(); i++)
                    {
                        var ap = apFrame.getAnimationParametersList()[i];

                        if (ap.getMask())
                        {
                            firstAPFrame.setValue(i, ap.getValue());
                            firstAPFrame.setMask(i, true);
                        }
                    }

                    apFramesList.RemoveAt(j);
                    j--;
                }
            }

            // add as peek frame
            firstAPFrame.setFrameNumber(currentFrame);

            //apFramesList.Insert (0, firstAPFrame);
        }

        public void afficheFirstFrame()
        {
            var firstAPFrame = peek();
            var strMask = "";
            var strValue = "";
            for (var i = 0; i < firstAPFrame.size(); i++)
            {
                var ap = firstAPFrame.getAnimationParametersList()[i];
                strMask += ap.getMask() + " ";
                if (ap.getMask()) strValue += ap.getValue() + " ";
            }

            Debug.Log("firstAPFrame Mask: \n" + strMask + "\nand value: \n" + strValue);
        }

        public AnimationParametersFrame getCurrentFrame(long currentFrameNumber)
        {
            updateFrames(currentFrameNumber);
            /* if(peek (1)!=null){
            //Debug.Log ("second frame "+ peek (1).getFrameNumber ());
            }*/
            return peek();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public AnimationParametersFrame peek()
        {
            if (apFramesList.Count == 0)
                return null;
            return apFramesList[0];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public AnimationParametersFrame peek(int index)
        {
            //Debug.Log ("apFramesList.Count: "+apFramesList.Count+" index: " +index);
            if (apFramesList.Count <= index)
                return null;
            return apFramesList[index];
        }
    }
}