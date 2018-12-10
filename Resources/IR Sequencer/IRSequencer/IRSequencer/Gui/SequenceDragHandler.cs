﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace IRSequencer.Gui
{
    
    public class SequenceDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Canvas mainCanvas;
        public UnityEngine.Sprite background;

        public GameObject draggedItem;
        public Transform dropZone;
        public bool createCopy = false;

        public IRSequencer.Core.Sequence linkedSequence;

        protected Vector2 startingPosition;
        protected Image draggedItemBG;
        protected int startingSiblingIndex = 0;

        public GameObject placeholder;
        protected const float PLACEHOLDER_MIN_HEIGHT = 10f;

        protected UIAnimationHelper animationHelper;

        protected float startingHeight;

        protected void SetPlaceholderHeight(float newHeight)
        {
            placeholder.GetComponent<LayoutElement>().preferredHeight = newHeight;
        }

        protected void SetDraggedItemPosition(Vector3 newPosition)
        {
            var t = draggedItem.transform as RectTransform;
            t.position = newPosition;
        }

        public virtual float GetDraggedItemHeight()
        {
            var hlg = draggedItem.GetComponent<HorizontalLayoutGroup> ();

            if (hlg == null)
                return 26;
            
            return hlg.preferredHeight;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if(draggedItem == null)
                draggedItem = this.transform.parent.gameObject;
            dropZone = draggedItem.transform.parent;
            startingSiblingIndex = draggedItem.transform.GetSiblingIndex();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(draggedItem.transform as RectTransform, eventData.position, eventData.pressEventCamera, out startingPosition);

            placeholder = new GameObject();
            placeholder.transform.SetParent(draggedItem.transform.parent, false);
            placeholder.transform.SetSiblingIndex(startingSiblingIndex);
            var rt = placeholder.AddComponent<RectTransform>();
            rt.pivot = Vector2.zero;

            var le = placeholder.AddComponent<LayoutElement>();
            le.preferredHeight = startingHeight = GetDraggedItemHeight();
            //le.flexibleWidth = 1;

            animationHelper = draggedItem.AddComponent<UIAnimationHelper>();
            animationHelper.SetHeight = SetPlaceholderHeight;
            animationHelper.SetPosition = SetDraggedItemPosition;

            animationHelper.AnimateHeight(le.preferredHeight, PLACEHOLDER_MIN_HEIGHT, 0.1f);

            var cg = draggedItem.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            draggedItemBG = draggedItem.AddComponent<Image>();
            draggedItemBG.sprite = background;
            draggedItemBG.type = Image.Type.Sliced;
            draggedItemBG.color = Color.white;
            draggedItemBG.fillCenter = true;

            draggedItem.transform.SetParent(mainCanvas.transform, false);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            var rt = draggedItem.transform as RectTransform;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                rt.localPosition = localPointerPosition - startingPosition;
            }
            
            //we don't want to change siblings while we are still animating
            if (animationHelper.isHeightActive)
                return;

            if(eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<SequenceDropHandler>() != null)
            {
                dropZone = eventData.pointerEnter.transform;
                placeholder.transform.SetParent(dropZone,false);
            }

            var currentSiblingIndex = placeholder.transform.GetSiblingIndex();
            var newSiblingIndex = dropZone.childCount-1;

            for (int i=0; i< dropZone.childCount; i++)
            {
                var child = dropZone.GetChild(i);
                if(localPointerPosition.y > child.position.y)
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
                animationHelper.AnimateHeight(PLACEHOLDER_MIN_HEIGHT, startingHeight, 0.1f);
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (animationHelper.isHeightActive)
            {
                animationHelper.StopHeight();
            }
            RectTransform t = draggedItem.transform as RectTransform;
            RectTransform p = placeholder.transform as RectTransform;

            Vector3 newPosition = new Vector3(p.position.x, p.position.y - startingHeight + PLACEHOLDER_MIN_HEIGHT, p.position.z);

            if(p.sizeDelta.y > PLACEHOLDER_MIN_HEIGHT)
                newPosition = p.position;

            animationHelper.AnimatePosition(t.position, newPosition, 0.07f);
            animationHelper.AnimateHeight(placeholder.GetComponent<LayoutElement>().preferredHeight, startingHeight, 0.1f, OnEndDragAnimateEnd);
        }

        protected void OnEndDragAnimateEnd()
        {
            var cg = draggedItem.GetComponent<CanvasGroup>();
            if (cg!= null)
            {
                cg.blocksRaycasts = true;
                Destroy(cg);
            }

            var sequenceDropHandler = dropZone.GetComponent<SequenceDropHandler>();
            if ( sequenceDropHandler != null)
            {
                Logger.Log("[SequenceDragHandler] No SequenceDropHandler on dropzone object");
                sequenceDropHandler.onSequenceDrop(this);
            }

            draggedItem.transform.SetParent(dropZone, false);
            draggedItem.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            draggedItem = null;
            
            Destroy(placeholder);
            Destroy(animationHelper);
            Destroy(draggedItemBG);
        }
    }

}
