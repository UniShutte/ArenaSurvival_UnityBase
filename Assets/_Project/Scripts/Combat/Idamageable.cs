namespace ArenaSurvival.Combat
{
    /// <summary>
    /// Контракт объекта, который может получать урон.
    ///
    /// Projectile не обязан знать, попал ли он
    /// в противника, мишень или разрушаемый объект.
    /// Ему достаточно работать с IDamageable.
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(int amount);
    }
}