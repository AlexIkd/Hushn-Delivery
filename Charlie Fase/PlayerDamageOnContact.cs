using UnityEngine;

/// <summary>
/// Componente opcional para causar dano ao tocar a personagem.
/// Pode ser usado em inimigos, espinhos e outros perigos.
/// </summary>
public class PlayerDamageOnContact : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField, Min(1)] private int damageAmount = 1;
    [SerializeField] private bool damageOnTrigger = true;
    [SerializeField] private bool damageOnCollision = true;

    public int DamageAmount => damageAmount;
    public bool DamageOnCollision => damageOnCollision;
    public bool DamageOnTrigger => damageOnTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (!damageOnTrigger)
            return;

        TryDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!damageOnCollision)
            return;

        TryDamage(collision.collider);
    }

    private void TryDamage(Collider other)
    {
        PlayerHealthSystem health = other.GetComponentInParent<PlayerHealthSystem>();
        if (health != null)
            health.TakeDamage(damageAmount, transform.position);
    }
}
