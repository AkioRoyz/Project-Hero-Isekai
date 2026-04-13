using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "DialogueSpeaker_", menuName = "Game/Dialogue/Dialogue Speaker Data")]
public class DialogueSpeakerData : ScriptableObject
{
    [Header("Speaker")]
    [SerializeField] private LocalizedString speakerName;
    [SerializeField] private Sprite portrait;

    public LocalizedString SpeakerName => speakerName;
    public Sprite Portrait => portrait;
}