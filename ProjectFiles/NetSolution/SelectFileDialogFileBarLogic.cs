#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Core;
using System.IO;
using FTOptix.CoreBase;
using System;
using FTOptix.HMIProject;
using FilesystemBrowserHelper;
using FTOptix.System;
#endregion

public class SelectFileDialogFileBarLogic : BaseNetLogic
{
	public override void Start()
	{
		resourceUriHelper = new ResourceUriHelper(LogicObject.NodeId.NamespaceIndex);
		var widgetBackground = Owner.Owner.Owner.Owner;

		filesystemBrowser = widgetBackground.Owner as IUAObject;
		if (filesystemBrowser == null)
			throw new CoreConfigurationException("FilesystemBrowser cannot be null");

		folderPathVariable = widgetBackground.Owner.GetVariable("StartingPath");
		if (folderPathVariable == null)
			throw new CoreConfigurationException("Path variable not found");

		fullPathVariable = widgetBackground.GetVariable("TmpFile");
		if (fullPathVariable == null)
			throw new CoreConfigurationException("TmpFile variable not found");

		accessFullFilesystemVariable = filesystemBrowser.GetVariable("AccessFullFilesystem");
		if (accessFullFilesystemVariable == null)
			throw new CoreConfigurationException("AccessFullFilesystem variable not found");

		accessNetworkDrivesVariable = filesystemBrowser.GetVariable("AccessNetworkDrives");
		if (accessNetworkDrivesVariable == null)
			throw new CoreConfigurationException("AccessNetworkDrives variable not found");

		if (accessFullFilesystemVariable.Value && !PlatformConfigurationHelper.IsFreeNavigationSupported())
			return;

		fileTextBox = Owner as TextBox;

		confirmButton = Owner.Owner.Owner.Get<Button>("ButtonsBar/Confirm");
		confirmButton.Enabled = false;

		folderPathVariable.VariableChange += PathVariable_VariableChange;
		fileTextBox.TextVariable.VariableChange += FileTextBox_VariableChange;
		fileTextBox.OnUserTextChanged += FileTextBox_UserTextChanged;

		filesDatagrid = Owner.Owner.Owner.Get<DataGrid>("DataGrid");
		filesDatagrid.OnUserSelectionChanged += Datagrid_OnUserSelectionChanged;
	}

	public override void Stop()
	{
		folderPathVariable.VariableChange -= PathVariable_VariableChange;

		fileTextBox.TextVariable.VariableChange -= FileTextBox_VariableChange;
		fileTextBox.OnUserTextChanged -= FileTextBox_UserTextChanged;

		filesDatagrid.OnUserSelectionChanged -= Datagrid_OnUserSelectionChanged;
	}

	private void Datagrid_OnUserSelectionChanged(object sender, UserSelectionChangedEvent e)
	{
		var selectedItemNodeId = e.SelectedItem;
		if (selectedItemNodeId == null || selectedItemNodeId.IsEmpty)
			return;

		var entry = (SelectFileDialogEntry)InformationModel.GetObject(selectedItemNodeId);
		if (entry == null)
			return;

		if (entry.IsDirectory)
		{
			fileTextBox.Text = "";
		}
		else
		{
            fileTextBox.Text = entry.FileName;
        }

        confirmButton.Enabled = !string.IsNullOrEmpty(fileTextBox.Text);
    }

	private void PathVariable_VariableChange(object sender, VariableChangeEventArgs e)
	{
		var updatedPathResourceUri = new ResourceUri(e.NewValue);
		if (!resourceUriHelper.IsFolderPathAllowed(updatedPathResourceUri, accessFullFilesystemVariable.Value, accessNetworkDrivesVariable.Value))
		{
			Log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name, $"Cannot browse to {updatedPathResourceUri} since this path is not allowed in current configuration");
			return;
		}

		var insertedFileText = fileTextBox.Text;
		if (string.IsNullOrEmpty(insertedFileText))
		{
			confirmButton.Enabled = false;
			return;
		}

		// Set the FullPath output parameter
		confirmButton.Enabled = SetFullPathVariable(updatedPathResourceUri.Uri, insertedFileText);
	}

	// In case an item has been selected from the datagrid it is necessary to press the confirmation button to start the configured action.
	private void FileTextBox_VariableChange(object sender, VariableChangeEventArgs e)
	{
		var insertedFileText = ((LocalizedText)e.NewValue.Value).Text;
		if (string.IsNullOrEmpty(insertedFileText))
		{
			confirmButton.Enabled = false;
			return;
		}

		// Addition of namespace prefix information is necessary when folder path is configured via
		// FTOptixStudio placeholders like %APPLICATIONDIR%\, %PROJECTDIR%\.
		// This can happen at the start of the project
		var folderPath = resourceUriHelper.AddNamespacePrefixToFTOptixRuntimeFolder(folderPathVariable.Value);
		string updatedFolderPathSystemPath = new ResourceUri(folderPath).Uri;

		confirmButton.Enabled = SetFullPathVariable(updatedFolderPathSystemPath, insertedFileText);
	}

	// If the file name is inserted by the user the action is started when Enter is pressed.
	private void FileTextBox_UserTextChanged(object sender, UserTextChangedEvent e)
	{
        var insertedFileText = e.NewText.Text;
        if (string.IsNullOrEmpty(insertedFileText))
        {
            confirmButton.Enabled = false;
            return;
        }
		if (Directory.Exists(insertedFileText))
		{
			// Check if this is a folder
			confirmButton.Enabled = false;
            return;
        }

        // Addition of namespace prefix information is necessary when folder path is configured via
        // FTOptixStudio placeholders like %APPLICATIONDIR%\, %PROJECTDIR%\.
        // This can happen at the start of the project
        var folderPath = resourceUriHelper.AddNamespacePrefixToFTOptixRuntimeFolder(folderPathVariable.Value);
        string updatedFolderPathSystemPath = new ResourceUri(folderPath).Uri;

        confirmButton.Enabled = SetFullPathVariable(updatedFolderPathSystemPath, insertedFileText);
    }

	private bool SetFullPathVariable(string folderPath, string selectedFileName)
	{
		// Folder paths must be rejected
		if (Path.IsPathRooted(selectedFileName))
		{
			Log.Error(System.Reflection.MethodBase.GetCurrentMethod().Name, $"{selectedFileName} is a full path, but only a file name is allowed");
			return false;
		}

		fullPathVariable.Value = ResourceUri.FromAbsoluteFilePath(Path.Combine(folderPath, selectedFileName));

		return true;
	}

	private IUAObject filesystemBrowser;
	private TextBox fileTextBox;
	private IUAVariable folderPathVariable;
	private IUAVariable fullPathVariable;
	private DataGrid filesDatagrid;
	private IUAVariable accessFullFilesystemVariable;
	private IUAVariable accessNetworkDrivesVariable;
	private Button confirmButton;

	private ResourceUriHelper resourceUriHelper;
}
