# dapr-httpclient-extravaganza

Samples using Dapr service invocation with HttpClient.

Related to the discussion [here](https://github.com/dapr/dotnet-sdk/issues/526).

The goal was to write the same sample code a few different times with different client libraries keeping the style as close as possible.

> Ex: The first scenario is to check if the account already exists. This uses the `HttpClient.GetFromJsonAsync` method which throws an exception on 404. So the other samples should use the features of those libraries that are equivalent in spirit/style.

The good stuff is in `samples/BankClient/*Example.cs`

## How to run

1. Run the server app (`samples/BankApp`) with Dapr

```sh
dapr run --app-id bank --app-port 5000 -- dotnet run
```

2. Run the client app (`samples/BankClient`) with your choice of samples 

```sh
dapr run -- dotnet run 0 # runs HttpClientExample
```

You'll need to use ctrl+C to exit the sample app

## Want to add something?

PRs welcome if you spot a bug or want to your favorite library.
