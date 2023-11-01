using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using ReactiveUI.Blazor;
using LazyStack.Utils;

namespace LazyStack.Blazor;

public class CoreComponentBasePassViewModel<T> : LzReactiveComponentBaseAssignViewModel<T>
    where T : class, INotifyPropertyChanged
{

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (ViewModel == null) throw new Exception($"{this.GetType().Name}, CoreComponentBasePassViewModel: ViewModel is null. Pass ViewModel as Parameter");
        await base.OnInitializedAsync();
    }

}
