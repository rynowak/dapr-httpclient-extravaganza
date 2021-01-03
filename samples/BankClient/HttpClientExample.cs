using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace BankClient
{
    public class HttpClientExample : Example
    {
        public override string DisplayName => "Regular HttpClient";

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            var client = new HttpClient(new ServiceInvocationHandler(new HttpClientHandler()))
            {
                // Using app-id as the hostname.
                BaseAddress = new Uri("http://bank"),
            };

            // Scenario 1: Check if the account already exists.
            Account? account = null;
            try
            {
                account = await client.GetFromJsonAsync<Account>("/accounts/17", cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Account does not exist.
            }

            Console.WriteLine($"Scenario 1: account '17' {(account is null ? "does not exist" : "already exists")}");

            // Scenario 2: Deposit some money
            var transaction = new Transaction()
            {
                Amount = 100m,
                Id = "17",
            };

            var response = await client.PostAsJsonAsync("/deposit", transaction, cancellationToken);
            response.EnsureSuccessStatusCode();

            // read updated balance
            account = await response.Content.ReadFromJsonAsync<Account>(cancellationToken: cancellationToken);
            Console.WriteLine($"Scenario 2: account '17' has '{account?.Balance}' money");

            // Scenario 3: Handle a validation error without exceptions
            transaction = new Transaction()
            {
                Amount = 1_000_000m,
                Id = "17",
            };
            
            response = await client.PostAsJsonAsync("/withdraw", transaction, cancellationToken);
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                // We don't actually expect this example to succeed - we expect a 400
                Console.WriteLine("Something went wrong :(");
                return;
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            Console.WriteLine($"Scenario 3: got the following errors:");
            foreach (var kvp in problem!.Errors)
            {
                Console.WriteLine($"{kvp.Key}: {string.Join(", ", kvp.Value)}");
            }
        }
    }
}