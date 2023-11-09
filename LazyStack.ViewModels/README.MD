# ViewModel Guidelines

## General 
- All ViewModels should inherit from ReactiveObject. This implements INotifyPropertyChanged.
- All ViewModels shouuld use ReactiveUI.Fody.Helpers where possible. [Reactive] [ObservableAsProperty]
- Always use a ViewModel, never directly hook up a View directly to the ClientSDK. This allows adding observability features in the ViewModel later on with little change to the Views. You can hook up a View to a Model through the ViewModel; e.g. make the Model a property of the ViewModel. (Direct Binding)
- Put all Persistence Methods, Model transformation logic in ViewModel
- Put non-trivial Model validation rules in ViewModel
- Models should implement INotifyPropertyChanged (inheriting ReactiveObject is one quick way of achiving this)

## When to use Observability 
[Reactive] properties handle two-way binding to Model properties. This is used to bind View component elements to an instance of a Model in a ViewModel. 
If the Component inherits from ReactiveComponentBase<T> then the StateHasChanged() method will be called when the bound values are updated. In otherwords, 
you don't have to do anything special to get two-way binding to work with Models that implment INotifyPropertChanged. 

[ObservableAsProperty] handle read properties that emit events that can be subscribed to by ViewModels and/or Components (both the ViewModel they 
are defined in and other ViewModels). Generally, it is better to implement all the event handling at the ViewModel level for testability and simplicity at the View level.



## ViewModel Classes 
This library provides a set of base classes that you can use to create your own ViewModels. 

### LzViewModelBase 
```
public abstract class LzViewModelBase: ReactiveObject, IDisposable
```
This ViewModel class is the base class for all ViewModels.


### ItemViewModel
These ViewModels represent a single item and provides a CRUDL interface to the item.
```
public abstract class LzItemViewModelBase<TDTO, TModel> : LzViewModelBase, ILzItemViewModel<TModel>
public abstract class LzItemViewModelNotificationsBase<TDTO, TModel> : LzItemViewModelBase<TDTO,TModel>, ILzItemViewModelNotificationsBase<TModel>
```

When using these classes, you will need to implement the abstract methods and properties. There are also methods and properties 
that are virtual, with default implementations, that you can override if you need to.
#### Virtual Properties 
```
public abstract string? Id { get; }	
public abstract long UpdatedAt { get; }
```

### ItemsViewModel
These ItemsViewModel classes represent a collection of ItemViewModels. 
```
public abstract class LzItemsViewModelBase<TVM, TDTO, TModel> : LzViewModelBase, INotifyCollectionChanged, ILzItemsViewModelBase<TVM, TDTO, TModel> 
public abstract class LzItemsViewModelNotificationsBase<TVM, TDTO, TModel> :  LzItemsViewModelBase<TVM, TDTO, TModel>,  INotifyCollectionChanged, ILzItemsViewModelNotificationsBase<TVM, TDTO, TModel>
```

## StoreageAPI 
LzItemViewModelBase and LzItemsViewModelBase both have a StorageAPI property. This is a reference to the 
StorageAPI that is used for CRUDL operations. 
```
public enum StorageAPI
{
    Default, // Usually Rest API
    Rest, // API calls, generally requires auth
    S3, // bucket access, generally requireds auth
    Http, // limited to gets
    Local, // local device storage
    Content, // _content access
    Internal // class implementation handles persistence (if any). Use for updating data in place.
}
```

## Service APIs 
When you create a ViewModel, you will need to provide a ServiceAPI. This is the API that is used to perform 
CRUDL operations. Here are the ServiceAPIs that are available, some have default implementations, some do not. 

### LzItemViewModelBase 
API Access 
- SvcCreateAsync(DTO)
- SvcCreateIdAsync(Id,DTO)
- SvcReadAsync(), this.Id to read the item 
- SvcReadIdAsync(Id)
- SvcUpdateAsync(DTO)
- SvcDeleteIdAsync(Id)
 
S3 Access 
- S3CreateIdAsync(s3objectname) = IContentAccess.S3CreateIdAsync(s3objectname)
- S3ReadIdAsync(s3objectname) = IContentAccess.S3ReadIdAsync(s3objectname)
- S3UpdateIdAsync(s3objectname) = IContentAccess.S3UpdateIdAsync(s3objectname)
- S3DeleteIdAsync(s3objectname) = IContentAccess.S3DeleteIdAsync(s3objectname)

Http Access 
- HttpSvcReadIdAsync(url) = IContentAccess.HttpSvcReadIdAsync(url)

Local Access
- LocalSvcCreateIdAsync(filename,filecontent)
- LocalSvcReadIdAsync(filename)
- LocalSvcUpdateIdAsync(filename,filecontent)
- LocalSvcDeleteIdAsync(filename)

Content Access
- ContentSvcReadIdAsync(filename) // ex: "_content/MyApp/MyFile.txt"
 
Internal
- User  implemented methods. Override the default CRUD implementations you want to use. 

## Notes

