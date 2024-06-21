using System;
using System.Collections.Generic;
using System.Text;

namespace LazyStack.Client.Base;

public enum MsgItemState { Clean, Dirty, New }	

public class MsgItem
{
	public MsgItem(LzMessageSet? parent = null)
	{
	      this._parent = parent;
    }

    #region public properties
    public string Msg { get; set; } = "";
	public bool Editable { get; set; } = false;
    #endregion

    #region private fields
    private LzMessageSet? _parent;
    private bool _isDirty = false;
	private bool _isNew = false;
	private bool _isEdit = false;
	private string originalMsg = "";
	#endregion

	#region public methods 
	public void SetIsNew() => _isNew = true;
	public void SetParent(LzMessageSet parent) => this._parent = parent;	

	public MsgItemState GetState()
	{
	    if(_isNew)
            return MsgItemState.New;
        if(_isDirty)
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
        _isDirty = false;
		_isNew = false;
		_isEdit = false;
		_parent?.UpdateMsgs();
    }
    #endregion
}
