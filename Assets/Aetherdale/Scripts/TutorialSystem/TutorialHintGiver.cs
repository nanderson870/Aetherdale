using UnityEngine;

public class TutorialHintGiver : MonoBehaviour
{
    [SerializeField] TutorialHint hint;

    public void GiveHint()
    {
        TutorialManager.SetHint(hint);
    }
}
