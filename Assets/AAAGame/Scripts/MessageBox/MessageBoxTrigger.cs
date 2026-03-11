using UnityEngine;
using UnityEngine.EventSystems;

public class MessageBoxTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    /// <summary>
    /// 要显示的标题文本
    /// </summary>
    public string header;
    /// <summary>
    /// 要显示的内容文本
    /// </summary>
    [TextArea]
    public string content;
    /// <summary>
    /// 生成的信息框
    /// </summary>
    private MessageBox MyMessageBox;
    private float waitTimeCount;
    /// <summary>
    /// 鼠标是否在触发器内
    /// </summary>
    private bool isPointerInThis;
    private PointerEventData pointerEventData;
    private Vector2 mousePosition = new Vector2();

    // private CardItem cardItem;
    //
    // private void Awake()
    // {
    //     cardItem = GetComponent<CardItem>();
    // }

    public void SetText(string header,string content)
    {
        this.header = header;
        this.content = content;
    }

    private void Update()
    {
        if (MyMessageBox != null && !MyMessageBox.gameObject.activeInHierarchy)
        {
            MyMessageBox = null;
            waitTimeCount = 0;
        }
        if (mousePosition == (Vector2)Input.mousePosition)
        {
            if (isPointerInThis)
            {
                waitTimeCount += Time.deltaTime;
            }
            else
            {
                waitTimeCount = 0;
            }
        }
        else
        {
            mousePosition = Input.mousePosition;
        }
        if (waitTimeCount > MessageBoxesSystem.instance.waitTime && MyMessageBox == null)
        {
            Show();
        }
        if (!isPointerInThis && MyMessageBox != null)
        {
            if (MyMessageBox.isLock)
            {
                MyMessageBox = null;
                waitTimeCount = 0;
            }
            else
            {
                Hide();
            }
        }
    }

    /// <summary>
    /// 显示信息框
    /// </summary>
    protected virtual void Show()
    {
        //if (cardItem.IsFlipped == false) return;
        MyMessageBox = MessageBoxesSystem.instance.ShowMessageBox(header, content);
    }

    /// <summary>
    /// 隐藏信息框
    /// </summary>
    private void Hide()
    {
        MessageBoxesSystem.instance.HideMessageBox(MyMessageBox);
        MyMessageBox = null;
        waitTimeCount = 0;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInThis = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInThis = false;
        pointerEventData = eventData;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerInThis = false;
        pointerEventData = eventData;
    }

}
