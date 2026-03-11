using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MessageBox : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{

    /// <summary>
    /// 标题
    /// </summary>
    public TMP_Text header;
    /// <summary>
    /// 内容
    /// </summary>
    public TMP_Text content;
    /// <summary>
    /// 锁定进度条
    /// </summary>
    public Image lockProgressBar;
    /// <summary>
    /// 父信息框
    /// </summary>
    public MessageBox parentMessageBox;
    /// <summary>
    /// 子信息框
    /// </summary>
    public MessageBox subMessageBox;

    public LayoutElement layoutElement;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    /// <summary>
    /// 换行字符数量限制
    /// </summary>
    public int characterWrapLimit;
    public bool enableInteractionLock = true;
    /// <summary>
    /// 是否锁定
    /// </summary>
    public bool isLock;
    private float lockTimeCount;
    /// <summary>
    /// 鼠标指针是否在框内
    /// </summary>
    private bool isPointerIn;
    /// <summary>
    /// 选择的链接索引
    /// </summary>
    private int selectedLinkIndex;
    private float waitTimeCount;
    /// <summary>
    /// 信息框中心点
    /// </summary>
    public Vector2 pivot = new Vector2(0.05f, 0.95f);
    public Vector2 screenOffset = new Vector2(48f, -36f);
    private bool isChangeColor;
    private Camera uiCamera;
    private RectTransform parentRectTransform;

    private void Awake()
    {
        RefreshReferences();
    }


    private void OnEnable()
    {
        RefreshReferences();
        parentMessageBox = null;
        subMessageBox = null;
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        isLock = false;
        lockTimeCount = 0;
        isChangeColor = false;
        if (lockProgressBar != null)
        {
            lockProgressBar.gameObject.SetActive(enableInteractionLock);
            lockProgressBar.fillAmount = 0f;
        }
    }

    private void RefreshReferences()
    {
        if (uiCamera == null)
        {
            var uiCameraObject = GameObject.Find("UICamera");
            if (uiCameraObject != null)
            {
                uiCamera = uiCameraObject.GetComponent<Camera>();
            }
        }
        if (uiCamera == null)
        {
            uiCamera = GFBuiltin.UICamera != null ? GFBuiltin.UICamera : Camera.main;
        }
    }

    /// <summary>
    /// 设置信息框文字内容 并限制信息框宽度
    /// </summary>
    /// <param name="headerText">标题文字</param>
    /// <param name="contentText">内容文字</param>
    public void SetTexts(string headerText, string contentText)
    {
        bool isHeaderNeedWrap = false;
        bool isContentNeedWrap = false;
        if (headerText != null)
        {
            header.gameObject.SetActive(true);
            header.text = headerText;
            isHeaderNeedWrap = headerText.Length > characterWrapLimit;
        }
        else
        {
            header.gameObject.SetActive(false);
        }
        if (contentText != null)
        {
            content.gameObject.SetActive(true);
            content.text = contentText;
            isContentNeedWrap = contentText.Length > characterWrapLimit;
        }
        else
        {
            content.gameObject.SetActive(false);
        }
        layoutElement.enabled = isHeaderNeedWrap
            || isContentNeedWrap;
    }

    private void Update()
    {
        //初始化链接颜色
        if (!isChangeColor)
        {
            isChangeColor = true;
            for (int i = 0; i < content.textInfo.linkCount; i++)
            {
                ChangeLinkColor(i, MessageBoxesSystem.instance.linkColor);
            }
        }

        if (subMessageBox != null && !subMessageBox.gameObject.activeInHierarchy)
        {
            subMessageBox = null;
            waitTimeCount = 0;
        }
        if (content.textInfo.linkCount > 0)
        {
            GetSelectLink();
        }
        //隐藏本信息框
        if (isLock && !isPointerIn && subMessageBox == null)
        {
            MessageBoxesSystem.instance.HideMessageBox(this);
        }

        //锁定计时并设置位置
        if (!enableInteractionLock)
        {
            SetPosition();
        }
        else if (lockTimeCount < MessageBoxesSystem.instance.lockTime)
        {
            lockTimeCount += Time.deltaTime;
            lockProgressBar.fillAmount = lockTimeCount / MessageBoxesSystem.instance.lockTime;
            SetPosition();
        }
        else
        {
            if (lockProgressBar != null)
            {
                lockProgressBar.fillAmount = 1;
            }
            canvasGroup.blocksRaycasts = true;
            isLock = true;
        }

    }

    /// <summary>
    /// 获取链接并生成子信息框
    /// </summary>
    private void GetSelectLink()
    {
        int currentSelectLinkIndex = -1;
        if (isLock)
        {
            currentSelectLinkIndex = TMP_TextUtilities.FindIntersectingLink(content, Input.mousePosition, uiCamera);
        }
        if ((currentSelectLinkIndex == -1 && selectedLinkIndex != -1) && currentSelectLinkIndex != selectedLinkIndex)
        {
            ChangeLinkColor(selectedLinkIndex, MessageBoxesSystem.instance.linkColor);
            selectedLinkIndex = -1;
            if (subMessageBox != null && subMessageBox.isLock == false)
            {
                MessageBoxesSystem.instance.HideMessageBox(subMessageBox);
                subMessageBox = null;
            }
        }
        if (currentSelectLinkIndex != -1 && selectedLinkIndex != currentSelectLinkIndex)
        {
            selectedLinkIndex = currentSelectLinkIndex;
            ChangeLinkColor(currentSelectLinkIndex, MessageBoxesSystem.instance.selectLinkColor);
        }
        if (selectedLinkIndex == currentSelectLinkIndex && currentSelectLinkIndex != -1 && subMessageBox == null)
        {
            waitTimeCount += Time.deltaTime;
        }
        else
        {
            waitTimeCount = 0;
        }
        if (waitTimeCount > MessageBoxesSystem.instance.waitTime)
        {
            TMP_LinkInfo linkInfo = content.textInfo.linkInfo[selectedLinkIndex];
            string[] MessageBoxTexts = MessageBoxesSystem.instance.GetMessageBoxText(linkInfo.GetLinkID());
            subMessageBox = MessageBoxesSystem.instance.ShowMessageBox(MessageBoxTexts[0], MessageBoxTexts[1]);
            subMessageBox.parentMessageBox = this;
            waitTimeCount = 0;
        }
    }

    /// <summary>
    /// 改变链接颜色（图片只改变亮度）
    /// </summary>
    /// <param name="linkIndex">链接索引</param>
    /// <param name="changeColor">改变的颜色</param>
    private void ChangeLinkColor(int linkIndex, Color32 changeColor)
    {
        int startIndex = content.textInfo.linkInfo[linkIndex].linkTextfirstCharacterIndex;
        int linkLength = content.textInfo.linkInfo[linkIndex].linkTextLength;
        for (int i = startIndex; i < startIndex + linkLength; i++)
        {
            if (content.textInfo.characterInfo[i].elementType == TMP_TextElementType.Character)
            {
                //改变文字颜色
                int materialIndex = content.textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = content.textInfo.characterInfo[i].vertexIndex;
                Color32[] dst_colors = content.textInfo.meshInfo[materialIndex].colors32;
                dst_colors[vertexIndex + 0] = changeColor;
                dst_colors[vertexIndex + 1] = changeColor;
                dst_colors[vertexIndex + 2] = changeColor;
                dst_colors[vertexIndex + 3] = changeColor;
            }
            else
            {
                //改变图片颜色
                int materialIndex = content.textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = content.textInfo.characterInfo[i].vertexIndex;
                Color32[] dst_colors = content.textInfo.meshInfo[materialIndex].colors32;
                int rgb = changeColor.r + changeColor.g + changeColor.b;
                changeColor = new Color32((byte)(rgb > 255 ? 255 : rgb), (byte)(rgb > 255 ? 255 : rgb), (byte)(rgb > 255 ? 255 : rgb), 255);
                dst_colors[vertexIndex + 0] = changeColor;
                dst_colors[vertexIndex + 1] = changeColor;
                dst_colors[vertexIndex + 2] = changeColor;
                dst_colors[vertexIndex + 3] = changeColor;
            }
        }
        content.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// 设置位置跟随鼠标
    /// </summary>
    public void SetPosition()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (parentRectTransform == null)
        {
            parentRectTransform = transform.parent as RectTransform;
        }
        if (uiCamera == null)
        {
            RefreshReferences();
        }
        if (rectTransform == null || parentRectTransform == null)
        {
            return;
        }

        Vector2 screenPoint = (Vector2)Input.mousePosition + screenOffset;
        if (screenPoint.x > Screen.width / 2f)
        {
            rectTransform.pivot = new Vector2(pivot.y, rectTransform.pivot.y);
            screenPoint.x -= screenOffset.x * 2f;
        }
        else
        {
            rectTransform.pivot = new Vector2(pivot.x, rectTransform.pivot.y);
        }
        if (screenPoint.y > Screen.height / 2f)
        {
            rectTransform.pivot = new Vector2(rectTransform.pivot.x, pivot.y);
        }
        else
        {
            rectTransform.pivot = new Vector2(rectTransform.pivot.x, pivot.x);
            screenPoint.y -= screenOffset.y * 2f;
        }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPoint, uiCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    /// <summary>
    ///  获取指针是否在信息框内 否则获取是否在父信息框内
    /// </summary>
    /// <param name="currentPointerRaycastGameObject">当前鼠标接触的物体</param>
    public void GetPointerIsInGameObject(GameObject currentPointerRaycastGameObject)
    {
        if (currentPointerRaycastGameObject != null)
        {
            MessageBox messageBox = currentPointerRaycastGameObject.GetComponentInParent<MessageBox>();
            if (messageBox == this)
            {
                isPointerIn = true;
            }
            else if (subMessageBox == null)
            {
                isPointerIn = false;
                GetPointerIsInParentMessageBoxGameObject(currentPointerRaycastGameObject);
            }
        }
        else
        {
            if (subMessageBox == null)
            {
                isPointerIn = false;
                GetPointerIsInParentMessageBoxGameObject(currentPointerRaycastGameObject);
            }
        }
    }

    /// <summary>
    ///  获取指针是否在父信息框内
    /// </summary>
    /// <param name="currentPointerRaycastGameObject">当前鼠标接触的物体</param>
    private void GetPointerIsInParentMessageBoxGameObject(GameObject currentPointerRaycastGameObject)
    {
        if (parentMessageBox != null)
        {
            parentMessageBox.subMessageBox = null;
            parentMessageBox.GetPointerIsInGameObject(currentPointerRaycastGameObject);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetPointerIsInGameObject(eventData.pointerCurrentRaycast.gameObject);
        subMessageBox = null;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetPointerIsInGameObject(eventData.pointerCurrentRaycast.gameObject);
    }
}
