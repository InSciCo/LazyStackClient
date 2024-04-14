namespace LazyStack.Client.ViewModels;
/// <summary>
/// This class manages a list of ViewModels
/// TVM is the ViewModel Class in the list
/// TDTO is the data transfer object that the TVM model uses
/// TModel is the data model derived from the TDTO that the TVM presents to views.
/// Remember: During construction, Assign SvcReadChildren or SvcReadChildrenId, and EntityName.
/// Also Implement NewViewModel()
/// </summary>
/// <typeparam name="TVM"></typeparam>
/// <typeparam name="TDTO"></typeparam>
/// <typeparam name="TModel"></typeparam>
public abstract class LzItemsViewModel<TVM, TDTO, TModel> : LzViewModel, INotifyCollectionChanged, ILzItemsViewModel<TVM, TDTO, TModel> where TDTO : class, new()
    where TModel : class, TDTO, IRegisterObservables, new()
    where TVM : class,  ILzItemViewModel<TModel>
{
    // Public Properties
    public LzItemsViewModel(
        ILzSessionViewModel sessionViewModel,
        IDictionary<string, TDTO>? dtos = null,
        IDictionary<string, TModel>? models = null
        )
    {
        _LzBaseSessionViewModel = sessionViewModel ?? throw new ArgumentNullException(nameof(sessionViewModel));
        Models = models;  
        DTOs = dtos;

        // Assign default storage API handlers
        _ContentReadIdAsync = sessionViewModel.OSAccess.ReadContentAsync;
        _S3ReadIdAsync = sessionViewModel.OSAccess.S3ReadAsync;
        _LocalReadIdAsync = sessionViewModel.OSAccess.LocalReadAsync;
        _HttpReadIdAsync = sessionViewModel.OSAccess.HttpReadAsync;
        CanList = true;
        CanAdd = true;
    }
    public string? Id { get; set; }
    public virtual Dictionary<string, TVM> ViewModels { get; set; } = new();
  
    public bool SourceIsList { get; set; }  
    private TVM? currentViewModel;
    public TVM? CurrentViewModel
    {
        get => currentViewModel;
        set
        {
            if (value != null && value != LastViewModel && value!.State != LzItemViewModelState.New)
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
    public IDictionary<string, TModel>? Models { get; set; }
    public IDictionary<string, TDTO>? DTOs { get; set; }    

    // Protected Properties 
    protected ILzSessionViewModel _LzBaseSessionViewModel { get; init; }

    // Storage Access
    protected StorageAPI _StorageAPI { get; init; }
    protected Func<string, Task<ICollection<TDTO>>>? _DTOReadListId { get; init; }
    protected Func<Task<ICollection<TDTO>>>? _DTOReadListAsync { get; init; }
    protected Func<string, Task<string>>? _S3ReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _LocalReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _ContentReadIdAsync { get; init; }
    protected Func<string, Task<string>>? _HttpReadIdAsync { get; init; }
    protected string _EntityName { get; set; } = string.Empty;

    // Public Methods
    public virtual void Clear()
    {
        ViewModels.Clear();
        IsLoaded = false;
        IsChanged = true;
    }   
    public virtual async Task<(bool, string)> ReadAsync(bool forceload = false, StorageAPI storageAPI = StorageAPI.DTO)
        => await ReadAsync(string.Empty, forceload, storageAPI);
    public virtual async Task<(bool, string)> ReadAsync(string id, bool forceload = false, StorageAPI storageAPI = StorageAPI.DTO)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (storageAPI == StorageAPI.Default)
                ? StorageAPI.DTO
                : storageAPI;

        var userMsg = "Can't read " + _EntityName + " id:" + id;
        try
        {
            CheckAuth(storageAPI);  
            switch(storageAPI)
            {
                case StorageAPI.DTO:
                    if(string.IsNullOrEmpty(id) && _DTOReadListAsync == null)
                        throw new Exception("SvcReadList function not assigned");   
                    if(!string.IsNullOrEmpty(id) && _DTOReadListId == null)
                        throw new Exception("SvcReadListId function not assigned");
                    IsLoading = true;
                    var items = (!string.IsNullOrEmpty(id))
                        ? await _DTOReadListId!(id)
                        : await _DTOReadListAsync!(); 
                    return await UpdateDataAsync(items, forceload, storageAPI);
                case StorageAPI.S3:
                    if (string.IsNullOrEmpty(id)) throw new Exception("ParentId required for S3SvcReadId");
                    if (_S3ReadIdAsync == null) throw new Exception("S3SvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var s3Text = await _S3ReadIdAsync(id);
                    return await UpdateDataFromTextAsync(s3Text, forceload, storageAPI);
                case StorageAPI.Local:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for LocalSvcReadId");   
                    if(_LocalReadIdAsync == null) throw new Exception("LocalSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var localText = await _LocalReadIdAsync(id);
                    return await UpdateDataFromTextAsync(localText, forceload, storageAPI);    
                case StorageAPI.Content:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for ContentSvcReadId");
                    if(_ContentReadIdAsync == null) throw new Exception("ContentSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var contentText = await _ContentReadIdAsync(id);
                    return await UpdateDataFromTextAsync(contentText, forceload, storageAPI);
                case StorageAPI.Http:
                    if(string.IsNullOrEmpty(id)) throw new Exception("ParentId required for HttpSvcReadId");
                    if(_HttpReadIdAsync == null) throw new Exception("HttpSvcReadIdAsync not assigned.");
                    IsLoading = true;
                    var httpText = await _HttpReadIdAsync(id);
                    return await UpdateDataFromTextAsync(httpText, forceload, storageAPI);
                case StorageAPI.Model:
                    if(Models != null)
                        return await UpdateDataAsync(Models, forceload, storageAPI);
                    if(DTOs != null)
                        return await UpdateDataAsync(DTOs, forceload, storageAPI);
                    throw new Exception("Either Models or DTOs need to be assigned");
                case StorageAPI.None:
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
    public virtual async Task<(bool, string)> CancelCurrentViewModelEditAsync()
    {
        try
        {
            if (CurrentViewModel == null)
                return (false, "CurrentViewModel is null");
            if (CurrentViewModel.State != LzItemViewModelState.New && CurrentViewModel.State != LzItemViewModelState.Edit)
                throw new Exception("State != Edit && State != New");
            await CurrentViewModel.CancelEditAsync();
            if (CurrentViewModel.State == LzItemViewModelState.New)
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
        var isAdd = CurrentViewModel.State == LzItemViewModelState.New;
        var (success, msg) = await CurrentViewModel.SaveEditAsync(id);
        if (success && isAdd)
        {
            if (CurrentViewModel.Id == null)
                throw new Exception("ItemViewModel.Id is null");
            ViewModels.TryAdd(CurrentViewModel.Id, CurrentViewModel);
        }
        return (success, msg);
    }
    public virtual void CheckAuth(StorageAPI storageAPI)
    {
        return;
    }

    // Protected Methods
    public virtual (TVM viewmodel, string id) NewViewModel(TDTO dto)
        => throw new NotImplementedException();
    public virtual(TVM viewmodel, string id) NewViewModel(string key, TModel model)
        => throw new NotImplementedException();
    public virtual(TVM viewmodel, string id) NewViewModel(string key, TDTO dto)
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
                vm.State = LzItemViewModelState.Current;
                if (AutoReadChildren)
                    tasks.Add(ViewModels![id].ReadChildrenAsync(forceload, storageAPI));
            }
            catch
            {
                Console.WriteLine($"Could not load item:");
            }
        }
        await Task.WhenAll(tasks);
        var result = tasks
            .Where(x => x.Result.success == false)
            .Select(x => x.Result)
            .FirstOrDefault((success: true, msg: string.Empty));
        IsLoaded = result.success;
        return result;
    }
    protected virtual async Task<(bool, string)> UpdateDataAsync(IDictionary<string,TModel> list, bool forceload, StorageAPI storageAPI)
    {
        var tasks = new List<Task<(bool success, string msg)>>();
        foreach (var item in list)
        {
            try
            {
                var (vm, itemMsg) = NewViewModel(item.Key, item.Value);
                var id = vm.Id;
                if (id is null)
                    throw new Exception("NewViewModel return null id");
                if (!ViewModels!.ContainsKey(id))
                    ViewModels!.Add(id, vm);
                else
                    ViewModels![id] = vm;
                vm.State = LzItemViewModelState.Current;
                if (AutoReadChildren)
                    tasks.Add(ViewModels![id].ReadChildrenAsync(forceload, storageAPI));
            }
            catch
            {
                Console.WriteLine($"Could not load item:");
            }
        }
        await Task.WhenAll(tasks);
        var result = tasks.Where(x => x.Result.success == false).Select(x => x.Result).FirstOrDefault((success: true, msg: string.Empty));
        IsLoaded = result.success;
        return result;
    }

    protected virtual async Task<(bool, string)> UpdateDataAsync(IDictionary<string, TDTO> list, bool forceload, StorageAPI storageAPI)
    {
        var tasks = new List<Task<(bool success, string msg)>>();
        foreach (var item in list)
        {
            try
            {
                var (vm, itemMsg) = NewViewModel(item.Key, item.Value);
                var id = vm.Id;
                if (id is null)
                    throw new Exception("NewViewModel return null id");
                if (!ViewModels!.ContainsKey(id))
                    ViewModels!.Add(id, vm);
                else
                    ViewModels![id] = vm;
                vm.State = LzItemViewModelState.Current;
                if (AutoReadChildren)
                    tasks.Add(ViewModels![id].ReadChildrenAsync(forceload, storageAPI));
            }
            catch
            {
                Console.WriteLine($"Could not load item:");
            }
        }
        await Task.WhenAll(tasks);
        var result = tasks.Where(x => x.Result.success == false).Select(x => x.Result).FirstOrDefault((success: true, msg: string.Empty));
        IsLoaded = result.success;
        return result;
    }

}
