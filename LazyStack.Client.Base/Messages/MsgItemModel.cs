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
    public LzMessageSet? Parent { get; set; }
    public string Key { get; set; } = "";
    public string File { get; set; } = "";
    public DocMetaData DocMetaData { get; set; } = new();
    public string Culture { get; set; } = "";

    #region private fields
    private bool _isDirty = false;
    private bool _isNew = false;
    private bool _isEdit = false;
    private string originalMsg = "";
    #endregion

    #region public methods 
    public void SetIsNew() => _isNew = true;

    public MsgItemState GetState()
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
    }
    public void CancelEdit()
    {
        Msg = originalMsg;
        _isDirty = false;
        _isEdit = false;
    }
    public void SaveEdit()
    {
        originalMsg = "";
        _isDirty = !originalMsg.Equals(Msg);
        _isNew = false;
        _isEdit = false;
        UpdateMsg();
    }

    private void UpdateMsg()
    {
        var doc = Parent?.MessageDocs[File];
        doc!.Messages[Key] = new MsgItem() { Msg = this.Msg, Editable = this.Editable };
        doc.Dirty |= _isDirty;
        // todo: need an UpdateMsgs(key)
        Parent!.UpdateMsgs();
    }

    #endregion


}
