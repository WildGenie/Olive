# Olive Microservices: Integration
You can create yor APIs using the ASP.NET Web Api architecture which is a very powerful technology.
It's designed to be cross-platform compatible, meaning that your APIs can be invoked by any consumer application written in any technology (.Net, Java, Javascript, Php, ...) via Http.

If you're building an internal business application however, there is a good chance that **most, if not all, of your Apis will be used only by other .NET microservices within the same solution**.

## The problem with normal Api calling
Of course the .NET framework provides you with standard classes to invoke Http based web-api services.
You can see [examples here](https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/calling-a-web-api-from-a-net-client) for how to use HttpClient and related classes to achieve this.

However that way you have to write a lot of code which can be time consuming and error prone. You also have to deal with many issues yourself:
- Caching 
- Exception handling and logging
- Making sure the Api Urls are up to date
- Making sure the correct version of the service is being invoked (Dev v.s. Staging v.s. Production)
- Create DTO classes for request and response types

## Solution: Olive Api Proxy Generator
To free you from the problems mentioned above, there is a utility named **OliveApiProxyGenerator** that can automatically create a proxy Dll for your Apis. You will then install that dll in the consumer services and use it to access the API with maximume ease.

It will automatically take care of all of the problems mentioned above and gives you a strongly typed and simplified approach to **invoke remote Apis as if they are local functions in the same consumer service**.

## Creating an Api
To benefit from the architecture and tools explained here you should use the pattern explained here for implementing your Apis.
The following file should be created in ***Website\Api*** folder:

```csharp
namespace MyPublisherService
{
    [Route("api")]
    public class MyApi : BaseController
    {
        [HttpGet, Route("...")]
        [Returns(typeof(MyReturnType)]
        // Todo: add the required [Authorize(Role=...)] settings
        public async Task<IActionResult> MyFunction(string someParameter1, stringsomeParameter2)
        {
            // TODO: Add any custom validation
            if ({Some invalid condition}) return BadRequest();

            MyReturnType result = ....;
            return Json(result);
        }
        
        public class MyReturnType
        {
            public string Property1 {get; set;}
            public int Property2 {get; set;}
            ...
        }
    }
}
```
### Namespace: MyPublisherService
The namespace of your Api class should be the name of the microservice that publishes the Api followed by *"Service"*. For example PeopleService, AuthService, OrderService...

The same namespace will be used in generating the proxy dll for use in the Api consumers. This way they know exactly which microservice they are calling the Api from.

### Controller class name: MyApi
The controller class name should be the **logical name** of this particular Api followed by *"Api"* and named after the purpose that it serves for *one particular consumer*.

> Aim to create **one Api controller class per consumer micro-service** and even name it after that consumer.

**Warning:** Of course the are cases where multiple consumer microservices seem to need the same thing, and you might be tempted to generalise the Api to be used by all of them. However **beware** that: 
- Sharing one Api with multiple consumers makes it hard to change and *adapt it overtime to suit the requirements of each particular consumer*, since a change that is desirable for consumer A could break consumer B. 
- Each consumer may need different fields of data. To satisfy everyone's need you may have to over-expose data in a shared Api to satisfy everyone. But that can cause security issues as well as inefficiency.

## Generating an Api Proxy
1. Compile the website project (which includes the Api code)
2. Right click on the Api controller class in Visual Studio solution explorer.
3. Select "Generate Proxy Dll..." which will just invoke the following command:
```
..\M#\lib\generate-proxy.exe /assembly:....Websit.dll /serviceName:"«PublisherService»}" /controller:MyPublisherService.MyApi /output:....Website\obj\proxy-gen\MyPublisherService.MyApi\
```

> Note: ***«PublisherService»*** will be set from *appSettings.config*, under the key ***Microservice:Name***

The generate-proxy.exe tool will generate a *.Net class library project* as explained below.

## Using the generated proxy
In the consumer application (service) reference the generated dll.
You can then create a proxy using the appropriate security option, and then call the remote Api function. For example:
```csharp
var result = await MyPublisherService.MyApi.AsServiceUser().MyFunction(...);
```


# Under the hood: The generated proxy dll
When running generate-proxy.exe a class will be generated with the *same class name and namespace* as the Api controller.

```csharp
namespace MyPublisherService
{
    public class MyApi
    {
        Action<ApiClient> Config;
        public static MyApi Create(Action<ApiClient> config) => Config = config;
        
        public static MyApi AsServiceUser() => Create(x => x.AsServiceUser());
        
        public static MyApi AsHttpUser() => Create(x => x.AsHttpUser());
    
        // Note: For each Api action method a method with the same name and parameters will be generated.
        // But the return type will be determined by the [Returns...] attribute.
        public async Task<MyReturnType> MyFunction(string someParameter1, stringsomeParameter2, 
           ApiResponseCache cacheChoice = ApiResponseCache.Accept)
        {
           ...
        }
    }
}
```
### Proxy method implementation
The body of the generated method will be based on the following:

```csharp
 var url = new RouteTemplate("{RouteTemplate}").Merge(new { someParameter1 , stringsomeParameter2 });
 var client = Microservice.Api("«PublisherService»", url);
 Config?.Invoke(client);
 return await client.Get<MyReturnType>(cacheChoice);
 // Note: If the http verb of the Api is POST, PUT or DELETE, then the appropriate method will be called instead of Get, and without the cache choice.
 
```

***{RouteTemplate}*** will be generated by concatinating the value of [Route] attributes on the controller class and action method, joined together by "/".

Note: If the method takes no input parameters, then the whole line will simply be:
```csharp
  var url = "{RouteTemplate}";
```

### Generatd DTO classes
In addition to the proxy class, it will generate a DTO class for each of the following:
- Return type of the Api method (as specified by the attribute).
- From any argument of the Api method other than primitive types.