using UnityEngine;
using UnityEngine.UI;

public class InventoryButton : MonoBehaviour
{
    public Image iconImage;

    public void Setup(ItemData item)
    {
        if (item == null) return;
        
        // Se o ícone não for arrastado, tenta pegar no próprio objeto
        if (iconImage == null) iconImage = GetComponent<Image>();

        if (iconImage != null) 
        {
            iconImage.sprite = item.icon;
            // Garante que o ícone mantenha a cor branca/original e não a cor do botão
            iconImage.color = Color.white;
        }
    }
}
