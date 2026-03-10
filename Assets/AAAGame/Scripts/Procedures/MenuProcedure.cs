using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MenuProcedure : ProcedureBase
{
    private int menuUIFormId;
    private IFsm<IProcedureManager> procedure;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        procedure = procedureOwner;
        ShowMenu();
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        // Click blank area to start a run.
        if (Input.GetMouseButtonDown(0) && !GF.UI.IsPointerOverUIObject(Input.mousePosition))
        {
            EnterGame();
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (!isShutdown)
        {
            GF.UI.CloseUIForm(menuUIFormId);
        }

        base.OnLeave(procedureOwner, isShutdown);
    }

    public void EnterGame()
    {
        ChangeState<GameProcedure>(procedure);
    }

    private void ShowMenu()
    {
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        GF.UI.CloseAllLoadingUIForms();
        GF.UI.CloseAllLoadedUIForms();
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();

        menuUIFormId = GF.UI.OpenUIForm(UIViews.MenuUIForm);
        GF.BuiltinView.HideLoadingProgress();
    }
}
