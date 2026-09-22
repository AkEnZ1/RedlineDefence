using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private int damage;
    private AbilityType abilityType;

    private Vector3 randomOffset;

    public void Seek(Transform _target, float _speed, int _damage, AbilityType _type, Color _color)
    {
        target = _target;
        speed = _speed;
        damage = _damage;
        abilityType = _type;

        transform.localScale = Vector3.one;

        // генерируем случайное смещение от центра врага для эффекта разброса
        randomOffset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.4f), Random.Range(-0.4f, 0.4f));

        if (target != null)
        {
            // принудительно поворачиваем пулю к цели в нулевом кадре, перекрывая кривые углы FirePoint
            Vector3 aimPoint = target.position + randomOffset;
            transform.LookAt(aimPoint);
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material.color = _color;
            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.SetColor("_EmissionColor", _color * 2f);
            }
        }
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // точка прицеливания смещена относительно центра врага
        Vector3 aimPoint = target.position + randomOffset;
        Vector3 direction = aimPoint - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        // старый рабочий метод движения в мировых координатах
        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
        transform.LookAt(aimPoint);
    }

    void HitTarget()
    {
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            if (abilityType != AbilityType.None)
            {
                enemy.ApplyAbility(abilityType);
            }
        }
        Destroy(gameObject);
    }
}