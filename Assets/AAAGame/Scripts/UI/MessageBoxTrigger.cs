using UnityEngine;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MessageBoxTrigger : MonoBehaviour
{
    [TextArea(1, 4)]
    [SerializeField] private string m_Title;
    [TextArea(2, 8)]
    [SerializeField] private string m_Content;
    [SerializeField] private bool m_ShowClose;

    public void Trigger()
    {
        MessageBoxesSystem.Show(m_Title, m_Content, null, null, m_ShowClose);
    }

    public void Trigger(string title, string content, bool showClose = false)
    {
        MessageBoxesSystem.Show(title, content, null, null, showClose);
    }
}
