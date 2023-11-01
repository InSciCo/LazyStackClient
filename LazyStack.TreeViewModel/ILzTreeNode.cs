namespace LazyStack.TreeViewModel;

public interface ILzTreeNode
{
    Task<ILzTreeNodeViewModel> GetTreeNodeAsync();
}
 