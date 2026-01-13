using TMPro;
using UnityEngine;

public class RunTimer : MonoBehaviour
{
    public TextMeshProUGUI tmp;

    public void Update()
    {
        if (AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            tmp.enabled = true;
            int secondsInSequence=AreaSequencer.GetAreaSequencer().GetSecondsInSequence();
            tmp.text = $"{secondsInSequence / 60}:{secondsInSequence % 60:D2}";
        }
        else
        {
            tmp.enabled = false;
        }
    }
}
