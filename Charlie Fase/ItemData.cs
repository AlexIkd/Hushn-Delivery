using UnityEngine;

[CreateAssetMenu(fileName = "NovoItem", menuName = "Sistema/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea(3, 10)]
    public string description;
    public Sprite icon;
    public GameObject modelPrefab; // O modelo que será instanciado no inspetor
}
