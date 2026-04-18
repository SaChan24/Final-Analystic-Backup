using UnityEngine;

public class ToggleComponentsOnEvent : MonoBehaviour
{
    [Header("Enable / Disable Components")]
    [Tooltip("คอมโพเนนต์ที่จะเปิด (Behaviour = Script, Collider, NavMeshAgent ฯลฯ)")]
    [SerializeField] private Behaviour[] componentsToEnable;
    [Tooltip("คอมโพเนนต์ที่จะปิด")]
    [SerializeField] private Behaviour[] componentsToDisable;

    [Header("GameObjects")]
    [Tooltip("GameObject ที่จะเปิด")]
    [SerializeField] private GameObject[] objectsToEnable;
    [Tooltip("GameObject ที่จะปิด")]
    [SerializeField] private GameObject[] objectsToDisable;

    public void ApplyToggle()
    {
        if (componentsToEnable != null)
        {
            foreach (var comp in componentsToEnable)
            {
                if (comp != null) comp.enabled = true;
            }
        }

        if (componentsToDisable != null)
        {
            foreach (var comp in componentsToDisable)
            {
                if (comp != null) comp.enabled = false;
            }
        }

        if (objectsToEnable != null)
        {
            foreach (var obj in objectsToEnable)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (objectsToDisable != null)
        {
            foreach (var obj in objectsToDisable)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }
}
