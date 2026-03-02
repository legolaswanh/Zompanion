using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZombieCatalog", menuName = "Zombie/Zombie Catalog")]
public class ZombieCatalogSO : ScriptableObject
{
    [SerializeField] private List<ZombieDefinitionSO> definitions = new List<ZombieDefinitionSO>();

    public IReadOnlyList<ZombieDefinitionSO> Definitions => definitions;
}
