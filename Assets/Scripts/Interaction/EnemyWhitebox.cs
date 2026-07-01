using UnityEngine;

public class EnemyWhitebox : MonoBehaviour, IDamageable
{
    public int hp = 3;

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP = {hp}");

        transform.localScale *= 0.9f;

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}