using System;
using UnityEngine;

[Serializable]
public class QuestMarkerLinkData
{
    [Header("Quest")]
    [SerializeField] private string questId;

    [Header("Marker Visibility")]
    [SerializeField] private bool showAvailableMarker = true;
    [SerializeField] private bool showInProgressMarker = true;
    [SerializeField] private bool showReadyToTurnInMarker = true;

    public string QuestId => questId;
    public bool ShowAvailableMarker => showAvailableMarker;
    public bool ShowInProgressMarker => showInProgressMarker;
    public bool ShowReadyToTurnInMarker => showReadyToTurnInMarker;
}