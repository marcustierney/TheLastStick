using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavDiagnostic : MonoBehaviour
{
    private void Update()
    {
        if (!UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        Debug.Log("=== NAV DIAGNOSTIC ===");
        Debug.Log($"Selectable.allSelectablesArray count: {Selectable.allSelectablesArray.Length}");

        foreach (Selectable s in Selectable.allSelectablesArray)
        {
            string type = s.GetType().Name;
            bool active = s.gameObject.activeInHierarchy;
            bool interactable = s.IsInteractable();
            Vector3 pos = s.transform.position;

            Selectable down = s.FindSelectableOnDown();
            Selectable up   = s.FindSelectableOnUp();

            Debug.Log($"[{type}] '{s.name}' | active={active} interactable={interactable} | pos={pos} | navDown='{(down != null ? down.name : "NULL")}' navUp='{(up != null ? up.name : "NULL")}'");

            if (s is Slider)
            {
                // Walk up the hierarchy to surface parent CanvasGroups affecting slider interaction.
                Transform t = s.transform.parent;
                while (t != null)
                {
                    CanvasGroup cg = t.GetComponent<CanvasGroup>();
                    if (cg != null)
                        Debug.Log($"  CanvasGroup on '{t.name}': interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} alpha={cg.alpha}");
                    t = t.parent;
                }
            }
        }

        Debug.Log("=== END ===");
    }
}