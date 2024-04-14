namespace LazyStack.Client.ViewModels;

/// <summary>
/// ItemViewModelBase<T,TEdit>
/// </summary>
/// <typeparam name="TDTO">DTO Type</typeparam>
/// <typeparam name="TModel">Model Type (extended model off of TDTO)</typeparam>
public abstract class LzItemViewModel<TDTO, TModel> : LzViewModel, ILzItemViewModel<TModel>
    where TDTO : class, new()
    where TModel : class, TDTO, IRegisterObservables, new()
{
    // Public Properties
    public LzItemViewModel(ILzSessionViewModel sessionViewModel, TDTO? dto = null, TModel? model = null, bool? isLoaded = null)
    {
        LzBaseSessionViewModel = sessionViewModel;    
        CanCreate = true;
        CanRead = true;
        CanUpdate = true;
        CanDelete = true;
        IsLoaded = false;
        IsDirty = false;

        // Assign default storage API handlers
        _ContentReadIdAsync = sessionViewModel.OSAccess.ReadContentAsync;
        _S3CreateIdAsync = sessionViewModel.OSAccess.S3CreateAsync;
        _S3ReadIdAsync = sessionViewModel.OSAccess.S3ReadAsync;
        _S3UpdateIdAsync = sessionViewModel.OSAccess.S3UpdateAsync;
        _S3DeleteIdAsync = sessionViewModel.OSAccess.S3DeleteAsync;
        _LocalCreateIdAsync = sessionViewModel.OSAccess.LocalCreateAsync;
        _LocalReadIdAsync = sessionViewModel.OSAccess.LocalReadAsync;
        _LocalUpdateIdAsync = sessionViewModel.OSAccess.LocalUpdateAsync;
        _LocalDeleteIdAsync = sessionViewModel.OSAccess.LocalDeleteAsync;
        _HttpReadIdAsync = sessionViewModel.OSAccess.HttpReadAsync;
        _ModelCreateAsync = ModelCreateAsync;
        _ModelReadIdAsync = ModelReadAsync;
        _ModelUpdateAsync = ModelUpdateAsync;
        _ModelDeleteIdAsync = ModelDeleteAsync;
        _ModelUpdateIdAsync = ModelUpdateIdAsync;
        

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.New)
            .ToPropertyEx(this, x => x.IsNew);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Edit)
            .ToPropertyEx(this, x => x.IsEdit);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Current)
            .ToPropertyEx(this, x => x.IsCurrent);

        this.WhenAnyValue(x => x.State, (x) => x == LzItemViewModelState.Deleted)
            .ToPropertyEx(this, x => x.IsDeleted);

        if (model is not null && dto is not null)
            throw new Exception("itemModel and itemDTO cannot both be assigned.");

        if(dto is not null)
            _DTO = dto;

        // Init Model Data 
        if (model != null)
        {
            Data = model;
            State = LzItemViewModelState.Current;
            IsLoaded = true;
        }
        else
        {
            if (dto != null)
                dto.DeepCloneTo(Data = new());
            State = (Data == null) ? LzItemViewModelState.New : LzItemViewModelState.Current;
            IsLoaded = isLoaded ??= Data != null;
            Data ??= new();
        }
        Data.RegisterObservables();

    }
    public bool AutoLoadChildren { get; set; } = true;
    public abstract string? Id { get; }
    public abstract long UpdatedAt { get; }
    
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

    // protected Properties
    protected TDTO? _DTO { get; init; } // used as a reference to a DTO object when using StorageAPI.Memory
    protected ILzSessionViewModel LzBaseSessionViewModel { get; init; }
    protected string _EntityName { get; init; } = string.Empty;
    protected string _DataCopyJson = string.Empty;
    // Storage 
    protected StorageAPI _StorageAPI { get; init; }
    // DTO access - requires authentication
    protected Func<TDTO, Task<TDTO>>? _DTOCreateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TDTO, Task<TDTO>>? _DTOCreateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task<TDTO>>? _DTOReadIdAsync { get; init; }
    protected Func<Task<TDTO>>? _DTOReadAsync { get; init; } // Read using this.Id
    protected Func<TDTO, Task<TDTO>>? _DTOUpdateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TDTO, Task<TDTO>>? _DTOUpdateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task>? _DTODeleteIdAsync { get; init; }
    // S3 access - requires authentication
    // Id is S3 bucket reference
    protected Func<string, string, Task>? _S3CreateIdAsync { get; init; }
    protected Func<string, Task<string>>? _S3ReadIdAsync { get; init; }
    protected Func<string, string, Task>? _S3UpdateIdAsync { get; init; }
    protected Func<string, Task>? _S3DeleteIdAsync { get; init; }
    // Local storage access
    // Id is full path reference
    protected Func<string, string, Task>? _LocalCreateIdAsync { get; init; }
    protected Func<string, Task<string>>? _LocalReadIdAsync { get; init; }
    protected Func<string, string, Task>? _LocalUpdateIdAsync { get; init; }
    protected Func<string, Task>? _LocalDeleteIdAsync { get; init; }
    // _content access 
    // Id is something like "_content/library/somefile"
    // WASM implements this using HttpClient - assumes resource is under wwwroot
    // MAUI implements this using FileSystem.OpenAppPackageFileAsync(id)
    protected Func<string, Task<string>>? _ContentReadIdAsync { get; init; }
    // Http access - general http calls
    // Id is URL
    protected Func<string, Task<string>>? _HttpReadIdAsync { get; init; }

    // Model access - no authentication
    protected Func<TModel, Task<TModel>>? _ModelCreateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TModel, Task<TModel>>? _ModelCreateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task<TModel>>? _ModelReadIdAsync { get; init; }
    protected Func<Task<TModel>>? _ModelReadAsync { get; init; } // Read using this.Id
    protected Func<TModel, Task<TModel>>? _ModelUpdateAsync { get; init; } // Assumes storage Id is in TDTO
    protected Func<string, TModel, Task<TModel>>? _ModelUpdateIdAsync { get; init; } // Assumes storage Id is passed separate from TDTO
    protected Func<string, Task>? _ModelDeleteIdAsync { get; init; }
    protected void CheckId(string? id)
    {
        if (id is null)
            throw new Exception("Id is null");
    }

    // Public Methods
    public virtual void CheckAuth(StorageAPI storageAPI)
    {
    }
    public virtual async Task<(bool, string)> CreateAsync(string? id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_StorageAPI == StorageAPI.Default) 
                ? StorageAPI.DTO
                : _StorageAPI;

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
                case StorageAPI.DTO:
                    if (id is null)
                    {
                        if (_DTOCreateAsync == null)
                            throw new Exception("SvcCreateAsync not assigned.");
                        item = await _DTOCreateAsync(item!);
                    }
                    else
                    {
                        if (_DTOCreateIdAsync == null)
                            throw new Exception("SvcCreateIdAsync not assigned.");
                        CheckId(id);
                        item = await _DTOCreateIdAsync(id!, item!);
                    }
                    UpdateData(item);
                    break;
                case StorageAPI.S3:
                    if (_S3CreateIdAsync == null)
                        throw new Exception("S3SvcCreateIdAsync not assigned.");
                    CheckId(id);
                    var s3Text = JsonConvert.SerializeObject(item);
                    await _S3CreateIdAsync(id!, s3Text!);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcCreateIdAsync is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcCreateIdAsync is not supported.");
                case StorageAPI.Local:
                    if (_LocalCreateIdAsync == null)
                        throw new Exception("LocalSvcCreateIdAsync not assigned.");
                    CheckId(id);
                    var localText = JsonConvert.SerializeObject(item);
                    await _LocalCreateIdAsync(id!,localText!);
                    break;
                case StorageAPI.Model:
                    if (id is null)
                    {
                        if (_ModelCreateAsync == null)
                            throw new Exception("InternalCreateAsync not assigned.");
                        await _ModelCreateAsync(Data!);
                    }
                    else
                    {
                        if (_ModelCreateIdAsync == null)
                            throw new Exception("InternalCreateIdAsync not assigned.");
                        CheckId(id);
                        await _ModelCreateIdAsync(id!, Data!);
                    }
                    UpdateData(Data);
                    break;
                case StorageAPI.None:
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
    public virtual async Task<(bool, string)> ReadAsync(string id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_StorageAPI == StorageAPI.Default)
                ? StorageAPI.DTO
                : _StorageAPI;
        var userMsg = "Can't load " + _EntityName;
        try
        {
            if (!CanRead) 
                throw new Exception("Read not authorized");

            CheckAuth(storageAPI);
            CheckId(id);

            // Perform storage operation
            switch (storageAPI)
            {
                case StorageAPI.DTO:
                    if (_DTOReadIdAsync == null)
                        throw new Exception("SvcReadIdAsync not assigned.");
                    UpdateData(await _DTOReadIdAsync(id));
                    break;
                case StorageAPI.S3:
                    if (_S3ReadIdAsync == null)
                        throw new Exception("S3SvcReadIdAsync not assigned.");
                    var s3Text = await _S3ReadIdAsync(id);
                    var s3Item = JsonConvert.DeserializeObject<TDTO>(s3Text);
                    UpdateData(s3Item!);
                    break;
                case StorageAPI.Http:
                    if (_HttpReadIdAsync == null)
                        throw new Exception("HttpSvcReadIdAsync not assigned.");
                    var httpText = await _HttpReadIdAsync(id);
                    var httpItem = JsonConvert.DeserializeObject<TDTO>(httpText);
                    UpdateData(httpItem!);
                    break;
                case StorageAPI.Content:
                    if (_ContentReadIdAsync == null)
                        throw new Exception("ContentSvcReadIdAsync not assigned.");
                    var contentText = await _ContentReadIdAsync(id);
                    var contextItem = JsonConvert.DeserializeObject<TDTO>(contentText);
                    UpdateData(contextItem!);
                    break;
                case StorageAPI.Local:
                    if (_LocalReadIdAsync == null)
                        throw new Exception("LocalSvcReadIdAsync not assigned.");
                    var localText = await _LocalReadIdAsync(id);
                    var localItem = JsonConvert.DeserializeObject<TDTO>(localText);
                    UpdateData(localItem!);
                    break;
                case StorageAPI.Model:
                    if (_ModelReadIdAsync == null)
                        throw new Exception("InternalReadIdAsync not assigned.");
                    UpdateData(await _ModelReadIdAsync(id));
                    break;
                case StorageAPI.None:
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
    // relevant to the DTO API but we may extend it to the S3 service as well.
    public virtual async Task<(bool, string)> ReadAsync(StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_StorageAPI == StorageAPI.Default)
                ? StorageAPI.DTO
                : _StorageAPI;
        try
        {
            if (!CanRead)
                throw new Exception("Read not authorized");

            CheckAuth(storageAPI);

            // Perform storage operation
            switch (storageAPI)
            {
                case StorageAPI.DTO:
                    if (_DTOReadAsync == null)
                        throw new Exception("SvcReadAsync not assigned.");
                    UpdateData(await _DTOReadAsync());
                    break;
                case StorageAPI.S3:
                    throw new Exception("S3SvcReadAsync not supported. Use S3SvcReadIdAsync instead.");
                case StorageAPI.Http:
                    throw new Exception("HttpSvcReadAsync not supported. Use HttpSvcReadIdAsync instead.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcReadAsync not supported. Use ContentSvcReadIdAsync instead.");
                case StorageAPI.Local:
                    throw new Exception("LocalSvcReadAsync not supported. Use LocalSvcReadIdAsync instead.");
                case StorageAPI.Model:
                    if (_ModelReadAsync == null)
                        throw new Exception("InternalReadAsync not assigned.");
                    UpdateData(await _ModelReadAsync());
                    break;
                case StorageAPI.None:
                    break;


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
            storageAPI = (_StorageAPI == StorageAPI.Default)
                ? StorageAPI.DTO
                : _StorageAPI;

        try
        {
            if (!CanUpdate) 
                throw new Exception("Update not authorized");

            // Todo: Review usecases to see if we need this.
            //if (State != LzItemViewModelState.Edit)
            //    throw new Exception("State != Edit.");

            if (Data is null)
                throw new Exception("Data not assigned");

            if (!Validate())
                throw new Exception("Validation failed.");

            CheckAuth(storageAPI);

            switch(storageAPI)
            {
                case StorageAPI.DTO:
                    if(id is null)
                    {
                        if (_DTOUpdateAsync == null)
                            throw new Exception("SvcUpdateAsync is not assigned.");
                        UpdateData(await _DTOUpdateAsync((TDTO)Data!));
                    }
                    else
                    {
                        if (_DTOUpdateIdAsync == null)
                            throw new Exception("SvcUpdateIdAsync is not assigned.");
                        CheckId(id);
                        UpdateData(await _DTOUpdateIdAsync(id,(TDTO)Data!));
                    }
                    break;
                case StorageAPI.S3:
                    if (_S3UpdateIdAsync == null)
                        throw new Exception("S3SvcUpdateIdAsync is not assigned.");
                    CheckId(id);
                    var s3Text = JsonConvert.SerializeObject(Data);
                    await _S3UpdateIdAsync(id!, s3Text);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcUpdateIdAsync is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcUpdateIdAsync is not supported.");
                case StorageAPI.Local:
                    if (_LocalUpdateIdAsync == null)
                        throw new Exception("LocalSvcUpdateIdAsync is not assigned.");
                    CheckId(id);
                    var localText = JsonConvert.SerializeObject(Data);
                    await _LocalUpdateIdAsync(id!,localText);
                    break;
                case StorageAPI.Model:
                    if (id is null)
                    {
                        if (_ModelUpdateAsync == null)
                            throw new Exception("InternalUpdateAsync is not assigned.");
                        UpdateData(await _ModelUpdateAsync(Data!));
                    }
                    else
                    {
                        if (_ModelUpdateIdAsync == null)
                            throw new Exception("InternalUpdateIdAsync is not assigned.");
                        CheckId(id);
                        UpdateData(await _ModelUpdateIdAsync(id,Data!));
                    }
                    break;
                case StorageAPI.None:
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
    public virtual async Task<(bool, string)> SaveEditAsync(string? id, StorageAPI storageAPI = StorageAPI.Default)
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
    public virtual async Task<(bool,string)> DeleteAsync(string id, StorageAPI storageAPI = StorageAPI.Default)
    {
        if (storageAPI == StorageAPI.Default)
            storageAPI = (_StorageAPI == StorageAPI.Default)
                ? StorageAPI.DTO
                : _StorageAPI;

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
                case StorageAPI.DTO:
                    if (_DTODeleteIdAsync == null)
                        throw new Exception("SvcDelete(id) is not assigned.");
                    await _DTODeleteIdAsync(Id!);
                    break;
                case StorageAPI.S3:
                    if (_S3DeleteIdAsync == null)
                        throw new Exception("S3SvcDelete(id) is not assigned.");
                    await _S3DeleteIdAsync(Id!);
                    break;
                case StorageAPI.Http:
                    throw new Exception("HttpSvcDelete(id) is not supported.");
                case StorageAPI.Content:
                    throw new Exception("ContentSvcDelete(id) is not supported.");
                case StorageAPI.Local:
                    if (_LocalDeleteIdAsync == null)
                        throw new Exception("LocalSvcDelete(id) is not assigned.");
                    await _LocalDeleteIdAsync(Id!);
                    break;
                case StorageAPI.Model:
                    if (_ModelDeleteIdAsync == null)
                        throw new Exception("InternalDelete(id) is not assigned.");
                    await _ModelDeleteIdAsync(Id!);
                    break;
                case StorageAPI.None:
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

    public virtual async Task<(bool, string)> ReadChildrenAsync(bool forceload, StorageAPI storageAPI)
    {
        await Task.Delay(0);
        return (true, string.Empty);
    }
    // Protected Methods

    /// <summary>
    /// We use PopulateObject to update the Data object to 
    /// preserve any event subscriptions.
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
    /// It is unlikely that MakeDataCopy is ever used in a use case 
    /// where performance is critical. If your use case requires 
    /// optimization, override this method (and the RestoreFromDataCopy method)
    /// and use individual property assignments. 
    /// </summary>
    protected virtual void MakeDataCopy()
    {
        Data ??= new();
        _DataCopyJson = JsonConvert.SerializeObject((TDTO)Data);
    }
    /// <summary>
    /// This method uses a json copy of the data. 
    /// Saving data using JSON is not fast. Using Force.DeepCloner
    /// for DataCopy is not possible because the clone process 
    /// fails if the source data has event subscriptions.
    /// It is unlikely that RestoreFromDataCopy is ever used in a use case 
    /// where performance is critical. If your use case requires 
    /// optimization, override this method (and the MakeDataCopy method)
    /// and use individual property assignments. 
    /// </summary>
    protected virtual void RestoreFromDataCopy()
    {
        // Restoring data from JSON is not fast. Using Force.DeepCloner 
        // DeepCloneTo(Data) is not possible because it overwrites any
        // event subscriptions.
        Data ??= new();
        JsonConvert.PopulateObject(_DataCopyJson, Data);
    }

    /// <summary>
    /// We use PopulateObject to update the Data object to 
    /// preserve any event subscriptions.
    /// </summary>
    /// <param name="item"></param>
    protected virtual void UpdateData(TModel item)
    {
        Data = item;
        IsDirty = false;
        this.RaisePropertyChanged(nameof(Data));
    }

    // Model Storage API Methods
    // Since the Data property points to the single instance of the model, these 
    // methods are essentially no-ops. They are here to provide a consistent processing
    // pattern for all storage APIs and allow the introduction of side effects
    // associated with each action. For instance, you might want to perform 
    // referential integrity checks in the Create and Update methods if you are 
    // not using Fluent Validation or some other validation library. 
    protected virtual async Task<TModel> ModelCreateAsync(TModel body)
    {
        // Perform any referential integrity checks here.
        await Task.Delay(0);
        return body;
    }
    protected async Task<TModel> ModelReadAsync(string id)
    {
        await Task.Delay(0);
        return Data!;
    }
    protected async Task<TModel> ModelUpdateAsync(TModel body)
    {
        if(_DTO is not null)
            ((TDTO)Data!).DeepCloneTo(_DTO);
        // Perform any referential integrity checks here
        await Task.Delay(0);
        return Data!;
    }
    protected async Task<TModel> ModelUpdateIdAsync(string id, TModel body)
    {
        // Perform any referential integrity checks here
        await Task.Delay(0);
        return Data!;
    }
    protected async Task ModelDeleteAsync(string id)
    {
        // Perform any referential integrity checks here
        await Task.Delay(0);
    }
}
