using System.Collections.Generic;
using UnityEngine;

public class MessageBoxesSystem : MonoBehaviour
{
    public static MessageBoxesSystem instance;

    /// <summary>
    /// 链接颜色
    /// </summary>
    public Color32 linkColor;
    /// <summary>
    /// 被选择链接的颜色
    /// </summary>
    public Color32 selectLinkColor;
    /// <summary>
    /// 信息框模板
    /// </summary>
    public GameObject messageBoxTemplate;
    /// <summary>
    /// 已缓存的信息框
    /// </summary>
    public List<MessageBox> messageBoxesBuffer;
    /// <summary>
    /// 缓存上限
    /// </summary>
    private int bufferLimit = 5;
    /// <summary>
    /// 唤出信息框的等待时间
    /// </summary>
    public float waitTime = 0.5f;
    /// <summary>
    /// 信息框锁定的时间
    /// </summary>
    public float lockTime = 2f;
    /// <summary>
    /// 通用描述
    /// </summary>
    public string[] generalDescription;
    /// <summary>
    /// 通用描述数据路径
    /// </summary>
    public string generalDescriptionDataPath = "generalDescription";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        //获取描述数据
        //generalDescription = Resources.Load<GeneralDescription>(generalDescriptionDataPath).Description;
    }

    private void Update()
    {

    }

    /// <summary>
    ///  显示信息框
    /// </summary>
    /// <param name="header">标题文本</param>
    /// <param name="content">内容文本</param>
    /// <returns>生成的信息框</returns>
    public MessageBox ShowMessageBox(string header, string content)
    {
        MessageBox useableMessageBox = null;
        for (int i = 0; i < messageBoxesBuffer.Count; i++)
        {
            if (!messageBoxesBuffer[i].gameObject.activeSelf)
            {
                useableMessageBox = messageBoxesBuffer[i];
                break;
            }
        }
        if (useableMessageBox == null)
        {
            useableMessageBox = Instantiate(messageBoxTemplate, transform).GetComponent<MessageBox>();
            messageBoxesBuffer.Add(useableMessageBox);
        }
        useableMessageBox.gameObject.SetActive(true);
        useableMessageBox.SetTexts(header, content);
        useableMessageBox.SetPosition();
        return useableMessageBox;
    }

    /// <summary>
    ///  隐藏信息框
    /// </summary>
    /// <param name="messageBox">要隐藏的信息框</param>
    public void HideMessageBox(MessageBox messageBox)
    {
        if (messageBoxesBuffer.Count > bufferLimit)
        {
            messageBoxesBuffer.Remove(messageBox);
            Destroy(messageBox.gameObject);
        }
        else
        {
            messageBox.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 获取信息框文字
    /// </summary>
    /// <param name="linkID">链接ID</param>
    /// <returns>标题文本和内容文本</returns>
    public string[] GetMessageBoxText(string linkID)
    {
        //Pattern pattern = GameDataManager.Instance.patternList.Find(p => p.id == int.Parse(linkID));
        // var table = GF.DataTable.GetDataTable<CardTable>().GetDataRow(p => p.Id == int.Parse(linkID));
        // if (table != null)
        // {
        //     return new string[2] { table.Name, table.Describe };
        // }
        // else
        // {
        //     return new string[2] { null, null };
        // }
        return new string[2] { null, null };
    }
}
