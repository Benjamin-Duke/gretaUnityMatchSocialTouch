using System;
using System.Collections.Generic;
using animationparameters;
using audioElements;
using autodeskcharacter;
using autodeskcharacter.bapmapper;
using autodeskcharacter.fapmapper;
using thriftImpl;
using time;
using tools;
using UnityEngine;
using OneDOF = autodeskcharacter.fapmapper.OneDOF;
using TwoDOF = autodeskcharacter.fapmapper.TwoDOF;

public class GretaCharacterAnimator : MonoBehaviour
{
    private static readonly int NUMBER_OF_FAPS = Enum.GetNames(typeof(FAPType)).Length;
    private static readonly int NUMBER_OF_BAPS = 296;

    // Delay in animation play to absorb network latencies
    private static readonly int
        ANIM_DELAY = 000; //Add up to 400ms delay when the connection is over a the web or wifi connection

    public bool sat;
    public float hipsDegrees = -90;
    public float kneesDegrees = 90;

    // from old code
    public bool distantConnection = true;
    public int FAP_RECEIVER_PORT = 9090;
    public int BAP_RECEIVER_PORT = 9900;
    public int AUDIO_RECEIVER_PORT = 9009;
    public int CMD_SENDER_PORT = 9912;
    public string GretaServersHost = "localhost";

    public float UpperBodyAnimationEffect = 0.9f;
    public bool agentPlaying;
    public int framesBeforeNotAgentPlaying = 2;
    public bool animateAgent = true;
    public string animationID = "noIdea";

    public string animationIDold = "noIdea";

    // DistantConnection: Thrift
    public bool thriftConsumerOpened;
    private readonly ConcatenateJoints concatenator = new ConcatenateJoints();

    private AudioSource _currentAudioSource;
    // Unity is a server receiving inputs from client

    public AudioFilePlayer audioFilePlayer;
    public AudioReceiver audioReceiver;
    private List<BapMapper> bapMappers;
    public BAPReceiver bapReceiver;

    // Time controler
    public TimeController characterTimer;
    public CommandSender commandSender;
    private int cptFrames;

    private List<FapMapper> fapMappers;
    public FAPReceiver fapReceiver;
    private AnimationParametersFrame lastBAPFrame;

    private AnimationParametersFrame lastFAPFrame;

    public bool useBapAnimation = false;

    // Use this for initialization
    private void Awake()
    {
        changeTPoseToNPose();
        setUpSkeleton();

        // from old code
        //Time.fixedDeltaTime = (float) 0.04;
        characterTimer = new TimeController();
        characterTimer.setTimeMillis(0);

        bapReceiver = new BAPReceiver(NUMBER_OF_BAPS, BAP_RECEIVER_PORT);
        fapReceiver = new FAPReceiver(NUMBER_OF_FAPS + 1, FAP_RECEIVER_PORT);
        audioReceiver = new AudioReceiver(AUDIO_RECEIVER_PORT);

        lastFAPFrame = new AnimationParametersFrame(NUMBER_OF_FAPS + 1, 0);
        lastBAPFrame = new AnimationParametersFrame(NUMBER_OF_BAPS, 0);
        cptFrames = 0;
        commandSender = new CommandSender(GretaServersHost, CMD_SENDER_PORT);
        agentPlaying = false;

        _currentAudioSource = getBone("Head").gameObject.AddComponent<AudioSource>();
        audioFilePlayer = new AudioFilePlayer();
    }

    public void FixedUpdate()
    {
        if (animateAgent)
        {
            AnimationParametersFrame currentFAPFrame = null;
            AnimationParametersFrame currentBAPFrame = null;

            AudioElement currentAudio = null;

            // Update of frames
            if (distantConnection)
            {
                // uses THRIFT for updating animation

                if (!thriftConsumerOpened)
                {
                    // standard connection
                    if (!fapReceiver.isConnected() && !fapReceiver.isOnConnection())
                    {
                        fapReceiver.startConnection();
                    }
                    else if (!bapReceiver.isConnected() && !bapReceiver.isOnConnection() && fapReceiver.isConnected())
                    {
                        Debug.Log("FAP Receiver started");
                        bapReceiver.startConnection();
                    }
                    else if (!audioReceiver.isConnected() && !audioReceiver.isOnConnection() &&
                             bapReceiver.isConnected())
                    {
                        Debug.Log("BAP Receiver started");
                        audioReceiver.startConnection();
                    }
                    else if (!commandSender.isConnected() && !commandSender.isOnConnection() &&
                             audioReceiver.isConnected())
                    {
                        Debug.Log("Audio Receiver started");
                        commandSender.startConnection();
                    }
                    else if (commandSender.isConnected())
                    {
                        Debug.Log("Connection Sender started");
                        thriftConsumerOpened = true;
                    }
                }
                else
                {
                    // FAP animation
                    if (fapReceiver.timer.isSynchronized())
                    {
                        //if (SceneManager.gretaClock <= 0)
                        characterTimer.setTimeMillis(fapReceiver.timer.getTimeMillis() -
                                                     ANIM_DELAY); // the ANIM_DELAY is to take into account delays on the network
                        SceneManager.gretaClock = characterTimer.getTimeMillis();
                        // Debug.Log(fapReceiver.timer.getTimeMillis()/40 );
                        //currentFAPFrame = fapReceiver.getCurrentFrame (fapReceiver.timer.getTimeMillis () / 40);
                        currentFAPFrame = fapReceiver.getCurrentFrame(characterTimer.getTimeMillis() / 40);
                    }

                    // BAP Animation
                    if (bapReceiver.timer.isSynchronized())
                    {
                        if (SceneManager.gretaClock <= 0)
                        {
                            characterTimer.setTimeMillis(bapReceiver.timer.getTimeMillis() -
                                                         ANIM_DELAY); // the ANIM_DELAY is to take into account delays on the network
                            SceneManager.gretaClock = characterTimer.getTimeMillis();
                        }

                        currentBAPFrame = bapReceiver.getCurrentFrame(characterTimer.getTimeMillis() / 40);
                    }

                    // AudioBuffer
                    if (fapReceiver.timer.isSynchronized())
                        // consumer AUDIO Buffer
                        currentAudio = audioReceiver.getCurrentAudioElement(characterTimer.getTimeMillis() / 40);
                }
            }

            // Animates agent using local files
            else
            {
                if (fapReceiver.isConnected())
                {
                    fapReceiver.stopConnector();
                    thriftConsumerOpened = false;
                }

                if (bapReceiver.isConnected())
                {
                    bapReceiver.stopConnector();
                    thriftConsumerOpened = false;
                }

                if (audioReceiver.isConnected())
                {
                    audioReceiver.stopConnector();
                    thriftConsumerOpened = false;
                }
            }

            // Update of animation
            if (currentFAPFrame != null)
            {
                if (lastFAPFrame.isEqualTo(currentFAPFrame))
                {
                    cptFrames++;
                    if (cptFrames > framesBeforeNotAgentPlaying)
                    {
                        agentPlaying = false;
                        cptFrames = 0;
                    }
                }
                else
                {
                    agentPlaying = true;
                    cptFrames = 0;
                    lastFAPFrame = new AnimationParametersFrame(currentFAPFrame);
                }

                applyFapFrame(currentFAPFrame);
            }

            if (currentBAPFrame != null)
            {
                if (!lastBAPFrame.isEqualTo(currentBAPFrame))
                    //Debug.Log(">> GRETA BAP frame received and applied");
                    if (lastBAPFrame.isEqualTo(currentBAPFrame))
                    {
                        cptFrames++;
                        if (cptFrames > framesBeforeNotAgentPlaying)
                        {
                            agentPlaying = false;
                            cptFrames = 0;
                        }
                    }
                    else
                    {
                        agentPlaying = true;
                        cptFrames = 0;
                        lastBAPFrame = new AnimationParametersFrame(currentBAPFrame);
                    }

                applyBapFrame(currentBAPFrame);
            }

            /*EB : START TEST FOR AUDIO BUFFER*/
            if (audioFilePlayer.isNewAudio() || audioReceiver.isNewAudio())
            {
                //EB : I reconstructed the short values computed by cereproc from the byte buffer sent by VIB
                // and used this short value to fill the float buffer needed by the audio clip
                if (currentAudio.rawData.Length > 0)
                {
                    var len = currentAudio.rawData.Length / 2;
                    //EB: I couldn't find in Unity how to clean an audio clip nor how to modify its buffer length,
                    // so I prefered to destroy the audio clip (to free the memory) and to create an audio clip
                    // which has the appropriate float buffer size.
                    // In theory the frequency should be provided by the currentAudio object (which should
                    // receive such an information in the message from VIB), but since this is not the case
                    // I hard coded the frequency (47250). It works fine with cereproc, but not with MaryTTS.
                    // For Mary you need to set the frequency to 16000. This is ugly, really!
                    // It should be a input and not hard coded. The problem is that the thrift message doesn't
                    // contain the information at all and I don't want to put my hands in that part of your code.
                    Destroy(_currentAudioSource.clip);

                    _currentAudioSource.clip = AudioClip.Create("text", len, 1, currentAudio.getSampleRate(), false);
                    var buffer = new float[len];
                    for (var iPCM = 44; iPCM < len; iPCM++)
                    {
                        float f;
                        var i = (short)((currentAudio.rawData[iPCM * 2 + 1] << 8) | currentAudio.rawData[iPCM * 2]);
                        f = i / (float)32768;
                        if (f > 1) f = 1;
                        if (f < -1) f = -1;
                        buffer[iPCM] = f;
                    }

                    _currentAudioSource.clip.SetData(buffer, 0);
                    _currentAudioSource.Play();

                    audioReceiver.setNewAudio(false);
                    audioFilePlayer.setNewAudio(false);
                }
                else
                {
                    if (_currentAudioSource != null && _currentAudioSource.clip != null)
                    {
                        var offSet = (characterTimer.getTimeMillis() - (float)currentAudio.getFrameNumber() * 40) /
                                     1000;
                        var samplesOffset = (int)(_currentAudioSource.clip.frequency * offSet *
                                                   _currentAudioSource.clip.channels);
                        _currentAudioSource.timeSamples = samplesOffset;
                        _currentAudioSource.Play();
                    }

                    audioReceiver.setNewAudio(false);
                    audioFilePlayer.setNewAudio(false);
                }
            }
        }
        else
        {
            if (_currentAudioSource != null) _currentAudioSource.Stop();
        }

        if (animationIDold != animationID)
        {
            PlayAgentAnimation(animationID);
            animationIDold = animationID;
        }
    }

    public void OnApplicationQuit()
    {
        // close THRIFT consumer if it's used
        if (SceneManager.isThrift)
        {
            if (fapReceiver.isConnected()) fapReceiver.stopConnector();
            if (bapReceiver.isConnected()) bapReceiver.stopConnector();
            if (audioReceiver.isConnected()) audioReceiver.stopConnector();
            if (commandSender.isConnected()) commandSender.stopConnector();
        }
    }

    public void PlayAgentAnimation(string animationID, InterpersonalAttitude attitude = null)
    {
        //Debug.Log("[GretaCharacterAnimator] PlayAgentAnimation called with: " + animationID);
        animateAgent = true;
        // Send "play" command to distant server
        if (distantConnection)
        {
            if (commandSender.isConnected())
            {
                // Debug.Log("sending play animation command: " + animationID + " with attitude: " + attitude);
                // Debug.Log("on host: " + commandSender.getHost() + " and port: " + commandSender.getPort());
                commandSender.playAnimation(animationID + ".xml", attitude);
            }
            else
            {
                Debug.LogWarning("AnimationReceiver on host: " + commandSender.getHost() + " and port: " +
                                 commandSender.getPort() + " not connected");
            }
        }
    }

    /// <summary>
    ///     Notifies GRETA that the given object has changed its position.
    ///     The GRETA agent will follow it with its gaze with the given gaze influence.<br />
    ///     If GRETA does not know the object, it will be created in its environment.<br />
    ///     If GRETA knows the object, it will be moved in its environment.<br />
    ///     The object is always represented by a cube in GRETA's environment.
    /// </summary>
    /// <param name="objectToFollow">object to be notified</param>
    /// <param name="gazeInfluence">gaze influence with which to gaze at the object</param>
    public void FollowObjectWithGaze(GameObject objectToFollow,
        GretaObjectTracker.Influence gazeInfluence = GretaObjectTracker.Influence.EYES)
    {
        animateAgent = true;
        // Send "play" command to distant server
        if (distantConnection)
        {
            if (commandSender.isConnected())
                commandSender.SendFollowObjectWithGaze(objectToFollow, gazeInfluence);
            else
                Debug.LogWarning("AnimationReceiver on host: " + commandSender.getHost() + " and port: " +
                                 commandSender.getPort() + " not connected");
        }
    }

    private void setUpSkeleton()
    {
        var oldRotation = transform.rotation;
        var oldPosition = transform.position;
        transform.rotation = Quaternion.identity;
        transform.position = Vector3.zero;

        if (sat)
        {
            correctBone("LeftLeg", Quaternion.AngleAxis(kneesDegrees, new Vector3(0, 1, 0)));
            correctBone("RightLeg", Quaternion.AngleAxis(kneesDegrees, new Vector3(0, 1, 0)));
            correctBone("LeftUpLeg", Quaternion.AngleAxis(hipsDegrees, new Vector3(1, 0, 0)));
            correctBone("RightUpLeg", Quaternion.AngleAxis(hipsDegrees, new Vector3(1, 0, 0)));
        }

        // setup the face
        fapMappers = new List<FapMapper>();
        setupFapMapper();

        //setup the body
        bapMappers = new List<BapMapper>();
        setupBapMapper();

        transform.position = oldPosition;
        transform.rotation = oldRotation;
    }

    private void changeTPoseToNPose()
    {
        float clavC = 9;
        var rclavC = Quaternion.AngleAxis(clavC, new Vector3(0, 0, 1));
        var lclavC = Quaternion.AngleAxis(clavC, new Vector3(0, 0, -1));

        var shoulderC = Quaternion.AngleAxis(90 - clavC, new Vector3(0, 0, 1));

        var rThumbC =
            Quaternion.AngleAxis(-33, new Vector3(0, 1, 0)) *
            Quaternion.AngleAxis(-20, new Vector3(0, 0, 1)) *
            Quaternion.AngleAxis(15, new Vector3(1, 0, 0));

        correctBone("RightShoulder", rclavC);
        correctBone("LeftShoulder", lclavC);
        correctBone("RightArm", shoulderC);
        correctBone("LeftArm", shoulderC);
        correctBone("RightHandThumb1", rThumbC);
        correctBone("LeftHandThumb1", rThumbC);
    }

    private void setupFapMapper()
    {
        float ens = 0;
        float es = 0;
        float mw = 0;
        float mns = 0;
        if (hasBone("LeftEye") && hasBone("RightEye"))
            es = Vector3.Distance(getBone("LeftEye").localPosition, getBone("RightEye").localPosition);
        if (hasBone("LeftEye") && hasBone("RightEye") && hasBone("Nostrils"))
            ens = Vector3.Distance(
                (getBone("LeftEye").localPosition + getBone("RightEye").localPosition) * 0.5f,
                getBone("Nostrils").localPosition
            );
        if (hasBone("LipCornerL") && hasBone("LipCornerR"))
            mw = Vector3.Distance(
                getBone("LipCornerL").localPosition,
                getBone("LipCornerR").localPosition);
        if (hasBone("LipCornerL") && hasBone("LipCornerR") && hasBone("Nostrils"))
            mns = Vector3.Distance(
                (getBone("LipCornerL").localPosition + getBone("LipCornerR").localPosition) * 0.5f,
                getBone("Nostrils").localPosition
            );

        if (hasBone("UpperLidL") && hasBone("LowerLidL"))
        {
            var upper = getBone("UpperLidL");
            var lower = getBone("LowerLidL");
            var dist = Vector3.Distance(lower.localPosition, upper.localPosition);
            fapMappers.Add(new OneDOF(upper, FAPType.close_t_l_eyelid, new Vector3(dist / 1024f, 0, 0)));
            fapMappers.Add(new OneDOF(lower, FAPType.close_b_l_eyelid, new Vector3(-dist / 1024f, 0, 0)));
        }

        if (hasBone("UpperLidR") && hasBone("LowerLidR"))
        {
            var upper = getBone("UpperLidR");
            var lower = getBone("LowerLidR");
            var dist = Vector3.Distance(lower.localPosition, upper.localPosition);
            fapMappers.Add(new OneDOF(upper, FAPType.close_t_r_eyelid, new Vector3(dist / 1024f, 0, 0)));
            fapMappers.Add(new OneDOF(lower, FAPType.close_b_r_eyelid, new Vector3(-dist / 1024f, 0, 0)));
        }

        if (hasBone("LeftEye"))
            fapMappers.Add(new Eye(getBone("LeftEye"), FAPType.pitch_l_eyeball, new Vector3(0, -1, 0),
                FAPType.yaw_l_eyeball, new Vector3(1, 0, 0)));
        if (hasBone("RightEye"))
            fapMappers.Add(new Eye(getBone("RightEye"), FAPType.pitch_r_eyeball, new Vector3(0, -1, 0),
                FAPType.yaw_r_eyeball, new Vector3(1, 0, 0)));
        if (hasBone("BrowInnerL"))
            fapMappers.Add(new TwoDOF(getBone("BrowInnerL"), FAPType.raise_l_i_eyebrow, new Vector3(-ens / 1024, 0, 0),
                FAPType.squeeze_l_eyebrow, new Vector3(0, -es / 1024, -es / 2048)));
        if (hasBone("BrowOuterL"))
            fapMappers.Add(new OneDOF(getBone("BrowOuterL"), FAPType.raise_l_o_eyebrow,
                new Vector3(-ens / 1024, 0, es / 4096)));
        if (hasBone("BrowInnerR"))
            fapMappers.Add(new TwoDOF(getBone("BrowInnerR"), FAPType.raise_r_i_eyebrow, new Vector3(-ens / 1024, 0, 0),
                FAPType.squeeze_r_eyebrow, new Vector3(0, es / 1024, -es / 2048)));
        if (hasBone("BrowOuterR"))
            fapMappers.Add(new OneDOF(getBone("BrowOuterR"), FAPType.raise_r_o_eyebrow,
                new Vector3(-ens / 1024, 0, es / 4096)));
        if (hasBone("CheekL"))
            fapMappers.Add(new OneDOF(getBone("CheekL"), FAPType.lift_l_cheek, new Vector3(-ens / 1024, 0, 0)));
        if (hasBone("CheekR"))
            fapMappers.Add(new OneDOF(getBone("CheekR"), FAPType.lift_r_cheek, new Vector3(-ens / 1024, 0, 0)));

        if (hasBone("LipCornerL"))
            fapMappers.Add(new Lip(getBone("LipCornerL"),
                FAPType.stretch_l_cornerlip_o, new Vector3(0, mw / 1024, 0),
                FAPType.raise_l_cornerlip_o, new Vector3(-mns / 1024, 0, 0),
                FAPType.stretch_l_cornerlip, new Vector3(1, 0, 0)));
        if (hasBone("LipCornerR"))
            fapMappers.Add(new Lip(getBone("LipCornerR"),
                FAPType.stretch_r_cornerlip_o, new Vector3(0, -mw / 1024, 0),
                FAPType.raise_r_cornerlip_o, new Vector3(-mns / 1024, 0, 0),
                FAPType.stretch_r_cornerlip, new Vector3(-1, 0, 0)));

        if (hasBone("LipLowerL"))
            fapMappers.Add(new MidLip(getBone("LipLowerL"),
                FAPType.raise_b_lip_lm_o, new Vector3(-mns / 1024, 0, 0),
                FAPType.push_b_lip, new Vector3(0, 0, -mns / 1024),
                FAPType.raise_b_lip_lm, new Vector3(0, 1, 0),
                FAPType.stretch_l_cornerlip_o, new Vector3(0, mw / 4096, 0)));
        if (hasBone("LipLowerR"))
            fapMappers.Add(new MidLip(getBone("LipLowerR"),
                FAPType.raise_b_lip_rm_o, new Vector3(-mns / 1024, 0, 0),
                FAPType.push_b_lip, new Vector3(0, 0, -mns / 1024),
                FAPType.raise_b_lip_rm, new Vector3(0, 1, 0),
                FAPType.stretch_r_cornerlip_o, new Vector3(0, -mw / 4096, 0)));

        if (hasBone("LipUpperL"))
            fapMappers.Add(new MidLip(getBone("LipUpperL"),
                FAPType.lower_t_lip_lm_o, new Vector3(mns / 1024, 0, 0),
                FAPType.push_t_lip, new Vector3(0, 0, -mns / 1024),
                FAPType.lower_t_lip_lm, new Vector3(0, -1, 0),
                FAPType.stretch_l_cornerlip_o, new Vector3(0, mw / 4096, 0)));
        if (hasBone("LipUpperR"))
            fapMappers.Add(new MidLip(getBone("LipUpperR"),
                FAPType.lower_t_lip_rm_o, new Vector3(mns / 1024, 0, 0),
                FAPType.push_t_lip, new Vector3(0, 0, -mns / 1024),
                FAPType.lower_t_lip_rm, new Vector3(0, -1, 0),
                FAPType.stretch_r_cornerlip_o, new Vector3(0, -mw / 4096, 0)));

        if (hasBone("Jaw"))
            fapMappers.Add(new Jaw(getBone("Jaw"),
                FAPType.open_jaw, new Vector3(0, -1, 0), 0.0008f,
                FAPType.shift_jaw, new Vector3(-1, 0, 0), 0.0008f,
                FAPType.thrust_jaw, new Vector3(0, 0, -mns / 1024)));
        if (hasBone("Nostrils"))
            fapMappers.Add(new Nostril(getBone("Nostrils"), FAPType.stretch_l_nose, FAPType.stretch_r_nose,
                new Vector3(ens / 256, ens / 32, ens / 512)));
    }

    private void setupBapMapper()
    {
        var typesUsed = new List<JointType>();

        //spine
        map(typesUsed, "Hips", JointType.HumanoidRoot);
        map(typesUsed, "Head", JointType.skullbase);
        map(typesUsed, "Neck1", JointType.vc5);
        map(typesUsed, "Neck", JointType.vc7);
        map(typesUsed, "Spine4", JointType.vt6);
        map(typesUsed, "Spine3", JointType.vt12);
        map(typesUsed, "Spine2", JointType.vl1);
        map(typesUsed, "Spine1", JointType.vl3);
        map(typesUsed, "Spine", JointType.vl5);
        map(typesUsed, "Spine2V", JointType.vl1, -0.5);
        map(typesUsed, "Spine1V", JointType.vl3, -0.5);
        map(typesUsed, "SpineV", JointType.vl5, -0.5);

        //legs
        map(typesUsed, "LeftUpLeg", "LeftUpLegRoll", JointType.l_hip, 0.5, false);
        map(typesUsed, "LeftLeg", JointType.l_knee);
        map(typesUsed, "LeftFoot", "LeftLegRoll", JointType.l_ankle, 0.5, true);
        map(typesUsed, "LeftToeBase", JointType.l_midtarsal);

        map(typesUsed, "RightUpLeg", "RightUpLegRoll", JointType.r_hip, 0.5, false);
        map(typesUsed, "RightLeg", JointType.r_knee);
        map(typesUsed, "RightFoot", "RightLegRoll", JointType.r_ankle, 0.5, true);
        map(typesUsed, "RightToeBase", JointType.r_midtarsal);

        // rigth arm
        map(typesUsed, "RightShoulder", JointType.r_sternoclavicular);
        mapShoulder(typesUsed, "RightArm", "RightArmRoll", JointType.r_shoulder, JointType.r_acromioclavicular, 0.5);
        map(typesUsed, "RightForeArm", JointType.r_elbow);
        map(typesUsed, "RightHand", "RightForeArmRoll", JointType.r_wrist, 0.75, true);

        map(typesUsed, "RightHandThumb1", JointType.r_thumb1);
        map(typesUsed, "RightHandThumb2", JointType.r_thumb2);
        map(typesUsed, "RightHandThumb3", JointType.r_thumb3);

        map(typesUsed, "RightHandIndex0", JointType.r_index0);
        map(typesUsed, "RightHandIndex1", JointType.r_index1);
        map(typesUsed, "RightHandIndex2", JointType.r_index2);
        map(typesUsed, "RightHandIndex3", JointType.r_index3);

        map(typesUsed, "RightHandMiddle1", JointType.r_middle1);
        map(typesUsed, "RightHandMiddle2", JointType.r_middle2);
        map(typesUsed, "RightHandMiddle3", JointType.r_middle3);

        map(typesUsed, "RightHandRing1", JointType.r_ring1);
        map(typesUsed, "RightHandRing2", JointType.r_ring2);
        map(typesUsed, "RightHandRing3", JointType.r_ring3);

        map(typesUsed, "RightHandPinky0", JointType.r_pinky0);
        map(typesUsed, "RightHandPinky1", JointType.r_pinky1);
        map(typesUsed, "RightHandPinky2", JointType.r_pinky2);
        map(typesUsed, "RightHandPinky3", JointType.r_pinky3);

        //left arm
        map(typesUsed, "LeftShoulder", JointType.l_sternoclavicular);
        mapShoulder(typesUsed, "LeftArm", "LeftArmRoll", JointType.l_shoulder, JointType.l_acromioclavicular, 0.5);

        map(typesUsed, "LeftForeArm", JointType.l_elbow);
        map(typesUsed, "LeftHand", "LeftForeArmRoll", JointType.l_wrist, 0.75, true);

        map(typesUsed, "LeftHandThumb1", JointType.l_thumb1);
        map(typesUsed, "LeftHandThumb2", JointType.l_thumb2);
        map(typesUsed, "LeftHandThumb3", JointType.l_thumb3);

        map(typesUsed, "LeftHandIndex0", JointType.l_index0);
        map(typesUsed, "LeftHandIndex1", JointType.l_index1);
        map(typesUsed, "LeftHandIndex2", JointType.l_index2);
        map(typesUsed, "LeftHandIndex3", JointType.l_index3);

        map(typesUsed, "LeftHandMiddle1", JointType.l_middle1);
        map(typesUsed, "LeftHandMiddle2", JointType.l_middle2);
        map(typesUsed, "LeftHandMiddle3", JointType.l_middle3);

        map(typesUsed, "LeftHandRing1", JointType.l_ring1);
        map(typesUsed, "LeftHandRing2", JointType.l_ring2);
        map(typesUsed, "LeftHandRing3", JointType.l_ring3);

        map(typesUsed, "LeftHandPinky0", JointType.l_pinky0);
        map(typesUsed, "LeftHandPinky1", JointType.l_pinky1);
        map(typesUsed, "LeftHandPinky2", JointType.l_pinky2);
        map(typesUsed, "LeftHandPinky3", JointType.l_pinky3);

        concatenator.setJointToUse(typesUsed);
    }

    private void map(List<JointType> typesUsed, string boneName, JointType joint)
    {
        map(typesUsed, boneName, null, joint, 0, true, 1);
    }

    private void map(List<JointType> typesUsed, string boneName, JointType joint, double scale)
    {
        map(typesUsed, boneName, null, joint, 0, true, scale);
    }

    private void map(List<JointType> typesUsed, string boneName, string twistBoneName, JointType joint,
        double twistFactor, bool before)
    {
        map(typesUsed, boneName, twistBoneName, joint, twistFactor, before, 1);
    }

    private void map(List<JointType> typesUsed, string boneName, string twistBoneName, JointType joint,
        double twistFactor, bool before, double scale)
    {
        if (hasBone(boneName))
        {
            var dofs = new List<Vector3>(3);
            var types = new List<BAPType>(3);
            if (joint.rotationX != BAPType.null_bap)
            {
                dofs.Add(new Vector3(1, 0, 0));
                types.Add(joint.rotationX);
            }

            if (joint.rotationY != BAPType.null_bap)
            {
                dofs.Add(new Vector3(0, -1, 0));
                types.Add(joint.rotationY);
            }

            if (joint.rotationZ != BAPType.null_bap)
            {
                dofs.Add(new Vector3(0, 0, -1));
                types.Add(joint.rotationZ);
            }

            if (dofs.Count == 0) return;
            if (dofs.Count == 1)
                bapMappers.Add(new autodeskcharacter.bapmapper.OneDOF(getBone(boneName), types[0], dofs[0]));
            if (dofs.Count == 2)
                bapMappers.Add(new autodeskcharacter.bapmapper.TwoDOF(getBone(boneName), types[0], dofs[0], types[1],
                    dofs[1]));
            if (dofs.Count == 3)
            {
                if (twistBoneName != null && hasBone(twistBoneName))
                {
                    if (before)
                        bapMappers.Add(new YawTwistBeforeMapper(getBone(boneName), getBone(twistBoneName), types[0],
                            dofs[0], types[1], dofs[1], types[2], dofs[2], twistFactor));
                    else
                        bapMappers.Add(new YawTwistAfterMapper(getBone(boneName), getBone(twistBoneName), types[0],
                            dofs[0], types[1], dofs[1], types[2], dofs[2], twistFactor));
                }
                else
                {
                    if (scale == 1)
                    {
                        bapMappers.Add(new ThreeDOF(getBone(boneName), types[0], dofs[0], types[1], dofs[1], types[2],
                            dofs[2]));
                    }
                    else
                    {
                        var bm = new ThreeDOFScaled(getBone(boneName), types[0], dofs[0], types[1], dofs[1], types[2],
                            dofs[2]);
                        bm.setScale(scale);
                        bapMappers.Add(bm);
                    }
                }
            }

            typesUsed.Add(joint);
        }
    }

    private void mapShoulder(List<JointType> typesUsed, string boneName, string twistBoneName, JointType shoulderJoint,
        JointType acromiumJoint, double twistFactor)
    {
        if (hasBone(boneName))
        {
            var dofs = new List<Vector3>(3);
            var types = new List<BAPType>(3);
            dofs.Add(new Vector3(1, 0, 0));
            types.Add(shoulderJoint.rotationX);
            dofs.Add(new Vector3(0, -1, 0));
            types.Add(shoulderJoint.rotationY);
            dofs.Add(new Vector3(0, 0, -1));
            types.Add(shoulderJoint.rotationZ);
            if (hasBone(twistBoneName))
                bapMappers.Add(new YawTwistAfterMapper(getBone(boneName), getBone(twistBoneName), types[0], dofs[0],
                    types[1], dofs[1], types[2], dofs[2], twistFactor));
            else
                bapMappers.Add(new ThreeDOF(getBone(boneName), types[0], dofs[0], types[1], dofs[1], types[2],
                    dofs[2]));
            typesUsed.Add(shoulderJoint);
        }
    }

    private void correctBone(string boneName, Quaternion correction)
    {
        var bone = findBone(boneName);
        if (bone != null) bone.localRotation = correction * bone.localRotation;
    }

    public Transform findBone(string name)
    {
        var allTransforms = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
            if (t.name == name)
                return t;
        return null;
    }

    public Transform getBone(string name)
    {
        return findBone(name);
    }

    public bool hasBone(string name)
    {
        return findBone(name) != null;
    }

    public void applyFapFrame(AnimationParametersFrame fapframe)
    {
        foreach (var mapper in fapMappers) mapper.applyFap(fapframe);
    }

    public void applyBapFrame(AnimationParametersFrame bapframe)
    {
        // Debug.Log($"[applyBapFrame] Frame #{bapframe.getFrameNumber()} - APVector count: {bapframe.APVector.Count}");
        // // Pour chaque valeur, logue les BAP utilisés (optionnel, mais utile pour voir si les bras/corps sont présents)
        // foreach (var bapType in Enum.GetValues(typeof(BAPType)))
        // {
        //     if (bapframe.getMask((BAPType)bapType))
        //     {
        //         Debug.Log($"BAP {bapType}: {bapframe.getValue((BAPType)bapType)}");
        //     }
        // }

        bapframe = concatenator.concatenateJoints(bapframe);
        foreach (var mapper in bapMappers) mapper.applyBap(bapframe);
    }
    
    private void LateUpdate()
    {
        // Réapplique la dernière frame BAP pour écraser l'Animator
        if (useBapAnimation && lastBAPFrame != null)
        {
            applyBapFrame(lastBAPFrame);
        }
    }
}