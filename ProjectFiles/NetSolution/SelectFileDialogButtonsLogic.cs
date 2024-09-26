#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Core;
using FTOptix.System;
#endregion

public class SelectFileDialogButtonsLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void ExecuteCallback()
    {
        var widgetBackground = Owner.Owner.Owner;
        var fileSelectorDialog = widgetBackground.Owner as FTOptix.UI.Dialog;
        fileSelectorDialog.GetVariable("FullPath").Value = widgetBackground.GetVariable("TmpFile").Value;
        fileSelectorDialog.Get<MethodInvocation>("FileSelectedCallback").Invoke();
        fileSelectorDialog.Close();
    }
}
