namespace LazyStack.Client.TreeViewModel;

public interface ILzTreeNode
{
    Task<ILzTreeNodeViewModel> GetTreeNodeAsync();
}
 