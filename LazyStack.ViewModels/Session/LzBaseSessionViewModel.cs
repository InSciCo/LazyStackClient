﻿using System.ComponentModel;

namespace LazyStack.ViewModels;

/// <summary>
/// Orchastrates the connection to services.
/// Decouples IAuthProcess from ILzClientConfig.
/// </summary>
public class LzBaseSessionViewModel : LzViewModelBase, ILzBaseSessionViewModel, INotifyPropertyChanged
{
    public LzBaseSessionViewModel(
        IOSAccess osAccess,   
        ILzClientConfig clientConfig, 
        IInternetConnectivitySvc internetConnectivity,
    	ILzMessages messages
		)
	{
        OSAccess = osAccess ?? throw new ArgumentNullException(nameof(osAccess));    
        ClientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
        InternetConnectivity = internetConnectivity ?? throw new ArgumentNullException(nameof(internetConnectivity));
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        // Maintain a local instance of the messageset so we can react to changes in that value 
        // to update the current messageset in Messages.
        MessageSet = new LzMessageSet(Messages.MessageSet.Culture, Messages.MessageSet.Units);

        this.WhenAnyValue(x => x.InternetConnectivity.IsOnline)
            .ToPropertyEx(this, x => x.IsOnline);

        this.WhenAnyValue(x => x.IsSignedIn) 
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(async (isSignedIn) =>
            {
                if (isSignedIn)
                    await OnSignedInAsync();
                else
                    await OnSignedOutAsync();
            });
        this.WhenAnyValue(x => x.MessageSet)
            .DistinctUntilChanged()
            .Subscribe(async (messageSet) =>
            { 
                // Note: The LzMessage instance MessageSet is not the same instance as the one in 
                // LzMessages. When it changes, we make a call to the LzMessages instance to update
                // the current message set.
                if(OSAccess != null)
				    await Messages.SetMessageSetAsync(messageSet.Culture, messageSet.Units);
			});
    }
    public IInternetConnectivitySvc InternetConnectivity { get; set; }  
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public ILzNotificationSvc? NotificationsSvc { get; set; } 
    public ILzClientConfig ClientConfig { get; set; }
    public IOSAccess OSAccess { get; set; }
    public ILzMessages? Messages { get; set; }

    // The ObservableAsProperty annotation is defined in ReactiveUI.Fody
    [ObservableAsProperty] public bool IsSignedIn { get; }
    [Reactive] public bool IsBusy { get; set; }
    [ObservableAsProperty] public bool HasChallenge { get; }
    [ObservableAsProperty] public bool IsAdmin { get; }
    [ObservableAsProperty] public bool IsOnline { get; }
    [Reactive] public bool IsLoading { get; set; }
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public LzMessageSet MessageSet { get; set; }
    public Task<bool> CheckInternetConnectivityAsync()
    {
        return InternetConnectivity.CheckInternetConnectivityAsync();
    }
    public virtual async Task OnSignedInAsync()
    {
        try
        {
            IsBusy = true;
            IsLoading = true;
            await LoadAsync();
            IsLoaded = true;

        } catch
        {
            return;
        } finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }
    public virtual async Task LoadAsync()
    {
        await Task.Delay(0);
    }
    public virtual async Task OnSignedOutAsync()
    {
        await UnloadAsync();    
    }
    public virtual async Task UnloadAsync()
    { 
        await Task.Delay(0);    
    }
}
