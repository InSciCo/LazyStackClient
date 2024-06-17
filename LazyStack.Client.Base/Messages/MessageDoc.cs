using System;
using System.Collections.Generic;
using System.Text;

namespace LazyStack.Client.Base;

public class MessageDoc
{
	public DocMetaData DocMetaData { get; set; } = new DocMetaData();
	public Dictionary<string, MsgItem> Messages { get; set; } = new Dictionary<string, MsgItem>();

}
