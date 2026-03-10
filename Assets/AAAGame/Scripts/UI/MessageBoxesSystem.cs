using System;
using GameFramework;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public static class MessageBoxesSystem
{
    public static bool Show(string title, string content, Action onPositive = null, Action onNegative = null, bool showClose = false)
    {
        return Show(new MessageBox
        {
            Title = title,
            Content = content,
            ShowClose = showClose,
            PositiveAction = onPositive,
            NegativeAction = onNegative
        });
    }

    public static bool Show(MessageBox messageBox)
    {
        if (messageBox == null || GF.UI == null)
        {
            return false;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarString>("Title", messageBox.Title ?? string.Empty);
        uiParams.Set<VarString>("Content", messageBox.Content ?? string.Empty);
        uiParams.Set<VarBoolean>("ShowClose", messageBox.ShowClose);

        if (messageBox.PositiveAction != null)
        {
            GameFrameworkAction action = () => messageBox.PositiveAction?.Invoke();
            uiParams.Set("PositiveAction", action);
        }

        if (messageBox.NegativeAction != null)
        {
            GameFrameworkAction action = () => messageBox.NegativeAction?.Invoke();
            uiParams.Set("NegativeAction", action);
        }

        GF.UI.OpenUIForm(UIViews.CommonDialog, uiParams);
        return true;
    }
}
