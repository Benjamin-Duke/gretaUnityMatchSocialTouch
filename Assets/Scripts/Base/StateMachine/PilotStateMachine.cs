using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Pattern { hit, tap, stroke }
public class PilotStateMachine : MonoBehaviour
{
    public GameObject hitPanel;
    public GameObject tapPanel;
    public GameObject strokePanel;

    //Get the TCP Comm from scene
    public TCPCommunication comm;

    private Toggle[] time_toggles;
    private Toggle[] intensity_toggles;
    private Toggle[] mode_toggles;

    public int tap_wait = 300;

    public float tap_irregular_wait = 700f;

    //Audio source pour tester la cohérence audio-haptique
    public AudioSource source;

    // Start is called before the first frame update
    void Start()
    {
        openHitPanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void openTapPanel()
    {
        hitPanel.SetActive(false);
        strokePanel.SetActive(false);
        tapPanel.SetActive(true);
        time_toggles = tapPanel.transform.GetChild(0).gameObject.GetComponentsInChildren<Toggle>();
        intensity_toggles = tapPanel.transform.GetChild(1).gameObject.GetComponentsInChildren<Toggle>();
    }

    public void openHitPanel()
    {
        hitPanel.SetActive(true);
        strokePanel.SetActive(false);
        tapPanel.SetActive(false);

        time_toggles = hitPanel.transform.GetChild(0).gameObject.GetComponentsInChildren<Toggle>();
        intensity_toggles = hitPanel.transform.GetChild(1).gameObject.GetComponentsInChildren<Toggle>();
    }

    public void openStrokePanel()
    {
        hitPanel.SetActive(false);
        strokePanel.SetActive(true);
        tapPanel.SetActive(false);


        time_toggles = strokePanel.transform.GetChild(0).gameObject.GetComponentsInChildren<Toggle>();
        intensity_toggles = strokePanel.transform.GetChild(1).gameObject.GetComponentsInChildren<Toggle>();
        mode_toggles = strokePanel.transform.GetChild(2).gameObject.GetComponentsInChildren<Toggle>();
    }

    public void playPattern()
    {
        comm.SendSignal(createStringPattern());
    }

    public void stopPattern()
    {
        comm.UpdateDevice();
        comm.SendSignal("stopAll");
        Debug.Log("stop signal sent");
    }

    private string createStringPattern()
    {
        string temp = "play=";

        string temp_state = "";

        string temp_time = "";

        foreach (Toggle tog in time_toggles)
        {
            if (tog.isOn)
            {
                temp_time = tog.GetComponentInChildren<Text>().text;
                break;
            }
        }

        string temp_intensity = "";

        foreach (Toggle tog in intensity_toggles)
        {
            if (tog.isOn)
            {
                temp_intensity = tog.GetComponentInChildren<Text>().text;
                break;
            }
        }

        if (strokePanel.activeInHierarchy)
        {
            temp_state = "stroke";

            string temp_mode = "";
            if (mode_toggles[0].isOn)
            {
                temp_mode = "l";
                if (temp_time == "Fast")
                {
                    temp_time = "2000";
                }
                else if (temp_time == "Medium")
                {
                    temp_time = "2250";
                }
                else if (temp_time == "Slow")
                {
                    temp_time = "2500";
                }
                else if (temp_time == "Extra_slow")
                {
                    temp_time = "2750";
                }
            }
            else
            {
                temp_mode = "p";
                if (temp_time == "Fast")
                {
                    temp_time = "2150";
                }
                else if (temp_time == "Medium")
                {
                    temp_time = "2450";
                }
                else if (temp_time == "Slow")
                {
                    temp_time = "2750";
                }
                else if (temp_time == "Extra_slow")
                {
                    temp_time = "3050";
                }
            }


            temp = temp + temp_state + "_" + temp_mode + "_" + temp_time + "_i_" + temp_intensity + ".txt,1,0,2" ;
        }
        else if(hitPanel.activeInHierarchy)
        {
            temp_state = "hit";

            temp = temp + temp_state + "_" + temp_time +  "_i_" + temp_intensity + ".txt,1,0,2" ;
        }
        else
        {
            temp_state = "tap";
            temp = temp + temp_state + "_" + temp_time + "_i_" + temp_intensity + ".txt,5," + tap_wait + ",2" ;
        }

        Debug.Log(temp);

        return temp;
    }


    private IEnumerator IrregularTap()
    {
        float _short = (40f + tap_wait)/1000;
        float _long = (40f + tap_irregular_wait)/1000;
        
        //Play first tap
        comm.SendSignal("play=tap_40_i_40.txt,1,0,2");
        source.Play();
        //Play second tap after tap time + 150 ms
        yield return new WaitForSeconds(_short);
        comm.SendSignal("play=tap_40_i_40.txt,1,0,2");


        // Play last three tap with 150 ms
        yield return new WaitForSeconds(_long);
        comm.SendSignal("play=tap_40_i_40.txt,1,0,2");

        //4rth tap
        yield return new WaitForSeconds(_short);
        comm.SendSignal("play=tap_40_i_40.txt,1,0,2");

        //5th tap
        yield return new WaitForSeconds(_short);
        comm.SendSignal("play=tap_40_i_40.txt,1,0,2");
    }

    public void playPatternedTap()
    {
        StartCoroutine(IrregularTap());
        Debug.Log("coroutine started");
    }
}
