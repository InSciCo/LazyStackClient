namespace LazyStack.Blazor;

public class LzLayoutComponentBasePassViewModel<T> : LzReactiveLayoutComponentBaseAssignViewModel<T>
    where T : class, INotifyPropertyChanged
{
    [Inject]
    new public ILzMessages? Messages { get; set; }
    new protected MarkupString Msg(string key) => (MarkupString)Messages!.Msg(key);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (ViewModel == null) throw new Exception($"{this.GetType().Name}, CoreComponentBasePassViewModel: ViewModel is null. Pass ViewModel as Parameter");
        await base.OnInitializedAsync();
    }

}
