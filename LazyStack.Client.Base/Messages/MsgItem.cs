using System;
using System.Collections.Generic;
using System.Text;

namespace LazyStack.Client.Base;

public enum MsgItemState { Clean, Dirty, New }	

/// <summary>
/// MsgItem contains all the information necessary to 
/// create/edit/save a message to a message file in 
/// a message set.
/// </summary>
public class MsgItem
{
    public string Msg { get; set; } = "";
    public bool? Editable { get; set; }
}
