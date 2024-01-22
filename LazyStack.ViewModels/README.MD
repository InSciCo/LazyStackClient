# ViewModel Guidelines

## General 
- All ViewModels should inherit from ReactiveObject. This implements INotifyPropertyChanged.
- All ViewModels should use ReactiveUI.Fody.Helpers where possible. [Reactive] [ObservableAsProperty]
- Always use a ViewModel, never directly hook up a View directly to the ClientSDK. This allows adding observability features in the ViewModel later on with little change to the Views. You can hook up a View to a Model through the ViewModel; e.g. make the Model a property of the ViewModel. (Direct Binding)
- Put all Persistence Methods, Model transformation logic in ViewModel
- Put non-trivial Model validation rules in ViewModel
- Models should implement INotifyPropertyChanged (LzViewModels inherits from ReactiveObject to achieve this)

## When to use Observability 
[Reactive] properties handle two-way binding to Model properties. This is used to bind View component elements to an instance of a Model in a ViewModel. 
If the Component inherits from ReactiveComponentBase<T> then the StateHasChanged() method will be called when the bound values are updated. In other words, 
you don't have to do anything special to get two-way binding to work with Models that implement INotifyPropertyChanged. 

[ObservableAsProperty] handle read properties that emit events that can be subscribed to by ViewModels and/or Components (both the ViewModel they 
are defined in and other ViewModels). Generally, it is better to implement most event handling at the ViewModel level for testability and simplicity. 


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
    Http, // limited to gets, no auth
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

For a Blazor app, content is retrieved from the http server and cached in the browser. If the Blazor app is a PWA, then the service-worker will pre-cache content so it is available offline.

For a MAUI app, content is packaged with the app and is available offline in the BlazorWebView. MAUI code does not have direct access this content. However, you can use JS Interop to fetch static content from a "_content/*" source.
 
Internal
- User  implemented methods. Override the default CRUD implementations you want to use. 


## Configuration Content 
Configuration content is content that is used to configure the app. Configuration content can reside in:
- Content Storage ( Blazor static content. e.g. _content/* )
- S3 Storage
- Http Storage 
- Assembly Embedded Resources
- Environment Variables 

By convention, each LazyStack viewmodel project will have a Config folder containing Configuration class that loads configuration content from embedded resources. This class also provides default DI registrations for classes in the assembly.

Configuration data typically includes:
- Connection data (e.g. API endpoints, S3 buckets, etc.)
- Tenancy data (e.g. static content, default settings, etc.)
- Embedded resources configuration (configuration data loaded from assemblies)

### WASM Apps
WASM apps are loaded from AWS CloudFront. CloudFront maps URL paths to Origins. There are typically four origins in a WASM app:
- S3 Spa bucket contains the SPA app
- S3 Config bucket contains connection information for the app
- S3 Tenancy bucket contains static content for a tenancy 

CloudFront plays an important role by orchestrating these three origins based on the domain used to access the app. 

WASM apps provide access to static content through the _content path.
The Config bucket is accessed through _content/Config/\*.
The Tenancy bucket is accessed through _content/Tenancy/\*.

### Local Development Configuration 
When developing locally, we don't use CloudFront. Instead, we use local projects to proxy the Config and Tenancy origins. 

#### Config Project
The Config project is a Razor Class Library that contains configuration content in the wwwroot folder.

#### Tenancy Project
The Tenancy project is a Razor Class Library that contains tenancy content in the wwwroot folder.

When running locally, the dev server hosting the app will serve content from the Config and Tenancy projects via the _content paths. 

## Deployment 
The deployment pipeline doesn't use the Config Project or the Tenancy Project. Instead, it is responsible for writing the configuration content to the Config and Tenancy buckets.

1. AWS utilities to gather AWS configuration data and write it to the Config bucket.
2. Tenancy buckets are generally configured by checking out tenancy content from a git repo and writing it to the Tenancy bucket or by an application specific process. For instance, a client tenancy onboarding/configuration application may be responsible for writing tenancy content to the Tenancy bucket.

Note: The Tenancy and Config projects csproj files contain directives to exclude all content for publishing so no internal development configuration is deployed to the cloud.

### PWA WASM Apps 
PWA service workers precache content from the SPA, Config and Tenancy buckets to support offline access. From the PWA's perspective, there is no difference between content from the SPA, Config or Tenancy buckets.

### BlazorWebView MAUI Apps
MAUI apps use the BlazorWebView to host the SPA. The BlazorWebView is a Chromium based browser that is embedded in the MAUI app. The Config project and Tenancy project data must be packaged with the app. For this reason, the production build of a MAUI app requires we replace the development versions of the Config and Tenancy projects with the production versions specific to the tenancy the app is being built for. This is done by the deployment pipeline. 

## Development Config Project
This project contains build directives to copy connection information from the main project content to the wwwroot config.json file based on the development environment.

