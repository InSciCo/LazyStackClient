using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;

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
        this.WhenAnyValue(x => x.Msg)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .Subscribe(x => {
                MsgItemsModel.UpdatePreview();
                Dirty = true;
            });
    }

    #region Public Properties
    public MsgItemsModel? MsgItemsModel { get; private set; }
    public DocMetaData DocMetaData => MsgItemsModel!.MessageSet.MessageDocs[_filePath].DocMetaData;
    public bool Dirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }
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
    public void OpenEdit()
    {
        originalMsg = Msg;
        _isEdit = true;
        Dirty = true;
        MsgItemState = MsgItemState.Dirty;
    }
    public void CancelEdit()
    {
        Msg = originalMsg;
        Dirty = false;
        _isEdit = false;
        MsgItemState = MsgItemState.Clean;
    }
    public void SaveEdit()
    {
        Dirty = !originalMsg.Equals(Msg);
        _isEdit = false;
        if (Dirty)
        {
            _isNew = false;
            MsgItemState = MsgItemState.Dirty;
        }
        else
            MsgItemState = MsgItemState.Clean;
    }
    //public void FinishEdit()
    //{
    //    Dirty = !originalMsg.Equals(Msg);
    //    _isEdit = false;
    //    MsgItemState = MsgItemState.Dirty;
    //}

    #endregion
}
