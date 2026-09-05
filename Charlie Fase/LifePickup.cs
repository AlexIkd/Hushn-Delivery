using UnityEngine;

    /// <summary>
    /// Coletável opcional que adiciona vidas e restaura todos os hits da vida atual.
    /// O objeto precisa ter um Collider com Is Trigger ativado.
    /// </summary>
public class LifePickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int livesToAdd = 1;
    [SerializeField] private bool destroyAfterPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealthSystem health = other.GetComponentInParent<PlayerHealthSystem>();
        if (health == null)
            return;

        health.AddLife(livesToAdd);
        health.RestoreHits();

        if (destroyAfterPickup)
            Destroy(gameObject);
    }
}
