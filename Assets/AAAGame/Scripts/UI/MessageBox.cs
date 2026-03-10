using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public sealed class MessageBox
{
    public string Title;
    public string Content;
    public bool ShowClose = false;
    public Action PositiveAction;
    public Action NegativeAction;
}
