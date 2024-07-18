using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Linq;

namespace LazyStack.Client.Base
{
    public class MsgItemsModel : NotifyBase
    {

        public MsgItemsModel(LzMessageSet messageSet, string key) 
        { 
            MessageSet = messageSet;
            this.Key = key;
            UpdateItems();
        }  

        #region Public Propeties
        public string Key { get; private set; } 
       
        public Dictionary<string, MsgItemModel> Items { get; set; } = new Dictionary<string, MsgItemModel>(); // key is Doc FilePath
        private string _imperialPreview = "";
        public string ImperialPreview 
        {   get => _imperialPreview; 
            private set => SetProperty(ref _imperialPreview, value); 
        }
        public string _metricPreview = "";
        public string MetricPreview 
        {   get => _metricPreview; 
            private set=> SetProperty(ref _imperialPreview, value); } 
        public List<MsgItemModel> ItemsOrderedByPrecedents { get; private set; } = new List<MsgItemModel>();
        public LzMessageSet MessageSet { get; set; }
       
        #endregion

        #region Private Members
        #endregion

        #region Private Methods

        private Dictionary<string, MsgItemModel> UpdateItems()
        {

            var lastMsg = ""; // Used to provide default on new MstgItem creation
            foreach (var messageDoc in MessageSet.MessageDocs)
            {
                if (Items.TryGetValue( messageDoc.Key, out MsgItemModel? existingMsgItemModel))
                {
                    if (existingMsgItemModel!.MsgItemState != MsgItemState.Clean)
                    {
                        Items.Add(messageDoc.Key, existingMsgItemModel);
                        continue;
                    }
                }
                MsgItem? msgItem;
                _ = messageDoc.Value.Messages.TryGetValue(Key, out msgItem);
                var isEditable = (msgItem != null && (msgItem.Editable ?? false)) || messageDoc.Value.DocMetaData.Editable;
                var isEmpty = msgItem == null || string.IsNullOrEmpty(msgItem.Msg);

                if (!isEditable && isEmpty)
                    continue;

                var msgItemModel = new MsgItemModel(this, messageDoc.Key);

                if (msgItem is not null)
                {
                    if (msgItemModel.MsgItemState == MsgItemState.Clean)
                    {
                        msgItemModel.Msg = msgItem.Msg;
                        msgItemModel.Editable = msgItem.Editable ?? messageDoc.Value.DocMetaData.Editable;
                    }
                }
                else
                {
                    msgItemModel.Msg = lastMsg;
                    msgItemModel.Editable = messageDoc.Value.DocMetaData.Editable;
                }
                lastMsg = msgItemModel.Msg;

                Items.Add(messageDoc.Key, msgItemModel);
            }

            List<string> orderedDocListKeys =
                MessageSet
                .MessageDocs
                .Reverse()
                .Select(x => x.Key).ToList();

            ItemsOrderedByPrecedents.Clear();
            foreach (var docKey in orderedDocListKeys)
                if(Items.ContainsKey(docKey))
                    ItemsOrderedByPrecedents.Add(Items[docKey]);

            return Items;
        }
        private void UpdatePreview()
        {
            ImperialPreview = MessageSet!.Msg(Key, LzMessageUnits.Imperial);
            MetricPreview = MessageSet!.Msg(MetricPreview, LzMessageUnits.Metric);
        }
        #endregion
    }
}
