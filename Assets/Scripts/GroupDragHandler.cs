using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace InfernalRobotics.Gui
{
    
    public class GroupDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Canvas mainCanvas;

        private Transform startingParent;
        private GameObject draggedItem;
        private Transform dropZone;
        private Vector2 dragHandleOffset;
        private int startingSiblingIndex = 0;
        private GameObject placeholder;
        private IEnumerator _AnimateHeightCoroutine;
        private IEnumerator _AnimatePositionCoroutine;

        private const float PLACEHOLDER_MIN_HEIGHT = 10f;

        private float startingHeight;

        private void SetHeight(float newHeight)
        {
            placeholder.GetComponent<LayoutElement>().preferredHeight = newHeight;
        }

        private void AnimatePlaceholderHeight(float from, float to, float duration, Action callback = null)
        {
            if (_AnimateHeightCoroutine != null)
            {
                StopCoroutine(_AnimateHeightCoroutine);
            }

            _AnimateHeightCoroutine = AnimateHeightCoroutine(from, to, duration, callback);
            StartCoroutine(_AnimateHeightCoroutine);
        }

        private IEnumerator AnimateHeightCoroutine(float from, float to, float duration, Action callback)
        {
            // wait for end of frame so that only the last call to fade that frame is honoured.
            yield return new WaitForEndOfFrame();

            float progress = 0.0f;

            while (progress <= 1.0f)
            {
                progress += Time.deltaTime / duration;

                SetHeight(Mathf.Lerp(from, to, progress));

                yield return null;
            }

            if (callback != null)
                callback.Invoke();

            _AnimateHeightCoroutine = null;
        }

        private void AnimateDragItemPosition(Vector2 from, Vector2 to, float duration, Action callback = null)
        {
            if (_AnimatePositionCoroutine != null)
            {
                StopCoroutine(_AnimatePositionCoroutine);
            }

            _AnimatePositionCoroutine = AnimatePositionCoroutine(from, to, duration, callback);
            StartCoroutine(_AnimatePositionCoroutine);
        }

        private IEnumerator AnimatePositionCoroutine(Vector2 from, Vector2 to, float duration, Action callback)
        {
            // wait for end of frame so that only the last call to fade that frame is honoured.
            yield return new WaitForEndOfFrame();

            float progress = 0.0f;

            while (progress <= 1.0f)
            {
                progress += Time.deltaTime / duration;

                Vector2 newPosition = new Vector2(Mathf.Lerp(from.x, to.x, progress), Mathf.Lerp(from.y, to.y, progress));
                var t = draggedItem.transform as RectTransform;
                t.position = newPosition;

                yield return null;
            }

            if (callback != null)
                callback.Invoke();

            _AnimatePositionCoroutine = null;
        }



        public void OnBeginDrag(PointerEventData eventData)
        {
            draggedItem = this.transform.parent.parent.gameObject; //need to get the whole line as dragged item
            dropZone = draggedItem.transform.parent;
            startingSiblingIndex = draggedItem.transform.GetSiblingIndex();
            startingParent = draggedItem.transform.parent;
            dragHandleOffset = this.transform.position - draggedItem.transform.position;
            
            placeholder = new GameObject();
            placeholder.transform.SetParent(draggedItem.transform.parent);
            placeholder.transform.SetSiblingIndex(startingSiblingIndex);
            var rt = placeholder.AddComponent<RectTransform>();
            rt.pivot = Vector2.zero;

            var le = placeholder.AddComponent<LayoutElement>();
            le.preferredHeight = startingHeight = draggedItem.GetComponent<VerticalLayoutGroup>().preferredHeight;
            le.flexibleWidth = 1;

            AnimatePlaceholderHeight(le.preferredHeight, PLACEHOLDER_MIN_HEIGHT, 0.1f);

            var cg = draggedItem.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            draggedItem.transform.SetParent(mainCanvas.transform);

            Debug.Log("OnBeginDrag: draggedItem.name = " + draggedItem.name + ", dropZone.name" + dropZone.name);
        }

        public void OnDrag(PointerEventData eventData)
        {
            draggedItem.transform.position = eventData.position - dragHandleOffset;

            //we don't want to change siblings while we are still animating
            if (_AnimateHeightCoroutine != null)
                return;

            var currentSiblingIndex = placeholder.transform.GetSiblingIndex();
            var newSiblingIndex = dropZone.childCount-1;

            for (int i=0; i< dropZone.childCount; i++)
            {
                var child = dropZone.GetChild(i);
                if(eventData.position.y > child.position.y)
                {
                    newSiblingIndex = i;

                    if (currentSiblingIndex < newSiblingIndex)
                        newSiblingIndex--;

                    break;
                }
            }

            if (newSiblingIndex != placeholder.transform.GetSiblingIndex())
            {
                placeholder.transform.SetSiblingIndex(newSiblingIndex);
                AnimatePlaceholderHeight(PLACEHOLDER_MIN_HEIGHT, startingHeight, 0.1f);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_AnimateHeightCoroutine != null)
            {
                StopCoroutine(_AnimateHeightCoroutine);
            }
            RectTransform t = draggedItem.transform as RectTransform;
            RectTransform p = placeholder.transform as RectTransform;

            Vector2 newPosition = new Vector2(p.position.x, p.position.y - startingHeight + PLACEHOLDER_MIN_HEIGHT);

            if(p.sizeDelta.y > PLACEHOLDER_MIN_HEIGHT)
                newPosition = p.position;

            AnimateDragItemPosition(t.position, newPosition, 0.07f);
            AnimatePlaceholderHeight(placeholder.GetComponent<LayoutElement>().preferredHeight, startingHeight, 0.1f, OnEndDragAnimateEnd);
            
            Debug.Log("OnEndDrag");
        }

        private void OnEndDragAnimateEnd()
        {
            var cg = draggedItem.GetComponent<CanvasGroup>();
            if (cg!= null)
            {
                cg.blocksRaycasts = true;
                Destroy(cg);
            }
                
            draggedItem.transform.SetParent(startingParent);
            draggedItem.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            draggedItem = null;
            
            Destroy(placeholder);
        }
    }

}
