using GameFramework.Event;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MenuUIForm : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        RefreshMoneyText();

        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, true);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        OpenSubUIForm(UIViews.Topbar, 1, uiparms);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(PlayerDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        if (btId == "SETTING")
        {
            GF.UI.OpenUIForm(UIViews.SettingDialog);
        }
    }

    private void OnUserDataChanged(object sender, GameFramework.Event.GameEventArgs e)
    {
        var args = e as PlayerDataChangedEventArgs;
        if (args.DataType == PlayerDataType.Coins)
        {
            RefreshMoneyText();
        }
    }

    private void RefreshMoneyText()
    {
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        var coinText = UtilityBuiltin.Valuer.ToCoins(playerDm.Coins);
        moneyText.text = GF.Localization.GetString("FlipRun.MenuTips", coinText);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        RefreshMoneyText();
    }
}
