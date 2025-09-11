# BTMS Gateway

The BTMS Gateway is a .NET application which proxies HTTP SOAP requests between CDS and ALVS.
Proxied requests will be passed to/from CDS and ALVS as well as routing a copy of the request to BTMS, via SNS/SQS, by converting the SOAP payload to a BTMS JSON model.

It also proxies HTTP requests from ALVS to IPAFFS.

The Gateway exposes a health check endpoint, for verifying the health of the Gateway itself, at the endpoint <code>/health</code>.
This will return a 200 OK response and an empty response body if the Gateway is healthy.

The Gateway also exposes endpoints for doing various health checks against the Health URLs configured in appsettings.json.
This is to verify the health of the target systems where requests are routed to i.e. CDS, ALVS and IPAFFS.
These health checks will perform an HTTP call to the endpoint as well as an NSLookup and Dig.
The available health endpoints can be viewed by visiting the swagger endpoint <code>/swagger/index.html</code>.

* [Prerequisites](#prerequisites)
* [Setup Process](#setup-process)
* [How to run in development](#how-to-run-in-development)
* [How to run Tests](#how-to-run-tests)
* [Running](#running)
* [Deploying](#deploying)
* [SonarCloud](#sonarCloud)
* [Dependabot](#dependabot)
* [Licence Information](#licence-information)
* [About the Licence](#about-the-licence)

### Prerequisites

- .NET 9 SDK
- NSLookup and Dig for performing health checks
- Docker
  - For testing locally using SNS/SQS queues setup with localstack
  - You will optionally need a running instance of Gateway Stub (https://github.com/DEFRA/btms-gateway-stub) if any of the configured <code>NamedRoutes</code> in your appsettings are configured to forward requests to <code>Stub</code>

### Setup Process

- Install the .NET 9 SDK installed
- Ensure you have NSLookup and Dig available on your machine if you wish to execute the Health Checks locally
- This project reference Nuget packages that are hosted in Github. In order for the build to access these packages you will need to configure a PAT token that gives you access to the Github Nuget.
  - Create a Github PAT token for your account and configure it in the .env file. See .env.example file for an example of how your .env file should look
- Install Docker
  - Run the following Docker Compose to set up locally running queues, wiremock instance for testing as well as the Gateway application itself
  ```bash
  docker compose -f compose.yml up -d
  ```

### How to run in development

Run the application and dependent resources using Docker Compose:

```bash
docker compose -f compose.yml up -d
```
This will run the SNS/SQS resources, a Wiremock instance and the Gateway application itself, within Docker containers.

You can run the application locally with the command:

```bash
dotnet run --project BtmsGateway --launch-profile BtmsGateway
```

Ensure it's not already running in the Docker Compose stack or you may get port clashes.

### How to run Tests

Run the Unit tests with:

```bash
dotnet test --filter "Category!=IntegrationTest & Dependence!=localstack"
```

Unit tests execute without a running instance of the web server.

End-to-end tests can start the web server using `TestWebServer.BuildAndRun()` taking `ServiceDescriptors` to replace services with mocked or stubbed versions. The `TestWebServer` provides properties:
- `Services` allows access to injected services.
- `HttpServiceClient` provide a pre-configured `HttpClient` that can be used to access the web server.
- `OutboundTestHttpHandler` is a `TestHttpHandler` class that intercepts all `HttpClient` requests to dependant services called by the web server.

The End-to-end tests, contained within the EndToEnd directory of the BtmsGateway.Test project, require the SNS/SQS localstack queues to be running. You can run these using Docker Compose (see above).  
The tests themselves are decorated with the test trait "Dependence" "localstack".
End-to-end tests can be run with:

```bash
dotnet test --filter "Dependence=localstack"
```
You can run these along with the Unit tests with:

```bash
dotnet test --filter "Category!=IntegrationTest"
```

### How to run Integration Tests

Run the integration tests with:

```bash
dotnet test --filter "Category=IntegrationTest"
```

Integration tests require the Docker Compose stack to be running.  
The application itself can be running inside the Docker Compose stack or you can run it locally yourself (see above).

### Deploying

Before deploying via CDP set the correct config for the environment as per the `appsettings.Development.json`.

### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

### Dependabot

We are using dependabot.

Connection to the private Defra nuget packages is provided by a user generated PAT stored in this repo's settings - /settings/secrets/dependabot - see `DEPENDABOT_PAT` secret.

The PAT is a classic token and needs permissions of `read:packages`.

At time of writing, using PAT is the only way to make Dependabot work with private nuget feeds.

Should the user who owns the PAT leave Defra then another user on the team should create a new PAT and update the settings in this repo.

### Licence Information

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

### About the licence

The Open Government Licence (OGL) was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
