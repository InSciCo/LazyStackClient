namespace LazyStack.ViewModels;

/// <summary>
/// ItemViewModelBase<T,TEdit>
/// </summary>
/// <typeparam name="TDTO">DTO Type</typeparam>
/// <typeparam name="TModel">Model Type (extended model off of TDTO)</typeparam>
public abstract class LzItemViewModel<TDTO, TModel> : LzViewModel, ILzItemViewModel<TModel>
    where TDTO : class, new()
    where TModel : class, TDTO, IRegisterObservables, new()
{
    public LzItemViewModel(ILzSessionViewModel sessionViewModel, TDTO? item = null, bool? isLoaded = null)
    {
        LzBaseSessionViewModel = sessionViewModel;    
        CanCreate = true;
        CanRead = true;
        CanUpdate = true;
        CanDelete = true;
        IsLoaded = false;
        IsDirty = false;

        // Assign default storage API handlers
        _ContentSvcReadIdAsync = sessionViewModel.OSAccess.ReadContentAsync;
        _S3SvcCreateIdAsync = sessionViewModel.OSAccess.S3CreateAsync;
        _S3SvcReadIdAsync = sessionViewModel.OSAccess.S3ReadAsync;
        _S3SvcUpdateIdAsync = sessionViewModel.OSAccess.S3UpdateAsync;
        _S3SvcDeleteIdAsync = sessionViewModel.OSAccess.S3DeleteAsync;
        _LocalSvcCreateIdAsync = sessionViewModel.OSAccess.LocalCreateAsync;
        _LocalSvcReadIdAsync = sessionViewModel.OSAccess.LocalReadAsync;
        _LocalSvcUpdateIdAsync = sessionViewModel.OSAccess.LocalUpdateAsync;
        _LocalSvcDeleteIdAsync = sessionViewModel.OSAccess.LocalDeleteAsync;
        _HttpSvcReadIdAsync = sessionViewModel.OSAccess.HttpReadAsync;

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.New)
            .ToPropertyEx(this, x => x.IsNew);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Edit)
            .ToPropertyEx(this, x => x.IsEdit);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Current)
            .ToPropertyEx(this, x => x.IsCurrent);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Deleted)
            .ToPropertyEx(this, x => x.IsDeleted);

        // Init Model Data 
        if (item != null)
            item.DeepCloneTo(Data = new());
        State = (Data == null) ? LzItemViewModelState.New : LzItemViewModelState.Current;
        IsLoaded = isLoaded ??= Data != null;
        Data ??= new();
        Data.RegisterObservables();

    }
    protected ILzSessionViewModel LzBaseSessionViewModel { get; init; }
    protected string entityName = string.Empty;
    //public string UpdateTickField { get; set; } = "UpdatedAt";
    public bool AutoLoadChildren { get; set; } = true;
    public abstract string? Id { get; }
    public abstract long UpdatedAt { get; }

    public string dataCopyJson = string.Empty;
    [Reactive] public TModel? Data { get; set; }

    [Reactive] public LzItemViewModelState State { get; set; }
   
    [Reactive] public bool CanCreate { get; set; }
    [Reactive] public bool CanRead { get; set; }
    [Reactive] public bool CanUpdate { get; set; }
    [Reactive] public bool CanDelete { get; set; }
    [Reactive] public bool IsLoaded { get; set; }
    [Reactive] public virtual long UpdateCount { get; set; }
    [ObservableAsProperty] public bool IsNew { get; }
    [ObservableAsProperty] public bool IsEdit { get; }
    [ObservableAsProperty] public bool IsCurrent { get; }
    [ObservableAsProperty] public bool IsDeleted { get; }
    [Reactive] public bool IsDirty { get; set; }
    public ILzParentViewModel? ParentViewModel { get; set; }
    protected StorageAPI _storageAPI { get; set; }
    // API access - requires authentication
    protected Func<TDTO, Task<TDTO>>? _SvcCreateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TDTO, Task<TDTO>>? _SvcCreateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task<TDTO>>? _SvcReadIdAsync { get; init; }
    protected Func<Task<TDTO>>? _SvcReadAsync { get; init; } // Read using this.Id
    protected Func<TDTO, Task<TDTO>>? _SvcUpdateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TDTO, Task<TDTO>>? _SvcUpdateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task>? _SvcDeleteIdAsync { get; init; }
    // S3 access - requires authentication
    // Id is S3 bucket reference
    protected Func<string, string, Task>? _S3SvcCreateIdAsync { get; init; }
    protected Func<string, Task<string>>? _S3SvcReadIdAsync { get; init; }
    protected Func<string, string, Task>? _S3SvcUpdateIdAsync { get; init; }
    protected Func<string, Task>? _S3SvcDeleteIdAsync { get; init; }
    // Local storage access
    // Id is full path reference
    protected Func<string, string, Task>? _LocalSvcCreateIdAsync { get; init; }
    protected Func<string, Task<string>>? _LocalSvcReadIdAsync { get; init; }
    protected Func<string, string, Task>? _LocalSvcUpdateIdAsync { get; init; }
    protected Func<string, Task>? _LocalSvcDeleteIdAsync { get; init; }
    // _content access 
    // Id is something like "_content/library/somefile"
    // WASM implements this using HttpClient - assumes resource is under wwwroot
    // MAUI implements this using FileSystem.OpenAppPackageFileAsync(id)
    protected Func<string, Task<string>>? _ContentSvcReadIdAsync { get; init; }
    // Http access - general http calls
    // Id is URL
    protected Func<string, Task<string>>? _HttpSvcReadIdAsync { get; init; }

    public virtual void CheckAuth(StorageAPI storageAPI)
    {
    }
    protected void CheckId(string? id)
    {
        if (id is null)
            throw new Exception("Id is null");
    }
    public virtual async Task<(bool, string)> CreateAsync(string? id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_storageAPI == StorageAPI.Default) 
                ? StorageAPI.Rest
                : _storageAPI;

        try
        {
            if (!CanCreate)
                throw new Exception("Create not authorized");

            if (State != LzItemViewModelState.New)
                throw new Exception("State != New");

            if (Data == null)
                throw new Exception("Data not assigned");

            var item = (TDTO)Data;

            if (!Validate())
                throw new Exception("Validation failed.");

            CheckAuth(storageAPI);

            // Perform storage operation
            switch (storageAPI)
            {
                case StorageAPI.Rest:
                    if (id is null)
                    {
                        if (_SvcCreateAsync == null)
                            throw new Exception("SvcCreateAsync not assigned.");
                        item = await _SvcCreateAsync(item!);
                    }
                    else
                    {
                        if (_SvcCreateIdAsync == null)
                            throw new Exception("SvcCreateIdAsync not assigned.");
                        CheckId(id);
                        item = await _SvcCreateIdAsync(id!, item!);
                    }
                    break;
                case StorageAPI.S3:
                    if (_S3SvcCreateIdAsync == null)
                        throw new Exception("S3SvcCreateIdAsync not assigned.");
                    CheckId(id);
                    var s3Text = JsonConvert.SerializeObject(item);
                    await _S3SvcCreateIdAsync(id!, s3Text!);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcCreateIdAsync is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcCreateIdAsync is not supported.");
                case StorageAPI.Local:
                    if (_LocalSvcCreateIdAsync == null)
                        throw new Exception("LocalSvcCreateIdAsync not assigned.");
                    CheckId(id);
                    var localText = JsonConvert.SerializeObject(item);
                    await _LocalSvcCreateIdAsync(id!,localText!);
                    break;
                case StorageAPI.Internal:
                    break;
            }

            UpdateData(item);
            State = LzItemViewModelState.Current;
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, Log(MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    public virtual async Task<(bool, string)> ReadAsync(string id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_storageAPI == StorageAPI.Default)
                ? StorageAPI.Rest
                : _storageAPI;
        var userMsg = "Can't load " + entityName;
        try
        {
            if (!CanRead) 
                throw new Exception("Read not authorized");

            CheckAuth(storageAPI);
            CheckId(id);

            // Perform storage operation
            switch (storageAPI)
            {
                case StorageAPI.Rest:
                    if (_SvcReadIdAsync == null)
                        throw new Exception("SvcReadIdAsync not assigned.");
                    UpdateData(await _SvcReadIdAsync(id));
                    break;
                case StorageAPI.S3:
                    if (_S3SvcReadIdAsync == null)
                        throw new Exception("S3SvcReadIdAsync not assigned.");
                    var s3Text = await _S3SvcReadIdAsync(id);
                    var s3Item = JsonConvert.DeserializeObject<TDTO>(s3Text);
                    UpdateData(s3Item!);
                    break;
                case StorageAPI.Http:
                    if (_HttpSvcReadIdAsync == null)
                        throw new Exception("HttpSvcReadIdAsync not assigned.");
                    var httpText = await _HttpSvcReadIdAsync(id);
                    var httpItem = JsonConvert.DeserializeObject<TDTO>(httpText);
                    UpdateData(httpItem!);
                    break;
                case StorageAPI.Content:
                    if (_ContentSvcReadIdAsync == null)
                        throw new Exception("ContentSvcReadIdAsync not assigned.");
                    var contentText = await _ContentSvcReadIdAsync(id);
                    var contextItem = JsonConvert.DeserializeObject<TDTO>(contentText);
                    UpdateData(contextItem!);
                    break;
                case StorageAPI.Local:
                    if (_LocalSvcReadIdAsync == null)
                        throw new Exception("LocalSvcReadIdAsync not assigned.");
                    var localText = await _LocalSvcReadIdAsync(id);
                    var localItem = JsonConvert.DeserializeObject<TDTO>(localText);
                    UpdateData(localItem!);
                    break;
                case StorageAPI.Internal:
                    break;
                
            }
            
            State = LzItemViewModelState.Current;

            if (AutoLoadChildren)
                return await ReadChildrenAsync(forceload: true, storageAPI);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {

            return (false, Log(userMsg + " " + MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    // ReadAsync without an Id is used to read from an API, often where  the API uses the 
    // logged in identity of the caller as an id for data retrieval. The identity of the 
    // caller is contained in the JWT or Authentication Signature so as to make it 
    // impossible for a sniffer to see the id of the data requested. Currently, this is 
    // relevant to the Rest API but we may extend it to the S3 service as well.
    public virtual async Task<(bool, string)> ReadAsync(StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_storageAPI == StorageAPI.Default)
                ? StorageAPI.Rest
                : _storageAPI;
        try
        {
            if (!CanRead)
                throw new Exception("Read not authorized");

            CheckAuth(storageAPI);

            // Perform storage operation
            switch (storageAPI)
            {
                case StorageAPI.Rest:
                    if (_SvcReadAsync == null)
                        throw new Exception("SvcReadAsync not assigned.");
                    UpdateData(await _SvcReadAsync());
                    break;
                case StorageAPI.S3:
                    throw new Exception("S3SvcReadAsync not supported. Use S3SvcReadIdAsync instead.");
                case StorageAPI.Http:
                    throw new Exception("HttpSvcReadAsync not supported. Use HttpSvcReadIdAsync instead.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcReadAsync not supported. Use ContentSvcReadIdAsync instead.");
                case StorageAPI.Local:
                    throw new Exception("LocalSvcReadAsync not supported. Use LocalSvcReadIdAsync instead.");
                case StorageAPI.Internal:
                    throw new Exception("LocalSvcReadAsync not supported.");

            }
            // Id = Data!.Id;
            State = LzItemViewModelState.Current;
            IsLoaded = true;
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, Log(MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    public virtual async Task<(bool, string)> UpdateAsync(string? id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_storageAPI == StorageAPI.Default)
                ? StorageAPI.Rest
                : _storageAPI;

        try
        {
            if (!CanUpdate) 
                throw new Exception("Update not autorized");

            if (State != LzItemViewModelState.Edit)
                throw new Exception("State != Edit.");

            if (Data is null)
                throw new Exception("Data not assigned");

            if (!Validate())
                throw new Exception("Validation failed.");

            CheckAuth(storageAPI);

            switch(storageAPI)
            {
                case StorageAPI.Rest:
                    if(id is null)
                    {
                        if (_SvcUpdateAsync == null)
                            throw new Exception("SvcUpdateAsync is not assigned.");
                        UpdateData(await _SvcUpdateAsync((TDTO)Data!));
                    }
                    else
                    {
                        if (_SvcUpdateIdAsync == null)
                            throw new Exception("SvcUpdateIdAsync is not assigned.");
                        CheckId(id);
                        UpdateData(await _SvcUpdateIdAsync(id,(TDTO)Data!));
                    }
                    break;
                case StorageAPI.S3:
                    if (_S3SvcUpdateIdAsync == null)
                        throw new Exception("S3SvcUpdateIdAsync is not assigned.");
                    CheckId(id);
                    var s3Text = JsonConvert.SerializeObject(Data);
                    await _S3SvcUpdateIdAsync(id!, s3Text);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcUpdateIdAsync is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcUpdateIdAsync is not supported.");
                case StorageAPI.Local:
                    if (_LocalSvcUpdateIdAsync == null)
                        throw new Exception("LocalSvcUpdateIdAsync is not assigned.");
                    CheckId(id);
                    var localText = JsonConvert.SerializeObject(Data);
                    await _LocalSvcUpdateIdAsync(id!,localText);
                    break;
                case StorageAPI.Internal:
                    break;
            }
            State = LzItemViewModelState.Current;
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, Log(MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    public virtual async Task<(bool,string)> DeleteAsync(string id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_storageAPI == StorageAPI.Default)
                ? StorageAPI.Rest
                : _storageAPI;

        try
        {
            if (!CanDelete)
                throw new Exception("Delete(id) not authorized.");

            if (State != LzItemViewModelState.Current)
                throw new Exception("State != Current");

            CheckAuth(storageAPI);
            CheckId(id); 
            switch(storageAPI)
            {
                case StorageAPI.Rest:
                    if (_SvcDeleteIdAsync == null)
                        throw new Exception("SvcDelete(id) is not assigned.");
                    await _SvcDeleteIdAsync(Id!);
                    break;
                case StorageAPI.S3:
                    if (_S3SvcDeleteIdAsync == null)
                        throw new Exception("S3SvcDelete(id) is not assigned.");
                    await _S3SvcDeleteIdAsync(Id!);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcDelete(id) is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcDelete(id) is not supported.");
                case StorageAPI.Local:
                    if (_LocalSvcDeleteIdAsync == null)
                        throw new Exception("LocalSvcDelete(id) is not assigned.");
                    await _LocalSvcDeleteIdAsync(Id!);
                    break;
                case StorageAPI.Internal:
                    break;
            }

            State = LzItemViewModelState.Deleted;
            Data = null;
            IsDirty = false;
            return(true,String.Empty);

        }
        catch (Exception ex)
        {
            return (false, Log(MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    public virtual void OpenEdit(bool forceCopy = false)
    {
        if (!forceCopy && State == LzItemViewModelState.Edit)
            return;

        if (State != LzItemViewModelState.New)
            State = LzItemViewModelState.Edit;
        MakeDataCopy();
    }
    public virtual Task OpenEditAsync(bool forceCopy = false)
    {
        if (!forceCopy && State == LzItemViewModelState.Edit)
            return Task.CompletedTask;

        if(State != LzItemViewModelState.New)
            State = LzItemViewModelState.Edit;
        MakeDataCopy();
        return Task.CompletedTask;
    }
    public virtual async Task<(bool,string)> SaveEditAsync(string? id, StorageAPI storageAPI = StorageAPI.Default)
    {
        try
        {
            var (success, msg) =
                State == LzItemViewModelState.New
                ? await CreateAsync(id, storageAPI)
                : await UpdateAsync(id, storageAPI);

            State = LzItemViewModelState.Current;
            IsLoaded = true;
            return (success, msg);
        } 
        catch (Exception ex)
        {
            return (false, Log(MethodBase.GetCurrentMethod()!, ex.Message));
        }
    }
    public virtual (bool, string) CancelEdit()
    {
        if (State != LzItemViewModelState.Edit && State != LzItemViewModelState.New)
            return (false, Log(MethodBase.GetCurrentMethod()!, "No Active Edit"));

        State = (IsLoaded) ? LzItemViewModelState.Current : LzItemViewModelState.New;

        RestoreFromDataCopy();
        return (true, String.Empty);
    }
    public virtual async Task<(bool,string)> CancelEditAsync()
    {
        await Task.Delay(0);
        return CancelEdit();
    }
    public virtual bool Validate()
    {
        return true;
    }
    /// <summary>
    /// This method uses a json copy of the data.
    /// Updating data from JSON is not fast. Using 
    /// Force.DeepCloner DeepCloneTo(Data) is not possible 
    /// because it overwrites any event subscriptions.
    /// If your usecase requires optimization, override 
    /// this method and use individual property assignments.
    /// Using individual property assignments is a maintenance 
    /// load but nothing is faster.    
    /// </summary>
    /// <param name="item"></param>
    protected virtual void UpdateData(TDTO item)
    {

        Data ??= new();
        var json = JsonConvert.SerializeObject(item);
        JsonConvert.PopulateObject(json, Data);
        IsDirty = false;
        this.RaisePropertyChanged(nameof(Data));
    }
    /// <summary>
    /// This method uses a json copy of the data. 
    /// Saving data using JSON is not fast. Using Force.DeepCloner
    /// for DataCopy is not possible because the clone process 
    /// fails if the source data has event subscriptions.
    /// It is unlikely that MakeDataCopy is ever used in a usecase 
    /// where performance is critical. If your usecase requires 
    /// opmization, override this method (and the RestoreFromDataCopy method)
    /// and use individual property assignments. 
    /// </summary>
    protected virtual void MakeDataCopy()
    {
        Data ??= new();
        dataCopyJson = JsonConvert.SerializeObject(Data);
    }
    /// <summary>
    /// This method uses a json copy of the data. 
    /// Saving data using JSON is not fast. Using Force.DeepCloner
    /// for DataCopy is not possible because the clone process 
    /// fails if the source data has event subscriptions.
    /// It is unlikely that RestoreFromDataCopy is ever used in a usecase 
    /// where performance is critical. If your usecase requires 
    /// opmization, override this method (and the MakeDataCopy method)
    /// and use individual property assignments. 
    /// </summary>
    protected virtual void RestoreFromDataCopy()
    {
        // Restoring data from JSON is not fast. Using Force.DeepCloner 
        // DeepCloneTo(Data) is not possible because it overwrites any
        // event subscriptions.
        Data ??= new();
        JsonConvert.PopulateObject(dataCopyJson, Data);
    }
    public virtual async Task<(bool, string)> ReadChildrenAsync(bool forceload, StorageAPI storageAPI)
    {
        await Task.Delay(0);
        return (true, string.Empty);
    }

}
