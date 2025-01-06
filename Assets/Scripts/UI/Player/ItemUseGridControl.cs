using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ItemUseGridControl : MonoBehaviour
{
    public GameObject backButton;
    public RadialLayout display;
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    IGridClickListener listener;
    IGridSource curInHand;
    readonly Stack<IGridSource> History = new Stack<IGridSource>();

    public void Display(Vector3 ScreenPos, IGridSource source, IGridClickListener listener)
    {
        curInHand = source;
        this.listener = curInHand is not null ? listener : null;
        History.Clear();
        ToggleBackButton();
        if (curInHand != null)
        {
            transform.position = ScreenPos;
            display.Render(curInHand, OnClick);
            gameObject.SetActive(!gameObject.activeInHierarchy);
            ToggleBackButton();
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnClick(int index, IGridItem clicked, PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
        {
            if (clicked is Category source)
            {
                if(source.recipes.Count == 1)
                {
                    listener?.OnClick(source.recipes[0]);
                    Close();
                }
                History.Push(curInHand);
                curInHand = source;
                display.Render(source, OnClick);
            }
            else
            {
                Close();
            }
        }
        listener?.OnClick(clicked);
        ToggleBackButton();
    }

    public void OnBack()
    {
        if(History.Count > 0)
        {
            curInHand = History.Pop();
            display.Render(curInHand, OnClick);
        }
        ToggleBackButton();
    }

    private void ToggleBackButton()
    {
        backButton.SetActive(History.Count > 0);
    }
}
