﻿namespace LazyStack.Blazor;

/// <summary>
/// A base component for handling property changes and updating the blazer view appropriately.
/// </summary>
/// <typeparam name="T">The type of view model. Must support INotifyPropertyChanged.</typeparam>
public class LzLayoutComponentBase<T> : LzReactiveLayoutComponentBase<T>
    where T : class, INotifyPropertyChanged
{

    //[Inject]
    //new public T _myViewModel { set => ViewModel = value; }

    [Inject]
    new public ILzMessages? Messages { get; set; }
    new protected MarkupString Msg(string key) => (MarkupString)Messages!.Msg(key);

    /// <inheritdoc />

}