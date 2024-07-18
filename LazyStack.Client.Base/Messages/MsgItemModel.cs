using System;
using System.Collections.Generic;
using System.Text;

namespace LazyStack.Client.Base;

/// <summary>
/// This class is a model for a message item. It is used to create, edit, 
/// and save messages.
/// </summary>
public class MsgItemModel : MsgItem
{
    public MsgItemModel(MsgItemsModel msgItemsModel, string filePath)

    {
        MsgItemsModel = msgItemsModel;  
        _filePath = filePath; // Key into the MsgItesmModel dictionary

    }

    #region Public Properties
    public MsgItemsModel? MsgItemsModel { get; private set; }
    public DocMetaData DocMetaData => MsgItemsModel!.MessageSet.MessageDocs[_filePath].DocMetaData;
    #endregion

    #region private fields
    private string _filePath  = string.Empty;
    private bool _isDirty = false;
    private bool _isNew = false;
    private bool _isEdit = false;
    private string originalMsg = "";
    #endregion

    #region public methods 
    public void SetIsNew() => _isNew = true;
    public MsgItemState MsgItemState { get; private set; } 
    private MsgItemState SetState()
    {
        if (_isNew)
            return MsgItemState.New;
        if (_isDirty)
            return MsgItemState.Dirty;
        return MsgItemState.Clean;
    }
    public void OpenEdit()
    {
        originalMsg = Msg;
        _isEdit = true;
        _isDirty = true;
        SetState();
    }
    public void CancelEdit()
    {
        Msg = originalMsg;
        _isDirty = false;
        _isEdit = false;
        SetState();
    }
    public void SaveEdit()
    {
        originalMsg = "";
        _isDirty = !originalMsg.Equals(Msg);
        _isNew = false;
        _isEdit = false;
        SetState();

    }


    #endregion


}
