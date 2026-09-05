using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ScriptableObject que define uma linha de diálogo do NPC.
/// Crie via: Assets > Create > Dialogue > NPC Line
/// </summary>
[CreateAssetMenu(fileName = "NPCLine", menuName = "Dialogue/NPC Line", order = 1)]
public class NPCLineData : ScriptableObject
{
    [Tooltip("Nome que aparece acima da fala")]
    public string speakerName = "NPC";

    [Tooltip("O texto da fala")]
    [TextArea(3, 8)]
    public string text = "";

    [Tooltip("Opcional: som de tecla ao digitar")]
    public AudioClip typeSound;
}

/// <summary>
/// ScriptableObject que define a sequência completa de diálogo de um NPC.
/// Crie via: Assets > Create > Dialogue > NPC Dialogue
/// </summary>
[CreateAssetMenu(fileName = "NPCDialogue", menuName = "Dialogue/NPC Dialogue", order = 2)]
public class NPCDialogueData : ScriptableObject
{
    [Tooltip("Todas as falas na ordem em que aparecem")]
    public NPCLineData[] lines;

    [Tooltip("Executa um evento no Unity quando o diálogo termina")]
    public UnityEngine.Events.UnityEvent onDialogueFinished;
}
