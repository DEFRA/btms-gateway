# BTMS Gateway

Core delivery C# ASP.NET backend template.

* [Testing](#testing)
* [Running](#running)
* [Deploying](#deploying)
* [SonarCloud](#sonarCloud)
* [Dependabot](#dependabot)

### Testing

Run the tests with:

```
dotnet test
```

Unit tests execute without a running instance of the web server. 
End-to-end tests can start the web server using `TestWebServer.BuildAndRun()` taking `ServiceDescriptors` to replace services with mocked or stubbed versions. The `TestWebServer` provides properties:
- `Services` allows access to injected services.
- `HttpServiceClient` provide a pre-configured `HttpClient` that can be used to access the web server.
- `OutboundTestHttpHandler` is a `TestHttpHandler` class that intercepts all `HttpClient` requests to dependant services called by the web server.

### Running

Run CDP-Deployments application:
```
dotnet run --project BtmsGateway --launch-profile Development
```
### Deploying

Before deploying via CDP set the correct config for the environment as per the `appsettings.Development.json`.

### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

### Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`
