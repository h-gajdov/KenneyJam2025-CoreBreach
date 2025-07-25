using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PointerSoundManager : MonoBehaviour
{
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private GameObject prevHoveredObject;

    private void Update() {
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        if (results.Count <= 1) {
            prevHoveredObject = null;
            return;
        }

        foreach (var result in results) {
            GameObject curr = result.gameObject;
            if (prevHoveredObject == curr) break;

            Button btn = curr.GetComponent<Button>();
            if (btn != null && btn.interactable && prevHoveredObject != curr) {
                AudioManager.Play("Hover");
                prevHoveredObject = curr;
                break;
            }
        }

        if (prevHoveredObject == null) return;

        if (Input.GetMouseButtonDown(0)) {
            AudioManager.Play("ButtonPress");
        }
    }
}
