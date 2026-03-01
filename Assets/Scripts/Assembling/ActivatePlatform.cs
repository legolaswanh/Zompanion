using UnityEngine;

public class ActivatePlatform : MonoBehaviour
{
    public GameObject oldFurniture;
    public GameObject platformObject;
    
    
    public void UnlockPlatform() 
    {
        oldFurniture.SetActive(false);
        platformObject.SetActive(false);
    }
}
