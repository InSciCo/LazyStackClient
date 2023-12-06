using AwsSignatureVersion4.Private;

namespace LazyStack.ViewModels;
/// <summary>
/// This class manages a list of ViewModels
/// TVM is the ViewModel Class in the list
/// TDTO is the data transfer object that the TVM model uses
/// TModel is the data model derived from the TDTO that the TVM presents to views.
/// Remember: During construction, Assign SvcReadChildren or SvcReadChildrenId, and entityName.
/// Also Implement NewViewModel()
/// </summary>
/// <typeparam name="TVM"></typeparam>
/// <typeparam name="TDTO"></typeparam>
/// <typeparam name="TModel"></typeparam>
public class LzItemsViewModelBase<TVM, TDTO, TModel> : LzViewModelBase, INotifyCollectionChanged, ILzItemsViewModelBase<TVM, TDTO, TModel> where TDTO : class, new()
    where TModel : class, TDTO, IRegisterObservables, new()
    where TVM : class,  ILzItemViewModelBase<TModel>
{
    public LzItemsViewModelBase(ILzBaseSessionViewModel sessionViewModel)
    {
        LzBaseSessionViewModel = sessionViewModel;

        // Assign default storage API handlers
        _ContentSvcReadIdAsync = sessionViewModel.OSAccess.ContentReadAsync;
        _S3SvcReadIdAsync = sessionViewModel.OSAccess.S3ReadAsync;
        _LocalSvcReadIdAsync = sessionViewModel.OSAccess.LocalReadAsync;
        _HttpSvcReadIdAsync = sessionViewModel.OSAccess.HttpReadAsync;
        CanList = true;
        CanAdd = true;
    }
    protected ILzBaseSessionViewModel LzBaseSessionViewModel { get; init; }
    public string? Id { get; set; }
    public Dictionary<string, TVM> ViewModels { get; set; } = new();
    public bool SourceIsList { get; set; }  
    private TVM? currentViewModel;
    public TVM? CurrentViewModel
    {
        get => currentViewModel;
        set
        {
            if (value != null && value != LastViewModel && value!.State != LzItemViewModelBaseState.New)
                LastViewModel = value;
            this.RaiseAndSetIfChanged(ref currentViewModel, value);
        }
    }
    [Reactive] public TVM? LastViewModel { get; set; }
    protected int changeCount;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public bool IsChanged
    {
        get => changeCount > 0;
        set => this.RaiseAndSetIfChanged(ref changeCount, changeCount + 1);
    }
    public bool AutoReadChildren { get; set; } = true;
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public bool IsLoading { get; set; }
    [Reactive] public long LastLoadTick { get; set; }
    [Reactive] public bool CanList { get; set; }
    [Reactive] public bool CanAdd { get; set; }
    [Reactive] public virtual long UpdateCount { get; set; }
    protected StorageAPI _storageAPI { get; set; }

    // API Access
    protected Func<string, Task<ICollection<TDTO>>>? _SvcReadListId { get; init; }
    protected Func<Task<ICollection<TDTO>>>? _SvcReadList { get; init; }
    protected Func<string, Task<string>>? _S3SvcReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _LocalSvcReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _ContentSvcReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _HttpSvcReadIdAsync { get; init; }

    protected string entityName { get; set; } = string.Empty;
    protected virtual (TVM viewmodel, string id) NewViewModel(TDTO dto)
        => throw new NotImplementedException();
    public virtual async Task<(bool, string)> ReadAsync(bool forceload = false, StorageAPI storageAPI = StorageAPI.Rest)
        => await ReadAsync(string.Empty, forceload, storageAPI);
    public virtual async Task<(bool, string)> ReadAsync(string id, bool forceload = false, StorageAPI storageAPI = StorageAPI.Rest)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (storageAPI == StorageAPI.Default)
                ? StorageAPI.Rest
                : storageAPI;

        var userMsg = "Can't read " + entityName + " id:" + id;
        try
        {
            CheckAuth(storageAPI);  
            switch(storageAPI)
            {
                case StorageAPI.Rest:
                    if(string.IsNullOrEmpty(id) && _SvcReadList == null)
                        throw new Exception("SvcReadList function not assigned");   
                    if(!string.IsNullOrEmpty(id) && _SvcReadListId == null)
                        throw new Exception("SvcReadListId function not assigned");
                    IsLoading = true;
                    var items = (!string.IsNullOrEmpty(id))
                        ? await _SvcReadListId!(id)
                        : await _SvcReadList!(); 
                    return await UpdateDataAsync(items, forceload, storageAPI);
                case StorageAPI.S3:
                    if (string.IsNullOrEmpty(id)) throw new Exception("ParentId required for S3SvcReadId");
                    if (_S3SvcReadIdAsync == null) throw new Exception("S3SvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var s3Text = await _S3SvcReadIdAsync(id);
                    return await UpdateDataFromTextAsync(s3Text, forceload, storageAPI);
                case StorageAPI.Local:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for LocalSvcReadId");   
                    if(_LocalSvcReadIdAsync == null) throw new Exception("LocalSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var localText = await _LocalSvcReadIdAsync(id);
                    return await UpdateDataFromTextAsync(localText, forceload, storageAPI);    
                case StorageAPI.Content:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for ContentSvcReadId");
                    if(_ContentSvcReadIdAsync == null) throw new Exception("ContentSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var contentText = await _ContentSvcReadIdAsync(id);
                    return await UpdateDataFromTextAsync(contentText, forceload, storageAPI);
                case StorageAPI.Http:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for HttpSvcReadId");
                    if(_HttpSvcReadIdAsync == null) throw new Exception("HttpSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var httpText = await _HttpSvcReadIdAsync(id);
                    return await UpdateDataFromTextAsync(httpText, forceload, storageAPI);
                case StorageAPI.Internal:
                    return (true, string.Empty);
                default:
                    return (false, Log(userMsg, "StorageAPI not implemented")); 
            }
        }
        catch (Exception ex)
        {
            return (false, Log(userMsg, ex.Message));
        }
        finally { IsLoading = false; }
    }
    public virtual string GetId(TDTO dto)
        => throw new NotImplementedException();
    protected virtual async Task<(bool, string)> UpdateDataFromTextAsync(string jsonContent, bool forceload, StorageAPI storageAPI)
    {
        var items = JsonConvert.DeserializeObject<ICollection<TDTO>>(jsonContent);
        if (items == null) throw new Exception("UpdateDataFromJsonAsync returned null");
        return await UpdateDataAsync(items, forceload, storageAPI);
    }
    protected virtual async Task<(bool, string)> UpdateDataAsync(ICollection<TDTO> list, bool forceload, StorageAPI storageAPI)
    {
        var tasks = new List<Task<(bool success, string msg)>>();
        foreach (var item in list)
        {
            try
            {
                var (vm, itemMsg) = NewViewModel(item);
                var id = vm.Id;
                if (id is null)
                    throw new Exception("NewViewModel return null id");
                if (!ViewModels!.ContainsKey(id))
                    ViewModels!.Add(id, vm);
                else
                    ViewModels![id] = vm;
                vm.State = LzItemViewModelBaseState.Current;
                if (AutoReadChildren)
                    tasks.Add(ViewModels![id].ReadChildrenAsync(forceload, storageAPI));
            } catch (Exception ex)
            {
                Console.WriteLine($"Could not load item:");
            }
        }
        await Task.WhenAll(tasks);
        var result = tasks.Where(x => x.Result.success == false).Select(x => x.Result).FirstOrDefault((success: true, msg: string.Empty));
        IsLoaded = result.success;
        return result;
    }
    public virtual async Task<(bool, string)> CancelCurrentViewModelEditAsync()
    {
        try
        {
            if (CurrentViewModel == null)
                return (false, "CurrentViewModel is null");
            if (CurrentViewModel.State != LzItemViewModelBaseState.New && CurrentViewModel.State != LzItemViewModelBaseState.Edit)
                throw new Exception("State != Edit && State != New");
            await CurrentViewModel.CancelEditAsync();
            if (CurrentViewModel.State == LzItemViewModelBaseState.New)
            {
                if (LastViewModel?.Id != null && ViewModels.ContainsKey(LastViewModel.Id!))
                    CurrentViewModel = LastViewModel;
                else
                    CurrentViewModel = null;
                return (true, string.Empty);
            }
            return (true, String.Empty);
        }
        catch (Exception ex)
        {
            return (false, Log(string.Empty, ex.Message));
        }
    }
    public virtual async Task<(bool, string)> SaveCurrentViewModelAsync(string? id)
    {
        if (CurrentViewModel == null)
            return (false, "CurrentViewModel is null");
        var isAdd = CurrentViewModel.State == LzItemViewModelBaseState.New;
        var (success, msg) = await CurrentViewModel.SaveEditAsync(id);
        if (success && isAdd)
        {
            if (CurrentViewModel.Id == null)
                throw new Exception("ItemViewModel.Id is null");
            ViewModels.TryAdd(CurrentViewModel.Id, CurrentViewModel);
        }
        return (success, msg);
    }
    private void CheckAuth(StorageAPI storageAPI)
    {
        // Check for Auth
        switch (storageAPI)
        {
            case StorageAPI.Rest:
            case StorageAPI.S3:
                if (!LzBaseSessionViewModel.IsSignedIn)
                    throw new Exception("Not signed in.");
                break;
            default:
                break;
        }
    }
}
