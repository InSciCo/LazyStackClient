namespace LazyStack.Blazor;

public class CoreComponentBaseAssignViewModel<T> : LzReactiveComponentBaseAssignViewModel<T>
    where T : class, INotifyPropertyChanged
{

    /// <inheritdoc />

    protected override async Task OnInitializedAsync()
    {
        var derivedClassName = this.GetType().Name;
        if (ViewModel == null) throw new Exception($"{derivedClassName}, CoreComponentBaseAssignViewModel: ViewModel is null. Assign it in the OnIntializedAsync method.");
        await base.OnInitializedAsync();
    }


}
