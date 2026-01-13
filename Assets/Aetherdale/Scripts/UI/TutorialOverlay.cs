using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialOverlay : MonoBehaviour
{
    public TextMeshProUGUI hintTMP;

    public static TutorialOverlay Find()
    {
        return FindAnyObjectByType<TutorialOverlay>();
    }

    public void SetHint(TutorialHint tutorialHint, float timeout = -1)
    {
        hintTMP.enabled = true;
        hintTMP.text = tutorialHint.hintText;

        if (timeout > 0)
        {
            StartCoroutine(ClearAfterTimeout(tutorialHint.hintText, timeout));
        }
    }

    public void ClearHint()
    {
        hintTMP.enabled = false;
        hintTMP.text = "";
    }

    IEnumerator ClearAfterTimeout(string originalDescription, float timeout)
    {
        float timeoutRemaining = timeout;

        while (timeoutRemaining > 0)
        {

            if (hintTMP.text != originalDescription)
            {
                break;
            }

            timeoutRemaining -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        // Don't clear it if the hint has changed arleady
        if (hintTMP.text == originalDescription)
        {
            ClearHint();
        }
    }

}
