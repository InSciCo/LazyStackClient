namespace LazyStack.Blazor;

public class LzLayoutComponentBaseAssignViewModel<T> : LzReactiveLayoutComponentBaseAssignViewModel<T>
    where T : class, INotifyPropertyChanged
{

    [Inject]
    new public ILzMessages? Messages { get; set; }
    new protected MarkupString Msg(string key) => (MarkupString)Messages!.Msg(key);
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var derivedClassName = this.GetType().Name;
        if (ViewModel == null) throw new Exception($"{derivedClassName}, CoreComponentBaseAssignViewModel: ViewModel is null. Assign it in the OnIntializedAsync method.");
        await base.OnInitializedAsync();
    }


}
