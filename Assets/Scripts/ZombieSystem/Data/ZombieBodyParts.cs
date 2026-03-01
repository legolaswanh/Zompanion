using System;
using UnityEngine;

[Serializable]
[Obsolete("Unused. Use ZombieRecipeSO as the single source for assembly requirements.")]
public class ZombieBodyParts
{
    [SerializeField] private ItemDataSO torso;
    [SerializeField] private ItemDataSO arm;
    [SerializeField] private ItemDataSO leg;

    public ItemDataSO Torso => torso;
    public ItemDataSO Arm => arm;
    public ItemDataSO Leg => leg;
}
